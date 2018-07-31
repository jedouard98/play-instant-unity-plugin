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

using System.IO;
using GooglePlayInstant.LoadingScreen;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GooglePlayInstant.Editor.QuickDeploy
{
    /// <summary>
    /// Class that generates Unity loading scenes for instant apps.
    /// </summary>
    public class LoadingScreenGenerator
    {
        public const string LoadingSceneName = "play-instant-loading-screen-scene";

        private const string LoadingScreenJsonFileName = "LoadingScreenConfig.json";

        private const int LoadingBarFillPadding = 17;
        private const int LoadingBarHeight = 30;


        private static readonly string LoadingScreenScenePath =
            Path.Combine("Assets", "PlayInstantLoadingScreen");

        private static readonly string LoadingScreenResourcesPath = Path.Combine(LoadingScreenScenePath, "Resources");

        /// <summary>
        /// The path to a fullscreen image displayed in the background while the game loads.
        /// </summary>
        public static string LoadingScreenImagePath { get; set; }

        //TODO: fix wasteful sprite creation by deleting previous unused ones
        /// <summary>
        /// Creates a scene in the current project that acts as a loading scene until assetbundles are
        /// downloaded from the CDN. Takes in a loadingScreenImagePath, a path to the image shown in the loading scene,
        /// and an assetbundle URL. Replaces the current loading scene with a new one if it exists.
        /// </summary>
        public static void GenerateLoadingScreenScene(string assetBundleUrl)
        {
            if (!File.Exists(LoadingScreenImagePath))
            {
                Debug.LogErrorFormat("Loading screen image file cannot be found: {0}", LoadingScreenImagePath);
                return;
            }

            // Removes the loading scene if it is present, otherwise does nothing.
            EditorSceneManager.CloseScene(SceneManager.GetSceneByName(LoadingSceneName), true);

            Directory.CreateDirectory(LoadingScreenResourcesPath);

            GenerateLoadingScreenConfigFile(assetBundleUrl);

            var loadingScreenScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var loadingScreenGameObject = new GameObject("Canvas");

            AddLoadingScreenImageToScene(loadingScreenGameObject, LoadingScreenImagePath);
            AddLoadingScreenScript(loadingScreenGameObject);


            AddLoadingBar(loadingScreenGameObject);

            EditorSceneManager.SaveScene(loadingScreenScene,
                Path.Combine(LoadingScreenScenePath, LoadingSceneName + ".unity"));
        }

        //TODO: look into screen rotation options
        private static void AddLoadingBar(GameObject loadingScreenGameObject)
        {
            var loadingBarGameObject = new GameObject("Loading Bar");
            loadingBarGameObject.AddComponent<RectTransform>();
            loadingBarGameObject.transform.SetParent(loadingScreenGameObject.transform);

            var loadingBarGameObjectRectTransform = loadingBarGameObject.GetComponent<RectTransform>();

            var loadingScreenGameObjectRectTransform = loadingScreenGameObject.GetComponent<RectTransform>();

            //Set the size of the loading bar
            loadingBarGameObjectRectTransform.sizeDelta =
                new Vector2(loadingScreenGameObjectRectTransform.sizeDelta.x / 2f, LoadingBarHeight);

            //Set the position of the loading bar
            loadingBarGameObjectRectTransform.position =
                new Vector2(loadingScreenGameObjectRectTransform.position.x,
                    loadingScreenGameObjectRectTransform.position.y - 2f * loadingScreenGameObjectRectTransform.position.y);

            SetLoadingBarOutline(loadingBarGameObject);
            SetLoadingBarFill(loadingBarGameObject);
        }

        private static void SetLoadingBarOutline(GameObject loadingBarGameObject)
        {
            // create outline component
            var loadingBarOutlineGameObject = new GameObject("Loading Bar Outline");
            loadingBarOutlineGameObject.AddComponent<Image>();
            loadingBarOutlineGameObject.transform.SetParent(loadingBarGameObject.transform);

            var backgroundImage = loadingBarOutlineGameObject.GetComponent<Image>();
            backgroundImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd");
            backgroundImage.type = Image.Type.Tiled;
            backgroundImage.fillCenter = false;

            // Set size of component
            var loadingBarOutlineGameObjectRectTransform = loadingBarOutlineGameObject.GetComponent<RectTransform>();
            var loadingBarGameObjectRectTransform = loadingBarGameObject.GetComponent<RectTransform>();
            loadingBarOutlineGameObjectRectTransform.sizeDelta = loadingBarGameObjectRectTransform.sizeDelta;

            // Position outline component
            loadingBarOutlineGameObject.transform.position = loadingBarGameObject.transform.position;
        }

        private static void SetLoadingBarFill(GameObject loadingBarGameObject)
        {
            // create fill component
            var loadingBarFillGameObject = new GameObject("Loading Bar Fill");
            loadingBarFillGameObject.AddComponent<Image>();
            loadingBarFillGameObject.transform.SetParent(loadingBarGameObject.transform);

            var loadingBarFillImage = loadingBarFillGameObject.GetComponent<Image>();
            loadingBarFillImage.color = Color.red;

            // Set size of component
            var loadingBarFillGameObjectRectTransform = loadingBarFillGameObject.GetComponent<RectTransform>();
            var loadingBarGameObjectRectTransform = loadingBarGameObject.GetComponent<RectTransform>();
            loadingBarFillGameObjectRectTransform.sizeDelta = new Vector2(
                loadingBarGameObjectRectTransform.sizeDelta.x - LoadingBarFillPadding,
                loadingBarGameObjectRectTransform.sizeDelta.y - LoadingBarFillPadding);

            // Position outline component
            loadingBarFillGameObject.transform.position = loadingBarGameObject.transform.position;
        }

        private static void AddLoadingScreenScript(GameObject loadingScreenGameObject)
        {
            loadingScreenGameObject.AddComponent<LoadingScreenScript>();
        }


        private static void AddLoadingScreenImageToScene(GameObject loadingScreenGameObject,
            string pathToLoadingScreenImage)
        {
            loadingScreenGameObject.AddComponent<Canvas>();
            var loadingScreenCanvas = loadingScreenGameObject.GetComponent<Canvas>();
            loadingScreenCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var loadingScreenImageData = File.ReadAllBytes(pathToLoadingScreenImage);
            var tex = new Texture2D(1, 1);
            tex.LoadImage(loadingScreenImageData);

            var loadingImageSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));

            loadingScreenGameObject.AddComponent<Image>();
            var loadingScreenImage = loadingScreenGameObject.GetComponent<Image>();
            loadingScreenImage.sprite = loadingImageSprite;
        }

        private static void GenerateLoadingScreenConfigFile(string assetBundleUrl)
        {
            var loadingScreenConfig =
                new LoadingScreenConfig {assetBundleUrl = assetBundleUrl};

            var loadingScreenConfigJson = EditorJsonUtility.ToJson(loadingScreenConfig);

            var loadingScreenJsonPath = Path.Combine(LoadingScreenResourcesPath, LoadingScreenJsonFileName);

            File.WriteAllText(loadingScreenJsonPath, loadingScreenConfigJson);

            // Force asset to import synchronously so that testing can be completed immediately after generating a loading screen.
            AssetDatabase.ImportAsset(loadingScreenJsonPath, ImportAssetOptions.ForceSynchronousImport);
        }
    }
}