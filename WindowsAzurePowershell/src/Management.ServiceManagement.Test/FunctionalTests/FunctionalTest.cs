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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

using System.Threading;
using System.Reflection;





namespace Microsoft.WindowsAzure.Management.ServiceManagement.Test.FunctionalTests
{    
    using System.Collections.ObjectModel;
    using System.Management.Automation;
    using Microsoft.WindowsAzure.ServiceManagement;
    using Microsoft.WindowsAzure.Management.Model;
    using Microsoft.WindowsAzure.Management.ServiceManagement.Model;
    using Microsoft.WindowsAzure.Management.ServiceManagement.Test.Properties;
    using Microsoft.WindowsAzure.Management.ServiceManagement.Test.FunctionalTests.ConfigDataInfo;

    using System.IO;

    using System.Security.Cryptography.X509Certificates;
    

    [TestClass]
    public class FunctionalTest : ServiceManagementTest
    {
        bool cleanup = false;
        
        

        private static string defaultService;
        private static string defaultVm;
        private const string vhdBlob = "vhdstore/os.vhd";
        private string vhdName = "os.vhd";
        private string serviceName;
        private string vmName;
        protected static string vhdBlobLocation;
        private const string filePath = @".\vnetconfig.netcfg";
        

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            
            defaultService = Utilities.GetUniqueShortName(serviceNamePrefix);
            Assert.IsFalse(vmPowershellCmdlets.TestAzureServiceName(defaultService), String.Format("Service Name: {0} already exists.", defaultService)); // Assert the name is unique and not used before.

            defaultVm = Utilities.GetUniqueShortName(vmNamePrefix);
            Assert.IsNull(vmPowershellCmdlets.GetAzureVM(defaultVm, defaultService));

            vmPowershellCmdlets.NewAzureQuickVM(OS.Windows, defaultVm, defaultService, imageName, password, locationName);
            Console.WriteLine("Service Name: {0} is created.", defaultService);


            vhdBlobLocation = blobUrlRoot + vhdBlob;
            try
            {
                vmPowershellCmdlets.AddAzureVhd(new FileInfo(localFile), vhdBlobLocation);
            }
            catch (Exception e)
            {
                if (e.ToString().Contains("already exists"))
                {
                    // Use the already uploaded vhd.
                    Console.WriteLine("Using already uploaded blob..");
                }
                else
                {
                    throw;
                }
            }

            vmPowershellCmdlets.SetAzureVNetConfig(filePath);

            
        }
                               

        [TestInitialize]
        public void Initialize()
        {
            pass = false;
            testStartTime = DateTime.Now;         
            
        }
              
        [TestMethod(), TestCategory("Functional"), TestProperty("Feature", "IAAS"), Priority(1), Owner("hylee"), Description("Test the cmdlet (Get-AzureStorageAccount)")]
        [Ignore]
        public void ScriptTestSample()
        {
            
            var result = vmPowershellCmdlets.RunPSScript("Get-Help Save-AzureVhd -full");
        }  

        
        
    
        [TestMethod(), TestCategory("Functional"), TestProperty("Feature", "IAAS"), Priority(1), Owner("hylee"), Description("Test the cmdlet ((New,Get,Set,Remove)-AzureAffinityGroup)")]
        public void AzureAffinityGroupTest()
        {
            cleanup = false;

            StartTest(MethodBase.GetCurrentMethod().Name, testStartTime);

            string affinityName1 = "affinityName1";
            string affinityLabel1 = affinityName1;
            string location1 = "West US";
            string description1 = "Affinity group for West US";

            string affinityName2 = "affinityName2";
            string affinityLabel2 = "label2";
            string location2 = "East US";
            string description2 = "Affinity group for East US";

            try
            {
                ServiceManagementCmdletTestHelper vmPowershellCmdlets = new ServiceManagementCmdletTestHelper();

                // Remove previously created affinity groups
                foreach (var aff in vmPowershellCmdlets.GetAzureAffinityGroup(null))
                {
                    if (aff.Name == affinityName1 || aff.Name == affinityName2)
                    {
                        vmPowershellCmdlets.RemoveAzureAffinityGroup(aff.Name);
                    }                    
                }
               
                // New-AzureAffinityGroup
                vmPowershellCmdlets.NewAzureAffinityGroup(affinityName1, location1, affinityLabel1, description1);
                vmPowershellCmdlets.NewAzureAffinityGroup(affinityName2, location2, affinityLabel2, description2);
                Console.WriteLine("Affinity groups created: {0}, {1}", affinityName1, affinityName2);

                // Get-AzureAffinityGroup

                pass = AffinityGroupVerify(vmPowershellCmdlets.GetAzureAffinityGroup(affinityName1)[0], affinityName1, affinityLabel1, location1, description1);
                pass &= AffinityGroupVerify(vmPowershellCmdlets.GetAzureAffinityGroup(affinityName2)[0], affinityName2, affinityLabel2, location2, description2);
                

                // Set-AzureAffinityGroup
                vmPowershellCmdlets.SetAzureAffinityGroup(affinityName2, affinityLabel1, description1);
                Console.WriteLine("update affinity group: {0}", affinityName2);

                pass &= AffinityGroupVerify(vmPowershellCmdlets.GetAzureAffinityGroup(affinityName2)[0], affinityName2, affinityLabel1, location2, description1);
               

                // Remove-AzureAffinityGroup
                vmPowershellCmdlets.RemoveAzureAffinityGroup(affinityName2);
                Console.WriteLine("affinity group removed: {0}", affinityName2);
                Utilities.CheckRemove(vmPowershellCmdlets.GetAzureAffinityGroup, affinityName2);
                vmPowershellCmdlets.RemoveAzureAffinityGroup(affinityName1);
                
            }
            catch (Exception e)
            {
                pass = false;
                Assert.Fail(e.ToString());
            }
        }

