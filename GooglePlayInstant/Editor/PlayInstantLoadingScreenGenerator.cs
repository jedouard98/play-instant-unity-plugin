using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using System;
using System.IO;

namespace GooglePlayInstant.Editor
{
    public class PlayInstantLoadingScreenGenerator
    {
        public static void GenerateLoadingScreenScene(string pathToLoadingScreenImage, string assetBundleUrl)
        {
            GenerateLoadingScreenScript(assetBundleUrl);
            //create the scene and the game object
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var loadingScreenGO = new GameObject("Canvas");
            
            loadingScreenGO.AddComponent<Canvas>();
            
            var loadingScreenCanvas = loadingScreenGO.GetComponent<Canvas>();
            loadingScreenCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
   
        }

        private static void GenerateLoadingScreenScript(string assetBundleUrl)
        {
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