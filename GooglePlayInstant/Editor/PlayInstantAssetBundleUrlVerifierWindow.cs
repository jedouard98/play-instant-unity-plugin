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
    public class PlayInstantAssetBundleUrlVerifierWindow : EditorWindow
    {
        private static bool _assetBundleDownloadIsSuccessful;
        private static string _assetBundleUrl;
        private static long _responseCode;
        private static string _errorDescription;
        private static double _numOfMegabytes;

        private const int FieldMinWidth = 170;


        /// <summary>
        /// Creates a dialog box that details the success or failure of an AssetBundle retrieval from a given assetBundleUrl.
        /// </summary>
        public static void ShowWindow(string assetBundleUrl)
        {
            _assetBundleUrl = assetBundleUrl;
            getAssetBundleVerificationInfo();
            GetWindow(typeof(PlayInstantAssetBundleUrlVerifierWindow), true, "Play Instant AssetBundle Verify");
        }

        //TODO: improve on error descriptions and support Unity 5.6+
        private static void getAssetBundleVerificationInfo()
        {
            var www = UnityWebRequestAssetBundle.GetAssetBundle(_assetBundleUrl);
            www.SendWebRequest();
            while (!www.isDone)
            {
                //TODO: implement loading bar 
            }

            // Check to see if downloadeded item was an actual AssetBundle object.
            var bundle = DownloadHandlerAssetBundle.GetContent(www);

            _responseCode = www.responseCode;

            if (www.isNetworkError || www.isHttpError)
            {
                _assetBundleDownloadIsSuccessful = false;
                _errorDescription = www.error;
                Debug.LogErrorFormat("Problem retrieving AssetBundle from {0}: {1}", _errorDescription,
                    _assetBundleUrl);
            }
            else if (bundle == null)
            {
                _assetBundleDownloadIsSuccessful = false;
                _errorDescription = "Failed to decompress data for the AssetBundle.";
                // Debugging information is automatically logged.
            }
            else
            {
                _assetBundleDownloadIsSuccessful = true;
                _numOfMegabytes = ConvertBytesToMegabytes(www.downloadedBytes);

                // Discard AssetBundle from directory since it is not in use.
                bundle.Unload(false);
            }
        }

        private static double ConvertBytesToMegabytes(ulong bytes)
        {
            return bytes / 1024f / 1024f;
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("AssetBundle Download Status:", GUILayout.MinWidth(FieldMinWidth));
            EditorGUILayout.LabelField((_assetBundleDownloadIsSuccessful ? "SUCCESS" : "FAILED"));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("AssetBundle URL:", GUILayout.MinWidth(FieldMinWidth));
            EditorGUILayout.LabelField(_assetBundleUrl, EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("HTTP Status Code:", GUILayout.MinWidth(FieldMinWidth));
            EditorGUILayout.LabelField(_responseCode.ToString());
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Error Description:", GUILayout.MinWidth(FieldMinWidth));
            EditorGUILayout.LabelField((_assetBundleDownloadIsSuccessful ? "N/A" : _errorDescription));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Size (MB):", GUILayout.MinWidth(FieldMinWidth));
            EditorGUILayout.LabelField((_assetBundleDownloadIsSuccessful ? _numOfMegabytes.ToString("#.####") : "N/A"));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            if (GUILayout.Button("Refresh"))
            {
                getAssetBundleVerificationInfo();
            }
        }
    }
}