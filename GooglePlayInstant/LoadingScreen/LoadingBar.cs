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

        public const string LoadingBarGameObjectName = "Loading Bar";
        public const string LoadingBarOutlineGameObjectName = "Loading Bar Outline";
        public const string LoadingBarFillGameObjectName = "Loading Bar Fill";

        private static bool loadingIsDone = false;

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

        public static IEnumerator UpdateLoadingBar(UnityWebRequest www)
        {
            var loading = GameObject.Find(LoadingBarFillGameObjectName);
            var loadingBarFillRectTransform = loading.GetComponent<RectTransform>();

            var newLoadingBarFillRectTransformSizeDelta = loadingBarFillRectTransform.sizeDelta;

            var newLoadingBarFillRectTransformPosition = loadingBarFillRectTransform.position;

            var maxWidth = newLoadingBarFillRectTransformSizeDelta.x;
            var startingX = newLoadingBarFillRectTransformPosition.x;

            while (!www.isDone)
            {
                newLoadingBarFillRectTransformSizeDelta.x = maxWidth * www.downloadProgress;
                
                newLoadingBarFillRectTransformPosition.x =
                    startingX - (maxWidth - newLoadingBarFillRectTransformSizeDelta.x) / 2f;

                loadingBarFillRectTransform.sizeDelta = newLoadingBarFillRectTransformSizeDelta;

                loadingBarFillRectTransform.position = newLoadingBarFillRectTransformPosition;

                yield return null;
            }
        }
    }
}