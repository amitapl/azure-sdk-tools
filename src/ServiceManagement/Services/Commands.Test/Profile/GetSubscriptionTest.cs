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

using Microsoft.WindowsAzure.Commands.Common.Models;
using Microsoft.WindowsAzure.Commands.Common.Test.Mocks;

namespace Microsoft.WindowsAzure.Commands.Test.Profile
{
    using Commands.Profile;
    using Commands.Utilities.Common;
    using Moq;
    using System.Linq;
    using Utilities.Common;
    using VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class GetSubscriptionTest
    {
        private WindowsAzureProfile profile;
        private MockCommandRuntime mockCommandRuntime;
        private GetAzureSubscriptionCommand cmdlet;

        [TestInitialize]
        public void Setup()
        {
            profile = new WindowsAzureProfile(new Mock<IProfileStore>().Object);
            profile.ImportPublishSettings(Data.ValidPublishSettings.First());

            mockCommandRuntime = new MockCommandRuntime();

            cmdlet = new GetAzureSubscriptionCommand
            {
                Profile = profile,
                CommandRuntime = mockCommandRuntime
            };
        }

        [TestMethod]
        public void CanGetCurrentSubscription()
        {
            // Select a subscription that is not the default
            profile.CurrentSubscription = profile.Subscriptions.First(s => !s.IsDefault);

            cmdlet.GetCurrent();

            Assert.AreEqual(1, mockCommandRuntime.OutputPipeline.Count);
            Assert.AreEqual(profile.CurrentSubscription.SubscriptionName, 
                ((AzureSubscription)mockCommandRuntime.OutputPipeline[0]).Name);
            Assert.AreEqual(profile.CurrentSubscription.SubscriptionId,
                ((AzureSubscription)(mockCommandRuntime.OutputPipeline[0])).Id);
        }

        [TestMethod]
        public void CanGetDefaultSubscription()
        {
            // Select a subscription that is not the default
            profile.CurrentSubscription = profile.Subscriptions.First(s => !s.IsDefault);

            cmdlet.GetDefault();

            Assert.AreEqual(1, mockCommandRuntime.OutputPipeline.Count);
            Assert.AreEqual(profile.DefaultSubscription.SubscriptionName,
                ((AzureSubscription)mockCommandRuntime.OutputPipeline[0]).Name);
            Assert.AreEqual(profile.DefaultSubscription.SubscriptionId,
                ((AzureSubscription)(mockCommandRuntime.OutputPipeline[0])).Id);
            
        }
    }
}