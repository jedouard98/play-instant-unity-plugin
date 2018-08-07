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
    /// Provides functionality for tracking HTTP requests represented by corresponding WWW instances, as well as
    /// executing scheduled actions on the responses when the requests are complete.
    /// </summary>
    public class WwwRequestInProgress
    {
        private readonly WWW _www;
        private readonly string _progressBarTitleText;
        private readonly Action<WWW> _onDone;

        // Only one request can be performed at a time
        private static WwwRequestInProgress _requestInProgress;


        /// <summary>
        /// Monitor an HTTP request being made. Assumes one request will be made at a time, therefore will provide
        /// a warning message and stop monitoring a previous request if one was still being monitored.
        /// </summary>
        /// <param name="www">A www instance holding a request that was made</param>
        /// <param name="progressBarTitleText">A descriptive text to display as the title of the progress bar.</param>
        /// <param name="onDone">An action to be invoked on the result once it is available.</param>
        public static void TrackProgress(WWW www, string progressBarTitleText, Action<WWW> onDone)
        {
            if (_requestInProgress != null)
            {
                throw new Exception("Cannot start a another request while the previous one is not complete.");
            }

            _requestInProgress = new WwwRequestInProgress(www, progressBarTitleText, onDone);
        }

        /// <summary>
        /// Create an instance of the RequestInProgress class.
        /// </summary>
        /// <param name="www">A www instance holding a request that was made</param>
        /// <param name="progressBarTitleText">A descriptive text to display as the title of the progress bar.</param>
        /// <param name="onDone">An action to be invoked on the result once it is available.</param>
        private WwwRequestInProgress(WWW www, string progressBarTitleText, Action<WWW> onDone)
        {
            _www = www;
            _progressBarTitleText = progressBarTitleText;
            _onDone = onDone;
        }


        //TODO(audace): Include canceling the request when the user clicks the cancel button in the documentation.
        /// <summary>
        /// Verifies the state of the currently monitored request in progress. Displays progress bar for the request if
        /// it is still going on. If the request is done, this will invoke post completion action on the result and
        /// will stop monitoring the request.
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
                // Set the static field _requestInProgress to null before invoking the  post-completion action
                // because the action could be executing an action that will read or overwrite the static field.
                _requestInProgress = null;
                requestInProgress._onDone(requestInProgress._www);
            }
            else
            {
                if (EditorUtility.DisplayCancelableProgressBar(_requestInProgress._progressBarTitleText,
                    string.Format("Progress: {0}%", Math.Floor(_requestInProgress._www.uploadProgress * 100)),
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