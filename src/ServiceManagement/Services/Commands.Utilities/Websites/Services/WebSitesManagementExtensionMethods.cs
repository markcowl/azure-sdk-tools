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

namespace Microsoft.WindowsAzure.Commands.Utilities.Websites.Services
{
    using Management.WebSites;
    using Management.WebSites.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Utilities.Common;
    using WebEntities;

    /// <summary>
    /// Extension methods for converting return values from the websites
    /// management clients from "get" methods into corresponding
    /// other types so that we can easily send updates or return to callers.
    /// </summary>
    internal static class WebSitesManagementConversionMethods
    {
        internal static WebSiteUpdateConfigurationParameters ToUpdate(this WebSiteGetConfigurationResponse getConfigResponse)
        {
            var update = new WebSiteUpdateConfigurationParameters
            {
                DetailedErrorLoggingEnabled = getConfigResponse.DetailedErrorLoggingEnabled,
                HttpLoggingEnabled = getConfigResponse.HttpLoggingEnabled,
                NetFrameworkVersion = getConfigResponse.NetFrameworkVersion,
                NumberOfWorkers = getConfigResponse.NumberOfWorkers,
                PhpVersion = getConfigResponse.PhpVersion,
                PublishingPassword = getConfigResponse.PublishingPassword,
                PublishingUserName = getConfigResponse.PublishingUserName,
                RequestTracingEnabled = getConfigResponse.RequestTracingEnabled,
                RequestTracingExpirationTime = getConfigResponse.RequestTracingExpirationTime,
                ScmType = getConfigResponse.ScmType,
                Use32BitWorkerProcess = getConfigResponse.Use32BitWorkerProcess,
                ManagedPipelineMode = getConfigResponse.ManagedPipelineMode,
                WebSocketsEnabled = getConfigResponse.WebSocketsEnabled,
                RemoteDebuggingEnabled = getConfigResponse.RemoteDebuggingEnabled,
                RemoteDebuggingVersion = getConfigResponse.RemoteDebuggingVersion.GetValueOrDefault()
            };

            getConfigResponse.AppSettings.ForEach(kvp => update.AppSettings.Add(kvp.Key, kvp.Value));
            getConfigResponse.ConnectionStrings.ForEach(cs => update.ConnectionStrings.Add(new WebSiteUpdateConfigurationParameters.ConnectionStringInfo
            {
                ConnectionString = cs.ConnectionString,
                Name = cs.Name,
                Type = cs.Type
            }));
            getConfigResponse.DefaultDocuments.ForEach(dd => update.DefaultDocuments.Add(dd));
            getConfigResponse.HandlerMappings.ForEach(hm => update.HandlerMappings.Add(new WebSiteUpdateConfigurationParameters.HandlerMapping
            {
                Arguments = hm.Arguments,
                Extension = hm.Extension,
                ScriptProcessor = hm.ScriptProcessor
            }));
            getConfigResponse.Metadata.ForEach(kvp => update.Metadata.Add(kvp.Key, kvp.Value));

            return update;
        }

        internal static SiteConfig ToSiteConfig(this WebSiteGetConfigurationResponse getConfigResponse)
        {
            var config = new SiteConfig
            {
                NumberOfWorkers = getConfigResponse.NumberOfWorkers,
                DefaultDocuments = getConfigResponse.DefaultDocuments.ToArray(),
                NetFrameworkVersion = getConfigResponse.NetFrameworkVersion,
                PhpVersion = getConfigResponse.PhpVersion,
                RequestTracingEnabled = getConfigResponse.RequestTracingEnabled,
                HttpLoggingEnabled = getConfigResponse.HttpLoggingEnabled,
                DetailedErrorLoggingEnabled = getConfigResponse.DetailedErrorLoggingEnabled,
                PublishingUsername = getConfigResponse.PublishingUserName,
                PublishingPassword = getConfigResponse.PublishingPassword,
                AppSettings = getConfigResponse.AppSettings.Select(ToNameValuePair).ToList(),
                Metadata = getConfigResponse.Metadata.Select(ToNameValuePair).ToList(),
                ConnectionStrings = new ConnStringPropertyBag(
                    getConfigResponse.ConnectionStrings.Select(cs => new ConnStringInfo
                    {
                        ConnectionString = cs.ConnectionString,
                        Name = cs.Name,
                        Type = (DatabaseType)Enum.Parse(typeof(DatabaseType), cs.Type)
                    }).ToList()),
                HandlerMappings = getConfigResponse.HandlerMappings.Select(hm => new HandlerMapping
                {
                    Arguments = hm.Arguments,
                    Extension = hm.Extension,
                    ScriptProcessor = hm.ScriptProcessor
                }).ToArray(),
                ManagedPipelineMode = getConfigResponse.ManagedPipelineMode,
                WebSocketsEnabled = getConfigResponse.WebSocketsEnabled,
                RemoteDebuggingEnabled = getConfigResponse.RemoteDebuggingEnabled,
                RemoteDebuggingVersion = getConfigResponse.RemoteDebuggingVersion.GetValueOrDefault()
            };
            return config;
        }

