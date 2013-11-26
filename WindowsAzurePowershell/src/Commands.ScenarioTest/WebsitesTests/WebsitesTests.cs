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

namespace Microsoft.WindowsAzure.Commands.ScenarioTest.WebsitesTests
{
    using System.IO;
    using Commands.ScenarioTest.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class WebsitesTests : WindowsAzurePowerShellTest
    {
        private string currentDirectory;

        public WebsitesTests()
            : base("Websites\\Common.ps1",
                   "Websites\\WebsitesTests.ps1")
        {

        }

        [TestInitialize]
        public override void TestSetup()
        {
            base.TestSetup();
            powershell.AddScript("Initialize-WebsiteTest");
            currentDirectory = Directory.GetCurrentDirectory();
        }

        [TestCleanup]
        public override void TestCleanup()
        {
            base.TestCleanup();
            Directory.SetCurrentDirectory(currentDirectory);
        }

        #region Remove-AzureWebsite Scenario Tests

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.Websites)]
        [TestCategory(Category.WAPack)]
        public void TestRemoveAzureWebsiteWithInvalidCredentials()
        {
            RunPowerShellTest("Test-WithInvalidCredentials {Remove-AzureWebsite $(Get-WebsiteName) -Force }");
        }

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.Websites)]
        [TestCategory(Category.WAPack)]
        public void TestRemoveAzureServiceWithValidName()
        {
            RunPowerShellTest("Test-RemoveAzureServiceWithValidName");
        }

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.Websites)]
        [TestCategory(Category.WAPack)]
        public void TestRemoveAzureServiceWithNonExistingName()
        {
            RunPowerShellTest("Test-RemoveAzureServiceWithNonExistingName");
        }

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.Websites)]
        [TestCategory(Category.WAPack)]
        public void TestRemoveAzureServiceWithWhatIf()
        {
            RunPowerShellTest("Test-RemoveAzureServiceWithWhatIf");
        }

        #endregion

        #region Get-AzureWebsiteLog Scenario Tests

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.Websites)]
        [TestCategory(Category.WAPack)]
        public void TestGetAzureWebsiteLogWithInvalidCredentials()
        {
            RunPowerShellTest("Test-WithInvalidCredentials { Get-AzureWebsiteLog -Tail -Name $(Get-WebsiteName) }");
        }

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.Websites)]
        [TestCategory(Category.WAPack)]
        public void TestGetAzureWebsiteLogTail()
        {
            RunPowerShellTest("Test-GetAzureWebsiteLogTail");
        }

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.Websites)]
        [TestCategory(Category.WAPack)]
        public void TestGetAzureWebsiteLogTailPath()
        {
            RunPowerShellTest("Test-GetAzureWebsiteLogTailPath");
        }

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.Websites)]
        [TestCategory(Category.WAPack)]
        public void TestGetAzureWebsiteLogTailUriEncoding()
        {
            RunPowerShellTest("Test-GetAzureWebsiteLogTailUriEncoding");
        }

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.Websites)]
        [TestCategory(Category.WAPack)]
        public void TestGetAzureWebsiteLogListPath()
        {
            RunPowerShellTest("Test-GetAzureWebsiteLogListPath");
        }

        #endregion

        #region Get-AzureWebsite Scenario Tests

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.Websites)]
        [TestCategory(Category.WAPack)]
        public void TestGetAzureWebsite()
        {
            RunPowerShellTest("Test-GetAzureWebsite");
        }

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.Websites)]
        [TestCategory(Category.WAPack)]
        public void TestGetAzureWebsiteWithStoppedSite()
        {
            RunPowerShellTest("Test-GetAzureWebsiteWithStoppedSite");
        }

        #endregion

        #region Start-AzureWebsite Scenario Tests

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.Websites)]
        [TestCategory(Category.WAPack)]
        public void TestStartAzureWebsite()
        {
            RunPowerShellTest("Test-StartAzureWebsite");
        }

        #endregion

        #region Stop-AzureWebsite Scenario Tests

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.Websites)]
        [TestCategory(Category.WAPack)]
        public void TestStopAzureWebsite()
        {
            RunPowerShellTest("Test-StopAzureWebsite");
        }

        #endregion

        #region Restart-AzureWebsite Scenario Tests

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.Websites)]
        [TestCategory(Category.WAPack)]
        public void TestRestartAzureWebsite()
        {
            RunPowerShellTest("Test-RestartAzureWebsite");
        }

        #endregion

        #region Enable-AzureWebsiteApplicationDiagnostic Scenario Tests

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.Websites)]
        public void TestEnableApplicationDiagnosticOnTableStorage()
        {
            RunPowerShellTest("Test-EnableApplicationDiagnosticOnTableStorage");
        }

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.Websites)]
        [TestCategory(Category.WAPack)]
        public void TestEnableApplicationDiagnosticOnFileSystem()
        {
            RunPowerShellTest("Test-EnableApplicationDiagnosticOnFileSystem");
        }

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.Websites)]
        [TestCategory(Category.WAPack)]
        public void TestUpdateTheDiagnositicLogLevel()
        {
            RunPowerShellTest("Test-UpdateTheDiagnositicLogLevel");
        }
        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.Websites)]
        public void TestReconfigureStorageAppDiagnostics()
        {
            RunPowerShellTest("Test-ReconfigureStorageAppDiagnostics");
        }

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.Websites)]
        public void TestThrowsForInvalidStorageAccountName()
        {
            RunPowerShellTest("Test-ThrowsForInvalidStorageAccountName");
        }

        #endregion

        #region Disable-AzureWebsiteApplicationDiagnostic Scenario Tests

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.Websites)]
        public void TestDisableApplicationDiagnosticOnTableStorage()
        {
            RunPowerShellTest("Test-DisableApplicationDiagnosticOnTableStorage");
        }

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.Websites)]
        [TestCategory(Category.WAPack)]
        public void TestDisableApplicationDiagnosticOnFileSystem()
        {
            RunPowerShellTest("Test-DisableApplicationDiagnosticOnFileSystem");
        }

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.Websites)]
        public void TestDisableApplicationDiagnosticOnTableStorageAndFile()
        {
            RunPowerShellTest("Test-DisableApplicationDiagnosticOnTableStorageAndFile");
        }

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.Websites)]
        public void TestDisablesFileOnly()
        {
            RunPowerShellTest("Test-DisablesFileOnly");
        }

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.Websites)]
        public void TestDisablesStorageOnly()
        {
            RunPowerShellTest("Test-DisablesStorageOnly");
        }

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.Websites)]
        public void TestDisablesBothByDefault()
        {
            RunPowerShellTest("Test-DisablesBothByDefault");
        }

        #endregion

        #region Get-AzureWebsiteLocation Scenario Tests

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.Websites)]
        [TestCategory(Category.WAPack)]
        public void TestGetAzureWebsiteLocation()
        {
            RunPowerShellTest("Test-GetAzureWebsiteLocation");
        }

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.Websites)]
        [TestCategory(Category.WAPack)]
        public void TestKuduAppsExpressApp()
        {
            RunPowerShellTest("Test-KuduAppsExpressApp");
        }

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.Websites)]
        [TestCategory(Category.WAPack)]
        public void TestGetAzureWebSiteListNone()
        {
            RunPowerShellTest("Test-GetAzureWebSiteListNone");
        }

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.Websites)]
        [TestCategory(Category.WAPack)]
        public void TestAzureWebSiteListAll()
        {
            RunPowerShellTest("Test-AzureWebSiteListAll");
        }

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.Websites)]
        [TestCategory(Category.WAPack)]
        public void TestAzureWebSiteShowSingleSite()
        {
            RunPowerShellTest("Test-AzureWebSiteShowSingleSite");
        }

        #endregion

        #region AzureWebSiteGitHubAllParms Scenario Tests

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.Websites)]
        [TestCategory(Category.WAPack)]
        public void TestNewAzureWebSiteMultipleCreds()
        {
            RunPowerShellTest("Test-NewAzureWebSiteMultipleCreds");
        }

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.Websites)]
        [TestCategory(Category.WAPack)]
        public void TestNewAzureWebSiteGitHubAllParms()
        {
            RunPowerShellTest("Test-NewAzureWebSiteGitHubAllParms");
        }

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.Websites)]
        [TestCategory(Category.WAPack)]
        public void TestNewAzureWebSiteUpdateGit()
        {
            RunPowerShellTest("Test-NewAzureWebSiteUpdateGit");
        }
        #endregion

    }
}
