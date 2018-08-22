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
using System.IO;
using System.Runtime.CompilerServices;
using GooglePlayInstant.LoadingScreen;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[assembly: InternalsVisibleTo("GooglePlayInstant.Tests.Editor.QuickDeploy")]

namespace GooglePlayInstant.Editor.QuickDeploy
{
    /// <summary>
    /// Class that generates Unity loading scenes for instant apps.
    /// </summary>
    public class LoadingScreenGenerator
    {
        public const string LoadingSceneName = "play-instant-loading-screen-scene";

        private const string LoadingScreenCanvasName = "Loading Screen Canvas";

        private static readonly string LoadingScreenScenePath =
            Path.Combine("Assets", "PlayInstantLoadingScreen");

        private static readonly string LoadingScreenResourcesPath = Path.Combine(LoadingScreenScenePath, "Resources");

        private static readonly string LoadingScreenJsonPath =
            Path.Combine(LoadingScreenResourcesPath, LoadingScreenJsonFileName);

        // Visible for testing
        internal const string LoadingScreenJsonFileName = "LoadingScreenConfig.json";

        /// <summary>
        /// The path to a fullscreen image displayed in the background while the game loads.
        /// </summary>
        public static string LoadingScreenImagePath { get; set; }


        /// <summary>
        /// Creates a scene in the current project that acts as a loading scene until assetbundles are
        /// downloaded from the CDN. Takes in a loadingScreenImagePath, a path to the image shown in the loading scene,
        /// and an assetbundle URL. Replaces the current loading scene with a new one if it exists.
        /// </summary>
        public static void GenerateLoadingScreenScene(string assetBundleUrl)
        {
            if (!File.Exists(LoadingScreenImagePath))
            {
                var errorMessage = string.Format("Loading screen image file cannot be found: {0}",
                    LoadingScreenImagePath);

                ErrorLogger.DisplayError(ErrorLogger.LoadingScreenErrorTitle, errorMessage);
                Debug.LogErrorFormat(errorMessage);
                return;
            }

            // Removes the loading scene if it is present, otherwise does nothing.
            EditorSceneManager.CloseScene(SceneManager.GetSceneByName(LoadingSceneName), true);

            var loadingScreenScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

            Directory.CreateDirectory(LoadingScreenResourcesPath);

            var generatedLoadingScreenConfig = GenerateLoadingScreenConfigFile(assetBundleUrl, LoadingScreenJsonPath);

            if (!generatedLoadingScreenConfig)
            {
                // Exit function; Error has already been logged and displayed.
                return; 
            }

            var loadingScreenGameObject = new GameObject(LoadingScreenCanvasName);

            var addedLoadingScreenImage = AddLoadingScreenImageToScene(loadingScreenGameObject, LoadingScreenImagePath);

            if (!addedLoadingScreenImage)
            {
                // Exit function; Error has already been logged and displayed.
                return;
            }

            var addedLoadingScreenScript = AddLoadingScreenScript(loadingScreenGameObject);

            if (!addedLoadingScreenScript)
            {
                // Exit function; Error has already been logged and displayed.
                return; 
            }

            LoadingBar.AddLoadingScreenBarComponent(loadingScreenGameObject);

            bool saveOk = EditorSceneManager.SaveScene(loadingScreenScene,
                Path.Combine(LoadingScreenScenePath, LoadingSceneName + ".unity"));

            if (!saveOk)
            {
                var errorMessage = string.Format("Loading screen generator error: Issue while saving scene {0}.",
                    LoadingSceneName);

                ErrorLogger.DisplayError(ErrorLogger.LoadingScreenErrorTitle, errorMessage);
                Debug.LogErrorFormat(errorMessage);
            }
        }

        // Visible for testing
        internal static bool AddLoadingScreenScript(GameObject loadingScreenGameObject)
        {
            try
            {
                loadingScreenGameObject.AddComponent<LoadingScreenScript>();
            }
            catch (Exception ex)
            {
                const string errorMessage = "Error adding loading screen script to scene. See Console log for details.";

                ErrorLogger.DisplayError(ErrorLogger.LoadingScreenErrorTitle, errorMessage);
                Debug.LogErrorFormat(ex.ToString());

                return false;
            }

            return true;
        }


        // Visible for testing
        internal static bool AddLoadingScreenImageToScene(GameObject loadingScreenGameObject,
            string pathToLoadingScreenImage)
        {
            loadingScreenGameObject.AddComponent<Canvas>();
            var loadingScreenCanvas = loadingScreenGameObject.GetComponent<Canvas>();
            loadingScreenCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

            byte[] loadingScreenImageData;

            try
            {
                loadingScreenImageData = File.ReadAllBytes(pathToLoadingScreenImage);
            }
            catch (Exception ex)
            {
                const string errorMessage = "Error while reading loading image file. See Console log for details.";

                ErrorLogger.DisplayError(ErrorLogger.LoadingScreenErrorTitle, errorMessage);

                Debug.LogError(ex.ToString());

                return false;
            }

            var tex = new Texture2D(1, 1);

            var texLoaded = tex.LoadImage(loadingScreenImageData);

            if (!texLoaded)
            {
                const string errorMessage = "Error loading image as a texture for canvas game object. Data could not be loaded.";

                ErrorLogger.DisplayError(ErrorLogger.LoadingScreenErrorTitle, errorMessage);

                Debug.LogError(errorMessage);

                return false;
            }

            var loadingImageSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));

            loadingScreenGameObject.AddComponent<Image>();
            var loadingScreenImage = loadingScreenGameObject.GetComponent<Image>();
            loadingScreenImage.sprite = loadingImageSprite;

            return true;
        }

        // Visible for testing
        internal static bool GenerateLoadingScreenConfigFile(string assetBundleUrl, string targetLoadingScreenJsonPath)
        {
            var loadingScreenConfig =
                new LoadingScreenConfig {assetBundleUrl = assetBundleUrl};

            var loadingScreenConfigJson = EditorJsonUtility.ToJson(loadingScreenConfig);

            try
            {
                File.WriteAllText(targetLoadingScreenJsonPath, loadingScreenConfigJson);
            }
            catch (Exception ex)
            {
                const string errorMessage = "Error while reading loading image file. See Console log for details.";

                ErrorLogger.DisplayError(ErrorLogger.LoadingScreenErrorTitle, errorMessage);

                Debug.LogError(ex.ToString());

                return false;
            }

            // Force asset to import synchronously so that testing can be completed immediately after generating a loading screen.
            AssetDatabase.ImportAsset(targetLoadingScreenJsonPath, ImportAssetOptions.ForceSynchronousImport);

            return true;
        }
    }
}