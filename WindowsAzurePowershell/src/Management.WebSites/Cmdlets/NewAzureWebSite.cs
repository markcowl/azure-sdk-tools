﻿// ----------------------------------------------------------------------------------
//
// Copyright 2011 Microsoft Corporation
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

namespace Microsoft.WindowsAzure.Management.WebSites.Cmdlets
{
    using System;
    using System.Management.Automation;
    using System.ServiceModel;
    using Common;
    using Properties;
    using Services;

    /// <summary>
    /// Creates a new azure website.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "AzureWebSite")]
    public class NewAzureWebSiteCommand : WebsitesCmdletBase
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The geographic region to create the website")]
        [ValidateNotNullOrEmpty]
        public string Location
        {
            get;
            set;
        }

        [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "Custom host name to use.")]
        [ValidateNotNullOrEmpty]
        public string Hostname
        {
            get;
            set;
        }

        /// <summary>
        /// Initializes a new instance of the NewAzureWebSiteCommand class.
        /// </summary>
        public NewAzureWebSiteCommand()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the NewAzureWebSiteCommand class.
        /// </summary>
        /// <param name="channel">
        /// Channel used for communication with Azure's service management APIs.
        /// </param>
        public NewAzureWebSiteCommand(IWebsitesServiceManagement channel)
        {
            Channel = channel;
        }

        internal bool NewWebsiteProcess(string location, string hostname)
        {
            InvokeInOperationContext(() =>
            {
                try
                {
                    // New website
                    Website website = new Website
                                          {
                                              Name = hostname
                                          };

                    RetryCall(s => Channel.NewWebsite(s, location, website));
                }
                catch (CommunicationException ex)
                {
                    WriteErrorDetails(ex);
                }
            });

            return true;
        }

        protected override void ProcessRecord()
        {
            try
            {
                base.ProcessRecord();

                if (NewWebsiteProcess(Location, Hostname))
                {
                    SafeWriteObjectWithTimestamp(Resources.CompleteMessage);
                }
            }
            catch (Exception ex)
            {
                SafeWriteError(new ErrorRecord(ex, string.Empty, ErrorCategory.CloseError, null));
            }
        }
    }
}
