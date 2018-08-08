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
        private const string AddressPrefix = "http://localhost:5000/";


        [Test]
        public void TestGetAuthorizationResponse()
        {
            // TODO(audace): Write tests for Getting Authorization Response;
        }

        /// <summary>
        /// Tests the helper method that evaluates request query params with respect to defined policies to determine
        /// whether the request is valid.
        /// Check OAuth2Server.UriContainsValidQueryParams(Uri) for policies that define a valid request.
        /// </summary>
        [Test]
        public void TestUriContainsValidParams()
        {
            const string PolicyNumberOneText =
                "Uri query must include exactly one of either \"code\" or \"error\" as keys.";

            var validUriWithCode = new Uri(string.Format("{0}?code=someValue", AddressPrefix));
            Assert.IsTrue(OAuth2Server.UriContainsValidQueryParams(validUriWithCode),
                PolicyNumberOneText);
            var uriWithError = new Uri(string.Format("{0}?error=someValue", AddressPrefix));
            Assert.IsTrue(OAuth2Server.UriContainsValidQueryParams(uriWithError),
                "query with just \"error\" as param key should be valid.");


            const string PolicyNumberTwoText = "\"code\" and \"error\" cannot be present at the same time.";
            var invalidUriWithCodeAndError =
                new Uri(string.Format("{0}?code=codeValue&error=errorValue", AddressPrefix));
            Assert.IsFalse(OAuth2Server.UriContainsValidQueryParams(invalidUriWithCodeAndError),
                PolicyNumberTwoText);

            const string PolicyNumberThreeText =
                "No other keys apart from \"code\" and \"error\" are allowed.";
            var invalidUriWithOtherKeys =
                new Uri(string.Format("{0}?code=codeValue&otherKey=someValue", AddressPrefix));
            Assert.IsFalse(OAuth2Server.UriContainsValidQueryParams(invalidUriWithOtherKeys), PolicyNumberThreeText);
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