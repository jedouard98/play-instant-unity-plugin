using System;
using System.Collections.Generic;
using System.Threading;
using GooglePlayInstant.Editor;
using NUnit.Framework;
using UnityEngine;

namespace GooglePlayInstant.Tests.Editor.QuickDeploy
{
    [TestFixture]
    public class OAuth2ServerTest
    {
        private const int RequestResponseTimeout = 100;

        [Test]
        public void TestServerIsListeningAfterStart()
        {
            var server = new QuickDeployOAuth2Server(null);
            server.Start();
            Assert.IsTrue(server._httpListener.IsListening, "Server must be listening after a call to server.Start().");
            server.Stop();
        }

        [Test]
        public void TestServerStopsAndListenerIsDisposedAfterFirstRequest()
        {
            var server = new QuickDeployOAuth2Server(null);

            // Test with valid request
            server.Start();
            new WWW(string.Format("{0}?{1}={2}", server.CallbackEndpoint, "code", "codeValue"));
            Thread.Sleep(RequestResponseTimeout);
            Assert.IsNull(server._httpListener);

            // Test invalid request
            server.Start();
            new WWW(string.Format("{0}?{1}={2}", server.CallbackEndpoint, "notCode", "someValue"));
            Thread.Sleep(RequestResponseTimeout);
            Assert.IsNull(server._httpListener);
        }

        [Test]
        public void TestServerProvidesCorrectAndConsistentEndpointAfterStart()
        {
            var server = new QuickDeployOAuth2Server(null);
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

        [Test]
        public void TestServerBehavesCorrectlyForValidCodeRequests()
        {
            KeyValuePair<string, string> receivedCode;
            var server = new QuickDeployOAuth2Server(response => { receivedCode = response; });
            server.Start();
            KeyValuePair<string, string> sentCode = new KeyValuePair<string, string>("code", "codeValue");
            var request =
                new WWW(string.Format("{0}?{1}={2}", server.CallbackEndpoint, sentCode.Key, sentCode.Value));
            Thread.Sleep(RequestResponseTimeout);
            Assert.AreEqual(sentCode, receivedCode, "Expected received code to be the same as sent code.");
            Assert.AreEqual(QuickDeployOAuth2Server.CloseTabScript, request.text, "Expected script to close the tab.");
        }

        [Test]
        public void TestServerBehavesCorrectlyForValidErrorRequests()
        {
            KeyValuePair<string, string> receivedError;
            var server = new QuickDeployOAuth2Server(response => { receivedError = response; });
            server.Start();
            KeyValuePair<string, string> sentError = new KeyValuePair<string, string>("error", "errorValue");
            var request =
                new WWW(string.Format("{0}?{1}={2}", server.CallbackEndpoint, sentError.Key, sentError.Value));
            Thread.Sleep(RequestResponseTimeout);
            Assert.AreEqual(sentError, receivedError, "Expected received code to be the same as sent code.");
            Assert.AreEqual(QuickDeployOAuth2Server.CloseTabScript, request.text, "Expected script to close the tab.");
        }

        [Test]
        public void TestServerBehavesCorrenctlyForInvalidRequests()
        {
            KeyValuePair<string, string> receivedCode = new KeyValuePair<string, string>("key", "value");
            var server = new QuickDeployOAuth2Server(code => { receivedCode = code; });
            server.Start();
            var request = new WWW(string.Format("{0}?{1}={2}", server.CallbackEndpoint, "notCode", "someValue"));
            Thread.Sleep(RequestResponseTimeout);
            Assert.True(!string.IsNullOrEmpty(request.error) && request.error.StartsWith("404"),
                "Result should come with error 404 error");
            Assert.AreEqual(receivedCode, new KeyValuePair<string, string>("key", "value"),
                "Response handler should not be invoked on invalid requests");
        }

        [Test]
        public void TestUriContainsValidParamsMethod()
        {
            const string AddressPrefix = "http://localhost:5000/";
            
            const string Policy1 = "Uri must include exactly one of either \"code\" or \"error\" as keys.";
           
            var validUriWithCode = new Uri(string.Format("{0}?code=someValue", AddressPrefix));
            Assert.IsTrue(QuickDeployOAuth2Server.UriContainsValidQueryParams(validUriWithCode),
                Policy1);
            var uriWithError = new Uri(string.Format("{0}?error=someValue", AddressPrefix));
            Assert.IsTrue(QuickDeployOAuth2Server.UriContainsValidQueryParams(uriWithError),
                "query with just \"error\" as param key should be valid.");
             


            const string Policy2 = "\"code\" and \"error\" cannot be present at the same time.";
            var invalidUriWithCodeAndError = new Uri(string.Format("{0}?code=codeValue&error=errorValue", AddressPrefix));
            Assert.IsFalse(QuickDeployOAuth2Server.UriContainsValidQueryParams(invalidUriWithCodeAndError),
                Policy2);

            const string Policy3 = "No other keys apart from \"code\", \"error\" and \"scope\" are allowed.";
            var invalidUriWithOtherKeys = new Uri(string.Format("{0}?otherKey=someValue", AddressPrefix));
            Assert.IsFalse(QuickDeployOAuth2Server.UriContainsValidQueryParams(invalidUriWithOtherKeys), Policy3);

            const string Policy4 = "\"scope\" should only be present when there is \"code\"";
            var validUriWithScope = new Uri(string.Format("{0}?code=someValue&scope=someValue", AddressPrefix));
            var invalidUriWithScope = new Uri(string.Format("{0}?scope=someValue", AddressPrefix));
            Assert.IsTrue(QuickDeployOAuth2Server.UriContainsValidQueryParams(validUriWithScope), Policy4);
            Assert.IsFalse(QuickDeployOAuth2Server.UriContainsValidQueryParams(invalidUriWithScope), Policy4);
        }
    }
}