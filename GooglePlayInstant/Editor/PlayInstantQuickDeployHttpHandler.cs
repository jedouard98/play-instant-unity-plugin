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
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

[assembly: InternalsVisibleTo("GooglePlayInstant.Tests.Editor.QuickDeploy")]

namespace GooglePlayInstant.Editor
{
    /// <summary>
    /// A class with utility methods for sending GET and POST requests.
    /// </summary>
    public static class QuickDeployWwwRequestHandler
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
            AddHeadersToWwwForm(form, postHeaders);
            return new WWW(endpoint, postData, form.headers);
        }

        /// <summary>
        /// Send a general POST request to the provided endpoint, along with the data provided in the form and headers.
        /// parameters
        /// </summary>
        /// <param name="endpoint">Endpoint to which the data should be sent.</param>
        /// <param name="postForm">A set of key-value pairs to be added to the request body.</param>
        /// <param name="postHeaders"> A set of key-value pairs to be added to the request headers.</param>
        /// <returns>A reference to the WWW instance representing the request.</returns>
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
        /// <param name="endpoint">The endpoint where the GET request should be sent. Must have no query params</param>
        /// <param name="getParams">A collection of key-value pairs to be attached to the endpoint as GET
        /// parameters.</param>
        /// <param name="getHeaders">A collection of key-value pairs to be added to the request headers.</param>
        /// <returns>A reference to the WWW instance representing the request.</returns>
        public static WWW SendHttpGetRequest(string endpoint, Dictionary<string, string> getParams,
            Dictionary<string, string> getHeaders)
        {
            var uriBuilder = new UriBuilder(endpoint);
            if (getParams != null)
            {
                uriBuilder.Query = string.Join("&",
                    getParams.Select(kvp => string.Format("{0}={1}", WWW.EscapeURL(kvp.Key), WWW.EscapeURL(kvp.Value)))
                        .ToArray());
            }

            var form = new WWWForm();
            AddHeadersToWwwForm(form, getHeaders);
            return  new WWW(uriBuilder.ToString(), null, form.headers);
        }

        private static void AddHeadersToWwwForm(WWWForm form, Dictionary<string, string> headers)
        {
            if (headers != null)
            {
                foreach (var pair in headers)
                {
                    form.headers.Add(pair.Key, pair.Value);
                }
            }
        }
    }
}