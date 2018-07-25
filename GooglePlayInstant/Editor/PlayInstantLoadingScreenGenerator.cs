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
using GooglePlayInstant.LoadingScreenEngine;
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
        private static readonly string LoadingScreenDirectory = Path.Combine("Assets", "PlayInstantLoadingScreenAssets");

        public static string loadingScreenImagePath;

        /// <summary>
        /// Sets the loadingScreenImagePath target for future generated loading screens.
        /// </summary>
        public static void SetLoadingScreenImagePath()
        {
            loadingScreenImagePath =
                EditorUtility.OpenFilePanel("Select Image", "", "png,jpg,jpeg,tif,tiff,gif,bmp");
        }

        //TODO: fix wasteful sprite creation by deleting previous unused ones
        /// <summary>
        /// Creates a scene in the current project that acts as a loading scene until assetbundles are
        /// downloaded from the CDN. Takes in a loadingScreenImagePath, a path to the image shown in the loading scene,
        /// and an assetbundle URL. Replaces the current loading scene with a new one if it exists.
        /// </summary>
        public static void GenerateLoadingScreenScene()
        {
            if (!File.Exists(loadingScreenImagePath))
            {
                Debug.LogErrorFormat("Loading screen image file cannot be found: {0}", loadingScreenImagePath);
            }
            else
            {
                // Removes the loading scene if it is present, otherwise does nothing.
                EditorSceneManager.CloseScene(SceneManager.GetSceneByName(LoadingSceneName), true);

                Directory.CreateDirectory(LoadingScreenDirectory);

                GenerateLoadingScreenConfigfile();

                var loadingScreenScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                var loadingScreenGameObject = new GameObject("Canvas");

                AddLoadingScreenImageToScene(loadingScreenGameObject, loadingScreenImagePath);
                AddLoadingScreenScript(loadingScreenGameObject);

                EditorSceneManager.SaveScene(loadingScreenScene,
                    Path.Combine(LoadingScreenDirectory, LoadingSceneName + ".unity"));
            }
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

        private static void GenerateLoadingScreenConfigfile()
        {            
            var newLoadingScreenConfigObj =
                new LoadingScreenConfig {assetBundleUrl = QuickDeployConfig.Config.assetBundleUrl};

            var newLoadingScreenConfigJson = EditorJsonUtility.ToJson(newLoadingScreenConfigObj);

            File.WriteAllText(Path.Combine(LoadingScreenDirectory, LoadingScreenJsonFileName),
                newLoadingScreenConfigJson);
        }
    }
}