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
using System.Runtime.CompilerServices;
using UnityEngine;

[assembly: InternalsVisibleTo("GooglePlayInstant.Tests.Editor.QuickDeploy")]

namespace GooglePlayInstant.Editor
{
    /// <summary>
    /// A Class with utility methods for sending GET and POST requests
    /// </summary>
    public static class QuickDeployWwwRequestHandler
    {
        /// <summary>
        /// Sends a general POST request to the provided endpoint, along with the data provided in the byte array and header
        /// parameters
        /// </summary>
        /// <param name="endpoint">Endpoint to which the data should be sent </param>
        /// <param name="postData">An array of bytes representing the data that will be passed in the POST request body</param>
        /// <param name="postHeaders"> A dictionary representing a set of key-value pairs to be added to the request headers</param>
        /// <returns>A reference to the WWW instance representing the request in progress.</returns>
        public static WWW SendHttpPostRequest(string endpoint, byte[] postData,
            Dictionary<string, string> postHeaders)
        {
            var form = new WWWForm();
            if (postHeaders != null)
            {
                foreach (var pair in postHeaders)
                {
                    form.headers.Add(pair.Key, pair.Value);
                }
            }

            var www = new WWW(endpoint, postData, form.headers);
            return www;
        }

        /// <summary>
        /// Sends a general POST request to the provided endpoint, along with the data provided in the form and headers.
        /// parameters
        /// </summary>
        /// <param name="endpoint">Endpoint to which the data should be sent </param>
        /// <param name="postForm">A dictionary representing a set of key-value paris to be added to the request body</param>
        /// <param name="postHeaders"> A dictionary representing a set of key-value pairs to be added to the request headers</param>
        /// <returns>A reference to the WWW instance representing the request in progress.</returns>
        public static WWW SendHttpPostRequest(string endpoint, Dictionary<string, string> postForm,
            Dictionary<string, string> postHeaders)
        {
            var form = new WWWForm();
            if (postForm != null)
            {
                foreach (var pair in postForm)
                {
                    form.AddField(pair.Key, pair.Value);
                }
            }

            return SendHttpPostRequest(endpoint, postForm != null ? form.data : null, postHeaders);
        }

        /// <summary>
        /// Sends a general GET request to the specified endpoint along with specified parameters and headers.
        /// </summary>
        /// <param name="endpoint">The endpoint to with the get request should be sent</param>
        /// <param name="getParams"> A dictionary representing ke-value pairs that should be attached to the endpoint as GET parameters</param>
        /// <param name="getHeaders">A dictionary representing key-value pairs that constitude the data to be added to the request headers</param>
        /// <returns>A reference to the WWW instance representing the request in progress.</returns>
        public static WWW SendHttpGetRequest(string endpoint, Dictionary<string, string> getParams,
            Dictionary<string, string> getHeaders)
        {
            var fullEndpoint = endpoint + "?";
            var form = new WWWForm();
            if (getHeaders != null)
            {
                foreach (var pair in getHeaders)
                {
                    form.headers.Add(pair.Key, pair.Value);
                }
            }

            if (getParams != null)
            {
                foreach (var pair in getParams)
                {
                    fullEndpoint += pair.Key + "=" + pair.Value + "&";
                }
            }

            var www = new WWW(fullEndpoint, null, form.headers);
            return www;
        }
    }
}