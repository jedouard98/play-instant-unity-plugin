﻿using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using System;
using System.IO;

namespace GooglePlayInstant.Editor
{
    public class PlayInstantLoadingScreenGenerator
    {
        public static void GenerateLoadingScreenScene(string pathToLoadingScreenImage, string assetBundleUrl)
        {
            var loadingScreenScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var loadingScreenGO = new GameObject("Canvas");
            
            GenerateLoadingScreenScript(assetBundleUrl);
            AddBackgroundImageToScene(loadingScreenGO, pathToLoadingScreenImage);
            AddLoadingScreenScript(loadingScreenGO);

            EditorSceneManager.SaveScene(loadingScreenScene, "instant-play-loading-screen-scene.unity");
        }
        
        //TODO: get rid of error message associated with using the LoadingScreenScript reference
        private static void AddLoadingScreenScript(GameObject loadingScreenGO)
        {
            loadingScreenGO.AddComponent<LoadingScreenScript>();
        }
        
        private static void AddBackgroundImageToScene(GameObject loadingScreenGO, string pathToLoadingScreenImage)
        {
            loadingScreenGO.AddComponent<Canvas>();
            var loadingScreenCanvas = loadingScreenGO.GetComponent<Canvas>();
            loadingScreenCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            byte[] loadingScreenImageData = File.ReadAllBytes(pathToLoadingScreenImage);
            var tex = new Texture2D(1, 1);
            tex.LoadImage(loadingScreenImageData);

            var loadingImageSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));

            loadingScreenGO.AddComponent<Image>();
            var loadingScreenImage = loadingScreenGO.GetComponent<Image>();
            loadingScreenImage.sprite = loadingImageSprite;
        }

        private static void GenerateLoadingScreenScript(string assetBundleUrl)
        {
            //TODO: add better handling of finding assets folder
            var newloadingScreenScriptDir = Directory.GetCurrentDirectory() + "/Assets/GooglePlayInstantScript/LoadingScreenScript.cs";
            if (Directory.GetFiles(Directory.GetCurrentDirectory(), "GenericLoadingScreenScript.cs",
                    SearchOption.AllDirectories).Length == 0)
            {
                Debug.LogError("Cannot find Generic Loading Script from Google Play Instant Plugin.");
            }
            
            var genericloadingScreenScriptDir = Directory.GetFiles(Directory.GetCurrentDirectory(), "GenericLoadingScreenScript.cs", SearchOption.AllDirectories)[0];
            Directory.CreateDirectory(Directory.GetParent(newloadingScreenScriptDir).FullName);

            var genericLoadingScreenScript = File.ReadAllText(genericloadingScreenScriptDir);
            var newLoadingScreenScript = genericLoadingScreenScript.Replace("__ASSETBUNDLEURL__", assetBundleUrl)
                .Replace("GenericLoadingScreenScript", "LoadingScreenScript");
            File.WriteAllText(newloadingScreenScriptDir, newLoadingScreenScript);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }
    }
}