        private bool AffinityGroupVerify(AffinityGroupContext affContext, string name, string label, string location, string description)
        {
            bool result = true;

            Console.WriteLine("AffinityGroup: Name - {0}, Location - {1}, Label - {2}, Description - {3}", affContext.Name, affContext.Location, affContext.Label, affContext.Description);
            try
            {
                Assert.AreEqual(affContext.Name, name, "Error: Affinity Name is not equal!");
                Assert.AreEqual(affContext.Label, label, "Error: Affinity Label is not equal!");
                Assert.AreEqual(affContext.Location, location, "Error: Affinity Location is not equal!");
                Assert.AreEqual(affContext.Description, description, "Error: Affinity Description is not equal!");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                result = false;
            }
            return result;
        }


        
        [TestMethod(), TestCategory("Functional"), TestProperty("Feature", "IAAS"), Priority(1), Owner("hylee"), Description("Test the cmdlet ((Add,Get,Remove)-AzureCertificate)")]
        public void AzureCertificateTest()
        {
            cleanup = false;

            StartTest(MethodBase.GetCurrentMethod().Name, testStartTime);
            

            // Certificate1: get it from the installed certificate.
            string certLocation = "cert:\\CurrentUser\\My\\*";
            PSObject cert1 = vmPowershellCmdlets.RunPSScript("Get-Item " + certLocation)[0];            
            string cert1data = Convert.ToBase64String(((X509Certificate2)cert1.BaseObject).RawData);

            // Certificate2: pfx file
            string pfxFileName = "pstestcert.pfx";            
            string password = "p@ssw0rd";            
            string thumbprintAlgorithm = "sha1";

            X509Certificate2Collection cert2 = new X509Certificate2Collection();
            cert2.Import(pfxFileName, password, X509KeyStorageFlags.PersistKeySet);
            string cert2data = Convert.ToBase64String(cert2[0].RawData);
            string thumbprint = cert2[0].Thumbprint;

            // Certificate3: cer file (this cer file has the same key with pfx file)
            string cerFileName = "pstestcert.cer";


            try
            {
                RemoveAllExistingCerts(defaultService);

                // Add a cert item
                vmPowershellCmdlets.AddAzureCertificate(defaultService, cert1);
                CertificateContext getCert1 = vmPowershellCmdlets.GetAzureCertificate(defaultService)[0];

                Console.WriteLine("Cert is added: {0}", getCert1.Thumbprint);
                Assert.AreEqual(getCert1.Data, cert1data, "Cert is different!!");

                // Add .pfx file
                vmPowershellCmdlets.AddAzureCertificate(defaultService, pfxFileName, password);

                CertificateContext getCert = vmPowershellCmdlets.GetAzureCertificate(defaultService, thumbprint, thumbprintAlgorithm)[0];
                Console.WriteLine("Cert is added: {0}", thumbprint);              
                Assert.AreEqual(getCert.Data, cert2data, "Cert is different!!");

                vmPowershellCmdlets.RemoveAzureCertificate(defaultService, thumbprint, "sha1");
                pass = Utilities.CheckRemove(vmPowershellCmdlets.GetAzureCertificate, defaultService, thumbprint, thumbprintAlgorithm);


                // Add .cer file
                vmPowershellCmdlets.AddAzureCertificate(defaultService, cerFileName);
                getCert = vmPowershellCmdlets.GetAzureCertificate(defaultService, thumbprint, thumbprintAlgorithm)[0];
                Console.WriteLine("Cert is added: {0}", thumbprint);               
                Assert.AreEqual(getCert.Data, cert2data, "Cert is different!!");

                RemoveAllExistingCerts(defaultService);                
            }
            catch (Exception e)
            {
                pass = false;
                Assert.Fail(e.ToString());
            }
        }

        private void RemoveAllExistingCerts(string serviceName)
        {
            vmPowershellCmdlets.RunPSScript(String.Format("{0} -ServiceName {1} | {2}", Utilities.GetAzureCertificateCmdletName, serviceName, Utilities.RemoveAzureCertificateCmdletName)); 
        }


        [TestMethod(), TestCategory("Functional"), TestProperty("Feature", "IAAS"), Priority(1), Owner("hylee"), Description("Test the cmdlet (New-AzureCertificateSetting)")]
        public void AzureCertificateSettingTest()
        {

            StartTest(MethodBase.GetCurrentMethod().Name, testStartTime);
            
            string store = "My";

            // Get the first certificate from \CurrentUser\My\ location.
            string certLocation = "cert:\\CurrentUser\\My\\*";
            PSObject cert1 = vmPowershellCmdlets.RunPSScript("Get-Item " + certLocation)[0];
            string thumbprint = ((X509Certificate2)cert1.BaseObject).Thumbprint;            
            
            try
            {
                vmName = Utilities.GetUniqueShortName("PSTestVM");
                serviceName = Utilities.GetUniqueShortName("PSTestService");

                vmPowershellCmdlets.NewAzureService(serviceName, locationName);
                vmPowershellCmdlets.AddAzureCertificate(serviceName, cert1);
                               
                CertificateSettingList certList = new CertificateSettingList();
                certList.Add(vmPowershellCmdlets.NewAzureCertificateSetting(store, thumbprint));
                
                AzureVMConfigInfo azureVMConfigInfo = new AzureVMConfigInfo(vmName, VMSizeInfo.Small, imageName);               
                AzureProvisioningConfigInfo azureProvisioningConfig = new AzureProvisioningConfigInfo(OS.Windows, certList, "Cert1234!");                                

                PersistentVMConfigInfo persistentVMConfigInfo = new PersistentVMConfigInfo(azureVMConfigInfo, azureProvisioningConfig, null, null);           
                
                PersistentVM vm = vmPowershellCmdlets.GetPersistentVM(persistentVMConfigInfo);
                
                vmPowershellCmdlets.NewAzureVM(serviceName, new [] {vm});

                
                PersistentVMRoleContext result = vmPowershellCmdlets.GetAzureVM(vmName, serviceName);

                Console.WriteLine("{0} is created", result.Name);

                pass = true;
                cleanup = true;

            }
            catch (Exception e)
            {
                pass = false;
                cleanup = false;
                Assert.Fail(e.ToString());                                
            }
            
        }    

