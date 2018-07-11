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

using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using GooglePlayInstant.Deployer;
using Debug = UnityEngine.Debug;

namespace GooglePlayInstant.Editor
{
    public class PlayInstantQuickDeployWindow : EditorWindow
    {
        private static int _toolbarSelectedButtonIndex = 0;
        private static readonly string[] ToolbarButtonNames = {"Create Bundle", "Deploy Bundle", "Verify Bundle", 
            "Loading Screen", "Build"};
        
        public enum ToolBarSelectedButton
        {
            CreateBundle,
            DeployBundle,
            VerifyBundle,
            LoadingScreen,
            Build
        }

        private const int FieldMinWidth = 100;
        private const int ButtonWidth = 200;

        public static void ShowWindow(ToolBarSelectedButton select)
        {
            GetWindow<PlayInstantQuickDeployWindow>("Quick Deploy");
            _toolbarSelectedButtonIndex = (int) select;
        }

        // TODO: replace stub strings with real values
        void OnGUI()
        {
            _toolbarSelectedButtonIndex = GUILayout.Toolbar(_toolbarSelectedButtonIndex, ToolbarButtonNames);
            switch((ToolBarSelectedButton) _toolbarSelectedButtonIndex) 
            {
                case ToolBarSelectedButton.CreateBundle:
                    OnGuiCreateBundleSelect();
                    break;
                case ToolBarSelectedButton.DeployBundle:
                    OnGuiDeployBundleSelect();
                    break;
                case ToolBarSelectedButton.VerifyBundle:
                    OnGuiVerifyBundleSelect();
                    break;
                case ToolBarSelectedButton.LoadingScreen:
                    OnGuiLoadingScreenSelect();
                    break;
                case ToolBarSelectedButton.Build:
                    OnGuiCreateBuildSelect();
                    break;
            }
        }

        private void OnGuiCreateBundleSelect() 
        {
            EditorGUILayout.LabelField("AssetBundle Creation", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Use the Unity Asset Bundle Browser to select your game's main scene " + 
                "and bundle it (and its dependencies) into an AssetBundle file.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(string.Format("AssetBundle Browser version: {0}", "not found"), EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space();
            var downloadAssetBundleBrowser =  GUILayout.Button ("Download AssetBundle Browser", GUILayout.Width(ButtonWidth));
            if (downloadAssetBundleBrowser)
            {
                Process.Start("https://docs.unity3d.com/Manual/AssetBundles-Browser.html");
            }

            EditorGUILayout.Space();
            var openAssetBundleBrowser = GUILayout.Button ("Open AssetBundle Browser", GUILayout.Width(ButtonWidth));
            if (openAssetBundleBrowser)
            {
                Debug.LogError("Sorry! Gotta figure this out!");
            }
        }

        private void OnGuiDeployBundleSelect() 
        {
            EditorGUILayout.LabelField("AssetBundle Deployment", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Use the Google Cloud Storage to host the AssetBundle as a public " +
                "file. Or host the file on your own CDN.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Local Path to Asset Bundle File: ", GUILayout.MinWidth(FieldMinWidth));
            DeveloperFieldInputs.LocalAssetBundlePath = EditorGUILayout.TextField(DeveloperFieldInputs.LocalAssetBundlePath, GUILayout.MinWidth(FieldMinWidth));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Preferred Google Cloud Bucket Name: ", GUILayout.MinWidth(FieldMinWidth));
            DeveloperFieldInputs.RemoteBucketName = EditorGUILayout.TextField(DeveloperFieldInputs.RemoteBucketName, GUILayout.MinWidth(FieldMinWidth));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Preferred Google Cloud Object Name: ", GUILayout.MinWidth(FieldMinWidth));
            DeveloperFieldInputs.RemoteObjectName = EditorGUILayout.TextField(DeveloperFieldInputs.RemoteObjectName, GUILayout.MinWidth(FieldMinWidth));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Google Cloud Project Id: ", GUILayout.MinWidth(FieldMinWidth));
            DeveloperFieldInputs.RemoteProjectId = EditorGUILayout.TextField(DeveloperFieldInputs.RemoteProjectId, GUILayout.MinWidth(FieldMinWidth));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Path to Google Cloud Credentials: ", GUILayout.MinWidth(FieldMinWidth));
            DeveloperFieldInputs.CredentialsPath = EditorGUILayout.TextField(DeveloperFieldInputs.CredentialsPath, GUILayout.MinWidth(FieldMinWidth));
            EditorGUILayout.EndHorizontal();
            var uploadButtonClicked = GUILayout.Button ("Upload to Cloud Storage", GUILayout.Width(ButtonWidth));
            if (uploadButtonClicked)
            {
                // Deploy the bundle to the cloud now
                CloudClient.DeployBundle();
            }

            EditorGUILayout.Space();
            var openCloudStorageClicked = GUILayout.Button ("Open Cloud Storage Console", GUILayout.Width(ButtonWidth));
            if (openCloudStorageClicked)
            {
                // Open the link to cloud storage in the developer's default browser
                var linkToOpen = $"https://console.cloud.google.com/storage/browser?project={DeveloperFieldInputs.RemoteProjectId}";
                Process.Start(linkToOpen);
            }
        }

        private void OnGuiVerifyBundleSelect() 
        {
            EditorGUILayout.LabelField("AssetBundle Verification", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Verifies that the file at the specified URL is available and reports " +
                "metadata including file version and compression type.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("AssetBundle URL", GUILayout.MinWidth(FieldMinWidth));
            EditorGUILayout.TextField("http://storage.googleapis.com/mycorp_awesome_game/mainscene", 
                GUILayout.MinWidth(FieldMinWidth));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginVertical();
            GUILayout.Button ("Verify AssetBundle", GUILayout.Width(ButtonWidth));
            EditorGUILayout.EndVertical();
        }

        private void OnGuiLoadingScreenSelect() 
        {
            EditorGUILayout.LabelField("Loading Screen", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("A loading screen scene displays a progress bar over the image " +
                "specified below while downloading and opening the main scene.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Image File Name", GUILayout.MinWidth(FieldMinWidth));
            EditorGUILayout.TextField("c:\\loading.png", GUILayout.MinWidth(FieldMinWidth));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            GUILayout.Button ("Create Loading Scene", GUILayout.Width(ButtonWidth));
        }

        private void OnGuiCreateBuildSelect() 
        {
            EditorGUILayout.LabelField("Deployment", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Build the APK using the IL2CPP engine.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("APK File Name", GUILayout.MinWidth(FieldMinWidth));
            EditorGUILayout.TextField("c:\\base.apk", GUILayout.MinWidth(FieldMinWidth));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            GUILayout.Button ("Build Base APK", GUILayout.Width(ButtonWidth));
        }
    }
}
