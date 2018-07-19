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
using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;

namespace GooglePlayInstant.Editor
{
    /// <summary>
    /// Window that verifies that valid AssetBundles from given URLs.
    /// </summary>
    public class PlayInstantAssetBundleUrlVerifierWindow : EditorWindow
    {
        private static UnityWebRequest www;
        private static string _errorDescription;
        private static long _responseCode;

        private static string _assetBundleUrl;

        private static bool _assetBundleDownloadSuccessful;
        private static double _numOfBytes;
        private static double _numOfKilobytes;


        /// <summary>
        /// Creates a dialog box that details the success or failure of an AssetBundle retrieval from a given assetBundleUrl.
        /// </summary>
        public static void ShowWindow(string assetBundleUrl)
        {
            _assetBundleUrl = assetBundleUrl;
            verifyAssetBundle();
            GetWindow(typeof(PlayInstantAssetBundleUrlVerifierWindow), true, "Play Instant AssetBundle Verify");
        }

        //TODO: improve on error descriptions and support older versions of Unity
        private static void verifyAssetBundle()
        {
            www = UnityWebRequestAssetBundle.GetAssetBundle(_assetBundleUrl);
            www.SendWebRequest();
            while (!www.isDone)
            {
                //TODO: implement code to update some type of loading bar 
            }

            // check to see if item downloaded was an actual assetbundle object.
            AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(www);
            _responseCode = www.responseCode;

            if (www.isNetworkError || www.isHttpError)
            {
                Debug.Log(www.error);
                _errorDescription = www.error;
                _assetBundleDownloadSuccessful = false;
            }
            else if (bundle == null)
            {
                _errorDescription = "Failed to decompress data for the AssetBundle.";
                _assetBundleDownloadSuccessful = false;
            }
            else
            {
                _numOfBytes = www.downloadedBytes;
                _numOfKilobytes = _numOfBytes / 1024;
                _assetBundleDownloadSuccessful = true;
            }

            // Discard AssetBundle from directory.
            bundle.Unload(false);
        }

        //TODO: fix styling
        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("AssetBundle Download Status:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField((_assetBundleDownloadSuccessful ? "SUCCESS" : "FAILED"));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("URL for AssetBundle:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(_assetBundleUrl);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("HTTP Status Code:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(_responseCode.ToString());
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Error Description:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField((_assetBundleDownloadSuccessful ? "N/A" : _errorDescription));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Number of Bytes:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField((_assetBundleDownloadSuccessful ? _numOfBytes.ToString() : "N/A"));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Number of Kilobytes:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField((_assetBundleDownloadSuccessful ? _numOfKilobytes.ToString() : "N/A"));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            if (GUILayout.Button("Refresh"))
            {
                verifyAssetBundle();
            }
        }
    }
}