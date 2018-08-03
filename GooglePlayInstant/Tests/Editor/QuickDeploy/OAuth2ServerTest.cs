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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using GooglePlayInstant.Editor.QuickDeploy;
using NUnit.Framework;
using UnityEngine;

namespace GooglePlayInstant.Tests.Editor.QuickDeploy
{
    /// <summary>
    /// A test class for OAuth2Server
    /// </summary>
    [TestFixture]
    public class OAuth2ServerTest
    {
        private const int RequestResponseThresholdInMillis = 50;
        private const string AddressPrefix = "http://localhost:5000/";

        /// <summary>
        /// Tests that the server starts listening for requests right after it is started.
        /// </summary>
        [Test]
        public void TestServerIsListeningAfterStart()
        {
            var server = new OAuth2Server(null);
            server.Start();
            Assert.IsTrue(server._httpListener.IsListening, "Server must be listening after a call to server.Start().");
            server.Stop();
        }

        /// <summary>
        /// Tests that the server stops running and disposes its listener after responding to the first request.
        /// </summary>
        [Test]
        public void TestServerStopsAndListenerIsDisposedAfterFirstRequest()
        {
            var server = new OAuth2Server(null);

            // Test with valid request
            server.Start();
            new WWW(string.Format("{0}?{1}={2}", server.CallbackEndpoint, "code", "codeValue"));
            Thread.Sleep(RequestResponseThresholdInMillis);
            Assert.IsNull(server._httpListener);

            // Test invalid request
            server.Start();
            new WWW(string.Format("{0}?{1}={2}", server.CallbackEndpoint, "notCode", "someValue"));
            Thread.Sleep(RequestResponseThresholdInMillis);
            Assert.IsNull(server._httpListener);
        }

        /// <summary>
        /// Tests that the server provides an non-empty and consistent endpoint after being started.
        /// </summary>
        [Test]
        public void TestServerProvidesCorrectAndConsistentEndpointAfterStart()
        {
            var server = new OAuth2Server(null);
            server.Start();

            var endPointResponses = new List<string>();
            for (var tries = 0; tries < 10; tries++)
            {
                endPointResponses.Add(server.CallbackEndpoint);
            }

            Assert.IsTrue(endPointResponses.TrueForAll(endpoint => !string.IsNullOrEmpty(endpoint)),
                "Server should not have empty endpoint after being started.");
            Assert.IsTrue(endPointResponses.TrueForAll(endpoint => endPointResponses.TrueForAll(endpoint.Equals)),
                "Server should have consistent endpoint after being started.");
        }

        /// <summary>
        /// Tests that the server behaves correctly on valid requests containing code responses. Check
        /// OAuth2Server.UriContainsValidQueryParams(Uri) for policies that define a valid request.
        /// </summary>
        [Test]
        public void TestServerBehavesCorrectlyForValidCodeRequests()
        {
            KeyValuePair<string, string> receivedCode;
            var server = new OAuth2Server(response => { receivedCode = response; });
            server.Start();
            KeyValuePair<string, string> sentCode = new KeyValuePair<string, string>("code", "codeValue");
            var request =
                new WWW(string.Format("{0}?{1}={2}", server.CallbackEndpoint, sentCode.Key, sentCode.Value));
            Thread.Sleep(RequestResponseThresholdInMillis);
            Assert.AreEqual(sentCode, receivedCode, "Expected received code to be the same as sent code.");
            Assert.AreEqual(OAuth2Server.CloseTabScript, request.text,
                "Expected script to close the tab of the browser window.");
        }

        /// <summary>
        /// Tests that the server behaves correctly as well for valid requests that contain error responses. Check
        /// OAuth2Server.UriContainsValidQueryParams(Uri) for policies that define a valid request.
        /// </summary>
        [Test]
        public void TestServerBehavesCorrectlyForValidErrorRequests()
        {
            KeyValuePair<string, string> receivedError;
            var server = new OAuth2Server(response => { receivedError = response; });
            server.Start();
            KeyValuePair<string, string> sentError = new KeyValuePair<string, string>("error", "errorValue");
            var request =
                new WWW(string.Format("{0}?{1}={2}", server.CallbackEndpoint, sentError.Key, sentError.Value));
            Thread.Sleep(RequestResponseThresholdInMillis);
            Assert.AreEqual(sentError, receivedError, "Expected received code to be the same as sent code.");
            Assert.AreEqual(OAuth2Server.CloseTabScript, request.text, "Expected script to close the tab.");
        }

