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
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

[assembly: InternalsVisibleTo("GooglePlayInstant.Tests.Editor.QuickDeploy")]

namespace GooglePlayInstant.Editor
{
    /// <summary>
    /// Provides functionality for tracking and visualizing useful information about WWW requests in progress.
    /// </summary>
    public class WwwRequestInProgress
    {
        private readonly WWW _www;
        private readonly string _progressBarTitleText;


        /// <summary>
        /// A method to be executed on the _www field when it is done.
        /// </summary>
        public delegate void DoneWwwHandler(WWW www);

        private DoneWwwHandler _onDone;

        // Only one request can be performed at a time
        private static WwwRequestInProgress _requestInProgress;


        public static void TrackProgress(WWW www, string progressBarTitleText, DoneWwwHandler onDone)
        {
            if (_requestInProgress != null)
            {
                Debug.LogWarning("Started another request while the previous one was not complete.");
            }

            _requestInProgress = new WwwRequestInProgress(www, progressBarTitleText, onDone);
        }

        /// <summary>
        /// Instantiate an instance of a RequestInProgress class.
        /// </summary>
        /// <param name="www">An instance of the WWW object representing the HTTP request being made.</param>
        /// <param name="progressBarTitleText">The high level action of the request. This is displayed as the title when displaying
        ///     the progress bar for this request in progress.</param>
        /// <param name="onDone">A handler for the www instance when the result is available.</param>
        private WwwRequestInProgress(WWW www, string progressBarTitleText, DoneWwwHandler onDone)
        {
            _www = www;
            _progressBarTitleText = progressBarTitleText;
            _onDone = onDone;
        }


        /// <summary>
        /// Clear done requests from the pipeline of requests in progress, and execute scheduled tasks for done requests
        /// that are still in the pipeline.
        /// </summary>
        public static void Update()
        {
            if (_requestInProgress == null)
            {
                return;
            }

            if (_requestInProgress._www.isDone)
            {
                EditorUtility.ClearProgressBar();
                var requestInProgress = _requestInProgress;
                _requestInProgress = null;
                requestInProgress._onDone.Invoke(requestInProgress._www);
            }
            else
            {
                if (EditorUtility.DisplayCancelableProgressBar(_requestInProgress._progressBarTitleText,
                    "Progress: " + Math.Floor(_requestInProgress._www.uploadProgress * 100) + "%",
                    _requestInProgress._www.uploadProgress))
                {
                    EditorUtility.ClearProgressBar();
                    _requestInProgress._www.Dispose();
                    _requestInProgress = null;
                }
            }
        }
    }
}