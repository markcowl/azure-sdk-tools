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
using System.Collections.Generic;
using Microsoft.WindowsAzure.Commands.ServiceManagement.Model;


namespace Microsoft.WindowsAzure.Commands.ServiceManagement.IaaS.Extensions
{
    using NSM = Management.Compute.Models;

    /// <summary>
    /// SQL Extension's context object used by Get-AzureVMSqlServerExtension
    /// </summary>
    public class VirtualMachineSqlServerExtensionContext : VirtualMachineExtensionContext
    {
        /// <summary>
        /// auto-patching settings
        /// </summary>
        public AutoPatchingSettings AutoPatchingSettings;

        /// <summary>
        /// auto-backup settings
        /// </summary>
        public AutoBackupSettings AutoBackupSettings;

        /// <summary>
        /// status messages reported by extension
        /// </summary>
        public List<string> StatusMessages;

        /// <summary>
        /// resource extension substatus list
        /// </summary>
        public IList<NSM.ResourceExtensionSubStatus> SubStatusList { get; set; }
    }
}