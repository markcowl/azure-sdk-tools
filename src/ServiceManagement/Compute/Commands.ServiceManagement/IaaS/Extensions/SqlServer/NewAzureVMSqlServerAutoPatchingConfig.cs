﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management.Automation;

namespace Microsoft.WindowsAzure.Commands.ServiceManagement.IaaS.Extensions
{
    /// <summary>
    /// Helper cmdlet to construct instance of AutoPatching settings class
    /// </summary>
   [Cmdlet(
        VerbsCommon.New,
        AzureVMSqlServerAutoPatchingConfigNoun),
    OutputType(
        typeof(AutoPatchingSettings))]
    public class NewAzureVMSqlServerAutoPatchingConfigCommand : PSCmdlet
    {
        protected const string AzureVMSqlServerAutoPatchingConfigNoun = "AzureVMSqlServerAutoPatchingConfig";

        [Parameter]
        public SwitchParameter Enable { get; set; }

        [Parameter]
        [ValidateSetAttribute(new string[] { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Everyday" })]
        public string DayOfWeek { get; set; }

        [Parameter]
        public int MaintenanceWindowStartingHour { get; set; }

        [Parameter]
        public int MaintenanceWindowDuration { get; set; }

        [Parameter]
        [ValidateSetAttribute(new string[] { "Important", "Optional" })]
        public string PatchCategory { get; set; }

        /// <summary>
        /// Initialzies a new instance of the <see cref="NewSqlServerAutoPatchingConfigCommand"/> class.
        /// </summary>
        public NewAzureVMSqlServerAutoPatchingConfigCommand()
        {
        }

        /// <summary>
        /// Creates and returns <see cref="AutoPatchingSettings"/> object.
        /// </summary>
        protected override void ProcessRecord()
        {
            AutoPatchingSettings autoPatchingSettings = new AutoPatchingSettings();

            autoPatchingSettings.Enable = (Enable.IsPresent) ? Enable.ToBool() : false;
            autoPatchingSettings.DayOfWeek = DayOfWeek;
            autoPatchingSettings.MaintenanceWindowStartingHour = MaintenanceWindowStartingHour;
            autoPatchingSettings.MaintenanceWindowDuration = MaintenanceWindowDuration;
            autoPatchingSettings.UpdatePatchingCategory(PatchCategory);

            WriteObject(autoPatchingSettings);
        }
    }
}