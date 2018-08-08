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

using System.Collections.Generic;
using System.Linq;
using UnityEngine.SocialPlatforms;

namespace GooglePlayInstant.Tests.Editor.QuickDeploy
{
    /// <summary>
    /// Contains helper methods for Quick Deploy http helper tests.
    /// </summary>
    public static class HttpRequestHelperTestHelper
    {
        /// <summary>
        /// Get a dictionary with a given number of key-value pairs.
        /// </summary>
        /// <param name="pairsNumber">The number of key-value pairs that the returned dictionary should contain.</param>
        internal static Dictionary<string, string> GetKeyValueDict(int keyStart, int valueStart, int count)
        {
            var getParams = new Dictionary<string, string>();
            for (var x = 0; x < count; x++)
            {
                getParams.Add(string.Format("key{0}", keyStart + x), string.Format("value{0}", valueStart + x));
            }

            return getParams;
        }

        /// <summary>
        /// Get a dictionary from a Url query string.
        /// </summary>
        /// <param name="query">The query string containing key-value params. Must start with "?"</param>
        /// <returns></returns>
        internal static Dictionary<string, string> GetDictFromUrlQuery(string query)
        {
            var getParams = new Dictionary<string, string>();
            foreach (var s in query.Substring(1).Split('&'))
            {
                var kvp = s.Split('=');
                getParams.Add(kvp[0], kvp[1]);
            }

            return getParams;
        }

        /// <summary>
        /// Get a url query from a dictionary.
        /// </summary>
        /// <param name="paramsDict">A dictionary corresponding to key-value params that the url query should contain.
        /// </param>
        /// <returns>A url query string starting with "?" generated from the given dictionary.
        /// Example: "?key1=value1&key2=value2</returns>
        internal static string GetUrlQueryFromDict(Dictionary<string, string> paramsDict)
        {
            return "?" + string.Join("&",
                       paramsDict.Select(kvp => string.Format("{0}={1}", kvp.Key, kvp.Value)).ToArray());
        }
    }
}