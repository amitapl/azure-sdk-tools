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

namespace Microsoft.WindowsAzure.Commands.Websites
{
    using System.Collections.Generic;
    using System.Management.Automation;
    using Microsoft.WindowsAzure.Commands.Utilities.Websites.Services.WebEntities;
    using Microsoft.WindowsAzure.Management.WebSites.Models;
    using Utilities.Websites;
    using Utilities.Websites.Common;
    using Utilities.Websites.Services.DeploymentEntities;

    [Cmdlet(VerbsLifecycle.Enable, "AzureWebsiteDebug"), OutputType(typeof(bool))]
    public class EnableAzureWebsiteDebugCommand : WebsiteContextBaseCmdlet
    {
        private Site website;

        private SiteConfig siteConfig;

        [Parameter(Mandatory = false)]
        public SwitchParameter PassThru { get; set; }

        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The visual studio version.")]
        public RemoteDebuggingVersion Version { get; set; }

        public override void ExecuteCmdlet()
        {
            // Get current config
            website = WebsitesClient.GetWebsite(Name);
            siteConfig = WebsitesClient.GetWebsiteConfiguration(Name);

            // Update the configuration
            if (!siteConfig.RemoteDebuggingEnabled.Value)
            {
                siteConfig.RemoteDebuggingEnabled = true;
                siteConfig.RemoteDebuggingVersion = Version;
                WebsitesClient.UpdateWebsiteConfiguration(Name, siteConfig);
            }

            if (PassThru.IsPresent)
            {
                WriteObject(true);
            }
        }
    }
}
