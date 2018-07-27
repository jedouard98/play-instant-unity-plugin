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
    /// Provides functionality for tracking and visualizing useful information about WWW requests in progress.
    /// </summary>
    public class WwwRequestHelper
    {
        // Thread Safety Note: Operations on this class are NOT threadsafe because they are are expected to be
        // run on the main thread.

        // Use an ordered collection for requests in progress so you can display progress bars in a consinstent order.
        // Shouldn't be readonly because it is mutable.
        private static List<WwwRequestHelper> _requestsInProgress = new List<WwwRequestHelper>();
        
        private WWW _www;
        private string _title;
        private string _info;

        // a method to be executed on the _www field when it is done
        public delegate void DoneWwwHandler(WWW www);
        private List<DoneWwwHandler> _onDoneTasks = new List<DoneWwwHandler>();

        /// <summary>
        /// Instantiate an instance of a RequestInProgress class.
        /// </summary>
        /// <param name="www">An instance of the WWW object representing the HTTP request being made.</param>
        /// <param name="title">The high level action of the request. This is displayed as the title when displaying
        ///     the progress bar for this request in progress.</param>
        /// <param name="info">A description of what this request is doing. This is displayed in the body when
        /// displaying the progress bar for this request in progress.</param>
        public WwwRequestHelper(WWW www, string title, string info)
        {
            _www = www;
            _title = title;
            _info = info;
        }

        /// <summary>
        /// Add instance to tracked requests in progress. After this call, a call to DisplayProgressForTrackedRequests
        /// will display information for this request if it's not completed.
        /// </summary>
        public void TrackProgress()
        {
            _requestsInProgress.Add(this);
        }

        /// <summary>
        /// Schedule a task to invoke on the request when the request is done.
        /// </summary>
        public void ScheduleTaksOnDone(DoneWwwHandler wwwHandler)
        {
            _onDoneTasks.Add(wwwHandler);
        }

        // Execute all the scheduled tasks for this instance. Clears all the tasks after executing them
        private void ExecuteScheduledTasks()
        {
            if (!_www.isDone)
            {
                throw new Exception("Request has not yet completed");
            }

            foreach (var wwwHandler in _onDoneTasks)
            {
                wwwHandler.Invoke(_www);
            }
            _onDoneTasks.Clear();
        }
        
        /// <summary>
        /// Clear done requests from the pipeline of requests in progress, and execute scheduled tasks for done requests
        /// that are still in the pipeline.
        /// </summary>
        public static void UpdateState()
        {
            // First put done requests in another collection before removing them from the list in order to avoid
            // concurrent modification exceptions.
            var doneRequests = new List<WwwRequestHelper>();
            foreach (var requestInProgress in _requestsInProgress)
            {
                if (requestInProgress._www.isDone)
                {
                    doneRequests.Add(requestInProgress);
                }
            }

            foreach (var doneRequest in doneRequests)
            {
                doneRequest.ExecuteScheduledTasks();
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
        /// Display a progress bar and request information for all class-wide tracked requests in progress.
        /// </summary>
        public static void DisplayProgressForTrackedRequests()
        {
            foreach (var requestInProgress in _requestsInProgress)
            {
                requestInProgress.DisplayProgress();
            }
        }
    }
}