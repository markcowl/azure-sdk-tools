// ----------------------------------------------------------------------------------
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
    using Microsoft.WindowsAzure.Commands.Utilities.Websites.Services.WebJobs;
    using Microsoft.WindowsAzure.Commands.Websites.WebJobs;
    using Microsoft.WindowsAzure.Management.WebSites.Models;
    using Microsoft.WindowsAzure.WebSitesExtensions.Models;
    using Services.DeploymentEntities;
    using Services.WebEntities;

    public interface IWebsitesClient
    {
        /// <summary>
        /// Starts log streaming for the given website.
        /// </summary>
        /// <param name="name">The website name</param>
        /// <param name="path">The log path, by default root</param>
        /// <param name="message">The substring message</param>
        /// <param name="endStreaming">Predicate to end streaming</param>
        /// <param name="waitInternal">The fetch wait interval</param>
        /// <returns>The log line</returns>
        IEnumerable<string> StartLogStreaming(
            string name,
            string path,
            string message,
            Predicate<string> endStreaming,
            int waitInternal);

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
        IEnumerable<string> StartLogStreaming(
            string name,
            string slot,
            string path,
            string message,
            Predicate<string> endStreaming,
            int waitInternal);

        /// <summary>
        /// List log paths for a given website.
        /// </summary>
        /// <param name="name">The website name</param>
        /// <param name="slot">The website slot name</param>
        /// <returns>The list of log paths</returns>
        List<LogPath> ListLogPaths(string name, string slot);

        /// <summary>
        /// List log paths for a given website.
        /// </summary>
        /// <param name="name">The website name</param>
        /// <returns>The list of log paths</returns>
        List<LogPath> ListLogPaths(string name);

        /// <summary>
        /// Gets the application diagnostics settings
        /// </summary>
        /// <param name="name">The website name</param>
        /// <returns>The website application diagnostics settings</returns>
        DiagnosticsSettings GetApplicationDiagnosticsSettings(string name);

        /// <summary>
        /// Gets the application diagnostics settings
        /// </summary>
        /// <param name="name">The website name</param>
        /// <param name="slot">The website slot name</param>
        /// <returns>The website application diagnostics settings</returns>
        DiagnosticsSettings GetApplicationDiagnosticsSettings(string name, string slot);

        /// <summary>
        /// Restarts a website.
        /// </summary>
        /// <param name="name">The website name</param>
        void RestartWebsite(string name);

        /// <summary>
        /// Starts a website.
        /// </summary>
        /// <param name="name">The website name</param>
        void StartWebsite(string name);

        /// <summary>
        /// Stops a website.
        /// </summary>
        /// <param name="name">The website name</param>
        void StopWebsite(string name);

        /// <summary>
        /// Gets a website instance.
        /// </summary>
        /// <param name="name">The website name</param>
        /// <returns>The website instance</returns>
        Site GetWebsite(string name);

        /// <summary>
        /// Create a new website in a given slot.
        /// </summary>
        /// <param name="webspaceName">Web space to create site in.</param>
        /// <param name="disablesClone">Flag to control cloning the website configuration.</param>
        /// <param name="siteToCreate">Details about the site to create.</param>
        /// <param name="slot">The slot name.</param>
        /// <returns>The created site object</returns>
        Site CreateWebsite(string webspaceName, SiteWithWebSpace siteToCreate, string slot);

        /// <summary>
        /// Update the set of host names for a website.
        /// </summary>
        /// <param name="site">The website name.</param>
        /// <param name="hostNames">The new host names.</param>
        void UpdateWebsiteHostNames(Site site, IEnumerable<string> hostNames);

        /// <summary>
        /// Update the set of host names for a website slot.
        /// </summary>
        /// <param name="site">The website name.</param>
        /// <param name="hostNames">The new host names.</param>
        /// <param name="slot">The website slot name.</param>
        void UpdateWebsiteHostNames(Site site, IEnumerable<string> hostNames, string slot);

        /// <summary>
        /// Gets the website configuration.
        /// </summary>
        /// <param name="name">The website name</param>
        /// <returns>The website configuration object</returns>
        SiteConfig GetWebsiteConfiguration(string name);

        /// <summary>
        /// Create a git repository for the web site.
        /// </summary>
        /// <param name="webspaceName">Webspace that site is in.</param>
        /// <param name="websiteName">The site name.</param>
        void CreateWebsiteRepository(string webspaceName, string websiteName);

        /// <summary>
        /// Update the website configuration
        /// </summary>
        /// <param name="name">The website name</param>
        /// <param name="newConfiguration">The website configuration object containing updates.</param>
        void UpdateWebsiteConfiguration(string name, SiteConfig newConfiguration);

        /// <summary>
        /// Update the website slot configuration
        /// </summary>
        /// <param name="name">The website name</param>
        /// <param name="newConfiguration">The website configuration object containing updates.</param>
        /// <param name="slot">The website slot name</param>
        void UpdateWebsiteConfiguration(string name, SiteConfig newConfiguration, string slot);

        /// <summary>
        /// Delete a website.
        /// </summary>
        /// <param name="webspaceName">webspace the site is in.</param>
        /// <param name="websiteName">website name.</param>
        /// <param name="deleteMetrics">pass true to delete stored metrics as part of removing site.</param>
        /// <param name="deleteEmptyServerFarm">Pass true to delete server farm is this was the last website in it.</param>
        void DeleteWebsite(string webspaceName, string websiteName, bool deleteMetrics = false, bool deleteEmptyServerFarm = false);

        /// <summary>
        /// Delete a website slot.
        /// </summary>
        /// <param name="webspaceName">webspace the site is in.</param>
        /// <param name="websiteName">website name.</param>
        /// <param name="slot">The website slot name</param>
        void DeleteWebsite(string webspaceName, string websiteName, string slot);

        /// <summary>
        /// Get the WebSpaces.
        /// </summary>
        /// <returns>Collection of WebSpace objects</returns>
        IList<WebSpace> ListWebSpaces();

        /// <summary>
        /// Get the sites in the given webspace
        /// </summary>
        /// <param name="spaceName">Name of webspace</param>
        /// <returns>The sites</returns>
        IList<Site> ListSitesInWebSpace(string spaceName);

        /// <summary>
        /// Get a list of the user names configured to publish to the space.
        /// </summary>
        /// <returns>The list of user names.</returns>
        IList<string> ListPublishingUserNames();

        /// <summary>
        /// Enables site diagnostic.
        /// </summary>
        /// <param name="name">The website name</param>
        /// <param name="webServerLogging">Flag for webServerLogging</param>
        /// <param name="detailedErrorMessages">Flag for detailedErrorMessages</param>
        /// <param name="failedRequestTracing">Flag for failedRequestTracing</param>
        void EnableSiteDiagnostic(
            string name,
            bool webServerLogging,
            bool detailedErrorMessages,
            bool failedRequestTracing);

        /// <summary>
        /// Disables site diagnostic.
        /// </summary>
        /// <param name="name">The website name</param>
        /// <param name="webServerLogging">Flag for webServerLogging</param>
        /// <param name="detailedErrorMessages">Flag for detailedErrorMessages</param>
        /// <param name="failedRequestTracing">Flag for failedRequestTracing</param>
        void DisableSiteDiagnostic(
            string name,
            bool webServerLogging,
            bool detailedErrorMessages,
            bool failedRequestTracing);

        /// <summary>
        /// Enables application diagnostic.
        /// </summary>
        /// <param name="name">The website name</param>
        /// <param name="output">The application log output, FileSystem or StorageTable</param>
        /// <param name="properties">The diagnostic setting properties</param>
        void EnableApplicationDiagnostic(
            string name,
            WebsiteDiagnosticOutput output,
            Dictionary<DiagnosticProperties, object> properties);

        /// <summary>
        /// Disables application diagnostic.
        /// </summary>
        /// <param name="name">The website name</param>
        /// <param name="output">The application log output, FileSystem or StorageTable</param>
        void DisableApplicationDiagnostic(string name, WebsiteDiagnosticOutput output);

        /// <summary>
        /// Sets an AppSetting of a website.
        /// </summary>
        /// <param name="name">The website name</param>
        /// <param name="key">The app setting name</param>
        /// <param name="value">The app setting value</param>
        void SetAppSetting(string name, string key, string value);

        /// <summary>
        /// Sets a connection string for a website.
        /// </summary>
        /// <param name="name">Name of the website.</param>
        /// <param name="key">Connection string key.</param>
        /// <param name="value">Value for the connection string.</param>
        /// <param name="connectionStringType">Type of connection string.</param>
        void SetConnectionString(string name, string key, string value, DatabaseType connectionStringType);

        /// <summary>
        /// Lists available website locations.
        /// </summary>
        /// <returns>List of location names</returns>
        List<string> ListAvailableLocations();

        /// <summary>
        /// Gets the default website DNS suffix for the current environment.
        /// </summary>
        /// <returns>The website DNS suffix</returns>
        string GetWebsiteDnsSuffix();

        /// <summary>
        /// Gets the default location for websites.
        /// </summary>
        /// <returns>The default location name.</returns>
        string GetDefaultLocation();

        /// <summary>
        /// Checks if a website exists.
        /// </summary>
        /// <param name="name">The website name.</param>
        /// <returns>True if exists, false otherwise.</returns>
        bool WebsiteExists(string name);

        /// <summary>
        /// Checks if a website exists.
        /// </summary>
        /// <param name="name">The website name.</param>
        /// <param name="name">The website slot name.</param>
        /// <returns>True if exists, false otherwise.</returns>
        bool WebsiteExists(string name, string slot);

        /// <summary>
        /// Updates a website compute mode.
        /// </summary>
        /// <param name="websiteToUpdate">The website to update</param>
        void UpdateWebsiteComputeMode(Site websiteToUpdate);

        /// <summary>
        /// Gets a website slot DNS name.
        /// </summary>
        /// <param name="name">The website name</param>
        /// <param name="slot">The slot name</param>
        /// <returns>the slot DNS name</returns>
        string GetSlotDnsName(string name, string slot);

        /// <summary>
        /// Gets a website slot.
        /// </summary>
        /// <param name="Name">The website name</param>
        /// <param name="Slot">The website slot name</param>
        /// <returns>The website slot object</returns>
        Site GetWebsite(string name, string slot);

        /// <summary>
        /// Gets all slots for a website
        /// </summary>
        /// <param name="Name">The website name</param>
        /// <returns>The website slots list</returns>
        List<Site> GetWebsiteSlots(string name);

        /// <summary>
        /// Lists all websites under the current subscription
        /// </summary>
        /// <returns>List of websites</returns>
        List<Site> ListWebsites();

        /// <summary>
        /// Lists all websites with the provided slot name.
        /// </summary>
        /// <param name="slot">The slot name</param>
        /// <returns>The list if websites</returns>
        List<Site> ListWebsites(string slot);

        /// <summary>
        /// Gets a website slot configuration
        /// </summary>
        /// <param name="name">The website name</param>
        /// <param name="slot">The website slot name</param>
        /// <returns>The website cobfiguration object</returns>
        SiteConfig GetWebsiteConfiguration(string name, string slot);

        /// <summary>
        /// Gets a website if it exists in the given webspace
        /// </summary>
        /// <param name="webSpace">The webSpace to search in</param>
        /// <param name="name">The name of the website</param>
        /// <returns>The website if it exists, otherwise null.</returns>
        Site GetWebsiteIfExists(WebSpace webSpace, string name);

        /// <summary>
        /// Enables application diagnostic on website slot.
        /// </summary>
        /// <param name="name">The website name</param>
        /// <param name="output">The application log output, FileSystem or StorageTable</param>
        /// <param name="properties">The diagnostic setting properties</param>
        /// <param name="slot">The website slot name</param>
        void EnableApplicationDiagnostic(
            string name,
            WebsiteDiagnosticOutput output,
            Dictionary<DiagnosticProperties, object> properties,
            string slot);

        /// <summary>
        /// Disables application diagnostic.
        /// </summary>
        /// <param name="name">The website name</param>
        /// <param name="output">The application log output, FileSystem or StorageTable</param>
        /// <param name="slot">The website slot name</param>
        void DisableApplicationDiagnostic(string name, WebsiteDiagnosticOutput output, string slot);

        /// <summary>
        /// Restarts a website.
        /// </summary>
        /// <param name="name">The website name</param>
        /// <param name="slot">The website slot name</param>
        void RestartWebsite(string name, string slot);

        /// <summary>
        /// Starts a website.
        /// </summary>
        /// <param name="name">The website name</param>
        /// <param name="slot">The website slot name</param>
        void StartWebsite(string name, string slot);

        /// <summary>
        /// Stops a website.
        /// </summary>
        /// <param name="name">The website name</param>
        /// <param name="slot">The website slot name</param>
        void StopWebsite(string name, string slot);

        /// <summary>
        /// Switches the given website slot with the production slot
        /// </summary>
        /// <param name="webspaceName">The webspace name</param>
        /// <param name="websiteName">The website name</param>
        /// <param name="slot1">The website's first slot name</param>
        /// <param name="slot2">The website's second slot name</param>
        void SwitchSlots(string webspaceName, string websiteName, string slot1, string slot2);

        /// <summary>
        /// Gets the slot name from the website name
        /// </summary>
        /// <param name="name">The website name</param>
        /// <returns>The slot name</returns>
        string GetSlotName(string name);

        /// <summary>
        /// Build a Visual Studio web project and generate a WebDeploy package.
        /// </summary>
        /// <param name="projectFile">The project file.</param>
        /// <param name="configuration">The configuration of the build, like Release or Debug.</param>
        /// <param name="logFile">The build log file if there is any error.</param>
        /// <returns>The full path of the generated WebDeploy package.</returns>
        string BuildWebProject(string projectFile, string configuration, string logFile);

        /// <summary>
        /// Gets the website WebDeploy publish profile.
        /// </summary>
        /// <param name="websiteName">Website name.</param>
        /// <param name="slot">Slot name. By default is null.</param>
        /// <returns>The publish profile.</returns>
        WebSiteGetPublishProfileResponse.PublishProfile GetWebDeployPublishProfile(string websiteName, string slot = null);

        /// <summary>
        /// Publish a WebDeploy package folder to a web site.
        /// </summary>
        /// <param name="websiteName">The name of the web site.</param>
        /// <param name="slot">The name of the slot.</param>
        /// <param name="package">The WebDeploy package.</param>
        /// <param name="connectionStrings">The connection strings to overwrite the ones in the Web.config file.</param>
        void PublishWebProject(string websiteName, string slot, string package, Hashtable connectionStrings);

        /// <summary>
        /// Parse the Web.config files to get the connection string names.
        /// </summary>
        /// <param name="defaultWebConfigFile">The default Web.config file.</param>
        /// <param name="overwriteWebConfigFile">The additional Web.config file for the specificed configuration, like Web.Release.Config file.</param>
        /// <returns>An array of connection string names from the Web.config files.</returns>
        string[] ParseConnectionStringNamesFromWebConfig(string defaultWebConfigFile, string overwriteWebConfigFile);

        /// <summary>
        /// Gets the website name without slot part
        /// </summary>
        /// <param name="name">The website full name which may include slot name</param>
        /// <returns>The website name</returns>
        string GetWebsiteNameFromFullName(string name);

        /// Filters the web jobs.
        /// </summary>
        /// <param name="options">The web job filter options</param>
        /// <returns>The filtered web jobs list</returns>
        List<PSWebJob> FilterWebJobs(WebJobFilterOptions options);

        /// <summary>
        /// Creates new web job for a website
        /// </summary>
        /// <param name="name">The website name</param>
        /// <param name="slot">The website slot name</param>
        /// <param name="jobName">The web job name</param>
        /// <param name="jobType">The web job type</param>
        /// <param name="jobFile">The web job file name</param>
        /// <returns>The created web job instance</returns>
        PSWebJob CreateWebJob(string name, string slot, string jobName, WebJobType jobType, string jobFile);

        /// <summary>
        /// Deletes a web job for a website.
        /// </summary>
        /// <param name="name">The website name</param>
        /// <param name="slot">The slot name</param>
        /// <param name="jobName">The web job name</param>
        /// <param name="jobType">The web job type</param>
        void DeleteWebJob(string name, string slot, string jobName, WebJobType jobType);

        /// <summary>
        /// Starts a web job in a website.
        /// </summary>
        /// <param name="name">The website name</param>
        /// <param name="slot">The slot name</param>
        /// <param name="jobName">The web job name</param>
        /// <param name="jobType">The web job type</param>
        void StartWebJob(string name, string slot, string jobName, WebJobType jobType);

        /// <summary>
        /// Stops a web job in a website.
        /// </summary>
        /// <param name="name">The website name</param>
        /// <param name="slot">The slot name</param>
        /// <param name="jobName">The web job name</param>
        /// <param name="jobType">The web job type</param>
        void StopWebJob(string name, string slot, string jobName, WebJobType jobType);

        /// <summary>
        /// Filters a web job history.
        /// </summary>
        /// <param name="options">The web job filter options</param>
        /// <returns>The filtered web jobs run list</returns>
        List<WebJobRun> FilterWebJobHistory(WebJobHistoryFilterOptions options);

        /// <summary>
        /// Saves a web job logs to file.
        /// </summary>
        /// <param name="name">The website name</param>
        /// <param name="slot">The slot name</param>
        /// <param name="jobName">The web job name</param>
        /// <param name="jobType">The web job type</param>
        void SaveWebJobLog(string name, string slot, string jobName, WebJobType jobType);

        /// <summary>
        /// Saves a web job logs to file.
        /// </summary>
        /// <param name="name">The website name</param>
        /// <param name="slot">The slot name</param>
        /// <param name="jobName">The web job name</param>
        /// <param name="jobType">The web job type</param>
        /// <param name="output">The output file name</param>
        /// <param name="runId">The job run id</param>
        void SaveWebJobLog(string name, string slot, string jobName, WebJobType jobType, string output, string runId);

        /// <summary>
        /// Gets the hostname of the website
        /// </summary>
        /// <param name="name">The website name</param>
        /// <param name="slot">The website slot name</param>
        /// <returns>The hostname</returns>
        string GetHostName(string name, string slot);

        /// <summary>
        /// Checks whether a website name is available or not.
        /// </summary>
        /// <param name="name">The website name</param>
        /// <returns>True means available, false otherwise</returns>
        bool CheckWebsiteNameAvailability(string name);

        WebsiteInstance[] ListWebsiteInstances(string webSpace, string fullName);
    }

    public enum WebsiteState
    {
        Running,
        Stopped
    }

    public enum WebsiteDiagnosticType
    {
        Site,
        Application
    }

    public enum WebsiteDiagnosticOutput
    {
        FileSystem,
        StorageTable
    }

    public enum DiagnosticProperties
    {
        StorageAccountName,
        LogLevel
    }

    public enum WebsiteSlotName
    {
        Production,
        Staging
    }
}
