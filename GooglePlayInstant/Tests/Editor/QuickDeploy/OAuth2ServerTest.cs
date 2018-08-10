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
using System.IO;
using System.Linq;
using System.Threading;
using GooglePlayInstant.Editor.QuickDeploy;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GooglePlayInstant.Tests.Editor.QuickDeploy
{
    /// <summary>
    /// Contains unit tests for OAuth2Server methods.
    /// </summary>
    [TestFixture]
    public class OAuth2ServerTest
    {
        // Testing strategy:
        //     - Test methods that do not require starting the server.
        //     - TODO: Implement E2E tests to ensure that the server handles requests as expected.

        private const string AddressPrefix = "http://localhost:5000/";

        [Test]
        public void TestGetAuthorizationResponse()
        {
            var someString = Path.GetRandomFileName();
            // response is code
            var codeResponse =
                OAuth2Server.GetAuthorizationResponse(new Uri(string.Format("{0}?code={1}", AddressPrefix,
                    someString)));
            Assert.AreEqual(new KeyValuePair<string, string>("code", someString), codeResponse,
                "Expected valid code response");

            // response is error
            var errorResponse =
                OAuth2Server.GetAuthorizationResponse(new Uri(string.Format("{0}?error={1}", AddressPrefix,
                    someString)));
            Assert.AreEqual(new KeyValuePair<string, string>("error", someString), errorResponse,
                "Expected invalid error response");

            // response has invalid keys
            var invalidResponse = new Uri(string.Format("{0}?someKey=someValue", AddressPrefix));
            Assert.Throws<ArgumentException>(() => OAuth2Server.GetAuthorizationResponse(invalidResponse),
                "Expected ArgumentException");
            LogAssert.Expect(LogType.Error, OAuth2Server.InvalidQueryExceptionMessage);

            // No response. Uri has no params
            Assert.Throws<ArgumentException>(() => OAuth2Server.GetAuthorizationResponse(new Uri(AddressPrefix)),
                "Expected ARgumentException");
            LogAssert.Expect(LogType.Error, OAuth2Server.InvalidQueryExceptionMessage);
        }

        [Test]
        public void TestUriContainsValidParams()
        {
            const string codeOrErrorKeyMustBePresentText =
                "Uri query must include either \"code\" or \"error\" as keys.";

            var validUriWithCode = new Uri(string.Format("{0}?code=someValue", AddressPrefix));
            Assert.IsTrue(OAuth2Server.UriContainsValidQueryParams(validUriWithCode),
                codeOrErrorKeyMustBePresentText);
            var uriWithError = new Uri(string.Format("{0}?error=someValue", AddressPrefix));
            Assert.IsTrue(OAuth2Server.UriContainsValidQueryParams(uriWithError),
                codeOrErrorKeyMustBePresentText);

            var invalidUriWithCodeAndError =
                new Uri(string.Format("{0}?code=codeValue&error=errorValue", AddressPrefix));
            Assert.IsFalse(OAuth2Server.UriContainsValidQueryParams(invalidUriWithCodeAndError),
                "\"code\" and \"error\" cannot be present at the same time.");

            var invalidUriWithOtherKeys =
                new Uri(string.Format("{0}?code=codeValue&otherKey=someValue", AddressPrefix));
            Assert.IsFalse(OAuth2Server.UriContainsValidQueryParams(invalidUriWithOtherKeys),
                "No other keys apart from \"code\" and \"error\" are allowed.");

            var invalidUriWithEmptyQuery = new Uri(AddressPrefix);
            Assert.IsFalse(OAuth2Server.UriContainsValidQueryParams(invalidUriWithEmptyQuery),
                "Uri with empty query should be invalid");
        }

        [Test]
        public void TestGetQueryParamsFromUri()
        {
            // Case 1: Non-empty query
            var testDictionary = new Dictionary<string, string> {{"a", "b"}, {"c", "d"}};
            var uri = new Uri(AddressPrefix + "?a=b&c=d");
            Assert.AreEqual(testDictionary, OAuth2Server.GetQueryParamsFromUri(uri),
                "Should return expected correct query params");
            // Case 2: Empty query
            Assert.IsEmpty(OAuth2Server.GetQueryParamsFromUri(new Uri(AddressPrefix)),
                "Expected empty dictionary from empty query");
        }
    }
}