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
using System.IO;
using System.Linq;
using System.Text;
using GooglePlayInstant.Editor.QuickDeploy;
using NUnit.Framework;
using UnityEngine;

namespace GooglePlayInstant.Tests.Editor.QuickDeploy
{
    /// <summary>
    /// Tests for HttpRequestHelper class methods.
    /// </summary>
    [TestFixture]
    public class HttpRequestHelperTest
    {
        /*
         * Testing strategy:
         *     - Test helper methods that manipulate data that is fed into instantiation of WWW instances.
         *     - Exclude methods that instantiate the WWW class from unit tests to avoid sending HTTP requests.
         */

        [Test]
        public void TestGetWwwForm()
        {
            var postParams =
                HttpRequestHelperTestHelper.GetKeyValueDict(new[] {"a", "b", "c", "d"}, key => string.Concat(key, key));
            var form = HttpRequestHelper.GetWwwForm(postParams);
            Assert.AreEqual(postParams,
                HttpRequestHelperTestHelper.GetDictFromUrlQuery(string.Format("?{0}",
                    Encoding.UTF8.GetString(form.data))));
        }

        [Test]
        public void TestGetEndpointWithGetParams()
        {
            var getParams =
                HttpRequestHelperTestHelper.GetKeyValueDict(new[] {"a", "b", "c", "d"}, key => string.Concat(key, key));
            var endPointWithQuery = HttpRequestHelper.GetEndpointWithGetParams("http://localhost:5000", getParams);
            Assert.AreEqual(getParams,
                HttpRequestHelperTestHelper.GetDictFromUrlQuery(new Uri(endPointWithQuery).Query));
        }


        [Test]
        public void TestGetCombinedDictionary()
        {
            // Case 1: Dictionaries have mutually exclusive sets of keys.
            var firstDict =
                HttpRequestHelperTestHelper.GetKeyValueDict(new[] {"a", "b", "c", "d"}, key => key.ToUpper());
            var secondDict =
                HttpRequestHelperTestHelper.GetKeyValueDict(new[] {"A", "B", "C", "D"}, key => key.ToLower());
            var thirdDict = HttpRequestHelperTestHelper.GetKeyValueDict(new[] {"a", "b", "C", "D"}, key => key);

            var firstAndSecondDict = HttpRequestHelper.GetCombinedDictionary(secondDict, firstDict);
            Assert.AreEqual(8, firstAndSecondDict.Count);
            Assert.AreEqual(secondDict.Union(firstDict).ToDictionary(s => s.Key, s => s.Value), firstAndSecondDict);

            // Case 2: Dictionaries share some keys. Keys in the second dictionary override keys in the first.

            var firstAndThirdDict = HttpRequestHelper.GetCombinedDictionary(firstDict, thirdDict);
            Assert.AreEqual(6, firstAndThirdDict.Count);

            // Values for keys "a" and "b" should be from thirdDict.
            foreach (var key in new[] {"a", "b"})
            {
                Assert.AreEqual(thirdDict[key], firstAndThirdDict[key]);
            }

            // Values for keys "c" and "d" should be from firstDict.
            foreach (var key in new[] {"c", "d"})
            {
                Assert.AreEqual(firstDict[key], firstAndThirdDict[key]);
            }

            // Values for keys "C" and "D" should be from thirdDict.
            foreach (var key in new[] {"C", "D"})
            {
                Assert.AreEqual(thirdDict[key], firstAndThirdDict[key]);
            }
        }
    }
}