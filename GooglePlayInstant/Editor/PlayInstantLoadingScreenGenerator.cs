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

using System.CodeDom.Compiler;
using System.IO;
using GooglePlayInstant.LoadingScreen;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GooglePlayInstant.Editor
{
    /// <summary>
    /// Class that generates Unity loading scenes for instant apps.
    /// </summary>
    public class PlayInstantLoadingScreenGenerator
    {
        public const string LoadingSceneName = "play-instant-loading-screen-scene";

        private const string LoadingScreenJsonFileName = "LoadingScreenConfig.json";

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

        private static void AddLoadingBar(GameObject loadingScreenGameObject)
        {
            var loadingBarOutlineGameObject = new GameObject("Loading Bar Outline");
            loadingBarOutlineGameObject.AddComponent<Image>();
            loadingBarOutlineGameObject.transform.SetParent(loadingScreenGameObject.transform);
            
            // set background image
            var backgroundImage = loadingBarOutlineGameObject.GetComponent<Image>();
            backgroundImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd");
            backgroundImage.type = Image.Type.Tiled;
            backgroundImage.fillCenter = false;

            var rectTransformOutline = (RectTransform) loadingBarOutlineGameObject.transform;
            rectTransformOutline.position = loadingScreenGameObject.transform.position;
            
            var loadingBarPosition = rectTransformOutline.position;
            loadingBarPosition.y = loadingBarPosition.y - ((RectTransform) loadingScreenGameObject.transform).sizeDelta.y/2 ;
            rectTransformOutline.position = loadingBarPosition;
            
            
            rectTransformOutline.sizeDelta = new Vector2(((RectTransform) loadingScreenGameObject.transform).sizeDelta.x / 2f, 30);
            
            //////////////////////////////////////////////////////////////
            
            //adding loading bar
            var loadingBarProgressGameObject = new GameObject("Loading Bar Progress");
            loadingBarProgressGameObject.AddComponent<Image>();
            loadingBarProgressGameObject.transform.SetParent(loadingScreenGameObject.transform);
            
            //set the color for testing purposes

            var imageProgress = loadingBarProgressGameObject.GetComponent<Image>();
            imageProgress.color = Color.red;
            
            
            var rectTransformProgress = (RectTransform) loadingBarProgressGameObject.transform;
            rectTransformProgress.position = rectTransformOutline.position;
            
            var loadingBarProgressSize = rectTransformOutline.sizeDelta;
            loadingBarProgressSize.x = loadingBarProgressSize.x - 17;
            loadingBarProgressSize.y = loadingBarProgressSize.y - 17;

            rectTransformProgress.sizeDelta = loadingBarProgressSize;
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