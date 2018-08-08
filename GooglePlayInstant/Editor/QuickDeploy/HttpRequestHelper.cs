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
using System.Net;
using System.Runtime.CompilerServices;
using UnityEngine;

[assembly: InternalsVisibleTo("GooglePlayInstant.Tests.Editor.QuickDeploy")]

namespace GooglePlayInstant.Editor.QuickDeploy
{
    /// <summary>
    /// A class with utility methods for sending GET and POST requests. Uses WWW and WWWForm instances to send HTTP
    /// requests for backward compatibility with older versions of Unity.
    /// </summary>
    public static class HttpRequestHelper
    {
        /// <summary>
        /// Sends a general POST request to the provided endpoint, along with the data provided in the byte-array and
        /// header parameters.
        /// </summary>
        /// <param name="endpoint">Endpoint to which the data should be sent.</param>
        /// <param name="postData">An array of bytes representing the data that will be passed in the POST request
        /// body.</param>
        /// <param name="postHeaders">A collection of key-value pairs to be added to the request headers.</param>
        /// <returns>A reference to the WWW instance representing the request in progress.</returns>
        public static WWW SendHttpPostRequest(string endpoint, byte[] postData,
            Dictionary<string, string> postHeaders)
        {
            var form = new WWWForm();
            var newHeaders = GetCombinedDictionary(form.headers, postHeaders);
            return new WWW(endpoint, postData, newHeaders);
        }
        
        /// <summary>
        /// Sends a general GET request to the specified endpoint along with specified parameters and headers.
        /// </summary>
        /// <param name="endpoint">The endpoint where the GET request should be sent. Must have no query params</param>
        /// <param name="getParams">A collection of key-value pairs to be attached to the endpoint as GET
        /// parameters.</param>
        /// <param name="getHeaders">A collection of key-value pairs to be added to the request headers.</param>
        /// <returns>A reference to the WWW instance representing the request.</returns>
        public static WWW SendHttpGetRequest(string endpoint, Dictionary<string, string> getParams,
            Dictionary<string, string> getHeaders)
        {
            return new WWW(GetEndpointWithGetParams(endpoint, getParams), null,
                GetCombinedDictionary(new WWWForm().headers, getHeaders));
        }
        
        /// <summary>
        /// Combines endpoint with GET params and returns the result.
        /// </summary>
        internal static string GetEndpointWithGetParams(string endpoint, Dictionary<string, string> getParams)
        {
            var uriBuilder = new UriBuilder(endpoint);
            if (getParams != null)
            {
                uriBuilder.Query = string.Join("&",
                    getParams.Select(kvp => string.Format("{0}={1}", WWW.EscapeURL(kvp.Key), WWW.EscapeURL(kvp.Value)))
                        .ToArray());
            }

            return uriBuilder.ToString();
        }

        /// <summary>
        /// Combine two dictionaries into a single dictionary. Values in the second argument override values of the
        /// first argument for every key that is present to both dictionaries.
        /// </summary>
        internal static Dictionary<string, string> GetCombinedDictionary(Dictionary<string, string> firstDict,
            Dictionary<string, string> secondDict)
        {
            var combinedDict = new Dictionary<string, string>();
            if (firstDict != null)
            {
                foreach (var pair in firstDict)
                {
                    combinedDict[pair.Key] = pair.Value;
                }
            }

            if (secondDict != null)
            {
                foreach (var pair in secondDict)
                {
                    combinedDict[pair.Key] = pair.Value;
                }
            }

            return combinedDict;
        }
    }
}