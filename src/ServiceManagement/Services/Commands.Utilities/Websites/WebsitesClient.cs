// ----------------------------------------------------------------------------------
//
// Copyright Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------

namespace Microsoft.WindowsAzure.Commands.Utilities.Websites
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Web;
    using System.Xml.Linq;
    using CloudService;
    using Management.WebSites;
    using Management.WebSites.Models;
    using Microsoft.Build.Evaluation;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Logging;
    using Microsoft.Web.Deployment;
    using Microsoft.WindowsAzure.Commands.Utilities.Websites.Services.WebJobs;
    using Microsoft.WindowsAzure.Commands.Websites.WebJobs;
    using Microsoft.WindowsAzure.WebSitesExtensions;
    using Microsoft.WindowsAzure.WebSitesExtensions.Models;
    using Newtonsoft.Json.Linq;
    using Properties;
    using Services;
    using Services.DeploymentEntities;
    using Services.WebEntities;
    using Utilities.Common;

    public class WebsitesClient : IWebsitesClient
    {
        private const int UploadJobWaitTime = 2000;

        private readonly CloudServiceClient cloudServiceClient;

        public static string SlotFormat = "{0}({1})";

        public IWebSiteManagementClient WebsiteManagementClient { get; internal set; }

        public Action<string> Logger { get; set; }

        /// <summary>
        /// Creates new WebsitesClient
        /// </summary>
        /// <param name="subscription">Subscription containing websites to manipulate</param>
        /// <param name="logger">The logger action</param>
        public WebsitesClient(WindowsAzureSubscription subscription, Action<string> logger)
        {
            Logger = logger;
            cloudServiceClient = new CloudServiceClient(subscription, debugStream: logger);
            WebsiteManagementClient = subscription.CreateClient<WebSiteManagementClient>();
        }

        /// <summary>
        /// Gets website name in the current directory.
        /// </summary>
        /// <returns></returns>
        private string GetWebsiteFromCurrentDirectory()
        {
            return GitWebsite.ReadConfiguration().Name;
        }

        private Repository GetRepository(string websiteName)
        {
            Site site = WebsiteManagementClient.GetSiteWithCache(websiteName);

            if (site != null)
            {
                return new Repository(site);
            }

            throw new Exception(Resources.RepositoryNotSetup);
        }

        private HttpClient CreateDeploymentHttpClient(string websiteName)
        {
            Repository repository;
            ICredentials credentials;
            GetWebsiteDeploymentHttpConfiguration(websiteName, out repository, out credentials);
            return HttpClientHelper.CreateClient(repository.RepositoryUri, credentials);
        }

        private string GetWebsiteDeploymentHttpConfiguration(
            string name,
            out Repository repository,
            out ICredentials credentials)
        {
            name = SetWebsiteName(name, null);
            repository = GetRepository(name);
            credentials = new NetworkCredential(
                repository.PublishingUsername,
                repository.PublishingPassword);
            return name;
        }

        private string GetWebsiteName(string name)
        {
            return string.IsNullOrEmpty(name) ? GetWebsiteFromCurrentDirectory() : name;
        }

        private void ChangeWebsiteState(string name, string webspace, WebsiteState state)
        {
            WebsiteManagementClient.WebSites.Update(webspace, name, new WebSiteUpdateParameters
            {
                State = state.ToString(),
                // Set the following 3 collection properties to null since by default they are empty lists,
                // which will clear the corresponding settings of the web site, thus results in a 404 when browsing the web site.
                HostNames = null,
                HostNameSslStates = null,
                SslCertificates = null
            });
        }

        private void SetApplicationDiagnosticsSettings(
            string name,
            WebsiteDiagnosticOutput output,
            bool setFlag,
            Dictionary<DiagnosticProperties, object> properties = null)
        {
            Site website = GetWebsite(name);

            using (HttpClient client = CreateDeploymentHttpClient(website.Name))
            {
                DiagnosticsSettings diagnosticsSettings = GetApplicationDiagnosticsSettings(website.Name);
                switch (output)
                {
                    case WebsiteDiagnosticOutput.FileSystem:
                        diagnosticsSettings.AzureDriveTraceEnabled = setFlag;
                        diagnosticsSettings.AzureDriveTraceLevel = setFlag ?
                        (Services.DeploymentEntities.LogEntryType)properties[DiagnosticProperties.LogLevel] :
                        diagnosticsSettings.AzureDriveTraceLevel;
                        break;

                    case WebsiteDiagnosticOutput.StorageTable:
                        diagnosticsSettings.AzureTableTraceEnabled = setFlag;
                        if (setFlag)
                        {
                            const string storageTableName = "CLOUD_STORAGE_ACCOUNT";
                            string storageAccountName = (string)properties[DiagnosticProperties.StorageAccountName];
                            string connectionString = cloudServiceClient.GetStorageServiceConnectionString(
                                storageAccountName);
                            SetConnectionString(website.Name, storageTableName, connectionString, DatabaseType.Custom);

                            diagnosticsSettings.AzureTableTraceLevel = setFlag ?
                                (Services.DeploymentEntities.LogEntryType)properties[DiagnosticProperties.LogLevel] :
                                diagnosticsSettings.AzureTableTraceLevel;
                        }
                        break;

                    default:
                        throw new ArgumentException();
                }

                JObject json = new JObject(
                    new JProperty(UriElements.AzureDriveTraceEnabled, diagnosticsSettings.AzureDriveTraceEnabled),
                    new JProperty(UriElements.AzureDriveTraceLevel, diagnosticsSettings.AzureDriveTraceLevel.ToString()),
                    new JProperty(UriElements.AzureTableTraceEnabled, diagnosticsSettings.AzureTableTraceEnabled),
                    new JProperty(UriElements.AzureTableTraceLevel, diagnosticsSettings.AzureTableTraceLevel.ToString()));
                client.PostJson(UriElements.DiagnosticsSettings, json, Logger);
            }
        }

        private void SetSiteDiagnosticsSettings(
            string name,
            bool webServerLogging,
            bool detailedErrorMessages,
            bool failedRequestTracing,
            bool setFlag)
        {
            Site website = GetWebsite(name);

            var update = WebsiteManagementClient.WebSites.GetConfiguration(website.WebSpace, website.Name).ToUpdate();
            update.HttpLoggingEnabled = webServerLogging ? setFlag : update.HttpLoggingEnabled;
            update.DetailedErrorLoggingEnabled = detailedErrorMessages ? setFlag : update.DetailedErrorLoggingEnabled;
            update.RequestTracingEnabled = failedRequestTracing ? setFlag : update.RequestTracingEnabled;

            WebsiteManagementClient.WebSites.UpdateConfiguration(website.WebSpace, website.Name, update);
        }

        private bool IsProductionSlot(string slot)
        {
            return (!string.IsNullOrEmpty(slot)) &&
                (slot.Equals(WebsiteSlotName.Production.ToString(), StringComparison.OrdinalIgnoreCase));
        }

        private IWebSiteExtensionsClient GetWebSiteExtensionsClient(string websiteName)
        {
            Site website = GetWebsite(websiteName);
            Uri endpointUrl = new Uri("https://" + website.EnabledHostNames.First(url => url.Contains(".scm.")));
            return new WebSiteExtensionsClient(websiteName, GetWebSiteExtensionsCredentials(websiteName), endpointUrl)
                .WithHandler(new HttpRestCallLogger());
        }

        private BasicAuthenticationCloudCredentials GetWebSiteExtensionsCredentials(string name)
        {
            name = SetWebsiteName(name, null);
            Repository repository = GetRepository(name);
            return new BasicAuthenticationCloudCredentials()
            {
                Username = repository.PublishingUsername,
                Password = repository.PublishingPassword
            };
        }

        /// <summary>
        /// Starts log streaming for the given website.
        /// </summary>
        /// <param name="name">The website name</param>
        /// <param name="slot">The website slot name</param>
        /// <param name="path">The log path, by default root</param>
        /// <param name="message">The substring message</param>
        /// <param name="endStreaming">Predicate to end streaming</param>
        /// <param name="waitInternal">The fetch wait interval</param>
        /// <returns>The log line</returns>
        public IEnumerable<string> StartLogStreaming(
            string name,
            string slot,
            string path,
            string message,
            Predicate<string> endStreaming,
            int waitInternal)
        {
            name = SetWebsiteName(name, slot);
            return StartLogStreaming(name, path, message, endStreaming, waitInternal);
        }

        /// <summary>
        /// List log paths for a given website.
        /// </summary>
        /// <param name="name">The website name</param>
        /// <param name="slot">The website slot name</param>
        /// <returns>The list of log paths</returns>
        public List<LogPath> ListLogPaths(string name, string slot)
        {
            name = SetWebsiteName(name, slot);
            return ListLogPaths(name);
        }

        /// <summary>
        /// Starts log streaming for the given website.
        /// </summary>
        /// <param name="name">The website name</param>
        /// <param name="path">The log path, by default root</param>
        /// <param name="message">The substring message</param>
        /// <param name="endStreaming">Predicate to end streaming</param>
        /// <param name="waitInterval">The fetch wait interval</param>
        /// <returns>The log line</returns>
        public IEnumerable<string> StartLogStreaming(
            string name,
            string path,
            string message,
            Predicate<string> endStreaming,
            int waitInterval)
        {
            Repository repository;
            ICredentials credentials;
            name = GetWebsiteDeploymentHttpConfiguration(name, out repository, out credentials);
            path = HttpUtility.UrlEncode(path);
            message = HttpUtility.UrlEncode(message);

            RemoteLogStreamManager manager = new RemoteLogStreamManager(
                repository.RepositoryUri,
                path,
                message,
                credentials,
                Logger);

            using (LogStreamWaitHandle logHandler = new LogStreamWaitHandle(manager.GetStream().Result))
            {
                bool doStreaming = true;

                while (doStreaming)
                {
                    string line = logHandler.WaitNextLine(waitInterval);

                    if (line != null)
                    {
                        yield return line;
                    }

                    doStreaming = endStreaming == null || endStreaming(line);
                }
            }
        }

        /// <summary>
        /// List log paths for a given website.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public List<LogPath> ListLogPaths(string name)
        {
            using (HttpClient client = CreateDeploymentHttpClient(name))
            {
                return client.GetJson<List<LogPath>>(UriElements.LogPaths, Logger);
            }
        }

        /// <summary>
        /// Gets the application diagnostics settings
        /// </summary>
        /// <param name="name">The website name</param>
        /// <returns>The website application diagnostics settings</returns>
        public DiagnosticsSettings GetApplicationDiagnosticsSettings(string name)
        {
            using (HttpClient client = CreateDeploymentHttpClient(name))
            {
                return client.GetJson<DiagnosticsSettings>(UriElements.DiagnosticsSettings, Logger);
            }
        }

        /// <summary>
        /// Gets the application diagnostics settings
        /// </summary>
        /// <param name="name">The website name</param>
        /// <param name="slot">The website slot name</param>
        /// <returns>The website application diagnostics settings</returns>
        public DiagnosticsSettings GetApplicationDiagnosticsSettings(string name, string slot)
        {
            name = SetWebsiteName(name, slot);
            return GetApplicationDiagnosticsSettings(name);
        }

        /// <summary>
        /// Restarts a website.
        /// </summary>
        /// <param name="name">The website name</param>
        public void RestartWebsite(string name)
        {
            Site website = GetWebsite(name);
            ChangeWebsiteState(website.Name, website.WebSpace, WebsiteState.Stopped);
            ChangeWebsiteState(website.Name, website.WebSpace, WebsiteState.Running);
        }

        /// <summary>
        /// Starts a website.
        /// </summary>
        /// <param name="name">The website name</param>
        public void StartWebsite(string name)
        {
            Site website = GetWebsite(name);
            ChangeWebsiteState(website.Name, website.WebSpace, WebsiteState.Running);
        }

        /// <summary>
        /// Stops a website.
        /// </summary>
        /// <param name="name">The website name</param>
        public void StopWebsite(string name)
        {
            Site website = GetWebsite(name);
            ChangeWebsiteState(website.Name, website.WebSpace, WebsiteState.Stopped);
        }

        /// <summary>
        /// Restarts a website.
        /// </summary>
        /// <param name="name">The website name</param>
        /// <param name="slot">The website slot name</param>
        public void RestartWebsite(string name, string slot)
        {
            Site website = GetWebsite(name, slot);
            ChangeWebsiteState(website.Name, website.WebSpace, WebsiteState.Stopped);
            ChangeWebsiteState(website.Name, website.WebSpace, WebsiteState.Running);
        }

        /// <summary>
        /// Starts a website.
        /// </summary>
        /// <param name="name">The website name</param>
        /// <param name="slot">The website slot name</param>
        public void StartWebsite(string name, string slot)
        {
            Site website = GetWebsite(name, slot);
            ChangeWebsiteState(website.Name, website.WebSpace, WebsiteState.Running);
        }

        /// <summary>
        /// Stops a website.
        /// </summary>
        /// <param name="name">The website name</param>
        /// <param name="slot">The website slot name</param>
        public void StopWebsite(string name, string slot)
        {
            Site website = GetWebsite(name, slot);
            ChangeWebsiteState(website.Name, website.WebSpace, WebsiteState.Stopped);
        }

        public WebsiteInstance[] ListWebsiteInstances(string webSpace, string fullName)
        {
            IList<string> instanceIds = WebsiteManagementClient.WebSites.GetInstanceIds(webSpace, fullName).InstanceIds;
            return instanceIds.Select(s => new WebsiteInstance {InstanceId = s}).ToArray();
        }

        /// <summary>
        /// Gets a website slot instance.
        /// </summary>
        /// <param name="name">The website name</param>
        /// <param name="slot">The slot name</param>
        /// <returns>The website instance</returns>
        public Site GetWebsite(string name, string slot)
        {
            name = SetWebsiteName(name, slot);
            return GetWebsite(name);
        }

        /// <summary>
        /// Gets a website instance.
        /// </summary>
        /// <param name="name">The website name</param>
        /// <returns>The website instance</returns>
        public Site GetWebsite(string name)
        {
            name = SetWebsiteName(name, null);

            Site website = WebsiteManagementClient.GetSiteWithCache(name);

            if (website == null)
            {
                throw new CloudException(string.Format(Resources.InvalidWebsite, name));
            }

            return website;
        }

        public Site GetWebsiteIfExists(WebSpace webSpace, string name)
        {
            name = SetWebsiteName(name, null);
            Site siteToReturn = null;
            try
            {
                siteToReturn =  WebsiteManagementClient.GetSiteWithCache(name, webSpace);
            }
            catch (CloudException cloudException)
            {
                if (cloudException.Response.StatusCode != HttpStatusCode.NotFound)
                {
                    throw;
                }
            }

            return siteToReturn;
        }

        /// <summary>
        /// Gets all slots for a website
        /// </summary>
        /// <param name="name">The website name</param>
        /// <returns>The website slots list</returns>
        public List<Site> GetWebsiteSlots(string name)
        {
            name = SetWebsiteName(name, null);
            return ListWebsites()
                .Where(s =>
                    s.Name.IndexOf(string.Format("{0}(", name), StringComparison.OrdinalIgnoreCase) >= 0 ||
                    s.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        /// <summary>
        /// Lists all websites under the current subscription
        /// </summary>
        /// <returns>List of websites</returns>
        public List<Site> ListWebsites()
        {
            return ListWebSpaces().SelectMany(space => ListSitesInWebSpace(space.Name)).ToList();
        }

        /// <summary>
        /// Lists all websites with the provided slot name.
        /// </summary>
        /// <param name="slot">The slot name</param>
        /// <returns>The list if websites</returns>
        public List<Site> ListWebsites(string slot)
        {
            return ListWebsites().Where(s => s.Name.Contains(string.Format("({0})", slot))).ToList();
        }

        /// <summary>
        /// Gets the hostname of the website
        /// </summary>
        /// <param name="name">The website name</param>
        /// <param name="slot">The website slot name</param>
        /// <returns>The hostname</returns>
        public string GetHostName(string name, string slot)
        {
            slot = string.IsNullOrEmpty(slot) ? GetSlotName(name) : slot;
            name = SetWebsiteName(name, slot);
            string hostname = null;
            string dnsSuffix = GetWebsiteDnsSuffix();

            if (!string.IsNullOrEmpty(slot) &&
                !slot.Equals(WebsiteSlotName.Production.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                hostname = string.Format("{0}-{1}.{2}", GetWebsiteNameFromFullName(name), slot, dnsSuffix);
            }
            else
            {
                hostname = string.Format("{0}.{1}", name, dnsSuffix);
            }

            return hostname;
        }

        /// <summary>
        /// Create a new website.
        /// </summary>
        /// <param name="webspaceName">Web space to create site in.</param>
        /// <param name="siteToCreate">Details about the site to create.</param>
        /// <param name="slot">The slot name.</param>
        /// <returns>The created site object</returns>
        public Site CreateWebsite(string webspaceName, SiteWithWebSpace siteToCreate, string slot)
        {
            siteToCreate.Name = SetWebsiteName(siteToCreate.Name, slot);
            string[] hostNames = { GetHostName(siteToCreate.Name, slot) };
            siteToCreate.HostNames = hostNames;
            return CreateWebsite(webspaceName, siteToCreate);
        }

        /// <summary>
        /// Create a new website in production.
        /// </summary>
        /// <param name="webspaceName">Web space to create site in.</param>
        /// <param name="siteToCreate">Details about the site to create.</param>
        /// <returns>The created site object</returns>
        private Site CreateWebsite(string webspaceName, SiteWithWebSpace siteToCreate)
        {
            var options = new WebSiteCreateParameters
            {
                Name = siteToCreate.Name,
                WebSpaceName = siteToCreate.WebSpaceToCreate.Name,
                WebSpace = new WebSiteCreateParameters.WebSpaceDetails
                {
                    GeoRegion = siteToCreate.WebSpaceToCreate.GeoRegion,
                    Name = siteToCreate.WebSpaceToCreate.Name,
                    Plan = siteToCreate.WebSpaceToCreate.Plan
                }
            };

            siteToCreate.HostNames.ForEach(s => options.HostNames.Add(s));

            var response = WebsiteManagementClient.WebSites.Create(webspaceName, options);
            return response.WebSite.ToSite();
        }

        /// <summary>
        /// Update the set of host names for a website.
        /// </summary>
        /// <param name="site">The site name.</param>
        /// <param name="hostNames">The new host names.</param>
        public void UpdateWebsiteHostNames(Site site, IEnumerable<string> hostNames)
        {
            var update = new WebSiteUpdateParameters();
            foreach (var name in hostNames)
            {
                update.HostNames.Add(name);
            }

            WebsiteManagementClient.WebSites.Update(site.WebSpace, site.Name, update);
        }

        /// <summary>
        /// Update the set of host names for a website slot.
        /// </summary>
        /// <param name="site">The website name.</param>
        /// <param name="hostNames">The new host names.</param>
        /// <param name="slot">The website slot name.</param>
        public void UpdateWebsiteHostNames(Site site, IEnumerable<string> hostNames, string slot)
        {
            site.Name = SetWebsiteName(site.Name, slot);

            UpdateWebsiteHostNames(site, hostNames);
        }

        /// <summary>
        /// Gets the website configuration.
        /// </summary>
        /// <param name="name">The website name</param>
        /// <returns>The website configuration object</returns>
        public SiteConfig GetWebsiteConfiguration(string name)
        {
            Site website = GetWebsite(name);
            SiteConfig configuration =
                WebsiteManagementClient.WebSites.GetConfiguration(website.WebSpace, website.Name).ToSiteConfig();

            return configuration;
        }

        /// <summary>
        /// Gets a website slot configuration
        /// </summary>
        /// <param name="name">The website name</param>
        /// <param name="slot">The website slot name</param>
        /// <returns>The website cobfiguration object</returns>
        public SiteConfig GetWebsiteConfiguration(string name, string slot)
        {
            Site website = GetWebsite(name);
            website.Name = SetWebsiteName(website.Name, slot);
            return GetWebsiteConfiguration(website.Name);
        }

        /// <summary>
        /// Get the real website name.
        /// </summary>
        /// <param name="name">The website name from the -Name parameter.</param>
        /// <param name="slot">The website name from the -Slot parameter.</param>
        /// <returns>The real website name.</returns>
        private string SetWebsiteName(string name, string slot)
        {
            name = GetWebsiteName(name);
            slot = slot ?? GetSlotName(name);

            if (string.IsNullOrEmpty(slot) ||
                slot.Equals(WebsiteSlotName.Production.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return GetWebsiteNameFromFullName(name);
            }
            else if (name.Contains('(') && name.Contains(')'))
            {
                string currentSlot = GetSlotName(name);
                if (currentSlot.Equals(WebsiteSlotName.Production.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    return GetWebsiteNameFromFullName(name);
                }

                return name;
            }
            else
            {
                return GetSlotDnsName(name, slot);
            }
        }

        private string SetWebsiteNameForWebDeploy(string name, string slot)
        {
            return SetWebsiteName(name, slot).Replace("(", "__").Replace(")", string.Empty);
        }

        /// <summary>
        /// Gets the website name without slot part
        /// </summary>
        /// <param name="name">The website full name which may include slot name</param>
        /// <returns>The website name</returns>
        public string GetWebsiteNameFromFullName(string name)
        {
            if (!string.IsNullOrEmpty(GetSlotName(name)))
            {
                name = name.Split('(')[0];
            }

            return name;
        }

        /// <summary>
        /// Update the website configuration
        /// </summary>
        /// <param name="name">The website name</param>
        /// <param name="newConfiguration">The website configuration object containing updates.</param>
        public void UpdateWebsiteConfiguration(string name, SiteConfig newConfiguration)
        {
            Site website = GetWebsite(name);
            WebsiteManagementClient.WebSites.UpdateConfiguration(website.WebSpace, name,
                newConfiguration.ToConfigUpdateParameters());
        }

        /// <summary>
        /// Update the website slot configuration
        /// </summary>
        /// <param name="name">The website name</param>
        /// <param name="newConfiguration">The website configuration object containing updates.</param>
        /// <param name="slot">The website slot name</param>
        public void UpdateWebsiteConfiguration(string name, SiteConfig newConfiguration, string slot)
        {
            name = SetWebsiteName(name, slot);
            UpdateWebsiteConfiguration(name, newConfiguration);
        }

        /// <summary>
        /// Create a git repository for the web site.
        /// </summary>
        /// <param name="webspaceName">Webspace that site is in.</param>
        /// <param name="websiteName">The site name.</param>
        public void CreateWebsiteRepository(string webspaceName, string websiteName)
        {
            WebsiteManagementClient.WebSites.CreateRepository(webspaceName, websiteName);
        }

        /// <summary>
        /// Delete a website.
        /// </summary>
        /// <param name="webspaceName">webspace the site is in.</param>
        /// <param name="websiteName">website name.</param>
        /// <param name="deleteMetrics">pass true to delete stored metrics as part of removing site.</param>
        /// <param name="deleteEmptyServerFarm">Pass true to delete server farm is this was the last website in it.</param>
        public void DeleteWebsite(string webspaceName, string websiteName, bool deleteMetrics = false, bool deleteEmptyServerFarm = false)
        {
            WebSiteDeleteParameters input = new WebSiteDeleteParameters()
            {
                DeleteAllSlots = true,
                DeleteEmptyServerFarm = deleteEmptyServerFarm,
                DeleteMetrics = deleteMetrics
            };
            WebsiteManagementClient.WebSites.Delete(webspaceName, websiteName, input);
        }

        /// <summary>
        /// Delete a website slot.
        /// </summary>
        /// <param name="webspaceName">webspace the site is in.</param>
        /// <param name="websiteName">website name.</param>
        /// <param name="slot">The website slot name</param>
        public void DeleteWebsite(string webspaceName, string websiteName, string slot)
        {
            slot = slot ?? GetSlotName(websiteName) ?? WebsiteSlotName.Production.ToString();
            websiteName = SetWebsiteName(websiteName, slot);
            WebSiteDeleteParameters input = new WebSiteDeleteParameters()
            {
                /**
                 * DeleteAllSlots is set to true in case that:
                 * 1) We are trying to delete a production slot and,
                 * 2) The website has more than one slot.
                 */
                DeleteAllSlots = IsProductionSlot(slot),
                DeleteEmptyServerFarm = false,
                DeleteMetrics = false
            };
            WebsiteManagementClient.WebSites.Delete(webspaceName, websiteName, input);
        }

        /// <summary>
        /// Get the WebSpaces.
        /// </summary>
        /// <returns>Collection of WebSpace objects</returns>
        public IList<WebSpace> ListWebSpaces()
        {
            return WebsiteManagementClient.WebSpaces.List().WebSpaces.Select(ws => ws.ToWebSpace()).ToList();
        }

        /// <summary>
        /// Get the sites in the given webspace
        /// </summary>
        /// <param name="spaceName">Name of webspace</param>
        /// <returns>The sites</returns>
        public IList<Site> ListSitesInWebSpace(string spaceName)
        {
            WebSiteListParameters input = new WebSiteListParameters();
            input.PropertiesToInclude.Add("repositoryuri");
            input.PropertiesToInclude.Add("publishingpassword");
            input.PropertiesToInclude.Add("publishingusername");
            return WebsiteManagementClient.WebSpaces.ListWebSites(spaceName, input).WebSites.Select(s => s.ToSite()).ToList();
        }

        /// <summary>
        /// Sets an AppSetting of a website.
        /// </summary>
        /// <param name="name">The website name</param>
        /// <param name="key">The app setting name</param>
        /// <param name="value">The app setting value</param>
        public void SetAppSetting(string name, string key, string value)
        {
            Site website = GetWebsite(name);
            var update = WebsiteManagementClient.WebSites.GetConfiguration(website.WebSpace, website.Name).ToUpdate();

            update.AppSettings[name] = key;

            WebsiteManagementClient.WebSites.UpdateConfiguration(website.WebSpace, website.Name, update);
        }

        /// <summary>
        /// Sets a connection string for a website.
        /// </summary>
        /// <param name="name">Name of the website.</param>
        /// <param name="key">Connection string key.</param>
        /// <param name="value">Value for the connection string.</param>
        /// <param name="connectionStringType">Type of connection string.</param>
        public void SetConnectionString(string name, string key, string value, DatabaseType connectionStringType)
        {
            Site website = GetWebsite(name);

            var update = WebsiteManagementClient.WebSites.GetConfiguration(website.WebSpace, website.Name).ToUpdate();

            var csToUpdate = update.ConnectionStrings.FirstOrDefault(cs => cs.Name.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (csToUpdate == null)
            {
                csToUpdate = new WebSiteUpdateConfigurationParameters.ConnectionStringInfo
                {
                    ConnectionString = value,
                    Name = key,
                    Type = connectionStringType.ToString()
                };
                update.ConnectionStrings.Add(csToUpdate);
            }
            else
            {
                csToUpdate.ConnectionString = value;
                csToUpdate.Type = connectionStringType.ToString();
            }

            WebsiteManagementClient.WebSites.UpdateConfiguration(website.WebSpace, website.Name, update);
        }

        /// <summary>
        /// Enables website diagnostic.
        /// </summary>
        /// <param name="name">The website name</param>
        /// <param name="webServerLogging">Flag for webServerLogging</param>
        /// <param name="detailedErrorMessages">Flag for detailedErrorMessages</param>
        /// <param name="failedRequestTracing">Flag for failedRequestTracing</param>
        public void EnableSiteDiagnostic(
            string name,
            bool webServerLogging,
            bool detailedErrorMessages,
            bool failedRequestTracing)
        {
            SetSiteDiagnosticsSettings(name, webServerLogging, detailedErrorMessages, failedRequestTracing, true);
        }

        /// <summary>
        /// Disables site diagnostic.
        /// </summary>
        /// <param name="name">The website name</param>
        /// <param name="webServerLogging">Flag for webServerLogging</param>
        /// <param name="detailedErrorMessages">Flag for detailedErrorMessages</param>
        /// <param name="failedRequestTracing">Flag for failedRequestTracing</param>
        public void DisableSiteDiagnostic(
            string name,
            bool webServerLogging,
            bool detailedErrorMessages,
            bool failedRequestTracing)
        {
            SetSiteDiagnosticsSettings(name, webServerLogging, detailedErrorMessages, failedRequestTracing, false);
        }

        /// <summary>
        /// Enables application diagnostic on website slot.
        /// </summary>
        /// <param name="name">The website name</param>
        /// <param name="output">The application log output, FileSystem or StorageTable</param>
        /// <param name="properties">The diagnostic setting properties</param>
        /// <param name="slot">The website slot name</param>
        public void EnableApplicationDiagnostic(
            string name,
            WebsiteDiagnosticOutput output,
            Dictionary<DiagnosticProperties, object> properties,
            string slot)
        {
            SetApplicationDiagnosticsSettings(SetWebsiteName(name, slot), output, true, properties);
        }

        /// <summary>
        /// Disables application diagnostic.
        /// </summary>
        /// <param name="name">The website name</param>
        /// <param name="output">The application log output, FileSystem or StorageTable</param>
        /// <param name="slot">The website slot name</param>
        public void DisableApplicationDiagnostic(string name, WebsiteDiagnosticOutput output, string slot)
        {
            SetApplicationDiagnosticsSettings(SetWebsiteName(name, slot), output, false);
        }

        /// <summary>
        /// Enables application diagnostic on website slot.
        /// </summary>
        /// <param name="name">The website name</param>
        /// <param name="output">The application log output, FileSystem or StorageTable</param>
        /// <param name="properties">The diagnostic setting properties</param>
        public void EnableApplicationDiagnostic(
            string name,
            WebsiteDiagnosticOutput output,
            Dictionary<DiagnosticProperties, object> properties)
        {
            SetApplicationDiagnosticsSettings(name, output, true, properties);
        }

        /// <summary>
        /// Disables application diagnostic.
        /// </summary>
        /// <param name="name">The website name</param>
        /// <param name="output">The application log output, FileSystem or StorageTable</param>
        public void DisableApplicationDiagnostic(string name, WebsiteDiagnosticOutput output)
        {
            SetApplicationDiagnosticsSettings(name, output, false);
        }

        /// <summary>
        /// Lists available website locations.
        /// </summary>
        /// <returns>List of location names</returns>
        public List<string> ListAvailableLocations()
        {
            var webspacesGeoRegions = WebsiteManagementClient.WebSpaces.List()
                .WebSpaces.Select(w => w.GeoRegion);

            var availableRegionsResponse = WebsiteManagementClient.WebSpaces.ListGeoRegions();

            return availableRegionsResponse.GeoRegions.Select(r => r.Name).Union(webspacesGeoRegions).ToList();
        }

        /// <summary>
        /// Gets the default website DNS suffix for the current environment.
        /// </summary>
        /// <returns>The website DNS suffix</returns>
        public string GetWebsiteDnsSuffix()
        {
            return WebsiteManagementClient.WebSpaces.GetDnsSuffix().DnsSuffix;
        }

        /// <summary>
        /// Gets the default location for websites.
        /// </summary>
        /// <returns>The default location name.</returns>
        public string GetDefaultLocation()
        {
            return ListAvailableLocations().First();
        }

        /// <summary>
        /// Get a list of the user names configured to publish to the space.
        /// </summary>
        /// <returns>The list of user names.</returns>
        public IList<string> ListPublishingUserNames()
        {
            return WebsiteManagementClient.WebSpaces.ListPublishingUsers()
                .Users.Select(u => u.Name).Where(n => !string.IsNullOrEmpty(n)).ToList();
        }

        /// <summary>
        /// Checks if a website exists or not.
        /// </summary>
        /// <param name="name">The website name</param>
        /// <returns>True if exists, false otherwise</returns>
        public bool WebsiteExists(string name)
        {
            Site website = null;

            try
            {
                website = GetWebsite(name);
            }
            catch
            {
                // Ignore exception.
            }

            return website != null;
        }

        /// <summary>
        /// Checks if a website slot exists or not.
        /// </summary>
        /// <param name="name">The website name</param>
        /// <param name="slot">The website slot name</param>
        /// <returns>True if exists, false otherwise</returns>
        public bool WebsiteExists(string name, string slot)
        {
            Site website = null;

            try
            {
                website = GetWebsite(name, slot);
            }
            catch
            {
                // Ignore exception.
            }

            return website != null;
        }

        /// <summary>
        /// Updates a website compute mode.
        /// </summary>
        /// <param name="websiteToUpdate">The website to update</param>
        public void UpdateWebsiteComputeMode(Site websiteToUpdate)
        {
            WebsiteManagementClient.WebSites.Update(
                websiteToUpdate.WebSpace,
                websiteToUpdate.Name,
                new WebSiteUpdateParameters
                {
                    ComputeMode = websiteToUpdate.ComputeMode,
                    // Set the following 3 collection properties to null since by default they are empty lists,
                    // which will clear the corresponding settings of the web site, thus results in a 404 when browsing the web site.
                    HostNames = null,
                    HostNameSslStates = null,
                    SslCertificates = null
                });
        }

        /// <summary>
        /// Gets a website slot DNS name.
        /// </summary>
        /// <param name="name">The website name</param>
        /// <param name="slot">The slot name</param>
        /// <returns>the slot DNS name</returns>
        public string GetSlotDnsName(string name, string slot)
        {
            return string.Format(SlotFormat, name, slot);
        }

        /// <summary>
        /// Switches the given website slot with the production slot
        /// </summary>
        /// <param name="webspaceName">The webspace name</param>
        /// <param name="websiteName">The website name</param>
        /// <param name="slot1">The website's first slot name</param>
        /// <param name="slot2">The website's second slot name</param>
        public void SwitchSlots(string webspaceName, string websiteName, string slot1, string slot2)
        {
            Debug.Assert(!string.IsNullOrEmpty(slot1));
            Debug.Assert(!string.IsNullOrEmpty(slot2));

            WebsiteManagementClient.WebSites.SwapSlots(webspaceName, websiteName, slot1, slot2);
        }

        /// <summary>
        /// Gets the slot name from the website name.
        /// </summary>
        /// <param name="name">The website name</param>
        /// <returns>The slot name</returns>
        public string GetSlotName(string name)
        {
            string slotName = null;
            if (!string.IsNullOrEmpty(name))
            {
                if (name.Contains('(') && name.Contains(')'))
                {
                    string[] split = name.Split('(');
                    slotName = split[1].TrimEnd(')').ToLower();
                }
            }

            return slotName;
        }

        /// <summary>
        /// Checks whether a website name is available or not.
        /// </summary>
        /// <param name="name">The website name</param>
        /// <returns>True means available, false otherwise</returns>
        public bool CheckWebsiteNameAvailability(string name)
        {
            return WebsiteManagementClient.WebSites.IsHostnameAvailable(name).IsAvailable;
        }

        #region WebDeploy

        /// <summary>
        /// Build a Visual Studio web project and generate a WebDeploy package.
        /// </summary>
        /// <param name="projectFile">The project file.</param>
        /// <param name="configuration">The configuration of the build, like Release or Debug.</param>
        /// <param name="logFile">The build log file if there is any error.</param>
        /// <returns>The full path of the generated WebDeploy package.</returns>
        public string BuildWebProject(string projectFile, string configuration, string logFile)
        {
            ProjectCollection pc = new ProjectCollection();
            Project project = pc.LoadProject(projectFile);

            // Use a file logger to store detailed build info.
            FileLogger fileLogger = new FileLogger();
            fileLogger.Parameters = string.Format("logfile={0}", logFile);
            fileLogger.Verbosity = LoggerVerbosity.Diagnostic;

            // Set the configuration used by MSBuild.
            project.SetProperty("Configuration", configuration);

            // Set this property use "managedRuntimeVersion=v4.0".
            // Otherwise, WebDeploy will fail becasue Azure Web Site is expecting v4.0.
            project.SetProperty("VisualStudioVersion", "11.0");

            // Build the project.
            var buildSucceed = project.Build("Package", new ILogger[] { fileLogger });

            if (buildSucceed)
            {
                // If build succeeds, delete the build.log file since there is no use of it.
                File.Delete(logFile);
                return Path.Combine(Path.GetDirectoryName(projectFile), "obj", configuration, "Package", Path.GetFileNameWithoutExtension(projectFile) + ".zip");
            }
            else
            {
                // If build fails, tell the user to look at the build.log file.
                throw new Exception(string.Format(Resources.WebProjectBuildFailTemplate, logFile));
            }
        }

        /// <summary>
        /// Gets the website WebDeploy publish profile.
        /// </summary>
        /// <param name="websiteName">Website name.</param>
        /// <param name="slot">Slot name. By default is null.</param>
        /// <returns>The publish profile.</returns>
        public WebSiteGetPublishProfileResponse.PublishProfile GetWebDeployPublishProfile(string websiteName, string slot = null)
        {
            var site = this.GetWebsite(websiteName);

            var response = WebsiteManagementClient.WebSites.GetPublishProfile(site.WebSpace, SetWebsiteName(websiteName, slot));

            foreach (var profile in response)
            {
                if (string.Compare(profile.PublishMethod, Resources.WebDeployKeywordInWebSitePublishProfile) == 0)
                {
                    return profile;
                }
            }

            return null;
        }

        /// <summary>
        /// Publish a WebDeploy package folder to a web site.
        /// </summary>
        /// <param name="websiteName">The name of the web site.</param>
        /// <param name="slot">The name of the slot.</param>
        /// <param name="package">The WebDeploy package.</param>
        /// <param name="connectionStrings">The connection strings to overwrite the ones in the Web.config file.</param>
        public void PublishWebProject(string websiteName, string slot, string package, Hashtable connectionStrings)
        {
            if (File.GetAttributes(package).HasFlag(FileAttributes.Directory))
            {
                PublishWebProjectFromPackagePath(websiteName, slot, package, connectionStrings);
            }
            else
            {
                PublishWebProjectFromPackageFile(websiteName, slot, package, connectionStrings);
            }
        }

        /// <summary>
        /// Publish a WebDeploy package zip file to a web site.
        /// </summary>
        /// <param name="websiteName">The name of the web site.</param>
        /// <param name="slot">The name of the slot.</param>
        /// <param name="package">The WebDeploy package zip file.</param>
        /// <param name="connectionStrings">The connection strings to overwrite the ones in the Web.config file.</param>
        private void PublishWebProjectFromPackageFile(string websiteName, string slot, string package, Hashtable connectionStrings)
        {
            DeploymentBaseOptions remoteBaseOptions = CreateRemoteDeploymentBaseOptions(websiteName, slot);
            DeploymentBaseOptions localBaseOptions = new DeploymentBaseOptions();

            DeploymentProviderOptions remoteProviderOptions = new DeploymentProviderOptions(DeploymentWellKnownProvider.Auto);

            using (var deployment = DeploymentManager.CreateObject(DeploymentWellKnownProvider.Package, package, localBaseOptions))
            {
                DeploymentSyncParameter providerPathParameter = new DeploymentSyncParameter(
                    "Provider Path Parameter",
                    "Provider Path Parameter",
                    SetWebsiteNameForWebDeploy(websiteName, slot),
                    null);
                DeploymentSyncParameterEntry iisAppEntry = new DeploymentSyncParameterEntry(
                    DeploymentSyncParameterEntryKind.ProviderPath,
                    DeploymentWellKnownProvider.IisApp.ToString(),
                    ".*",
                    null);
                DeploymentSyncParameterEntry setAclEntry = new DeploymentSyncParameterEntry(
                    DeploymentSyncParameterEntryKind.ProviderPath,
                    DeploymentWellKnownProvider.SetAcl.ToString(),
                    ".*",
                    null);
                providerPathParameter.Add(iisAppEntry);
                providerPathParameter.Add(setAclEntry);
                deployment.SyncParameters.Add(providerPathParameter);

                // Replace the connection strings in Web.config with the ones user specifies from the cmdlet.
                ReplaceConnectionStrings(deployment, connectionStrings);

                DeploymentSyncOptions syncOptions = new DeploymentSyncOptions();

                deployment.SyncTo(remoteProviderOptions, remoteBaseOptions, syncOptions);
            }
        }

        /// <summary>
        /// Publish a WebDeploy package zip file to a web site.
        /// </summary>
        /// <param name="websiteName">The name of the web site.</param>
        /// <param name="slot">The name of the slot.</param>
        /// <param name="package">The WebDeploy package zip file.</param>
        /// <param name="connectionStrings">The connection strings to overwrite the ones in the Web.config file.</param>
        private void PublishWebProjectFromPackagePath(string websiteName, string slot, string package, Hashtable connectionStrings)
        {
            DeploymentBaseOptions remoteBaseOptions = CreateRemoteDeploymentBaseOptions(websiteName, slot);
            DeploymentBaseOptions localBaseOptions = new DeploymentBaseOptions();

            using (var deployment = DeploymentManager.CreateObject(DeploymentWellKnownProvider.ContentPath, package, localBaseOptions))
            {
                ReplaceConnectionStrings(deployment, connectionStrings);
                DeploymentSyncOptions syncOptions = new DeploymentSyncOptions();
                deployment.SyncTo(DeploymentWellKnownProvider.ContentPath, SetWebsiteNameForWebDeploy(websiteName, slot), remoteBaseOptions, syncOptions);
            }
        }

        /// <summary>
        /// Parse the Web.config files to get the connection string names.
        /// </summary>
        /// <param name="defaultWebConfigFile">The default Web.config file.</param>
        /// <param name="overwriteWebConfigFile">The additional Web.config file for the specificed configuration, like Web.Release.Config file.</param>
        /// <returns>An array of connection string names from the Web.config files.</returns>
        public string[] ParseConnectionStringNamesFromWebConfig(string defaultWebConfigFile, string overwriteWebConfigFile)
        {
            var names = new List<string>();
            var webConfigFiles = new string[] { defaultWebConfigFile, overwriteWebConfigFile };

            foreach (var file in webConfigFiles)
            {
                XDocument xdoc = XDocument.Load(file);
                names.AddRange(xdoc.Descendants("connectionStrings").SelectMany(css => css.Descendants("add")).Select(add => add.Attribute("name").Value));
            }

            return names.Distinct().ToArray<string>();
        }

        /// <summary>
        /// Create remote deployment base options using the web site publish profile.
        /// </summary>
        /// <returns>The remote deployment base options.</returns>
        private DeploymentBaseOptions CreateRemoteDeploymentBaseOptions(string websiteName, string slot)
        {
            // Get the web site publish profile.
            var publishProfile = GetWebDeployPublishProfile(websiteName, slot);

            DeploymentBaseOptions remoteBaseOptions = new DeploymentBaseOptions()
            {
                UserName = publishProfile.UserName,
                Password = publishProfile.UserPassword,
                ComputerName = string.Format(Resources.WebSiteWebDeployUriTemplate, publishProfile.PublishUrl, SetWebsiteNameForWebDeploy(websiteName, slot)),
                AuthenticationType = "Basic",
                TempAgent = false
            };

            return remoteBaseOptions;
        }

        /// <summary>
        /// Replace all the connection strings in the deployment.
        /// </summary>
        /// <param name="deployment">The deployment object.</param>
        /// <param name="connectionStrings">Connection strings.</param>
        private void ReplaceConnectionStrings(DeploymentObject deployment, Hashtable connectionStrings)
        {
            if (connectionStrings != null)
            {
                foreach (var key in connectionStrings.Keys)
                {
                    AddConnectionString(deployment, key.ToString(), connectionStrings[key].ToString());
                }
            }
        }

        /// <summary>
        /// Add a connection string parameter to the deployment.
        /// </summary>
        /// <param name="deployment">The deployment object.</param>
        /// <param name="name">Connection string name.</param>
        /// <param name="value">Connection string value.</param>
        private void AddConnectionString(DeploymentObject deployment, string name, string value)
        {
            var deploymentSyncParameterName = string.Format("Connection String {0} Parameter", name);
            DeploymentSyncParameter connectionStringParameter = new DeploymentSyncParameter(
                deploymentSyncParameterName,
                deploymentSyncParameterName,
                value,
                null);
            DeploymentSyncParameterEntry connectionStringEntry = new DeploymentSyncParameterEntry(
                DeploymentSyncParameterEntryKind.XmlFile,
                @"\\web.config$",
                string.Format(@"//connectionStrings/add[@name='{0}']/@connectionString", name),
                null);
            connectionStringParameter.Add(connectionStringEntry);
            deployment.SyncParameters.Add(connectionStringParameter);
        }

        #endregion WebDeploy

        #region WebJobs

        /// <summary>
        /// Filters the web jobs.
        /// </summary>
        /// <param name="options">The web job filter options</param>
        /// <returns>The filtered web jobs list</returns>
        public List<PSWebJob> FilterWebJobs(WebJobFilterOptions options)
        {
            options.Name = SetWebsiteName(options.Name, options.Slot);
            IWebSiteExtensionsClient client = GetWebSiteExtensionsClient(options.Name);
            List<WebJob> jobList = new List<WebJob>();

            if (string.IsNullOrEmpty(options.JobName) && string.IsNullOrEmpty(options.JobType))
            {
                jobList = client.WebJobs.List(new WebJobListParameters()).Jobs.ToList();
            }
            else if (string.IsNullOrEmpty(options.JobName) && !string.IsNullOrEmpty(options.JobType))
            {
                if (string.Compare(options.JobType, WebJobType.Continuous.ToString(), true) == 0)
                {
                    jobList = client.WebJobs.ListContinuous(new WebJobListParameters()).Jobs.ToList();
                }
                else if (string.Compare(options.JobType, WebJobType.Triggered.ToString(), true) == 0)
                {
                    jobList = client.WebJobs.ListTriggered(new WebJobListParameters()).Jobs.ToList();
                }
                else
                {
                    throw new ArgumentOutOfRangeException("JobType");
                }
            }
            else if (!string.IsNullOrEmpty(options.JobName) && !string.IsNullOrEmpty(options.JobType))
            {
                if (string.Compare(options.JobType, WebJobType.Continuous.ToString(), true) == 0)
                {
                    jobList = new List<WebJob>() { client.WebJobs.GetContinuous(options.JobName).WebJob };
                }
                else if (string.Compare(options.JobType, WebJobType.Triggered.ToString(), true) == 0)
                {
                    jobList = new List<WebJob>() { client.WebJobs.GetTriggered(options.JobName).WebJob };
                }
                else
                {
                    throw new ArgumentOutOfRangeException("JobType");
                }
            }
            else
            {
                if (string.IsNullOrEmpty(options.JobName))
                {
                    throw new ArgumentOutOfRangeException("JobName");
                }
                else if (string.IsNullOrEmpty(options.JobType))
                {
                    throw new ArgumentOutOfRangeException("JobType");
                }
                else
                {
                    throw new ArgumentOutOfRangeException("options");
                }
            }

            List<PSWebJob> result = new List<PSWebJob>();
            foreach (WebJob job in jobList)
            {
                result.Add(new PSWebJob(job));
            }
            return result;
        }

        /// <summary>
        /// Creates new web job for a website
        /// </summary>
        /// <param name="name">The website name</param>
        /// <param name="slot">The website slot name</param>
        /// <param name="jobName">The web job name</param>
        /// <param name="jobType">The web job type</param>
        /// <param name="jobFile">The web job file name</param>
        public PSWebJob CreateWebJob(string name, string slot, string jobName, WebJobType jobType, string jobFile)
        {
            WebJobFilterOptions options = new WebJobFilterOptions() { Name = name, Slot = slot, JobName = jobName, JobType = jobType.ToString() };
            name = SetWebsiteName(name, slot);
            IWebSiteExtensionsClient client = GetWebSiteExtensionsClient(name);

            if (Path.GetExtension(jobFile).ToLower() != ".zip")
            {
                throw new InvalidOperationException(Resources.InvalidWebJobFile);
            }

            switch (jobType)
            {
                case WebJobType.Continuous:
                    client.WebJobs.UploadContinuous(jobName, File.OpenRead(jobFile));
                    break;

                case WebJobType.Triggered:
                    client.WebJobs.UploadTriggered(jobName, File.OpenRead(jobFile));
                    break;

                default:
                    break;
            }
            PSWebJob webjob = null;

            Thread.Sleep(UploadJobWaitTime);

            try
            {
                webjob = FilterWebJobs(options).FirstOrDefault();
            }
            catch (CloudException e)
            {
                if (e.Response.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new ArgumentException(Resources.InvalidJobFile);
                }
            }

            return webjob;
        }

        /// <summary>
        /// Deletes a web job for a website.
        /// </summary>
        /// <param name="name">The website name</param>
        /// <param name="slot">The slot name</param>
        /// <param name="jobName">The web job name</param>
        /// <param name="jobType">The web job type</param>
        public void DeleteWebJob(string name, string slot, string jobName, WebJobType jobType)
        {
            name = SetWebsiteName(name, slot);
            IWebSiteExtensionsClient client = GetWebSiteExtensionsClient(name);

            if (jobType == WebJobType.Continuous)
            {
                client.WebJobs.DeleteContinuous(jobName, true);
            }
            else if (jobType == WebJobType.Triggered)
            {
                client.WebJobs.DeleteTriggered(jobName, true);
            }
            else
            {
                throw new ArgumentException("jobType");
            }
        }

        /// <summary>
        /// Starts a web job in a website.
        /// </summary>
        /// <param name="name">The website name</param>
        /// <param name="slot">The slot name</param>
        /// <param name="jobName">The web job name</param>
        /// <param name="jobType">The web job type</param>
        public void StartWebJob(string name, string slot, string jobName, WebJobType jobType)
        {
            name = SetWebsiteName(name, slot);
            IWebSiteExtensionsClient client = GetWebSiteExtensionsClient(name);

            if (jobType == WebJobType.Continuous)
            {
                client.WebJobs.StartContinuous(jobName);
            }
            else
            {
                client.WebJobs.RunTriggered(jobName);
            }
        }

        /// <summary>
        /// Stops a web job in a website.
        /// </summary>
        /// <param name="name">The website name</param>
        /// <param name="slot">The slot name</param>
        /// <param name="jobName">The web job name</param>
        /// <param name="jobType">The web job type</param>
        public void StopWebJob(string name, string slot, string jobName, WebJobType jobType)
        {
            name = SetWebsiteName(name, slot);
            IWebSiteExtensionsClient client = GetWebSiteExtensionsClient(name);

            if (jobType == WebJobType.Continuous)
            {
                client.WebJobs.StopContinuous(jobName);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Filters a web job history.
        /// </summary>
        /// <param name="options">The web job filter options</param>
        /// <returns>The filtered web jobs run list</returns>
        public List<WebJobRun> FilterWebJobHistory(WebJobHistoryFilterOptions options)
        {
            options.Name = SetWebsiteName(options.Name, options.Slot);
            IWebSiteExtensionsClient client = GetWebSiteExtensionsClient(options.Name);
            List<WebJobRun> result = new List<WebJobRun>();

            if (options.Latest)
            {
                result.Add(client.WebJobs.GetTriggered(options.JobName).WebJob.LatestRun);
            }
            else if (!string.IsNullOrEmpty(options.RunId))
            {
                result.Add(client.WebJobs.GetRun(options.JobName, options.RunId).JobRun);
            }
            else
            {
                result.AddRange(client.WebJobs.ListRuns(options.JobName, new WebJobRunListParameters()));
            }

            return result;
        }

        /// <summary>
        /// Saves a web job logs to file.
        /// </summary>
        /// <param name="name">The website name</param>
        /// <param name="slot">The slot name</param>
        /// <param name="jobName">The web job name</param>
        /// <param name="jobType">The web job type</param>
        /// <param name="output">The output file name</param>
        /// <param name="runId">The job run id</param>
        public void SaveWebJobLog(string name, string slot, string jobName, WebJobType jobType, string output, string runId)
        {
            if (jobType == WebJobType.Continuous && !string.IsNullOrEmpty(runId))
            {
                throw new InvalidOperationException();
            }
            name = SetWebsiteName(name, slot);
            IWebSiteExtensionsClient client = GetWebSiteExtensionsClient(name);

            throw new NotImplementedException();
        }

        /// <summary>
        /// Saves a web job logs to file.
        /// </summary>
        /// <param name="name">The website name</param>
        /// <param name="slot">The slot name</param>
        /// <param name="jobName">The web job name</param>
        /// <param name="jobType">The web job type</param>
        public void SaveWebJobLog(string name, string slot, string jobName, WebJobType jobType)
        {
            const string defaultLogFile = ".\\jobLog.zip";
            SaveWebJobLog(name, slot, jobName, jobType, defaultLogFile, null);
        }

        #endregion WebJobs
    }
}
