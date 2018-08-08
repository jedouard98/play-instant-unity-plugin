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

namespace GooglePlayInstant.Tests.Editor.QuickDeploy
{
    /// <summary>
    /// Contains helper methods for HttpRequestHelperTest class.
    /// </summary>
    public static class HttpRequestHelperTestHelper
    {
        public delegate string KeyToValueMapper(string key);

        /// <summary>
        /// Generate a dictionary from given keys and mapper function.
        /// </summary>
        /// <param name="keys">An array of keys to be included in the dictionary.</param>
        /// <param name="keyToValueMapper">A function to be applied to the keys to generate values from a new dictionary.</param>
        internal static Dictionary<string, string> GetKeyValueDict(string[] keys, KeyToValueMapper keyToValueMapper)
        {
            var getParams = new Dictionary<string, string>();
            foreach (var key in keys)
            {
                getParams.Add(key, keyToValueMapper(key));
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
    }
}