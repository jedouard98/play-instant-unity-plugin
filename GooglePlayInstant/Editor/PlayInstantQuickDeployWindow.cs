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
    public class PlayInstantQuickDeployWindow : EditorWindow
    {
        private static int toolbarInt = 0;
        private string[] toolbarStrings = new string[] {"Bundle Create", "Bundle Deploy", "Verify Bundle", 
            "Loading Screen", "Build"};

        public const int BundleCreateSelect = 0;
        public const int BundleDeploySelect = 1;
        public const int VerifyBundleSelect = 2;
        public const int LoadingScreenSelect = 3;
        public const int BuildSelect = 4;

        private const int FieldMinWidth = 100;
        private const int ButtonWidth = 200;

        public static void ShowWindow(int tabSelect)
        {
            GetWindow<PlayInstantQuickDeployWindow>("Quick Deploy");
            toolbarInt = tabSelect;
        }

        void OnGUI()
        {
            toolbarInt = GUILayout.Toolbar(toolbarInt, toolbarStrings);
            switch(toolbarInt) 
            {
                case BundleCreateSelect:
                    EditorGUILayout.LabelField("AssetBundle Creation", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField("Use the Unity Asset Bundle Browser to select your game's main scene " + 
                        "and bundle it (and its dependencies) into an AssetBundle file.", EditorStyles.wordWrappedLabel);
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("AssetBundle Browser version: not found", EditorStyles.wordWrappedLabel);
                    EditorGUILayout.Space();
                    GUILayout.Button ("Download AssetBundle Browser", GUILayout.Width(ButtonWidth));
                    EditorGUILayout.Space();
                    GUILayout.Button ("Open AssetBundle Browser", GUILayout.Width(ButtonWidth));
                    break;
                case BundleDeploySelect:
                    EditorGUILayout.LabelField("AssetBundle Deployment", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField("Use the Google Cloud Storage to host the AssetBundle as a public " +
                        "file. Or host the file on your own CDN.", EditorStyles.wordWrappedLabel);
                    EditorGUILayout.Space();
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Asset Bundle File Name", GUILayout.MinWidth(FieldMinWidth));
                    EditorGUILayout.TextField("c:\\mygame.assetbundle", GUILayout.MinWidth(FieldMinWidth));
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.Space();
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Cloud Storage Bucket Name", GUILayout.MinWidth(FieldMinWidth));
                    EditorGUILayout.TextField("mycorp_awesome_game", GUILayout.MinWidth(FieldMinWidth));
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.Space();
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Cloud Storage File Name", GUILayout.MinWidth(FieldMinWidth));
                    EditorGUILayout.TextField("mainscene", GUILayout.MinWidth(FieldMinWidth));
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.Space();
                    GUILayout.Button ("Upload to Cloud Storage", GUILayout.Width(ButtonWidth));
                    EditorGUILayout.Space();
                    GUILayout.Button ("Open Cloud Storage Console", GUILayout.Width(ButtonWidth));
                    break;
                case VerifyBundleSelect:
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
                    break;
                case LoadingScreenSelect:
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
                    break;
                case BuildSelect:
                    EditorGUILayout.LabelField("Deployment", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField("Build the APK using the IL2CPP engine.", EditorStyles.wordWrappedLabel);
                    EditorGUILayout.Space();
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("APK File Name", GUILayout.MinWidth(FieldMinWidth));
                    EditorGUILayout.TextField("c:\\base.apk", GUILayout.MinWidth(FieldMinWidth));
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.Space();
                    GUILayout.Button ("Build Base APK", GUILayout.Width(ButtonWidth));
                    break;
            }
        }
    }
}
