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
using UnityEngine.Networking;

namespace GooglePlayInstant.Editor
{
    /// <summary>
    /// Window that verifies AssetBundles from given URLs.
    /// </summary>
    public class AssetBundleVerifierWindow : EditorWindow
    {
        private bool _assetBundleDownloadIsSuccessful;
        private string _assetBundleUrl;
        private long _responseCode;
        private string _errorDescription;
        private string _mainScene;
        private double _numOfMegabytes;

        private UnityWebRequest www;

        private const int FieldMinWidth = 170;

        /// <summary>
        /// Creates a dialog box that details the success or failure of an AssetBundle retrieval from a given assetBundleUrl.
        /// </summary>
        public static void ShowWindow()
        {
            // Set AssetBundle url in a private variable so that information displayed in window is consistent with
            // the url that this was called on. 
            var window = (AssetBundleVerifierWindow) GetWindow(typeof(AssetBundleVerifierWindow), true,
                "Play Instant AssetBundle Verify");
            window._assetBundleUrl = QuickDeployConfig.Config.assetBundleUrl;

            window.startAssetBundleVerificationDownload();
        }

        //TODO: Support Unity 5.6.0+
        private void startAssetBundleVerificationDownload()
        {
            www = UnityWebRequestAssetBundle.GetAssetBundle(_assetBundleUrl);
            www.SendWebRequest();
        }

        //TODO: Support Unity 5.6.0+
        private void getAssetBundleInfoFromDownload()
        {
            var bundle = DownloadHandlerAssetBundle.GetContent(www);

            _responseCode = www.responseCode;

            if (www.isNetworkError || www.isHttpError)
            {
                _assetBundleDownloadIsSuccessful = false;
                _errorDescription = www.error;
                Debug.LogErrorFormat("Problem retrieving AssetBundle from {0}: {1}", _assetBundleUrl,
                    _errorDescription);
            }
            else if (bundle == null)
            {
                _assetBundleDownloadIsSuccessful = false;
                _errorDescription = "Error extracting AssetBundle. See Console log for details.";
                // No need to log since debugging information in this case is automatically logged by Unity.
            }
            else
            {
                _assetBundleDownloadIsSuccessful = true;
                _numOfMegabytes = ConvertBytesToMegabytes(www.downloadedBytes);

                var scenes = bundle.GetAllScenePaths();
                _mainScene = (scenes.Length == 0) ? "No scenes in AssetBundle" : scenes[0];

                // Free memory used by the AssetBundle since it will not be in use by the Editor. Set to true to destory
                // all objects that were loaded from this bundle.
                bundle.Unload(true);
            }

            // Turn request to null to be prepared for next call
            www = null;
        }

        private double ConvertBytesToMegabytes(ulong bytes)
        {
            return bytes / 1024f / 1024f;
        }

        //TODO: fix malformed url behavior
        private void Update()
        {
            if ((www != null) && (www.isDone))
            {
                getAssetBundleInfoFromDownload();
                Repaint();
            }
        }

        //TODO: add a loading state so client knows that some loading is going on behind the scenes.
        private void OnGUI()
        {
            AddVerifyComponentInfo("AssetBundle Download Status:",
                _assetBundleDownloadIsSuccessful ? "SUCCESS" : "FAILED");

            AddVerifyComponentInfo("AssetBundle URL:", _assetBundleUrl);

            AddVerifyComponentInfo("HTTP Status Code:", _responseCode == 0 ? "N/A" : _responseCode.ToString());

            AddVerifyComponentInfo("Error Description:", _assetBundleDownloadIsSuccessful ? "N/A" : _errorDescription);

            AddVerifyComponentInfo("Main Scene:", _assetBundleDownloadIsSuccessful ? _mainScene : "N/A");

            AddVerifyComponentInfo("Size (MB):",
                _assetBundleDownloadIsSuccessful ? _numOfMegabytes.ToString("#.####") : "N/A");

            if (GUILayout.Button("Refresh"))
            {
                startAssetBundleVerificationDownload();
            }
        }

        private void AddVerifyComponentInfo(string title, string response, GUIStyle layout = null)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(title, GUILayout.MinWidth(FieldMinWidth));
            EditorGUILayout.LabelField(response, EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
        }
    }
}