        [TestMethod(), TestCategory("Functional"), TestProperty("Feature", "IAAS"), Priority(1), Owner("hylee"), Description("Test the cmdlet ((Add,Get,Set,Remove)-AzureDataDisk)")]
        public void AzureDataDiskTest()
        {
            cleanup = false;
            StartTest(MethodBase.GetCurrentMethod().Name, testStartTime);
            
            string diskLabel1 = "disk1";
            int diskSize1 = 30;            
            int lunSlot1 = 0;

            string diskLabel2 = "disk2";
            int diskSize2 = 50;
            int lunSlot2 = 2;


            try
            {                
                AddAzureDataDiskConfig dataDiskInfo1 = new AddAzureDataDiskConfig(DiskCreateOption.CreateNew, diskSize1, diskLabel1, lunSlot1);
                AddAzureDataDiskConfig dataDiskInfo2 = new AddAzureDataDiskConfig(DiskCreateOption.CreateNew, diskSize2, diskLabel2, lunSlot2);

                vmPowershellCmdlets.AddDataDisk(defaultVm, defaultService, new [] {dataDiskInfo1, dataDiskInfo2}); // Add-AzureEndpoint with Get-AzureVM and Update-AzureVm  

                Assert.IsTrue(CheckDataDisk(defaultVm, defaultService, dataDiskInfo1, HostCaching.None), "Data disk is not properly added");
                Console.WriteLine("Data disk added correctly.");

                Assert.IsTrue(CheckDataDisk(defaultVm, defaultService, dataDiskInfo2, HostCaching.None), "Data disk is not properly added");
                Console.WriteLine("Data disk added correctly.");

                vmPowershellCmdlets.SetDataDisk(defaultVm, defaultService, HostCaching.ReadOnly, lunSlot1);
                Assert.IsTrue(CheckDataDisk(defaultVm, defaultService, dataDiskInfo1, HostCaching.ReadOnly), "Data disk is not properly changed");
                Console.WriteLine("Data disk is changed correctly.");

                pass = true;

            }
            catch (Exception e)
            {
                pass = false;
                Assert.Fail("Exception occurred: {0}", e.ToString());
                

            }
            finally
            {
                // Remove DataDisks created
                foreach (DataVirtualHardDisk disk in vmPowershellCmdlets.GetAzureDataDisk(defaultVm, defaultService))
                {
                    vmPowershellCmdlets.RemoveDataDisk(defaultVm, defaultService, new[] { disk.Lun }); // Remove-AzureDataDisk                    
                    RemoveDisk(disk.DiskName, 10);
                }
                Assert.AreEqual(0, vmPowershellCmdlets.GetAzureDataDisk(defaultVm, defaultService).Count, "DataDisk is not removed.");
                
            }
        }

        private void RemoveDisk(string diskName, int maxTry)
        {
            for (int i = 0; i < maxTry ; i++)
            {
                try
                {
                
                    vmPowershellCmdlets.RemoveAzureDisk(diskName, true);
                    break;
                }
                catch (Exception e)
                {
                    if (i == maxTry)
                    {
                        Console.WriteLine("Max try reached.  Couldn't delete the Virtual disk");
                    }
                    if (e.ToString().Contains("currently in use"))
                    {
                        Thread.Sleep(5000);
                        continue;
                    }
                }
            }            
        }


       



        private bool CheckDataDisk(string vmName, string serviceName, AddAzureDataDiskConfig dataDiskInfo, HostCaching hc)
        {            
            bool found = false;
            foreach (DataVirtualHardDisk disk in vmPowershellCmdlets.GetAzureDataDisk(vmName, serviceName))
            {
                Console.WriteLine("DataDisk - Name:{0}, Label:{1}, Size:{2}, LUN:{3}, HostCaching: {4}", disk.DiskName, disk.DiskLabel, disk.LogicalDiskSizeInGB, disk.Lun, disk.HostCaching);
                if (disk.DiskLabel == dataDiskInfo.DiskLabel && disk.LogicalDiskSizeInGB == dataDiskInfo.DiskSizeGB && disk.Lun == dataDiskInfo.LunSlot)
                {
                    if (disk.HostCaching == hc.ToString())
                    {
                        found = true;
                        Console.WriteLine("DataDisk found: {0}", disk.DiskLabel);
                    }
                }
            }
            return found;
        }