        internal static Site ToSite(this WebSiteGetResponse response)
        {
            return new Site
            {
                Name = response.WebSite.Name,
                State = response.WebSite.State.ToString(),
                HostNames = response.WebSite.HostNames.ToArray(),
                WebSpace = response.WebSite.WebSpace,
                SelfLink = response.WebSite.Uri,
                RepositorySiteName = response.WebSite.RepositorySiteName,
                Owner = response.WebSite.Owner,
                UsageState = (UsageState) (int) response.WebSite.UsageState,
                Enabled = response.WebSite.Enabled,
                AdminEnabled = response.WebSite.AdminEnabled,
                EnabledHostNames = response.WebSite.EnabledHostNames.ToArray(),
                SiteProperties = new SiteProperties
                {
                    Metadata = response.WebSite.SiteProperties.Metadata.Select(ToNameValuePair).ToList(),
                    Properties = response.WebSite.SiteProperties.Properties.Select(ToNameValuePair).ToList()
                },
                AvailabilityState = (SiteAvailabilityState) (int) response.WebSite.AvailabilityState,
                SSLCertificates = response.WebSite.SslCertificates.Select(ToCertificate).ToArray(),
                SiteMode = response.WebSite.SiteMode.ToString(),
                HostNameSslStates = new HostNameSslStates(response.WebSite.HostNameSslStates.Select(ToNameSslState).ToList()),
                ComputeMode = response.WebSite.ComputeMode
            };
        }

        internal static Site ToSite(this WebSite site)
        {
            return new Site
            {
                Name = site.Name,
                State = site.State.ToString(),
                HostNames = site.HostNames.ToArray(),
                WebSpace = site.WebSpace,
                SelfLink = site.Uri,
                RepositorySiteName = site.RepositorySiteName,
                Owner = site.Owner,
                UsageState = (UsageState) (int) site.UsageState,
                Enabled = site.Enabled,
                AdminEnabled = site.AdminEnabled,
                EnabledHostNames = site.EnabledHostNames.ToArray(),
                SiteProperties = new SiteProperties
                {
                    Metadata = site.SiteProperties.Metadata.Select(ToNameValuePair).ToList(),
                    Properties = site.SiteProperties.Properties.Select(ToNameValuePair).ToList()
                },
                AvailabilityState = (SiteAvailabilityState) (int) site.AvailabilityState,
                SSLCertificates = site.SslCertificates.Select(ToCertificate).ToArray(),
                SiteMode = site.SiteMode.ToString(),
                HostNameSslStates = new HostNameSslStates(site.HostNameSslStates.Select(ToNameSslState).ToList()),
                ComputeMode = site.ComputeMode
            };
        }

        private static NameValuePair ToNameValuePair(KeyValuePair<string, string> kvp)
        {
            return new NameValuePair {
                Name = kvp.Key,
                Value = kvp.Value
            };
        }

        private static KeyValuePair<string, string> ToKeyValuePair(NameValuePair nvp)
        {
            return new KeyValuePair<string, string>(nvp.Name, nvp.Value);
        }

        private static Certificate ToCertificate(WebSite.WebSiteSslCertificate certificate)
        {
            return new Certificate
            {
                FriendlyName = certificate.FriendlyName,
                SubjectName = certificate.SubjectName,
                HostName = certificate.HostNames.FirstOrDefault(),
                PfxBlob = certificate.PfxBlob,
                SiteName = certificate.SiteName,
                SelfLink = certificate.SelfLinkUri,
                Issuer = certificate.Issuer,
                IssueDate = certificate.IssueDate.Value,
                ExpirationDate = certificate.ExpirationDate.Value,
                Password = certificate.Password,
                Thumbprint = certificate.Thumbprint,
                Valid = certificate.IsValid
            };
        }

        private static HostNameSslState ToNameSslState(WebSite.WebSiteHostNameSslState state)
        {
            return new HostNameSslState
            {
                Name = state.Name,
                SslState = (SslState) (int) state.SslState
            };
        }

        internal static WebSpace ToWebSpace(this WebSpacesListResponse.WebSpace webspace)
        {
            return new WebSpace
            {
                Name = webspace.Name,
                Plan = webspace.Plan,
                Subscription = webspace.Subscription,
                GeoLocation = webspace.GeoLocation,
                GeoRegion = webspace.GeoRegion,
                ComputeMode = null, // TODO: Update
                WorkerSize =
                    webspace.WorkerSize.HasValue
                        ? new WorkerSizeOptions?((WorkerSizeOptions) (int) webspace.WorkerSize.Value)
                        : null,
                NumberOfWorkers = webspace.CurrentNumberOfWorkers,
                Status = (StatusOptions) (int) webspace.Status,
                AvailabilityState = (WebEntities.WebSpaceAvailabilityState) (int) webspace.AvailabilityState
            };
        }

