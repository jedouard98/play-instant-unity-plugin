﻿// Copyright 2018 Google LLC
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

using UnityEditor;

namespace GooglePlayInstant.Editor.QuickDeploy
{
    /// <summary>
    /// Utility class for displaying popup window errors.
    /// </summary>
    public static class ErrorLogger
    {
        /// <summary>
        /// Error titles for various error isplays.
        /// </summary>
        public const string AssetBundleBrowserErrorTitle = "Unity Asset Bundle Browser Error";

        public const string AssetBundleDeploymentErrorTitle = "AssetBundle Deployment Error";
        public const string AssetBundleCheckerErrorTitle = "AssetBundle Checker Error";
        public const string LoadingScreenCreationErrorTitle = "Loading Screen Creation Error";

        private const string OkButtonText = "OK";

        /// <summary>
        /// Displays a popup window that details an error message with a specified title.
        /// </summary>
        public static void DisplayError(string title, string message)
        {
            EditorUtility.DisplayDialog(title, message, OkButtonText);
        }
    }
}