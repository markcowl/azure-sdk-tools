﻿// ----------------------------------------------------------------------------------
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

namespace Microsoft.WindowsAzure.Management.CloudService.Node.Cmdlet
{
    using System;
    using System.Management.Automation;
    using Microsoft.WindowsAzure.Management.CloudService.ServiceConfigurationSchema;
    using Model;
    using Properties;

    /// <summary>
    /// Create scaffolding for a new node web role, change cscfg file and csdef to include the added web role
    /// </summary>
    [Cmdlet(VerbsCommon.Add, "AzureNodeWebRole")]
    public class AddAzureNodeWebRoleCommand : AddRole
    {
        public AddAzureNodeWebRoleCommand(string rootPath = null) :
            base(Resources.NodeScaffolding, Resources.AddRoleMessageCreateNode, true, rootPath)
        {

        }
    }
}