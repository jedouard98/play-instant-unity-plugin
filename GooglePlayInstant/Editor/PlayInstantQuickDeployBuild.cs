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

using UnityEditor;
using UnityEngine;

namespace GooglePlayInstant.Editor
{
    /// <summary>
    /// A handler for building apks using quick deploy.
    /// </summary>
    public static class QuickDeployBuilder
    {
        /// <summary>
        /// Determine whether or not the project is using IL2CPP as scripting backend.
        /// </summary>
        public static bool ProjectIsUsingIl2cpp()
        {
            return PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android) == ScriptingImplementation.IL2CPP;
        }

        /// <summary>
        /// Build an android apk with quick deploy. Prompts the user to enable IL2CPP and engine stripping if not
        /// enabled, and builds the apk with the settings that the user chooses.
        /// Produces a resulting apk that contains the splash scene and functionality that will load the game's
        /// asset bundle from the cloud at the game's runtime.
        /// Logs success message to the console with built apk's path when the apk is successfully built, otherwise logs
        /// error message.
        /// </summary>
        public static void BuildQuickDeployInstantGameApk()
        {
            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = new[] {PlayInstantLoadingScreenGenerator.LoadingSceneName + ".unity"},
                locationPathName = QuickDeployConfig.Config.apkFileName,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };


            if (!ProjectIsUsingIl2cpp() || !PlayerSettings.stripEngineCode)
            {
                var enableIl2cppAndEngineStripping = EditorUtility.DisplayDialog(
                    "IL2CPP or engine stripping not enabled",
                    "Your project is not using IL2CPP scripting runtime, or engine stripping is not enabled. Would " +
                    "you like to build the APK with IL2CPP and Engine Stripping to improve game " +
                    "performance and reduce APK size?", "Yes", "No");
                if (enableIl2cppAndEngineStripping)
                {
                    PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
                    PlayerSettings.stripEngineCode = true;
                }
            }

            if (PlayInstantBuilder.Build(buildPlayerOptions))
            {
                Debug.LogFormat("Apk successfully built at \"{0}\".", buildPlayerOptions.locationPathName);
            }
            else
            {
                Debug.LogError("Couldn't build apk.");
            }
        }
    }
}