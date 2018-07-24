// Copyright 2018 Google LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections;
using System.Collections.Generic;
using GooglePlayInstant.Editor;
using NUnit.Framework;

namespace GooglePlayInstant.Tests.Editor.QuickDeploy
{
    [TestFixture]
    public class Oauth2CallbackEndPointServerTest
    {
        [Test]
        public IEnumerator TestRandomPortGenerator()
        {
            var portString = QuickDeployOAuth2CallbackEndpointServer.GetRandomPortAsString();
            int n;
            Assert.True(int.TryParse(portString, out n), "Expected a port to be an int");
            yield return null;
        }

        [Test]
        public void TestEndPointGenerator()
        {
            var endPointString = QuickDeployOAuth2CallbackEndpointServer.GetRandomEndpoint();
            Assert.AreEqual(32, QuickDeployOAuth2CallbackEndpointServer.GetMD5Hash(endPointString).Length, 0.0,
                "Should return a 32 long string");
        }

        [Test]
        public void TestServerBehavesCorrectlyCodeResponses()
        {
            var server = new QuickDeployOAuth2CallbackEndpointServer();
            server.Start();
            Assert.True(server.IsListening());
            var validResponse = QuickDeployWwwRequestHandler.SendHttpGetRequest(
                server.CallbackEndpoint + "?code" + QuickDeployOAuth2CallbackEndpointServer.GetMD5Hash("test"), null,
                null);
            Assert.AreEqual(QuickDeployOAuth2CallbackEndpointServer.CallbackEndpointResponseOnSuccess, validResponse);
            Assert.True(server.HasOauth2AuthorizationResponse());

            var actualResponse = server.getAuthorizationResponse();
            var expectedResponse =
                new KeyValuePair<string, string>("code", QuickDeployOAuth2CallbackEndpointServer.GetMD5Hash("test"));
            Assert.True(string.Equals(expectedResponse.Key, actualResponse.Key) &&
                        string.Equals(expectedResponse.Value, actualResponse.Value));
        }

        [Test]
        public void TestServerBehavesCorrectlyOnErrorResponses()
        {
            var server = new QuickDeployOAuth2CallbackEndpointServer();
            server.Start();
            Assert.True(server.IsListening());
            var validResponse = QuickDeployWwwRequestHandler.SendHttpGetRequest(
                server.CallbackEndpoint + "?error" + QuickDeployOAuth2CallbackEndpointServer.GetMD5Hash("test"), null,
                null);
            Assert.AreEqual(QuickDeployOAuth2CallbackEndpointServer.CallBackEndpointResponseOnError, validResponse);
            Assert.True(server.HasOauth2AuthorizationResponse());

            var actualResponse = server.getAuthorizationResponse();
            var expectedResponse =
                new KeyValuePair<string, string>("error", QuickDeployOAuth2CallbackEndpointServer.GetMD5Hash("test"));
            Assert.True(string.Equals(expectedResponse.Key, actualResponse.Key) &&
                        string.Equals(expectedResponse.Value, actualResponse.Value));
        }
    }
}