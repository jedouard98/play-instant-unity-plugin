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
using System.IO;
using System.Linq;
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
        // Thread Safety Note: Operations on this class are NOT threadsafe because they are are expected to be
        // run on the main thread.

        // Use an ordered collection for requests in progress so you can display progress bars in a consinstent order.
        // Shouldn't be readonly because it is mutable.
        private static List<WwwRequestInProgress> _trackedRequestsInProgress = new List<WwwRequestInProgress>();
        private static List<WwwRequestInProgress> _scheduledForOnDone = new List<WwwRequestInProgress>();
        private readonly WWW _www;
        private readonly string _progressBarTitleText;
        private static int _counter = 0;


        /// <summary>
        /// A method to be executed on the _www field when it is done.
        /// </summary>
        public delegate void DoneWwwHandler(WWW www);

        private DoneWwwHandler _onDone = www => { };

        /// <summary>
        /// Instantiate an instance of a RequestInProgress class.
        /// </summary>
        /// <param name="www">An instance of the WWW object representing the HTTP request being made.</param>
        /// <param name="progressBarTitleText">The high level action of the request. This is displayed as the title when displaying
        ///     the progress bar for this request in progress.</param>
        public WwwRequestInProgress(WWW www, string progressBarTitleText)
        {
            _www = www;
            _progressBarTitleText = progressBarTitleText;
        }

        /// <summary>
        /// Add instance to tracked requests in progress. After this call, a call to DisplayProgressForTrackedRequests
        /// will display information for this request if it's not completed.
        /// </summary>
        public void TrackProgress()
        {
            _trackedRequestsInProgress.Add(this);
        }

        /// <summary>
        /// Schedule a task to invoke on the request when the request is done.
        /// </summary>
        public void ScheduleTaskOnDone(DoneWwwHandler wwwHandler)
        {
            _onDone += wwwHandler;


            if (!_scheduledForOnDone.Contains(this))
            {
                _scheduledForOnDone.Add(this);
            }
        }

        // Execute all the scheduled tasks for this instance. Clears all the tasks after executing them
        private void ExecuteScheduledTasks()
        {
            if (!_www.isDone)
            {
                throw new Exception("Request has not yet completed");
            }

            _onDone.Invoke(_www);
        }

        /// <summary>
        /// Clear done requests from the pipeline of requests in progress, and execute scheduled tasks for done requests
        /// that are still in the pipeline.
        /// </summary>
        public static void NextState()
        {
            // First put done requests in another collection before removing them from the list in order to avoid
            // concurrent modification exceptions.
            var doneRequests = new List<WwwRequestInProgress>();
            foreach (var requestInProgress in _trackedRequestsInProgress.ToList())
            {
                if (requestInProgress._www.isDone)
                {
                    Debug.LogFormat("\nCOMPLETED: {0}", requestInProgress._progressBarTitleText.ToUpper());
                    doneRequests.Add(requestInProgress);
                }
                else
                {
                    Debug.LogFormat("\nIN PROGRESS: {0}. Up: {1}%, Down: {2}%",
                        requestInProgress._progressBarTitleText.ToUpper(),
                        requestInProgress._www.uploadProgress*100, requestInProgress._www.progress*100);
                }
            }

            foreach (var doneRequest in doneRequests)
            {
                _trackedRequestsInProgress.Remove(doneRequest);
            }

            doneRequests.Clear();
            foreach (var request in _scheduledForOnDone.ToList())
            {
                if (request._www.isDone)
                {
                    request.ExecuteScheduledTasks();
                    doneRequests.Add(request);
                }
            }

            foreach (var request in doneRequests)
            {
                _scheduledForOnDone.Remove(request);
            }
        }
    }
}