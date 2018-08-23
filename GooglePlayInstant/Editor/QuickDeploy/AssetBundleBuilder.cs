using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GooglePlayInstant.Editor.QuickDeploy
{
    public static class AssetBundleBuilder
    {
        public static void BuildQuickDeployAssetBundle(string assetBundleName, string[] assetPaths)
        {
            AssetBundleBuild[] build = new AssetBundleBuild[1];
            var assetBundleBuild  =  new AssetBundleBuild();
            
            assetBundleBuild.assetBundleName = assetBundleName;
            
            assetBundleBuild.assetNames = assetPaths;
            if (!Directory.Exists("AssetBundles"))
            {
                Directory.CreateDirectory("AssetBundles");
            }


            build[0] = assetBundleBuild;
            
            var isBuilt = BuildPipeline.BuildAssetBundles("AssetBundles", build, BuildAssetBundleOptions.None, EditorUserBuildSettings.activeBuildTarget);
            if (!isBuilt)
            {
                throw new Exception("Did not build assetbundle");
            }


        }


        public static string[] GetUnselectedScenePaths()
        {
            List<string> unSelectedPaths = new List<string>();
            EditorBuildSettingsScene[] selectedScenes = EditorBuildSettings.scenes;
            foreach (var scene in selectedScenes)
            {
                if (!scene.enabled)
                {
                    Debug.Log("Scene is not enabled");
                    unSelectedPaths.Add(scene.path);
                }
            }

            return unSelectedPaths.ToArray();
        }
        
        public static void  DeployUnselected(string preferredName) {
            
            BuildQuickDeployAssetBundle(preferredName, GetUnselectedScenePaths());
        }

    }

   
}