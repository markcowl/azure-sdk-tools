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

namespace Microsoft.WindowsAzure.Management.CloudService.Test.Tests.Cmdlet
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Packaging;
    using System.Linq;
    using System.ServiceModel;
    using System.Text.RegularExpressions;
    using CloudService.Cmdlet;
    using CloudService.Model;
    using CloudService.Node.Cmdlet;
    using CloudService.Properties;
    using Management.Test.Stubs;
    using Management.Test.Tests.Utilities;
    using Microsoft.Samples.WindowsAzure.ServiceManagement;
    using Microsoft.WindowsAzure.Management.CloudService.ServiceConfigurationSchema;
    using Microsoft.WindowsAzure.Management.CloudService.ServiceDefinitionSchema;
    using Microsoft.WindowsAzure.Management.CloudService.Test.TestData;
    using Microsoft.WindowsAzure.Management.Extensions;
    using Microsoft.WindowsAzure.Management.Services;
    using Utilities;
    using VisualStudio.TestTools.UnitTesting;
    using ConfigConfigurationSetting = Microsoft.WindowsAzure.Management.CloudService.ServiceConfigurationSchema.ConfigurationSetting;

    /// <summary>
    /// Tests for the Publish-AzureServiceProject command.
    /// </summary>
    [TestClass]
    public class PublishAzureServiceProjectCommandTests : TestBase
    {
        private MockCommandRuntime mockCommandRuntime;

        private SimpleServiceManagement channel;

        private EnableAzureMemcacheRoleCommand enableCacheCmdlet;

        private AddAzureNodeWebRoleCommand addNodeWebCmdlet;

        private AddAzureNodeWorkerRoleCommand addNodeWorkerCmdlet;

        private AddAzureCacheWorkerRoleCommand addCacheRoleCmdlet;

        private PublishAzureServiceProjectCommand publishServiceCmdlet;

        [TestInitialize]
        public void SetupTest()
        {
            GlobalPathInfo.GlobalSettingsDirectory = Data.AzureSdkAppDir;
            CmdletSubscriptionExtensions.SessionManager = new InMemorySessionManager();
            mockCommandRuntime = new MockCommandRuntime();
            channel = new SimpleServiceManagement();

            enableCacheCmdlet = new EnableAzureMemcacheRoleCommand();
            addCacheRoleCmdlet = new AddAzureCacheWorkerRoleCommand();
            publishServiceCmdlet = new PublishAzureServiceProjectCommand(channel) { ShareChannel = true };

            addCacheRoleCmdlet.CommandRuntime = mockCommandRuntime;
            enableCacheCmdlet.CommandRuntime = mockCommandRuntime;
            publishServiceCmdlet.CommandRuntime = mockCommandRuntime;
        }

        /// <summary>
        /// Test a basic publish scenario.
        ///</summary>
        [TestMethod]
        public void PublishAzureServiceCreateBasicPackageTest()
        {
            // Create a temp directory that we'll use to "publish" our service
            using (FileSystemHelper files = new FileSystemHelper(this) { EnableMonitoring = true })
            {
                // Import our default publish settings
                files.CreateAzureSdkDirectoryAndImportPublishSettings();

                // Create a new channel to mock the calls to Azure and
                // determine all of the results that we'll need.
                

                // Create a new service that we're going to publish
                string serviceName = "TEST_SERVICE_NAME";
                
                string servicePath = files.CreateNewService(serviceName);
                channel.GetStorageServiceThunk = ss => new StorageService { ServiceName = serviceName };
                channel.GetStorageKeysThunk = sk => new StorageService { StorageServiceKeys = new StorageServiceKeys { Primary = serviceName } };

                // Get the publishing process started by creating the package
                
                publishServiceCmdlet.InitializeSettingsAndCreatePackage(servicePath);

                // Verify the generated files
                files.AssertFiles(new Dictionary<string, Action<string>>()
                {
                    {
                        serviceName + @"\deploymentSettings.json",
                        null
                    },
                    {
                        serviceName + @"\ServiceDefinition.csdef",
                        p => File.ReadAllText(p).Contains(serviceName)
                    },
                    {
                        serviceName + @"\ServiceConfiguration.Cloud.cscfg",
                        p => File.ReadAllText(p).Contains(serviceName)
                    },
                    {
                        serviceName + @"\ServiceConfiguration.Local.cscfg",
                        p => File.ReadAllText(p).Contains(serviceName)
                    },
                    {
                        serviceName + @"\cloud_package.cspkg",
                        p =>
                        {
                            using (Package package = Package.Open(p))
                            {
                                Assert.AreEqual(5, package.GetParts().Count());
                            }
                        }
                    }
                });
            }
        }

        /// <summary>
        /// Test a basic publish scenario.
        ///</summary>
        [TestMethod]
        public void PublishAzureServiceWithCacheWorkerRoleTest()
        {
            // Create a temp directory that we'll use to "publish" our service
            using (FileSystemHelper files = new FileSystemHelper(this) { EnableMonitoring = true })
            {
                // Import our default publish settings
                files.CreateAzureSdkDirectoryAndImportPublishSettings();

                // Create a new channel to mock the calls to Azure and
                // determine all of the results that we'll need.
                

                // Create a new service that we're going to publish
                string serviceName = "TEST_SERVICE_NAME";
                string storageName = "testx5fservicex5fname";
                string storageKey = "imba key";
                string cacheRoleName = "cache_worker";
                
                string servicePath = files.CreateNewService(serviceName);
                channel.GetStorageServiceThunk = ss => new StorageService { ServiceName = storageName };
                channel.GetStorageKeysThunk = sk => new StorageService { StorageServiceKeys = new StorageServiceKeys { Primary = storageKey } };

                // Add caching worker role
                addCacheRoleCmdlet.AddAzureCacheWorkerRoleProcess(cacheRoleName, 1, servicePath);

                // Get the publishing process started by creating the package
                
                publishServiceCmdlet.InitializeSettingsAndCreatePackage(servicePath);

                AzureService azureService = new AzureService(servicePath, null);
                RoleSettings cacheRole = azureService.Components.GetCloudConfigRole(cacheRoleName);
                AzureAssert.ConfigurationSettingExist(new ConfigConfigurationSetting { 
                    name = Resources.CachingConfigStoreConnectionStringSettingName, 
                    value = string.Format(Resources.CachingConfigStoreConnectionStringSettingValue, storageName, storageKey) }, cacheRole.ConfigurationSettings);
            }
        }

        /// <summary>
        /// Test a publish scenario with worker and web roles.
        ///</summary>
        [TestMethod]
        public void PublishAzureServiceCreateWorkersPackageTest()
        {
            // Create a temp directory that we'll use to "publish" our service
            using (FileSystemHelper files = new FileSystemHelper(this) { EnableMonitoring = true })
            {
                // Import our default publish settings
                files.CreateAzureSdkDirectoryAndImportPublishSettings();

                // Create a new channel to mock the calls to Azure and
                // determine all of the results that we'll need.
                

                // Create a new service that we're going to publish
                string serviceName = "TEST_SERVICE_NAME";
                
                string servicePath = files.CreateNewService(serviceName);
                // Add web and worker roles
                
                string webRoleName = "NODE_WEB_ROLE";
                addNodeWebCmdlet = new AddAzureNodeWebRoleCommand(servicePath) { CommandRuntime = mockCommandRuntime, Name = webRoleName, Instances = 2 };
                addNodeWebCmdlet.ExecuteCmdlet();
                
                string workerRoleName = "NODE_WORKER_ROLE";
                addNodeWorkerCmdlet = new AddAzureNodeWorkerRoleCommand(servicePath) { CommandRuntime = mockCommandRuntime, Name = workerRoleName, Instances = 2 };
                addNodeWorkerCmdlet.ExecuteCmdlet();
                channel.GetStorageServiceThunk = ss => new StorageService { ServiceName = serviceName };
                channel.GetStorageKeysThunk = sk => new StorageService { StorageServiceKeys = new StorageServiceKeys { Primary = serviceName } };

                // Get the publishing process started by creating the package
                
                publishServiceCmdlet.InitializeSettingsAndCreatePackage(servicePath);

                // Verify the generated files
                Action<string> verifyContainsNames =
                    p =>
                    {
                        string contents = File.ReadAllText(p);
                        Assert.IsTrue(contents.Contains(webRoleName));
                        Assert.IsTrue(contents.Contains(workerRoleName));
                    };
                files.AssertFiles(new Dictionary<string, Action<string>>()
                {
                    { serviceName + @"\deploymentSettings.json", null },
                    { serviceName + '\\' + webRoleName + @"\server.js", null },
                    { serviceName + '\\' + workerRoleName + @"\server.js", null },
                    { serviceName + @"\ServiceDefinition.csdef", verifyContainsNames },
                    { serviceName + @"\ServiceConfiguration.Cloud.cscfg", verifyContainsNames },
                    { serviceName + @"\ServiceConfiguration.Local.cscfg", verifyContainsNames },
                    {
                        serviceName + @"\cloud_package.cspkg",
                        p =>
                        {
                            using (Package package = Package.Open(p))
                            {
                                Assert.AreEqual(7, package.GetParts().Count());
                            }
                        }
                    }
                });
            }
        }

        /// <summary>
        /// Test a publish scenario with worker and web roles.
        ///</summary>
        [TestMethod]
        public void PublishAzureServiceManifestTest()
        {
            // Create a temp directory that we'll use to "publish" our service
            using (FileSystemHelper files = new FileSystemHelper(this) { EnableMonitoring = true })
            {
                // Import our default publish settings
                files.CreateAzureSdkDirectoryAndImportPublishSettings();

                // Create a new channel to mock the calls to Azure and
                // determine all of the results that we'll need.
                

                // Create a new service that we're going to publish
                string serviceName = "TEST_SERVICE_NAME";
                
                string servicePath = files.CreateNewService(serviceName);
                channel.GetStorageServiceThunk = ss => new StorageService { ServiceName = serviceName };
                channel.GetStorageKeysThunk = sk => new StorageService { StorageServiceKeys = new StorageServiceKeys { Primary = serviceName } };

                // Add web and worker roles
                
                string defaultWebRoleName = "WebRoleDefault";
                addNodeWebCmdlet = new AddAzureNodeWebRoleCommand(servicePath) { CommandRuntime = mockCommandRuntime, Name = defaultWebRoleName, Instances = 2 };
                addNodeWebCmdlet.ExecuteCmdlet();

                string defaultWorkerRoleName = "WorkerRoleDefault";
                addNodeWorkerCmdlet = new AddAzureNodeWorkerRoleCommand(servicePath) { CommandRuntime = mockCommandRuntime, Name = defaultWorkerRoleName, Instances = 2 };
                addNodeWorkerCmdlet.ExecuteCmdlet();

                AddAzureNodeWebRoleCommand matchWebRole = addNodeWebCmdlet;
                string matchWebRoleName = "WebRoleExactMatch";
                addNodeWebCmdlet = new AddAzureNodeWebRoleCommand(servicePath) { CommandRuntime = mockCommandRuntime, Name = matchWebRoleName, Instances = 2 };
                addNodeWebCmdlet.ExecuteCmdlet();

                AddAzureNodeWorkerRoleCommand matchWorkerRole = addNodeWorkerCmdlet;
                string matchWorkerRoleName = "WorkerRoleExactMatch";
                addNodeWorkerCmdlet = new AddAzureNodeWorkerRoleCommand(servicePath) { CommandRuntime = mockCommandRuntime, Name = matchWorkerRoleName, Instances = 2 };
                addNodeWorkerCmdlet.ExecuteCmdlet();

                AddAzureNodeWebRoleCommand overrideWebRole = addNodeWebCmdlet;
                string overrideWebRoleName = "WebRoleOverride";
                addNodeWebCmdlet = new AddAzureNodeWebRoleCommand(servicePath) { CommandRuntime = mockCommandRuntime, Name = overrideWebRoleName, Instances = 2 };
                addNodeWebCmdlet.ExecuteCmdlet();

                AddAzureNodeWorkerRoleCommand overrideWorkerRole = addNodeWorkerCmdlet;
                string overrideWorkerRoleName = "WorkerRoleOverride";
                addNodeWorkerCmdlet = new AddAzureNodeWorkerRoleCommand(servicePath) { CommandRuntime = mockCommandRuntime, Name = overrideWorkerRoleName, Instances = 2 };
                addNodeWorkerCmdlet.ExecuteCmdlet();

                AzureService testService = new AzureService(servicePath, null);
                RuntimePackageHelper.SetRoleRuntime(testService.Components.Definition, matchWebRoleName, testService.Paths, version: "0.8.2");
                RuntimePackageHelper.SetRoleRuntime(testService.Components.Definition, matchWorkerRoleName, testService.Paths, version: "0.8.2");
                RuntimePackageHelper.SetRoleRuntime(testService.Components.Definition, overrideWebRoleName, testService.Paths, overrideUrl: "http://OVERRIDE");
                RuntimePackageHelper.SetRoleRuntime(testService.Components.Definition, overrideWorkerRoleName, testService.Paths, overrideUrl: "http://OVERRIDE");
                testService.Components.Save(testService.Paths);

                // Get the publishing process started by creating the package
                
                publishServiceCmdlet.InitializeSettingsAndCreatePackage(servicePath, RuntimePackageHelper.GetTestManifest(files));

                AzureService updatedService = new AzureService(testService.Paths.RootPath, null);

                RuntimePackageHelper.ValidateRoleRuntime(updatedService.Components.Definition, defaultWebRoleName, "http://DATACENTER/node/default.exe;http://DATACENTER/iisnode/default.exe", null);
                RuntimePackageHelper.ValidateRoleRuntime(updatedService.Components.Definition, defaultWorkerRoleName, "http://DATACENTER/node/default.exe", null);
                RuntimePackageHelper.ValidateRoleRuntime(updatedService.Components.Definition, matchWorkerRoleName, "http://DATACENTER/node/foo.exe", null);
                RuntimePackageHelper.ValidateRoleRuntime(updatedService.Components.Definition, matchWebRoleName, "http://DATACENTER/node/foo.exe;http://DATACENTER/iisnode/default.exe", null);
                RuntimePackageHelper.ValidateRoleRuntime(updatedService.Components.Definition, overrideWebRoleName, null, "http://OVERRIDE");
                RuntimePackageHelper.ValidateRoleRuntime(updatedService.Components.Definition, overrideWorkerRoleName, null, "http://OVERRIDE");
            }
        }

        /// <summary>
        /// Test basic azure service deployment using a mock for the Azure
        /// calls.
        ///</summary>
        [TestMethod]
        public void PublishAzureNodeServiceSimpleDeployTest()
        {
            // Create a temp directory that we'll use to "publish" our service
            using (FileSystemHelper files = new FileSystemHelper(this) { EnableMonitoring = true })
            {
                // Import our default publish settings
                files.CreateAzureSdkDirectoryAndImportPublishSettings();

                // Create a new channel to mock the calls to Azure and
                // determine all of the results that we'll need.
                bool createdHostedService = false;
                bool createdOrUpdatedDeployment = false;
                Deployment expectedDeployment = new Deployment()
                {
                    Status = DeploymentStatus.Starting,
                    RoleInstanceList = new RoleInstanceList(
                        new RoleInstance[] { 
                            new RoleInstance() {
                                InstanceName = "Role_IN_0",
                                InstanceStatus = RoleInstanceStatus.ReadyRole 
                            } })
                };
                
                channel.GetStorageServiceThunk = ar => new StorageService();
                channel.CreateHostedServiceThunk = ar => createdHostedService = true;
                channel.GetHostedServiceWithDetailsThunk = ar => { throw new EndpointNotFoundException(); };
                channel.GetStorageKeysThunk = ar => new StorageService() { StorageServiceKeys = new StorageServiceKeys() { Primary = "VGVzdEtleSE=" } };
                channel.CreateOrUpdateDeploymentThunk = ar => createdOrUpdatedDeployment = true;
                channel.GetDeploymentBySlotThunk = ar => expectedDeployment;
                channel.ListCertificatesThunk = ar => new CertificateList();

                // Create a new service that we're going to publish
                string serviceName = "TEST_SERVICE_NAME";
                
                string servicePath = files.CreateNewService(serviceName);
                AzureService testService = new AzureService(servicePath, null);
                testService.AddWebRole(Data.NodeWebRoleScaffoldingPath);
                string cloudConfigFile = File.ReadAllText(testService.Paths.CloudConfiguration);
                File.WriteAllText(testService.Paths.CloudConfiguration, new Regex("<Certificates\\s*/>").Replace(cloudConfigFile, ""));
                // Get the publishing process started by creating the package
                
                publishServiceCmdlet.ShareChannel = true;
                publishServiceCmdlet.SkipUpload = true;
                mockCommandRuntime.ResetPipelines();
                publishServiceCmdlet.PublishService(servicePath);
                AzureService service = new AzureService(servicePath, null);

                // Verify the publish service attempted to create and update
                // the service through the mock.
                Assert.IsTrue(createdHostedService);
                Assert.IsTrue(createdOrUpdatedDeployment);
                Assert.AreEqual<string>(serviceName, service.ServiceName);
                Deployment actual = mockCommandRuntime.OutputPipeline[0] as Deployment;
                Assert.AreEqual<string>(expectedDeployment.Name, actual.Name);
                Assert.AreEqual<string>(expectedDeployment.Status, actual.Status);
                Assert.AreEqual<string>(expectedDeployment.DeploymentSlot, actual.DeploymentSlot);
            }
        }


        /// <summary>
        /// Test basic azure service deployment using a mock for the Azure
        /// calls.
        ///</summary>
        [TestMethod]
        public void PublishAzurePHPServiceSimpleDeployTest()
        {
            // Create a temp directory that we'll use to "publish" our service
            using (FileSystemHelper files = new FileSystemHelper(this) { EnableMonitoring = true })
            {
                // Import our default publish settings
                files.CreateAzureSdkDirectoryAndImportPublishSettings();

                // Create a new channel to mock the calls to Azure and
                // determine all of the results that we'll need.
                bool createdHostedService = false;
                bool createdOrUpdatedDeployment = false;
                Deployment expectedDeployment = new Deployment()
                {
                    Status = DeploymentStatus.Starting,
                    RoleInstanceList = new RoleInstanceList(
                        new RoleInstance[] { 
                            new RoleInstance() {
                                InstanceName = "Role_IN_0",
                                InstanceStatus = RoleInstanceStatus.ReadyRole 
                            } })
                };
                
                channel.GetStorageServiceThunk = ar => new StorageService();
                channel.CreateHostedServiceThunk = ar => createdHostedService = true;
                channel.GetHostedServiceWithDetailsThunk = ar => { throw new EndpointNotFoundException(); };
                channel.GetStorageKeysThunk = ar => new StorageService() { StorageServiceKeys = new StorageServiceKeys() { Primary = "VGVzdEtleSE=" } };
                channel.CreateOrUpdateDeploymentThunk = ar => createdOrUpdatedDeployment = true;
                channel.GetDeploymentBySlotThunk = ar => expectedDeployment;
                channel.ListCertificatesThunk = ar => new CertificateList();

                // Create a new service that we're going to publish
                string serviceName = "TEST_SERVICE_NAME";
                
                string servicePath = files.CreateNewService(serviceName);
                AzureService testService = new AzureService(servicePath, null);
                testService.AddWebRole(Resources.PHPScaffolding);
                string cloudConfigFile = File.ReadAllText(testService.Paths.CloudConfiguration);
                File.WriteAllText(testService.Paths.CloudConfiguration, new Regex("<Certificates\\s*/>").Replace(cloudConfigFile, ""));
                // Get the publishing process started by creating the package
                
                publishServiceCmdlet.ShareChannel = true;
                publishServiceCmdlet.SkipUpload = true;
                mockCommandRuntime.ResetPipelines();
                publishServiceCmdlet.PublishService(servicePath);
                AzureService service = new AzureService(servicePath, null);

                // Verify the publish service attempted to create and update
                // the service through the mock.
                Assert.IsTrue(createdHostedService);
                Assert.IsTrue(createdOrUpdatedDeployment);
                Assert.AreEqual<string>(serviceName, service.ServiceName);
                Deployment actual = mockCommandRuntime.OutputPipeline[0] as Deployment;
                Assert.AreEqual<string>(expectedDeployment.Name, actual.Name);
                Assert.AreEqual<string>(expectedDeployment.Status, actual.Status);
                Assert.AreEqual<string>(expectedDeployment.DeploymentSlot, actual.DeploymentSlot);
            }
        }

        /// <summary>
        /// Helper to create a function which will simulate returning different
        /// values each time its called.
        /// </summary>
        /// <typeparam name="T">Return type.</typeparam>
        /// <param name="responses">
        /// Compute the nth reponse when the function is called for the nth
        /// time.
        /// </param>
        /// <returns>
        /// Function to simulate different responses to multiple calls.
        /// </returns>
        public static Func<SimpleServiceManagementAsyncResult, T> MultiCallResponseBuilder<T>(params Func<T>[] responses)
        {
            int count = -1;
            return ar =>
            {
                count = Math.Min(count + 1, responses.Length - 1);
                return responses[count]();
            };
        }

        /// <summary>
        /// Test more complex azure service deployment using a mock for the
        /// Azure calls.
        ///</summary>
        [TestMethod]
        public void PublishAzureServiceInvolvedDeployTest()
        {
            // Create a temp directory that we'll use to "publish" our service
            using (FileSystemHelper files = new FileSystemHelper(this) { EnableMonitoring = true })
            {
                // Import our default publish settings
                files.CreateAzureSdkDirectoryAndImportPublishSettings();

                bool createdHostedService = false;
                bool createdOrUpdatedDeployment = false;
                bool storageCreated = false;
                StorageService storageService = new StorageService()
                {
                    StorageServiceProperties = new StorageServiceProperties() { Status = StorageServiceStatus.Creating }
                };
                Deployment deployment = new Deployment()
                {
                    Status = DeploymentStatus.Deploying,
                    RoleInstanceList = new RoleInstanceList(
                        new RoleInstance[] { 
                            new RoleInstance() {
                                InstanceName = "Role_IN_0",
                                InstanceStatus = RoleInstanceStatus.ReadyRole 
                            } })
                };

                // Create a new channel to mock the calls to Azure and
                // determine all of the results that we'll need.
                
                channel.GetStorageServiceThunk = MultiCallResponseBuilder(
                    () => null,
                    () => storageService,
                    () => { storageService.StorageServiceProperties.Status = StorageServiceStatus.Created; return storageService; });
                channel.CreateStorageServiceThunk = ar => storageCreated = true;
                channel.CreateHostedServiceThunk = ar => createdHostedService = true;
                channel.GetHostedServiceWithDetailsThunk = ar => { throw new EndpointNotFoundException(); };
                channel.GetStorageKeysThunk = ar => new StorageService() { StorageServiceKeys = new StorageServiceKeys() { Primary = "VGVzdEtleSE=" } };
                channel.CreateOrUpdateDeploymentThunk = ar =>
                {
                    createdOrUpdatedDeployment = true;
                    channel.GetDeploymentBySlotThunk = MultiCallResponseBuilder(
                () => deployment,
                () => { deployment.Status = DeploymentStatus.Starting; return deployment; });
                };
                channel.GetDeploymentBySlotThunk = ar => { throw new EndpointNotFoundException(); };
                channel.ListCertificatesThunk = ar => new CertificateList();

                // Create a new service that we're going to publish
                string serviceName = "TEST_SERVICE_NAME";
                
                string servicePath = files.CreateNewService(serviceName);

                // Get the publishing process started by creating the package
                
                publishServiceCmdlet.ShareChannel = true;
                publishServiceCmdlet.SkipUpload = true;
                publishServiceCmdlet.Launch = true;
                publishServiceCmdlet.PublishService(servicePath);
                AzureService service = new AzureService(servicePath, null);

                // Verify the publish service attempted to create and update
                // the service through the mock.
                Assert.IsTrue(storageCreated);
                Assert.IsTrue(createdHostedService);
                Assert.IsTrue(createdOrUpdatedDeployment);
                Assert.AreEqual(StorageServiceStatus.Created, storageService.StorageServiceProperties.Status);
                Assert.AreEqual(DeploymentStatus.Starting, deployment.Status);
                Assert.AreEqual<string>(serviceName, service.ServiceName);
            }
        }

        /// <summary>
        /// Test upgrading azure service deployment using a mock for the Azure
        /// calls.
        ///</summary>
        [TestMethod]
        public void PublishAzureServiceUpgradeTest()
        {
            // Create a temp directory that we'll use to "publish" our service
            using (FileSystemHelper files = new FileSystemHelper(this) { EnableMonitoring = true })
            {
                // Import our default publish settings
                files.CreateAzureSdkDirectoryAndImportPublishSettings();

                // Create a new channel to mock the calls to Azure and
                // determine all of the results that we'll need.
                bool createdHostedService = false;
                bool createdOrUpdatedDeployment = false;
                bool upgradedDeployment = false;
                
                channel.GetStorageServiceThunk = ar => new StorageService();
                channel.CreateHostedServiceThunk = ar => createdHostedService = true;
                channel.GetHostedServiceWithDetailsThunk = ar => new HostedService { Deployments = new DeploymentList() { new Deployment { DeploymentSlot = "Production" } } };
                channel.GetStorageKeysThunk = ar => new StorageService() { StorageServiceKeys = new StorageServiceKeys() { Primary = "VGVzdEtleSE=" } };
                channel.CreateOrUpdateDeploymentThunk = ar => createdOrUpdatedDeployment = true;
                channel.UpgradeDeploymentThunk = ar => upgradedDeployment = true;
                channel.GetDeploymentBySlotThunk = ar => new Deployment()
                {
                    Status = DeploymentStatus.Starting,
                    RoleInstanceList = new RoleInstanceList(
                        new RoleInstance[] { 
                            new RoleInstance() {
                                InstanceName = "Role_IN_0",
                                InstanceStatus = RoleInstanceStatus.ReadyRole 
                            } })
                };
                channel.ListCertificatesThunk = ar => new CertificateList();

                // Create a new service that we're going to publish
                string serviceName = "TEST_SERVICE_NAME";
                
                string servicePath = files.CreateNewService(serviceName);

                // Get the publishing process started by creating the package
                
                publishServiceCmdlet.ShareChannel = true;
                publishServiceCmdlet.SkipUpload = true;
                publishServiceCmdlet.PublishService(servicePath);

                // Verify the publish service upgraded the deployment
                Assert.IsFalse(createdHostedService);
                Assert.IsFalse(createdOrUpdatedDeployment);
                Assert.IsTrue(upgradedDeployment);
            }
        }

        /// <summary>
        /// Test that service name will change if user supplied a name parameter
        /// </summary>
        [TestMethod]
        public void PublishAzureServiceChangeServiceNameTest()
        {
            // Create a temp directory that we'll use to "publish" our service
            using (FileSystemHelper files = new FileSystemHelper(this) { EnableMonitoring = true })
            {
                // Import our default publish settings
                files.CreateAzureSdkDirectoryAndImportPublishSettings();

                // Create a new channel to mock the calls to Azure and
                // determine all of the results that we'll need.
                bool createdHostedService = false;
                bool createdOrUpdatedDeployment = false;
                
                channel.GetStorageServiceThunk = ar => new StorageService();
                channel.CreateHostedServiceThunk = ar => createdHostedService = true;
                channel.GetHostedServiceWithDetailsThunk = ar => { throw new EndpointNotFoundException(); };
                channel.GetStorageKeysThunk = ar => new StorageService() { StorageServiceKeys = new StorageServiceKeys() { Primary = "VGVzdEtleSE=" } };
                channel.CreateOrUpdateDeploymentThunk = ar => createdOrUpdatedDeployment = true;
                channel.UpgradeDeploymentThunk = ar => createdOrUpdatedDeployment = true;
                channel.GetDeploymentBySlotThunk = ar => new Deployment()
                {
                    Status = DeploymentStatus.Starting,
                    RoleInstanceList = new RoleInstanceList(
                         new RoleInstance[] { 
                            new RoleInstance() {
                                InstanceName = "Role_IN_0",
                                InstanceStatus = RoleInstanceStatus.ReadyRole 
                            } })
                };
                channel.ListCertificatesThunk = ar => new CertificateList();
                // Create a new service that we're going to publish
                string serviceName = "TEST_SERVICE_NAME";
                
                string servicePath = files.CreateNewService(serviceName);

                // Get the publishing process started by creating the package
                
                publishServiceCmdlet.ShareChannel = true;
                publishServiceCmdlet.SkipUpload = true;
                string newServiceName = "NewServiceName";
                publishServiceCmdlet.ServiceName = newServiceName;
                publishServiceCmdlet.PublishService(servicePath);
                AzureService service = new AzureService(servicePath, null);

                // Verify the publish service attempted to create and update
                // the service through the mock.
                Assert.IsTrue(createdHostedService);
                Assert.IsTrue(createdOrUpdatedDeployment);
                Assert.AreEqual<string>(newServiceName, service.ServiceName);
            }
        }

        /// <summary>
        /// Test scenario when a service already exists but no such deployment for the slot.
        /// </summary>
        [TestMethod]
        public void PublishAzureServiceCreateDeploymentForExistingService()
        {
            // Create a temp directory that we'll use to "publish" our service
            using (FileSystemHelper files = new FileSystemHelper(this) { EnableMonitoring = true })
            {
                // Import our default publish settings
                files.CreateAzureSdkDirectoryAndImportPublishSettings();

                // Create a new channel to mock the calls to Azure and
                // determine all of the results that we'll need.
                bool createdHostedService = false;
                bool createdOrUpdatedDeployment = false;
                
                channel.GetStorageServiceThunk = ar => new StorageService();
                channel.CreateHostedServiceThunk = ar => { };
                channel.GetHostedServiceWithDetailsThunk = ar => null;
                channel.GetStorageKeysThunk = ar => new StorageService() { StorageServiceKeys = new StorageServiceKeys() { Primary = "VGVzdEtleSE=" } };
                channel.CreateOrUpdateDeploymentThunk = ar => createdOrUpdatedDeployment = true;
                channel.GetDeploymentBySlotThunk = ar =>
                {
                    if (createdOrUpdatedDeployment)
                    {
                        Deployment deployment = new Deployment("TEST_SERVICE_NAME", "Production", DeploymentStatus.Running);
                        deployment.RoleInstanceList = new RoleInstanceList(new RoleInstance[] { new RoleInstance() { InstanceName = "Role_IN_0", InstanceStatus = RoleInstanceStatus.ReadyRole } });
                        return deployment;
                    }
                    else
                    {
                        throw new EndpointNotFoundException();
                    }
                };
                channel.ListCertificatesThunk = ar => new CertificateList();

                // Create a new service that we're going to publish
                string serviceName = "TEST_SERVICE_NAME";
                
                string servicePath = files.CreateNewService(serviceName);

                // Get the publishing process started by creating the package
                
                publishServiceCmdlet.ShareChannel = true;
                publishServiceCmdlet.SkipUpload = true;
                publishServiceCmdlet.PublishService(servicePath);
                AzureService service = new AzureService(servicePath, null);

                // Verify the publish service attempted to create and update
                // the service through the mock.
                Assert.IsFalse(createdHostedService);
                Assert.IsTrue(createdOrUpdatedDeployment);
                Assert.AreEqual<string>(serviceName, service.ServiceName);
            }
        }

        /// <summary>
        /// Ensure that any iisnode logs are removed prior to packaging the
        /// service.
        ///</summary>
        [TestMethod]
        public void PublishAzureServiceRemovesNodeLogs()
        {
            // Create a temp directory that we'll use to "publish" our service
            using (FileSystemHelper files = new FileSystemHelper(this) { EnableMonitoring = true })
            {
                // Import our default publish settings
                files.CreateAzureSdkDirectoryAndImportPublishSettings();

                // Create a new channel to mock the calls to Azure and
                // determine all of the results that we'll need.
                

                // Create a new service that we're going to publish
                string serviceName = "TEST_SERVICE_NAME";
                
                string servicePath = files.CreateNewService(serviceName);
                channel.GetStorageServiceThunk = ss => new StorageService { ServiceName = serviceName };
                channel.GetStorageKeysThunk = sk => new StorageService { StorageServiceKeys = new StorageServiceKeys { Primary = serviceName } };

                // Add a web role
                
                string webRoleName = "NODE_WEB_ROLE";
                addNodeWebCmdlet = new AddAzureNodeWebRoleCommand(servicePath) { CommandRuntime = mockCommandRuntime, Name = webRoleName, Instances = 2 };
                addNodeWebCmdlet.ExecuteCmdlet();
                string webRolePath = Path.Combine(servicePath, webRoleName);

                // Add a worker role
                
                string workerRoleName = "NODE_WORKER_ROLE";
                addNodeWorkerCmdlet = new AddAzureNodeWorkerRoleCommand(servicePath) { CommandRuntime = mockCommandRuntime, Name = workerRoleName, Instances = 2 };
                addNodeWorkerCmdlet.ExecuteCmdlet();
                string workerRolePath = Path.Combine(servicePath, workerRoleName);

                // Add second web and worker roles that we won't add log
                // entries to
                addNodeWebCmdlet = new AddAzureNodeWebRoleCommand(servicePath) { CommandRuntime = mockCommandRuntime, Name = "SECOND_WEB_ROLE", Instances = 2 };
                addNodeWebCmdlet.ExecuteCmdlet();
                addNodeWorkerCmdlet = new AddAzureNodeWorkerRoleCommand(servicePath) { CommandRuntime = mockCommandRuntime, Name = "SECOND_Worker_ROLE", Instances = 2 };
                addNodeWorkerCmdlet.ExecuteCmdlet();

                // Add fake logs directories for server.js
                string logName = "server.js.logs";
                string logPath = Path.Combine(webRolePath, logName);
                Directory.CreateDirectory(logPath);
                File.WriteAllText(Path.Combine(logPath, "0.txt"), "secret web role debug details were logged here");
                logPath = Path.Combine(Path.Combine(workerRolePath, "NestedDirectory"), logName);
                Directory.CreateDirectory(logPath);
                File.WriteAllText(Path.Combine(logPath, "0.txt"), "secret worker role debug details were logged here");

                // Get the publishing process started by creating the package
                
                publishServiceCmdlet.InitializeSettingsAndCreatePackage(servicePath);

                // Rip open the package and make sure we can't find the log
                string packagePath = Path.Combine(servicePath, "cloud_package.cspkg");
                using (Package package = Package.Open(packagePath))
                {
                    // Make sure the web role and worker role packages don't
                    // have any files with server.js.logs in the name
                    Action<string> validateRole = roleName =>
                    {
                        PackagePart rolePart = package.GetParts().Where(p => p.Uri.ToString().Contains(roleName)).First();
                        using (Package rolePackage = Package.Open(rolePart.GetStream()))
                        {
                            Assert.IsFalse(
                                rolePackage.GetParts().Any(p => p.Uri.ToString().Contains(logName)),
                                "Found {0} part in {1} package!",
                                logName,
                                roleName);
                        }
                    };
                    validateRole(webRoleName);
                    validateRole(workerRoleName);
                }
            }
        }

        /// <summary>
        /// Verifies that setup_web.cmd copies web.cloud.config to web.config
        /// </summary>
        [TestMethod]
        public void SetupWebCmdSuccessfull()
        {
            using (FileSystemHelper files = new FileSystemHelper(this) { EnableMonitoring = true })
            {
                string webRoleName = "webrole";
                string serviceName = "test";
                string rootPath = files.CreateNewService(serviceName);
                string setupWebPath = Path.Combine(rootPath, webRoleName, "bin", "setup_web.cmd");
                string webCloudConfigPath = Path.Combine(rootPath, webRoleName, "Web.cloud.config");
                string webConfigPath = Path.Combine(rootPath, webRoleName, "Web.config");
                addNodeWebCmdlet = new AddAzureNodeWebRoleCommand(rootPath) { CommandRuntime = mockCommandRuntime, Name = webRoleName};
                addNodeWebCmdlet.ExecuteCmdlet();
                Process.Start(setupWebPath).WaitForExit();

                Assert.AreEqual<string>(File.ReadAllText(webCloudConfigPath), File.ReadAllText(webConfigPath));
            }
        }

        /// <summary>
        /// Tests that creating package and pre publish preparations will succeed for
        /// a role without Startup task
        /// </summary>
        [TestMethod]
        public void PublishServiceWithEmptyStartup()
        {
            using (FileSystemHelper files = new FileSystemHelper(this) { EnableMonitoring = true })
            {
                string webRoleName = "webrole";
                string workerRoleName = "workerRole";
                string serviceName = "test";
                string rootPath = files.CreateNewService(serviceName);
                files.CreateAzureSdkDirectoryAndImportPublishSettings();
                AzureService service = new AzureService(rootPath, null);
                service.AddWebRole(Data.NodeWebRoleScaffoldingPath, webRoleName);
                service.AddWebRole(Data.NodeWorkerRoleScaffoldingPath, workerRoleName);
                WebRole webRole = service.Components.GetWebRole(webRoleName);
                WorkerRole workerRole = service.Components.GetWorkerRole(workerRoleName);
                webRole.Startup = new Startup();
                workerRole.Startup = new Startup();
                service.Components.Save(service.Paths);
                service = new AzureService(rootPath, null);
                channel.GetStorageServiceThunk = ss => new StorageService { ServiceName = serviceName };
                channel.GetStorageKeysThunk = sk => new StorageService { StorageServiceKeys = new StorageServiceKeys { Primary = serviceName } };

                publishServiceCmdlet.InitializeSettingsAndCreatePackage(rootPath, RuntimePackageHelper.GetTestManifest(files));

                webRole = service.Components.GetWebRole(webRoleName);
                workerRole = service.Components.GetWorkerRole(workerRoleName);
                Assert.IsNull(webRole.Startup.Task);
                Assert.IsNull(workerRole.Startup.Task);
                Assert.IsTrue(File.Exists(Path.Combine(rootPath, Resources.CloudPackageFileName)));
            }
        }

        /// <summary>
        /// Tests that creating package and pre publish preparations will succeed for
        /// a role without Startup element
        /// </summary>
        [TestMethod]
        public void PublishServiceWithoutStartup()
        {
            using (FileSystemHelper files = new FileSystemHelper(this) { EnableMonitoring = true })
            {
                string webRoleName = "webrole";
                string workerRoleName = "workerRole";
                string serviceName = "test";
                string rootPath = files.CreateNewService(serviceName);
                files.CreateAzureSdkDirectoryAndImportPublishSettings();
                AzureService service = new AzureService(rootPath, null);
                service.AddWebRole(Data.NodeWebRoleScaffoldingPath, webRoleName);
                service.AddWebRole(Data.NodeWorkerRoleScaffoldingPath, workerRoleName);
                WebRole webRole = service.Components.GetWebRole(webRoleName);
                WorkerRole workerRole = service.Components.GetWorkerRole(workerRoleName);
                webRole.Startup = null;
                workerRole.Startup = null;
                service.Components.Save(service.Paths);
                service = new AzureService(rootPath, null);
                channel.GetStorageServiceThunk = ss => new StorageService { ServiceName = serviceName };
                channel.GetStorageKeysThunk = sk => new StorageService { StorageServiceKeys = new StorageServiceKeys { Primary = serviceName } };

                publishServiceCmdlet.InitializeSettingsAndCreatePackage(rootPath, RuntimePackageHelper.GetTestManifest(files));

                webRole = service.Components.GetWebRole(webRoleName);
                workerRole = service.Components.GetWorkerRole(workerRoleName);
                Assert.IsNull(webRole.Startup);
                Assert.IsNull(workerRole.Startup);
                Assert.IsTrue(File.Exists(Path.Combine(rootPath, Resources.CloudPackageFileName)));
            }
        }
    }
}