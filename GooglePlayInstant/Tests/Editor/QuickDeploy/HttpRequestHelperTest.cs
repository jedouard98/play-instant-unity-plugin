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
using System.Net;
using System.Text;
using System.Threading;
using GooglePlayInstant.Editor.QuickDeploy;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using Random = System.Random;

namespace GooglePlayInstant.Tests.Editor.QuickDeploy
{
    /// <summary>
    /// Tests for Http Request Helper methods.
    /// </summary>
    [TestFixture]
    public class HttpRequestHelperTest
    {
        /*
         * Testing strategy:
         *     - Use a local server to inspect contents of the requests sent.
         *     - Partition inputs as follows:
         *         - Method tested is SendHttpGetRequest, SendHttpPostRequest, other helper methods.
         *         - Contents of requests are query params, form contents, bytes, headers.
         */


        [Test]
        public void TestGetWwwForm()
        {
            var postParams = HttpRequestHelperTestHelper.GetKeyValueDict(0, 0, 10);
            var form = HttpRequestHelper.GetWwwForm(postParams);
            var formString = Encoding.UTF8.GetString(form.data);
            Assert.AreEqual(postParams,
                HttpRequestHelperTestHelper.GetDictFromUrlQuery(string.Format("?{0}", formString)));
        }

        [Test]
        public void TestGetEndpointWithGetParams()
        {
            var getParams = HttpRequestHelperTestHelper.GetKeyValueDict(0, 0, 10);
            var endPointWithQuery = HttpRequestHelper.GetEndpointWithGetParams("http://localhost:5000", getParams);
            Assert.AreEqual(getParams,
                HttpRequestHelperTestHelper.GetDictFromUrlQuery(new Uri(endPointWithQuery).Query));
        }


        [Test]
        public void TestGetCombinedDictionary()
        {
            var form = new WWWForm();
            var newHeaders = HttpRequestHelperTestHelper.GetKeyValueDict(0, 0, 10);
            var combinedDictionary = HttpRequestHelper.GetCombinedDictionary(form.headers, newHeaders);
            Assert.AreEqual(form.headers.Union(newHeaders).ToDictionary(s => s.Key, s => s.Value), combinedDictionary);
        }
    }
}