        internal static WebSiteUpdateConfigurationParameters ToConfigUpdateParameters(this SiteConfig config)
        {
            var parameters = new WebSiteUpdateConfigurationParameters
            {
                DetailedErrorLoggingEnabled = config.DetailedErrorLoggingEnabled,
                HttpLoggingEnabled = config.HttpLoggingEnabled,
                NetFrameworkVersion = config.NetFrameworkVersion,
                NumberOfWorkers = config.NumberOfWorkers,
                PhpVersion = config.PhpVersion,
                PublishingPassword = config.PublishingPassword,
                PublishingUserName = config.PublishingUsername,
                RequestTracingEnabled = config.RequestTracingEnabled,
                ManagedPipelineMode = config.ManagedPipelineMode,
                WebSocketsEnabled = config.WebSocketsEnabled,
                RemoteDebuggingEnabled = config.RemoteDebuggingEnabled,
                RemoteDebuggingVersion = config.RemoteDebuggingVersion
            };
            config.AppSettings.ForEach(nvp => parameters.AppSettings.Add(ToKeyValuePair(nvp)));
            config.ConnectionStrings.ForEach(
                csi => parameters.ConnectionStrings.Add(new WebSiteUpdateConfigurationParameters.ConnectionStringInfo
                {
                    Name = csi.Name,
                    ConnectionString = csi.ConnectionString,
                    Type = csi.Type.ToString()
                }));

            config.DefaultDocuments.ForEach(d => parameters.DefaultDocuments.Add(d));
            config.HandlerMappings.ForEach(
                hm => parameters.HandlerMappings.Add(new WebSiteUpdateConfigurationParameters.HandlerMapping
                {
                    Arguments = hm.Arguments,
                    Extension = hm.Extension,
                    ScriptProcessor = hm.ScriptProcessor
                }));

            config.Metadata.ForEach(nvp => parameters.Metadata.Add(ToKeyValuePair(nvp)));

            return parameters;
        }
    }

    /// <summary>
    /// General extension methods on the various web site management operations
    /// </summary>
    public static class WebSitesManagementExtensionMethods
    {
        public static Site GetSiteWithCache(
            this IWebSiteManagementClient client,
            string website, WebSpace webSpace = null)
        {
            return GetFromCache(client, website) ?? GetFromAzure(client, website, webSpace);
        }


        private static Site GetFromCache(IWebSiteManagementClient client,
            string website)
        {
            Site site = Cache.GetSite(client.Credentials.SubscriptionId, website);
            if (site != null)
            {
                // Verify site still exists
                try
                {
                    WebSiteGetParameters input = new WebSiteGetParameters();
                    input.PropertiesToInclude.Add("repositoryuri");
                    input.PropertiesToInclude.Add("publishingpassword");
                    input.PropertiesToInclude.Add("publishingusername");

                    return client.WebSites.Get(site.WebSpace, site.Name, input).ToSite();
                }
                catch
                {
                    // Website is removed or webspace changed, remove from cache
                    Cache.RemoveSite(client.Credentials.SubscriptionId, site);
                    throw;
                }
            }
            return null;
        }

        private static Site GetFromAzure(IWebSiteManagementClient client,
            string website,
            WebSpace webSpace = null)
        {
            if (webSpace != null)
            {
                var siteToReturn = client.WebSites.Get(webSpace.Name, website, null);
                return null != siteToReturn ? siteToReturn.ToSite() : null; 
            }

            // Get all available webspace using REST API
            var spaces = client.WebSpaces.List();
            foreach (var space in spaces.WebSpaces)
            {
                WebSiteListParameters input = new WebSiteListParameters();
                input.PropertiesToInclude.Add("repositoryuri");
                input.PropertiesToInclude.Add("publishingpassword");
                input.PropertiesToInclude.Add("publishingusername");
                var sites = client.WebSpaces.ListWebSites(space.Name, input);
                Sites sitesToAdd = new Sites();
                sites.WebSites.ForEach<WebSite>(s => sitesToAdd.Add(s.ToSite()));
                Cache.SaveSites(client.Credentials.SubscriptionId, sitesToAdd);
                var site = sites.WebSites.FirstOrDefault(
                    ws => ws.Name.Equals(website, StringComparison.InvariantCultureIgnoreCase));
                if (site != null)
                {
                    return site.ToSite();
                }
            }

            // The website does not exist.
            return null;            
        }
    }
}
