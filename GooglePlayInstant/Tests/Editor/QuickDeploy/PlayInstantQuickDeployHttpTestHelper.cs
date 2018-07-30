using System.Collections.Generic;
using System.Linq;

namespace GooglePlayInstant.Tests.Editor.QuickDeploy
{
    /// <summary>
    /// Contains helper methods for Quick Deploy http helper tests.
    /// </summary>
    public static class QuickDeployHttpTestHelper
    {
        /// <summary>
        /// Get a dictionary with a given number of key-value pairs.
        /// </summary>
        /// <param name="pairsNumber">The number of key-value pairs that the returned dictionary should contain.</param>
        public static Dictionary<string, string> GetKeyValueDict(int pairsNumber)
        {
            var getParams = new Dictionary<string, string>();
            for (var x = 0; x < pairsNumber; x++)
            {
                getParams.Add(string.Format("key{0}", x.ToString()), string.Format("value{0}", x.ToString()));
            }

            return getParams;
        }

        /// <summary>
        /// Get a dictionary from a Url query string.
        /// </summary>
        /// <param name="query">The query string containing key-value params. Must start with "?"</param>
        /// <returns></returns>
        public static Dictionary<string, string> GetDictFromUrlQuery(string query)
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
        /// <param name="parameters">A dictionary corresponding to key-value params that the url query should contain.
        /// </param>
        /// <returns>A url query string starting with "?" generated from the given dictionary.
        /// Example: "?key1=value1&key2=value2</returns>
        public static string GetUrlQueryFromDict(Dictionary<string, string> paramsDict)
        {
            return "?" + string.Join("&",
                       paramsDict.Select(kvp => string.Format("{0}={1}", kvp.Key, kvp.Value)).ToArray());
        }
        
        public static bool DictsAreEqual(Dictionary<string, string> dict1, Dictionary<string, string> dict2)
        {
            return dict1.Count == dict2.Count && !dict1.Except(dict2).Any();
        }
    }
}