        [TestMethod(), TestCategory("Functional"), TestProperty("Feature", "IAAS"), Priority(1), Owner("hylee"), Description("Test the cmdlet ((Add,Get,Update,Remove)-AzureDisk)")]
        public void AzureDiskTest()
        {
            StartTest(MethodBase.GetCurrentMethod().Name, testStartTime);
            cleanup = false;
           
            

            string mediaLocation = String.Format("{0}vhdstore/{1}", blobUrlRoot, vhdName);
            

            try
            {
                vmPowershellCmdlets.AddAzureDisk(vhdName, mediaLocation, vhdName, null);

                bool found = false;
                foreach (DiskContext disk in vmPowershellCmdlets.GetAzureDisk(vhdName))
                {
                    Console.WriteLine("Disk: Name - {0}, Label - {1}, Size - {2},", disk.DiskName, disk.Label, disk.DiskSizeInGB);
                    if (disk.DiskName == vhdName && disk.Label == vhdName)
                    {
                        found = true;
                        Console.WriteLine("{0} is found", disk.DiskName);
                    }

                }
                Assert.IsTrue(found, "Error: Disk is not added");

                string newLabel = "NewLabel";
                vmPowershellCmdlets.UpdateAzureDisk(vhdName, newLabel);

                DiskContext disk2 = vmPowershellCmdlets.GetAzureDisk(vhdName)[0];

                Console.WriteLine("Disk: Name - {0}, Label - {1}, Size - {2},", disk2.DiskName, disk2.Label, disk2.DiskSizeInGB);
                Assert.AreEqual(newLabel, disk2.Label);
                Console.WriteLine("Disk Label is successfully updated");

                vmPowershellCmdlets.RemoveAzureDisk(vhdName, true);
                Assert.IsTrue(Utilities.CheckRemove(vmPowershellCmdlets.GetAzureDisk, vhdName), "The disk was not removed");

            }
            catch (Exception e)
            {
                pass = false;

                if (e.ToString().Contains("ResourceNotFound"))
                {
                    Console.WriteLine("Please upload {0} file to \\vhdtest\\ blob directory before running this test", vhdName);
                }
                
                Assert.Fail("Exception occurs: {0}", e.ToString());                
            }
        }

    
        [TestMethod(), TestCategory("Functional"), TestProperty("Feature", "PAAS"), Priority(1), Owner("hylee"), Description("Test the cmdlet ((New,Get,Set,Remove,Move)-AzureDeployment)")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.CSV", ".\\package.csv", "package#csv", DataAccessMethod.Sequential)]
        public void AzureDeploymentTest()
        {
            StartTest(MethodBase.GetCurrentMethod().Name, testStartTime);
            cleanup = true;


            // Choose the package and config files from local machine
            string packageName = Convert.ToString(TestContext.DataRow["packageName"]);
            string configName = Convert.ToString(TestContext.DataRow["configName"]);
            string upgradePackageName = Convert.ToString(TestContext.DataRow["upgradePackage"]);
            string upgradeConfigName = Convert.ToString(TestContext.DataRow["upgradeConfig"]);
            //string upgradeConfigName2 = Convert.ToString(TestContext.DataRow["upgradeConfig2"]);


            var packagePath1 = new FileInfo(@".\" + packageName);
            var configPath1 = new FileInfo(@".\" + configName);
            var packagePath2 = new FileInfo(@".\" + upgradePackageName);
            var configPath2 = new FileInfo(@".\" + upgradeConfigName);
            //var configPath3 = new FileInfo(@".\" + upgradeConfigName2);


            Assert.IsTrue(File.Exists(packagePath1.FullName), "VHD file not exist={0}", packagePath1);
            Assert.IsTrue(File.Exists(configPath1.FullName), "VHD file not exist={0}", configPath1);
            

            string deploymentName = "deployment1";
            string deploymentLabel = "label1";
            DeploymentInfoContext result;


            try
            {
                serviceName = Utilities.GetUniqueShortName("PSTestService");
                vmPowershellCmdlets.NewAzureService(serviceName, serviceName, locationName);
                Console.WriteLine("service, {0}, is created.", serviceName);

                vmPowershellCmdlets.NewAzureDeployment(serviceName, packagePath1.FullName, configPath1.FullName, DeploymentSlotType.Staging, deploymentLabel, deploymentName, false, false);
                result = vmPowershellCmdlets.GetAzureDeployment(serviceName, DeploymentSlotType.Staging);
                pass = Utilities.PrintAndCompareDeployment(result, serviceName, deploymentName, deploymentLabel, DeploymentSlotType.Staging, null, 1);
                Console.WriteLine("successfully deployed the package");


                // Move the deployment from 'Staging' to 'Production'
                vmPowershellCmdlets.MoveAzureDeployment(serviceName);
                result = vmPowershellCmdlets.GetAzureDeployment(serviceName, DeploymentSlotType.Production);
                pass &= Utilities.PrintAndCompareDeployment(result, serviceName, deploymentName, deploymentLabel, DeploymentSlotType.Production, null, 1);                
                Console.WriteLine("successfully moved");


                // Set the deployment status to 'Suspended'
                vmPowershellCmdlets.SetAzureDeploymentStatus(serviceName, DeploymentSlotType.Production, DeploymentStatus.Suspended);
                result = vmPowershellCmdlets.GetAzureDeployment(serviceName, DeploymentSlotType.Production);
                pass &= Utilities.PrintAndCompareDeployment(result, serviceName, deploymentName, deploymentLabel, DeploymentSlotType.Production, DeploymentStatus.Suspended, 1);
                Console.WriteLine("successfully changed the status");


                // Update the deployment
                vmPowershellCmdlets.SetAzureDeploymentConfig(serviceName, DeploymentSlotType.Production, configPath2.FullName);
                result = vmPowershellCmdlets.GetAzureDeployment(serviceName, DeploymentSlotType.Production);
                pass &= Utilities.PrintAndCompareDeployment(result, serviceName, deploymentName, deploymentLabel, DeploymentSlotType.Production, null, 2);
                Console.WriteLine("successfully updated the deployment");


                // Upgrade the deployment
                DateTime start = DateTime.Now;
                vmPowershellCmdlets.SetAzureDeploymentUpgrade(serviceName, DeploymentSlotType.Production, UpgradeType.Auto, packagePath2.FullName, configPath2.FullName);
                TimeSpan duration = DateTime.Now - start;
                Console.WriteLine("Auto upgrade took {0}.", duration);

                result = vmPowershellCmdlets.GetAzureDeployment(serviceName, DeploymentSlotType.Production);
                pass &= Utilities.PrintAndCompareDeployment(result, serviceName, deploymentName, serviceName, DeploymentSlotType.Production, null, 2);
                Console.WriteLine("successfully updated the deployment");

                // DISABLED: Upgrade the deployment simultaneously from 2 instances to 2 instances
                //start = DateTime.Now;
                //vmPowershellCmdlets.SetAzureDeploymentUpgrade(serviceName, DeploymentSlotType.Production, UpgradeType.Simultaneous, packagePath2.FullName, configPath2.FullName);
                //TimeSpan duration2 = DateTime.Now - start;
                //Console.WriteLine("Simultaneous Upgrade took {0}.", duration2);
                //Assert.IsTrue(duration2 < duration, "Simultaneous upgrade took more time!!");

                //result = vmPowershellCmdlets.GetAzureDeployment(serviceName, DeploymentSlotType.Production);
                //Utilities.PrintAndCompareDeployment(result, serviceName, deploymentName, serviceName, DeploymentSlotType.Production, null, 2);
                //Console.WriteLine("successfully updated the deployment");


                // DISABLED: Upgrade the deployment simultaneously from 2 instances to 4 instances

                //start = DateTime.Now;
                //vmPowershellCmdlets.SetAzureDeploymentUpgrade(serviceName, DeploymentSlotType.Production, UpgradeType.Simultaneous, packagePath2.FullName, configPath3.FullName);
                //TimeSpan duration3 = DateTime.Now - start;
                //Console.WriteLine("Simultaneous Upgrade took {0}.", duration3);

                //result = vmPowershellCmdlets.GetAzureDeployment(serviceName, DeploymentSlotType.Production);
                //Utilities.PrintAndCompareDeployment(result, serviceName, deploymentName, serviceName, DeploymentSlotType.Production, null, 4);
                //Console.WriteLine("successfully updated the deployment");

                               
                vmPowershellCmdlets.RemoveAzureDeployment(serviceName, DeploymentSlotType.Production, true);

                pass &= Utilities.CheckRemove(vmPowershellCmdlets.GetAzureDeployment, serviceName, DeploymentSlotType.Production);
                

            }
            catch (Exception e)
            {
                pass = false;
                Assert.Fail("Exception occurred: {0}", e.ToString());
            }
            finally
            {

            }
        }

        /// <summary>
        /// 
        /// </summary>
        [TestMethod(), TestCategory("Functional"), TestProperty("Feature", "IAAS"), Priority(1), Owner("hylee"), Description("Test the cmdlet ((New,Get)-AzureDns)")]
        public void AzureDnsTest()
        {
            cleanup = true;
            StartTest(MethodBase.GetCurrentMethod().Name, testStartTime);


            string dnsName = "OpenDns1";
            string ipAddress = "208.67.222.222";

            try
            {
                serviceName = Utilities.GetUniqueShortName("PSTestService");
                vmPowershellCmdlets.NewAzureService(serviceName, locationName);

                DnsServer dns = vmPowershellCmdlets.NewAzureDns(dnsName, ipAddress);

                AzureVMConfigInfo azureVMConfigInfo = new AzureVMConfigInfo(vmName, VMSizeInfo.ExtraSmall, imageName);
                AzureProvisioningConfigInfo azureProvisioningConfig = new AzureProvisioningConfigInfo(OS.Windows, "password1234!");     
           
                PersistentVMConfigInfo persistentVMConfigInfo = new PersistentVMConfigInfo(azureVMConfigInfo, azureProvisioningConfig, null, null);           
                
                PersistentVM vm = vmPowershellCmdlets.GetPersistentVM(persistentVMConfigInfo);  
           
                vmPowershellCmdlets.NewAzureVM(serviceName, new []{vm}, null, new[]{dns}, null, null, null, null, null, null);
                


                DnsServerList dnsList =  vmPowershellCmdlets.GetAzureDns(vmPowershellCmdlets.GetAzureDeployment(serviceName, DeploymentSlotType.Production).DnsSettings);
                foreach (DnsServer dnsServer in dnsList)
                {
                    Console.WriteLine("DNS Server Name: {0}, DNS Server Address: {1}", dnsServer.Name, dnsServer.Address);
                    Assert.AreEqual(dnsServer.Name, dns.Name);
                    Assert.AreEqual(dnsServer.Address, dns.Address);
                }

                pass = true;

            }
            catch (Exception e)
            {
                pass = false;
                Assert.Fail("Exception occurred: {0}", e.ToString());                
            }            
        }


        [TestMethod(), TestCategory("Functional"), TestProperty("Feature", "IAAS"), Priority(1), Owner("hylee"), Description("Test the cmdlet ((Add,Get,Set,Remove)-AzureEndpoint)")]
        public void AzureEndpointTest()
        {
            cleanup = false;
            StartTest(MethodBase.GetCurrentMethod().Name, testStartTime);

            string epName1 = "tcp1";
            int localPort1 = 60010;
            int port1 = 60011;

            string epName2 = "tcp2";
            int localPort2 = 60020;
            int port2 = 60021;


            try
            {
                // Add two new endpoints
                AzureEndPointConfigInfo epInfo1 = new AzureEndPointConfigInfo(ProtocolInfo.tcp, localPort1, port1, epName1);
                AzureEndPointConfigInfo epInfo2 = new AzureEndPointConfigInfo(ProtocolInfo.tcp, localPort2, port2, epName2);

                vmPowershellCmdlets.AddEndPoint(defaultVm , defaultService, new[] { epInfo1, epInfo2 }); // Add-AzureEndpoint with Get-AzureVM and Update-AzureVm                             
                Assert.IsTrue(CheckEndpoint(defaultVm, defaultService, epInfo1), "Error: Endpoint was not added!");
                Assert.IsTrue(CheckEndpoint(defaultVm, defaultService, epInfo2), "Error: Endpoint was not added!");

                // Change the endpoint
                AzureEndPointConfigInfo epInfo3 = new AzureEndPointConfigInfo(ProtocolInfo.tcp, 60030, 60031, epName2);
                vmPowershellCmdlets.SetEndPoint(defaultVm, defaultService, epInfo3); // Set-AzureEndpoint with Get-AzureVM and Update-AzureVm                 
                Assert.IsTrue(CheckEndpoint(defaultVm, defaultService, epInfo3), "Error: Endpoint was not changed!");

                // Remove Endpoint
                vmPowershellCmdlets.RemoveEndPoint(defaultVm, defaultService, new[] { epName1, epName2 }); // Remove-AzureEndpoint                
                Assert.IsFalse(CheckEndpoint(defaultVm, defaultService, epInfo1), "Error: Endpoint was not removed!");
                Assert.IsFalse(CheckEndpoint(defaultVm, defaultService, epInfo3), "Error: Endpoint was not removed!");

                pass = true;

            }
            catch (Exception e)
            {
                pass = false;
                Assert.Fail("Exception occurred: {0}", e.ToString());
            }            
        }

        private bool CheckEndpoint(string vmName, string serviceName, AzureEndPointConfigInfo epInfo)
        {
            bool found = false;
            foreach (InputEndpointContext ep in vmPowershellCmdlets.GetAzureEndPoint(vmPowershellCmdlets.GetAzureVM(vmName, serviceName)))
            {
                Console.WriteLine("Endpoint - Name:{0}, Protocol:{1}, Port:{2}, LocalPort:{3}, Vip:{4}", ep.Name, ep.Protocol, ep.Port, ep.LocalPort, ep.Vip);
                if (ep.Name == epInfo.EndpointName && ep.LocalPort == epInfo.InternalPort && ep.Port == epInfo.ExternalPort && ep.Protocol == epInfo.Protocol.ToString())
                {
                    found = true;
                    Console.WriteLine("Endpoint found: {0}", epInfo.EndpointName);
                }
            }
            return found;
        }


        [TestMethod(), TestCategory("Functional"), TestProperty("Feature", "IAAS"), Priority(1), Owner("hylee"), Description("Test the cmdlet (Get-AzureLocation)")]
        public void AzureLocationTest()
        {
            cleanup = false;
            StartTest(MethodBase.GetCurrentMethod().Name, testStartTime);

            try
            {
                foreach (LocationsContext loc in vmPowershellCmdlets.GetAzureLocation())
                {
                    Console.WriteLine("Location: Name - {0}, DisplayName - {1}", loc.Name, loc.DisplayName);
                }

                pass = true;

            }
            catch (Exception e)
            {
                pass = false;
                Assert.Fail("Exception occurred: {0}", e.ToString());
            }            
        }



  



        [TestMethod(), TestCategory("Functional"), TestProperty("Feature", "IAAS"), Priority(1), Owner("hylee"), Description("Test the cmdlet ((Get,Set)-AzureOSDisk)")]
        public void AzureOSDiskTest()
        {
            cleanup = false;
            StartTest(MethodBase.GetCurrentMethod().Name, testStartTime);

            try
            {
                PersistentVM vm = vmPowershellCmdlets.GetAzureVM(defaultVm, defaultService).VM;
                OSVirtualHardDisk osdisk = vmPowershellCmdlets.GetAzureOSDisk(vm);
                Console.WriteLine("OS Disk: Name - {0}, Label - {1}, HostCaching - {2}, OS - {3}", osdisk.DiskName, osdisk.DiskLabel, osdisk.HostCaching, osdisk.OS);
                Assert.IsTrue(osdisk.Equals(vm.OSVirtualHardDisk), "OS disk returned is not the same!");

                PersistentVM vm2 = vmPowershellCmdlets.SetAzureOSDisk(HostCaching.ReadOnly, vm);
                osdisk = vmPowershellCmdlets.GetAzureOSDisk(vm2);
                Console.WriteLine("OS Disk: Name - {0}, Label - {1}, HostCaching - {2}, OS - {3}", osdisk.DiskName, osdisk.DiskLabel, osdisk.HostCaching, osdisk.OS);
                Assert.IsTrue(osdisk.Equals(vm2.OSVirtualHardDisk), "OS disk returned is not the same!");

                pass = true;

            }
            catch (Exception e)
            {
                pass = false;
                Assert.Fail("Exception occurred: {0}", e.ToString());
            }
        }

        [TestMethod(), TestCategory("Functional"), TestProperty("Feature", "IAAS"), Priority(1), Owner("hylee"), Description("Test the cmdlet (Get-AzureOSVersion)")]
        public void AzureOSVersionTest()
        {
            cleanup = false;
            StartTest(MethodBase.GetCurrentMethod().Name, testStartTime);       

            try
            {
                foreach (OSVersionsContext osVersions in vmPowershellCmdlets.GetAzureOSVersion())
                {
                    Console.WriteLine("OS Version: Family - {0}, FamilyLabel - {1}, Version - {2}", osVersions.Family, osVersions.FamilyLabel, osVersions.Version);
                }

                pass = true;

            }
            catch (Exception e)
            {
                pass = false;
                Assert.Fail("Exception occurred: {0}", e.ToString());
            }
        }

        [TestMethod(), TestCategory("Functional"), TestProperty("Feature", "PAAS"), Priority(1), Owner("hylee"), Description("Test the cmdlet ((Get,Set)-AzureRole)")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.CSV", ".\\package.csv", "package#csv", DataAccessMethod.Sequential)]
        public void AzureRoleTest()
        {
            cleanup = false;
            StartTest(MethodBase.GetCurrentMethod().Name, testStartTime);

            // Choose the package and config files from local machine
            string packageName = Convert.ToString(TestContext.DataRow["packageName"]);
            string configName = Convert.ToString(TestContext.DataRow["configName"]);
            string upgradePackageName = Convert.ToString(TestContext.DataRow["upgradePackage"]);
            string upgradeConfigName = Convert.ToString(TestContext.DataRow["upgradeConfig"]);


            var packagePath1 = new FileInfo(@".\" + packageName);
            var configPath1 = new FileInfo(@".\" + configName);

            Assert.IsTrue(File.Exists(packagePath1.FullName), "VHD file not exist={0}", packagePath1);
            Assert.IsTrue(File.Exists(configPath1.FullName), "VHD file not exist={0}", configPath1);
            

            string deploymentName = "deployment1";
            string deploymentLabel = "label1";
            string slot = DeploymentSlotType.Production;

            //DeploymentInfoContext result;
            string roleName = "";

            try
            {
            

                serviceName = Utilities.GetUniqueShortName("PSTestService");
                vmPowershellCmdlets.NewAzureService(serviceName, serviceName, locationName);

                vmPowershellCmdlets.NewAzureDeployment(serviceName, packagePath1.FullName, configPath1.FullName, slot, deploymentLabel, deploymentName, false, false);

            
                foreach (RoleContext role in vmPowershellCmdlets.GetAzureRole(serviceName, slot, null, false))
                {
                    Console.WriteLine("Role: Name - {0}, ServiceName - {1}, DeploymenntID - {2}, InstanceCount - {3}", role.RoleName, role.ServiceName, role.DeploymentID, role.InstanceCount);
                    Assert.AreEqual(serviceName, role.ServiceName);
                    roleName = role.RoleName;
                }
                
                vmPowershellCmdlets.SetAzureRole(serviceName, slot, roleName, 2);

                foreach (RoleContext role in vmPowershellCmdlets.GetAzureRole(serviceName, slot, null, false))
                {
                    Console.WriteLine("Role: Name - {0}, ServiceName - {1}, DeploymenntID - {2}, InstanceCount - {3}", role.RoleName, role.ServiceName, role.DeploymentID, role.InstanceCount);
                    Assert.AreEqual(serviceName, role.ServiceName);
                    Assert.AreEqual(2, role.InstanceCount);                   
                }

                pass = true;
                cleanup = true;

            }
            catch (Exception e)
            {
                pass = false;
                Assert.Fail("Exception occurred: {0}", e.ToString());
            }
        }

        [TestMethod(), TestCategory("Functional"), TestProperty("Feature", "IAAS"), Priority(1), Owner("hylee"), Description("Test the cmdlet ((Get,Set)-AzureSubnet)")]
        public void AzureSubnetTest()
        {
            cleanup = true;
            StartTest(MethodBase.GetCurrentMethod().Name, testStartTime);

            try
            {
                serviceName = Utilities.GetUniqueShortName("PSTestService");
                vmPowershellCmdlets.NewAzureService(serviceName, serviceName, locationName);
                
                PersistentVM vm = vmPowershellCmdlets.NewAzureVMConfig(new AzureVMConfigInfo(vmName, VMSizeInfo.Small, imageName));
                AzureProvisioningConfigInfo azureProvisioningConfig = new AzureProvisioningConfigInfo(OS.Windows, "password1234!");
                azureProvisioningConfig.Vm = vm;

                string [] subs = new []  {"subnet1", "subnet2", "subnet3"};
                vm = vmPowershellCmdlets.SetAzureSubnet(vmPowershellCmdlets.AddAzureProvisioningConfig(azureProvisioningConfig), subs);
                
                SubnetNamesCollection subnets = vmPowershellCmdlets.GetAzureSubnet(vm);
                foreach (string subnet in subnets)
                {
                    Console.WriteLine("Subnet: {0}", subnet);
                }                
                CollectionAssert.AreEqual(subnets, subs);
                
                pass = true;
            }
            catch (Exception e)
            {
                pass = false;
                Assert.Fail("Exception occurred: {0}", e.ToString());
            }
        }


        [TestMethod(), TestCategory("Functional"), TestProperty("Feature", "IAAS"), Priority(1), Owner("hylee"), Description("Test the cmdlet ((New,Get)-AzureStorageKey)")]
        public void AzureStorageKeyTest()
        {
            cleanup = false;
            StartTest(MethodBase.GetCurrentMethod().Name, testStartTime);
            
            try
            {
                StorageServiceKeyOperationContext key1 = vmPowershellCmdlets.GetAzureStorageAccountKey(defaultAzureSubscription.CurrentStorageAccount); // Get-AzureStorageAccountKey
                Console.WriteLine("Primary - {0}", key1.Primary);
                Console.WriteLine("Secondary - {0}", key1.Secondary);

                StorageServiceKeyOperationContext key2 = vmPowershellCmdlets.NewAzureStorageAccountKey(defaultAzureSubscription.CurrentStorageAccount, KeyType.Secondary);
                Console.WriteLine("Primary - {0}", key2.Primary);
                Console.WriteLine("Secondary - {0}", key2.Secondary);

                Assert.AreEqual(key1.Primary, key2.Primary);
                Assert.AreNotEqual(key1.Secondary, key2.Secondary);

                pass = true;
            }
            catch (Exception e)
            {
                pass = false;
                Assert.Fail("Exception occurred: {0}", e.ToString());
            }            
        }


        [TestMethod(), TestCategory("Functional"), TestProperty("Feature", "IAAS"), Priority(1), Owner("hylee"), Description("Test the cmdlet ((New,Get,Set,Remove)-AzureStorageAccount)")]
        public void AzureStorageAccountTest()
        {
            cleanup = false;
            StartTest(MethodBase.GetCurrentMethod().Name, testStartTime);
            

            string storageName1 = Utilities.GetUniqueShortName("psteststorage");
            string locationName1 = "West US";
            string storageName2 = Utilities.GetUniqueShortName("psteststorage");
            string locationName2 = "East US";

            try
            {
                vmPowershellCmdlets.NewAzureStorageAccount(storageName1, locationName1, null, null, null);
                vmPowershellCmdlets.NewAzureStorageAccount(storageName2, locationName2, null, null, null);

                Assert.IsNotNull(vmPowershellCmdlets.GetAzureStorageAccount(storageName1));
                Console.WriteLine("{0} is created", storageName1);
                Assert.IsNotNull(vmPowershellCmdlets.GetAzureStorageAccount(storageName2));                
                Console.WriteLine("{0} is created", storageName2);

                vmPowershellCmdlets.SetAzureStorageAccount(storageName1, "newLabel", "newDescription", false);

                StorageServicePropertiesOperationContext storage = vmPowershellCmdlets.GetAzureStorageAccount(storageName1)[0];
                Console.WriteLine("Name: {0}, Label: {1}, Description: {2}, GeoReplication: {3}", storage.StorageAccountName, storage.Label, storage.StorageAccountDescription, storage.GeoReplicationEnabled.ToString());
                Assert.IsTrue((storage.Label == "newLabel" && storage.StorageAccountDescription == "newDescription" && storage.GeoReplicationEnabled == false), "storage account is not changed correctly");
                

                vmPowershellCmdlets.RemoveAzureStorageAccount(storageName1);
                vmPowershellCmdlets.RemoveAzureStorageAccount(storageName2);


                Assert.IsTrue(Utilities.CheckRemove(vmPowershellCmdlets.GetAzureStorageAccount, storageName1), "The storage account was not removed");
                Assert.IsTrue(Utilities.CheckRemove(vmPowershellCmdlets.GetAzureStorageAccount, storageName2), "The storage account was not removed");
                pass = true;

            }
            catch (Exception e)
            {
                pass = false;
                Assert.Fail("Exception occurred: {0}", e.ToString());
            }            
        }


        [TestMethod(), TestCategory("Functional"), TestProperty("Feature", "IAAS"), Priority(1), Owner("hylee"), Description("Test the cmdlet ((Add,Get,Save,Update,Remove)-AzureVMImage)")]
        public void AzureVMImageTest()
        {

            cleanup = false;
            StartTest(MethodBase.GetCurrentMethod().Name, testStartTime);

            string newImageName = Utilities.GetUniqueShortName("vmimage");            
            string mediaLocation = string.Format("{0}vhdstore/{1}", blobUrlRoot, vhdName);

            string oldLabel = "old label";
            string newLabel = "new label";            

            try
            {                
                OSImageContext result = vmPowershellCmdlets.AddAzureVMImage(newImageName, mediaLocation, OSType.Windows, oldLabel);
                

                OSImageContext resultReturned = vmPowershellCmdlets.GetAzureVMImage(newImageName)[0];                

                Assert.IsTrue(CompareContext<OSImageContext>(result, resultReturned));

                result = vmPowershellCmdlets.UpdateAzureVMImage(newImageName, newLabel);

                resultReturned = vmPowershellCmdlets.GetAzureVMImage(newImageName)[0];

                Assert.IsTrue(CompareContext<OSImageContext>(result, resultReturned));
               
                vmPowershellCmdlets.RemoveAzureVMImage(newImageName);

                pass = true;

            }
            catch (Exception e)
            {
                pass = false;
                Assert.Fail("Exception occurred: {0}", e.ToString());
            }            
        }

        



        /// <summary>
        /// AzureVNetGatewayTest()       
        /// </summary>
        /// Note: Create a VNet, a LocalNet from the portal without creating a gateway.
        [TestMethod(), TestCategory("Functional"), TestProperty("Feature", "IAAS"), Priority(1), Owner("hylee"), Description("Test the cmdlet ((New,Get,Set,Remove)-AzureVNetGateway)")]
        [Ignore]
        public void AzureVNetGatewayTest()
        {
            cleanup = false;
            StartTest(MethodBase.GetCurrentMethod().Name, testStartTime);

            string vnetName1 = "NewVNet1"; // For connect test
            string vnetName2 = "NewVNet2"; // For disconnect test
            string vnetName3 = "NewVNet3"; // For create test

            string localNet = "LocalNet1"; // Your local network site name.

            try
            {
                // New-AzureVNetGateway
                vmPowershellCmdlets.NewAzureVNetGateway(vnetName3);

                foreach (VirtualNetworkSiteContext site in vmPowershellCmdlets.GetAzureVNetSite(vnetName3))
                {
                    Console.WriteLine("Name: {0}, AffinityGroup: {1}", site.Name, site.AffinityGroup);
                }

                // Remove-AzureVnetGateway
                vmPowershellCmdlets.RemoveAzureVNetGateway(vnetName3);
                foreach (VirtualNetworkGatewayContext gateway in vmPowershellCmdlets.GetAzureVNetGateway(vnetName3))
                {
                    Console.WriteLine("State: {0}, VIP: {1}", gateway.State.ToString(), gateway.VIPAddress);
                }
                
                

                // Set-AzureVNetGateway -Connect Test
                vmPowershellCmdlets.SetAzureVNetGateway("connect", vnetName1, localNet);
                
                foreach (GatewayConnectionContext connection in vmPowershellCmdlets.GetAzureVNetConnection(vnetName1))
                {
                    Console.WriteLine("Connectivity: {0}, LocalNetwork: {1}", connection.ConnectivityState, connection.LocalNetworkSiteName);
                    Assert.IsFalse(connection.ConnectivityState.ToLowerInvariant().Contains("notconnected"));
                }
                foreach (VirtualNetworkGatewayContext gateway in vmPowershellCmdlets.GetAzureVNetGateway(vnetName1))
                {
                    Console.WriteLine("State: {0}, VIP: {1}", gateway.State.ToString(), gateway.VIPAddress);
                }


                // Set-AzureVNetGateway -Disconnect
                vmPowershellCmdlets.SetAzureVNetGateway("disconnect", vnetName2, localNet);
               
                foreach (GatewayConnectionContext connection in vmPowershellCmdlets.GetAzureVNetConnection(vnetName2))
                {
                    Console.WriteLine("Connectivity: {0}, LocalNetwork: {1}", connection.ConnectivityState, connection.LocalNetworkSiteName);
                    if (connection.LocalNetworkSiteName == localNet)
                    {
                        Assert.IsTrue(connection.ConnectivityState.ToLowerInvariant().Contains("notconnected"));
                    }
                }

                foreach (VirtualNetworkGatewayContext gateway in vmPowershellCmdlets.GetAzureVNetGateway(vnetName2))
                {
                    Console.WriteLine("State: {0}, VIP: {1}", gateway.State.ToString(), gateway.VIPAddress);
                }

                pass = true;

            }
            catch (Exception e)
            {
                pass = false;
                Assert.Fail("Exception occurred: {0}", e.ToString());
            }            
        }

        /// <summary>
        /// 
        /// </summary>
        /// Note: You have to manually create a virtual network, a Local network, a gateway, and connect them.
        [TestMethod(), TestCategory("Functional"), TestProperty("Feature", "IAAS"), Priority(1), Owner("hylee"), Description("Test the cmdlet (Get-AzureVNetGatewayKey, Get-AzureVNetConnection)")]
        [Ignore]
        public void AzureVNetGatewayKeyTest()
        {
            cleanup = false;
            StartTest(MethodBase.GetCurrentMethod().Name, testStartTime); ;
            
            string vnetName = "NewVNet1";
            

            try
            {                
                SharedKeyContext result = vmPowershellCmdlets.GetAzureVNetGatewayKey(vnetName, vmPowershellCmdlets.GetAzureVNetConnection(vnetName)[0].LocalNetworkSiteName);
                Console.WriteLine(result.Value);

                pass = true;

            }
            catch (Exception e)
            {
                pass = false;
                Assert.Fail("Exception occurred: {0}", e.ToString());
            }            
        }


        [TestMethod(), TestCategory("Functional"), TestProperty("Feature", "IAAS"), Priority(1), Owner("hylee"), Description("Test the cmdlet ((Get,Set,Remove)-AzureVNetConfig)")]
        public void AzureVNetConfigTest()
        {
            cleanup = false;
            StartTest(MethodBase.GetCurrentMethod().Name, testStartTime);


            
            string affinityGroup = "WestUsAffinityGroup";

            try
            {
                if (Utilities.CheckRemove(vmPowershellCmdlets.GetAzureAffinityGroup, affinityGroup))
                {
                    vmPowershellCmdlets.NewAzureAffinityGroup(affinityGroup, Resource.Location, null, null);
                }
                

                var result = vmPowershellCmdlets.GetAzureVNetConfig(filePath);

                vmPowershellCmdlets.SetAzureVNetConfig(filePath);

                Collection<VirtualNetworkSiteContext> vnetSites = vmPowershellCmdlets.GetAzureVNetSite(null);
                foreach (var re in vnetSites)
                {
                    Console.WriteLine("VNet: {0}", re.Name);
                }

                vmPowershellCmdlets.RemoveAzureVNetConfig();

                Collection<VirtualNetworkSiteContext> vnetSitesAfter = vmPowershellCmdlets.GetAzureVNetSite(null);

                Assert.AreNotEqual(vnetSites.Count, vnetSitesAfter.Count, "No Vnet is removed");
                
                foreach (var re in vnetSitesAfter)
                {
                    Console.WriteLine("VNet: {0}", re.Name);
                }

                pass = true;

            }
            catch (Exception e)
            {
                if (e.ToString().Contains("while in use"))
                {
                    Console.WriteLine(e.InnerException.ToString());
                }
                else
                {
                    pass = false;
                    Assert.Fail("Exception occurred: {0}", e.ToString());
                }
            }           
        }

        private bool CompareContext<T>(T obj1, T obj2)
        {
            bool result = true;
            Type type = typeof(T);
 
            foreach(PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                string typeName = property.PropertyType.FullName;
                if (typeName.Equals("System.String") || typeName.Equals("System.Int32") || typeName.Equals("System.Uri") || typeName.Contains("Nullable"))
                {

                    var obj1Value = property.GetValue(obj1, null);
                    var obj2Value = property.GetValue(obj2, null);

                    if (obj1Value == null)
                    {
                        result &= (obj2Value == null);
                    }
                    else
                    {
                        result &= (obj1Value.Equals(obj2Value));
                    }
                }
                else
                {
                    Console.WriteLine("This type is not compared: {0}", typeName);
                }
            }

            return result;
        }
 
       
        [TestCleanup]
        public virtual void CleanUp()
        {

            Console.WriteLine("Test {0}", pass ? "passed" : "failed");
            
            // Cleanup            
            if (cleanup)
            {
                Console.WriteLine("Starting to clean up created VM and service.");

                try
                {

                    vmPowershellCmdlets.RemoveAzureVM(vmName, serviceName);
                    Console.WriteLine("VM, {0}, is deleted", vmName);
                 
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error during removing VM: {0}", e.ToString());
                }

                try
                {
                    vmPowershellCmdlets.RemoveAzureService(serviceName);
                    Console.WriteLine("Service, {0}, is deleted", serviceName);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error during removing VM: {0}", e.ToString());
                }
                
            }            

        }

        [AssemblyCleanup]
        public static void CleanUpAssembly()
        {

            Retry(String.Format("Get-AzureDisk | Where {{$_.DiskName.Contains(\"{0}\")}} | Remove-AzureDisk -DeleteVhd", serviceNamePrefix), "currently in use");
        }

        private static void Retry(string cmdlet, string message, int maxTry = 20, int intervalSecond = 10)
        {            

            ServiceManagementCmdletTestHelper pscmdlet = new ServiceManagementCmdletTestHelper();

            for (int i = 0; i < maxTry; i++)
            {
                try
                {
                    pscmdlet.RunPSScript(cmdlet);
                    break;
                }
                catch (Exception e)
                {
                    if (i == maxTry)
                    {
                        Console.WriteLine("Max try reached.  Couldn't perform within the given time.");
                    }
                    if (e.ToString().Contains(message))
                    {
                        Thread.Sleep(intervalSecond * 1000);
                        continue;
                    }
                    else
                    {
                        throw;
                    }
                }
            }     
        }
    }
}
