using System.Collections.Generic;
using System.Linq;

namespace GooglePlayInstant.Tests.Editor.QuickDeploy
{
    public static class QuickDeployHttpTestHelper
    {
        public static Dictionary<string, string> GetKeyValueDict(int paramsNumber)
        {
            var getParams = new Dictionary<string, string>();
            for (var x = 0; x < paramsNumber; x++)
            {
                getParams.Add(string.Format("key{0}", x.ToString()), string.Format("value{0}", x.ToString()));
            }

            return getParams;
        }

        public static Dictionary<string, string> GetDictFromUriQuery(string query)
        {
            var getParams = new Dictionary<string, string>();
            foreach (var s in query.Substring(1).Split('&'))
            {
                var kvp = s.Split('=');
                getParams.Add(kvp[0], kvp[1]);
            }

            return getParams;
        }

        public static string GetUriQueryFromDict(Dictionary<string, string> parameters)
        {
            return "?" + string.Join("&",
                       parameters.Select(kvp => string.Format("{0}={1}", kvp.Key, kvp.Value)).ToArray());
        }
        
        public static bool DictsAreEqual(Dictionary<string, string> dict1, Dictionary<string, string> dict2)
        {
            return dict1.Count == dict2.Count && !dict1.Except(dict2).Any();
        }
    }
}