        /// <summary>
        /// Tests that the server behaves as expected by returning a "404 Not Found" error for requests that are
        /// invalid.
        /// </summary>
        [Test]
        public void TestServerBehavesCorrenctlyForInvalidRequests()
        {
            KeyValuePair<string, string> receivedCode = new KeyValuePair<string, string>("key", "value");
            var server = new OAuth2Server(code => { receivedCode = code; });
            server.Start();
            var request = new WWW(string.Format("{0}?{1}={2}", server.CallbackEndpoint, "notCode", "someValue"));
            Thread.Sleep(RequestResponseThresholdInMillis);
            Assert.True(!string.IsNullOrEmpty(request.error) && request.error.StartsWith("404"),
                "Result should come with error 404 error");
            Assert.AreEqual(receivedCode, new KeyValuePair<string, string>("key", "value"),
                "Response handler should not be invoked on invalid requests");
        }

        /// <summary>
        /// Tests the helper method that evaluates request query params with respect to defined policies to determine
        /// whether the request is valid.
        /// Check OAuth2Server.UriContainsValidQueryParams(Uri) for policies that define a valid request.
        /// </summary>
        [Test]
        public void TestUriContainsValidParams()
        {
            const string Policy1 = "Uri query must include exactly one of either \"code\" or \"error\" as keys.";

            var validUriWithCode = new Uri(string.Format("{0}?code=someValue", AddressPrefix));
            Assert.IsTrue(OAuth2Server.UriContainsValidQueryParams(validUriWithCode),
                Policy1);
            var uriWithError = new Uri(string.Format("{0}?error=someValue", AddressPrefix));
            Assert.IsTrue(OAuth2Server.UriContainsValidQueryParams(uriWithError),
                "query with just \"error\" as param key should be valid.");


            const string Policy2 = "\"code\" and \"error\" cannot be present at the same time.";
            var invalidUriWithCodeAndError =
                new Uri(string.Format("{0}?code=codeValue&error=errorValue", AddressPrefix));
            Assert.IsFalse(OAuth2Server.UriContainsValidQueryParams(invalidUriWithCodeAndError),
                Policy2);

            const string Policy3 = "No other keys apart from \"code\", \"error\" and \"scope\" are allowed.";
            var invalidUriWithOtherKeys =
                new Uri(string.Format("{0}?code=codeValue&otherKey=someValue", AddressPrefix));
            Assert.IsFalse(OAuth2Server.UriContainsValidQueryParams(invalidUriWithOtherKeys), Policy3);

            const string Policy4 = "\"scope\" should only be present when there is \"code\"";
            var validUriWithScope = new Uri(string.Format("{0}?code=someValue&scope=someValue", AddressPrefix));
            var invalidUriWithScope = new Uri(string.Format("{0}?scope=someValue", AddressPrefix));
            Assert.IsTrue(OAuth2Server.UriContainsValidQueryParams(validUriWithScope), Policy4);
            Assert.IsFalse(OAuth2Server.UriContainsValidQueryParams(invalidUriWithScope), Policy4);
        }

        /// <summary>
        /// Tests the helper method that reads the uri and returns query params contained in the URI.
        /// </summary>
        [Test]
        public void TestGetQueryParamsFromUri()
        {
            var testDict = new Dictionary<string, string>();
            for (var x = 0; x < 10; x++)
            {
                testDict.Add(string.Format("key{0}", x), string.Format("value{0}", x));
            }

            var query = "?" + string.Join("&", testDict.Select(kvp => kvp.Key + "=" + kvp.Value).ToArray());
            var uri = new Uri(AddressPrefix + query);
            Assert.AreEqual(testDict, OAuth2Server.GetQueryParamsFromUri(uri),
                "should return expected correct query params");
        }
    }
}