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
            var www = new WWW(endpoint, postData, form.headers);
            return www;
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
            var endPointbuilder = new UriBuilder(endpoint);
            if (getParams != null)
            {
                endPointbuilder.Query = string.Join("&",
                    getParams.Select(kvp => string.Format("{0}={1}", WWW.EscapeURL(kvp.Key), WWW.EscapeURL(kvp.Value)))
                        .ToArray());
            }

            var form = new WWWForm();
            AddHeadersToWwwForm(form, getHeaders);
            var www = new WWW(endPointbuilder.ToString(), null, form.headers);
            return www;
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

    /// <summary>
    /// Provides functionality for tracking and visualizing useful information about WWW requests in progress.
    /// </summary>
    public class WwwRequestInProgress
    {
        // Thread Safety Note: Operations on this class are NOT threadsafe because they are are expected to be
        // run on the main thread.

        private static readonly List<WwwRequestInProgress> _requestsInProgress = new List<WwwRequestInProgress>();
        private WWW _www;
        private string _title;
        private string _info;

        /// <summary>
        /// Instantiate an instance of a RequestInProgress class.
        /// </summary>
        /// <param name="www">An instance of the WWW object representing the HTTP request being made.</param>
        /// <param name="title">The high level action of the request. This is displayed as the title when displaying
        ///     the progress bar for this request in progress.</param>
        /// <param name="info">A description of what this request is doing. This is displayed in the body when
        /// displaying the progress bar for this request in progress.</param>
        public WwwRequestInProgress(WWW www, string title, string info)
        {
            _www = www;
            _title = title;
            _info = info;
            _requestsInProgress.Add(this);
        }

        /// <summary>
        /// Dispose completed requests and clear them from the class-global collection of requests in progress.
        /// </summary>
        public static void Update()
        {
            // First put done requests in another collection before removing them from the list in order to avoid
            // concurrent modification exceptions.
            var doneRequests = new List<WwwRequestInProgress>();
            foreach (var requestInProgress in _requestsInProgress)
            {
                if (requestInProgress._www.isDone)
                {
                    doneRequests.Add(requestInProgress);
                }
            }

            foreach (var doneRequest in doneRequests)
            {
                doneRequest._www.Dispose();
                _requestsInProgress.Remove(doneRequest);
            }
        }

        /// <summary>
        /// Display the progress bar for the contained request along with information about this request in progress.
        /// </summary>
        public void DisplayProgress()
        {
            EditorUtility.DisplayProgressBar(_title, _info, _www.progress);
        }

        /// <summary>
        /// Display a progress bar and request information for each of the class-wide contained requests in progress.
        /// </summary>
        public static void DisplayProgressForAllRequests()
        {
            foreach (var requestInProgress in _requestsInProgress)
            {
                requestInProgress.DisplayProgress();
            }
        }
    }
}