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

using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace GooglePlayInstant.LoadingScreen
{
    public static class LoadingBar
    {
        private const int LoadingBarFillPadding = 17;

        // Loading bar height in terms of pixels
        private const int LoadingBarHeight = 30;

        // Loading bar width as a percentage canvas object's automatic size
        private const float LoadingBarWidthPercentage = .5f;

        // Loading bar y axis placement as a percentage of canvas object's automatic y value
        private const float LoadingBarYAxisPercentage = 2f;

        // names for the gameobject components
        private const string LoadingBarGameObjectName = "Loading Bar";
        private const string LoadingBarOutlineGameObjectName = "Loading Bar Outline";
        private const string LoadingBarFillGameObjectName = "Loading Bar Fill";

        private static bool _assetBundleLoadingIsDone;
        private static bool _sceneLoadingIsDone;

        private const float AssetBundleDownloadMaxWidthPercentage = .8f;


        public static void AddLoadingScreenBarComponent(GameObject loadingScreenGameObject)
        {
            var loadingBarGameObject = new GameObject(LoadingBarGameObjectName);
            loadingBarGameObject.AddComponent<RectTransform>();
            loadingBarGameObject.transform.SetParent(loadingScreenGameObject.transform);

            var loadingBarGameObjectRectTransform = loadingBarGameObject.GetComponent<RectTransform>();

            var loadingScreenGameObjectRectTransform = loadingScreenGameObject.GetComponent<RectTransform>();

            //Set the size of the loading bar
            loadingBarGameObjectRectTransform.sizeDelta =
                new Vector2(loadingScreenGameObjectRectTransform.sizeDelta.x * LoadingBarWidthPercentage,
                    LoadingBarHeight);

            //Set the position of the loading bar
            loadingBarGameObjectRectTransform.position =
                new Vector2(loadingScreenGameObjectRectTransform.position.x,
                    loadingScreenGameObjectRectTransform.position.y -
                    LoadingBarYAxisPercentage * loadingScreenGameObjectRectTransform.position.y);

            SetLoadingBarOutline(loadingBarGameObject);
            SetLoadingBarFill(loadingBarGameObject);
        }

        private static void SetLoadingBarOutline(GameObject loadingBarGameObject)
        {
            var loadingBarOutlineGameObject = new GameObject(LoadingBarOutlineGameObjectName);
            loadingBarOutlineGameObject.AddComponent<Image>();
            loadingBarOutlineGameObject.transform.SetParent(loadingBarGameObject.transform);

            var loadingBarOutlineImage = loadingBarOutlineGameObject.GetComponent<Image>();
            loadingBarOutlineImage.sprite =
                AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd");
            loadingBarOutlineImage.type = Image.Type.Tiled;
            loadingBarOutlineImage.fillCenter = false;

            // Set size of component
            var loadingBarOutlineGameObjectRectTransform = loadingBarOutlineGameObject.GetComponent<RectTransform>();
            var loadingBarGameObjectRectTransform = loadingBarGameObject.GetComponent<RectTransform>();
            loadingBarOutlineGameObjectRectTransform.sizeDelta = loadingBarGameObjectRectTransform.sizeDelta;

            // Position outline component
            loadingBarOutlineGameObject.transform.position = loadingBarGameObject.transform.position;
        }

        private static void SetLoadingBarFill(GameObject loadingBarGameObject)
        {
            var loadingBarFillGameObject = new GameObject(LoadingBarFillGameObjectName);
            loadingBarFillGameObject.AddComponent<Image>();
            loadingBarFillGameObject.transform.SetParent(loadingBarGameObject.transform);

            var loadingBarFillImage = loadingBarFillGameObject.GetComponent<Image>();
            loadingBarFillImage.color = Color.green;

            // Set size of component
            var loadingBarFillGameObjectRectTransform = loadingBarFillGameObject.GetComponent<RectTransform>();
            var loadingBarGameObjectRectTransform = loadingBarGameObject.GetComponent<RectTransform>();
            loadingBarFillGameObjectRectTransform.sizeDelta = new Vector2(
                loadingBarGameObjectRectTransform.sizeDelta.x - LoadingBarFillPadding,
                loadingBarGameObjectRectTransform.sizeDelta.y - LoadingBarFillPadding);

            // Position fill component
            loadingBarFillGameObject.transform.position = loadingBarGameObject.transform.position;
        }

        public static IEnumerator UpdateLoadingBarForAssetBundleDownload(UnityWebRequest www)
        {
            var loadingBarRectTransform = GameObject.Find(LoadingBarGameObjectName).GetComponent<RectTransform>();

            var loadingBarFillRectTransform =
                GameObject.Find(LoadingBarFillGameObjectName).GetComponent<RectTransform>();

            var loadingBarFillMaxWidth = loadingBarRectTransform.sizeDelta.x - LoadingBarFillPadding;
            var assetBundleLoadingMaxWidth = loadingBarFillMaxWidth * AssetBundleDownloadMaxWidthPercentage;

            while (!www.isDone || !_assetBundleLoadingIsDone)
            {
                if (www.isDone)
                {
                    _assetBundleLoadingIsDone = true;
                }

                // Change the width of the loading bar fill rectangle appropriately for the amount of download
                // progress made
                loadingBarFillRectTransform.sizeDelta = new Vector2(assetBundleLoadingMaxWidth * www.downloadProgress,
                    loadingBarFillRectTransform.sizeDelta.y);

                // Changing the width of the rectangle makes it shorter (or larger) on both sides--thus requiring the rectangle's
                // x position to be moved left by half the amount it's been shortened.
                loadingBarFillRectTransform.position = new Vector2(
                    loadingBarRectTransform.position.x -
                    (loadingBarFillMaxWidth - loadingBarFillRectTransform.sizeDelta.x) / 2f,
                    loadingBarFillRectTransform.position.y);

                yield return null;
            }
        }

        public static IEnumerator UpdateLoadingBarForSceneLoading(AsyncOperation sceneLoad)
        {
            var loadingBarRectTransform = GameObject.Find(LoadingBarGameObjectName).GetComponent<RectTransform>();

            var loadingBarFillRectTransform =
                GameObject.Find(LoadingBarFillGameObjectName).GetComponent<RectTransform>();
            
            var loadingBarFillMaxWidth = loadingBarRectTransform.sizeDelta.x - LoadingBarFillPadding;

//            var sceneLoadingMaxWidth =  loadingBarRectTransform.sizeDelta.x - loadingBarFillMaxWidth;
            var sceneLoadingMaxWidth =  loadingBarFillMaxWidth - loadingBarFillRectTransform.sizeDelta.x;

            var currentFill = loadingBarFillRectTransform.sizeDelta.x;

            
            while (!sceneLoad.isDone || !_sceneLoadingIsDone)
            {
                if (sceneLoad.isDone)
                {
                    _sceneLoadingIsDone = true;
                }

                loadingBarFillRectTransform.sizeDelta = new Vector2(
                    currentFill + sceneLoadingMaxWidth * sceneLoad.progress,
                    loadingBarFillRectTransform.sizeDelta.y);

                // Changing the width of the rectangle makes it shorter (or larger) on both sides--thus requiring the rectangle's
                // x position to be moved left by half the amount it's been shortened.
                loadingBarFillRectTransform.position = new Vector2(
                    loadingBarRectTransform.position.x -
                    (loadingBarFillMaxWidth - loadingBarFillRectTransform.sizeDelta.x) / 2f,
                    loadingBarFillRectTransform.position.y);

                yield return null;
            }
        }
    }
}