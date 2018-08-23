using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace GooglePlayInstant.Editor.QuickDeploy
{
    public static class AssetBundleBuilder
    {
        public static void BuildQuickDeployAssetBundle(string assetBundleName, List<SceneAsset> scenes)
        {
            AssetBundleBuild[] build = new AssetBundleBuild[1];
            var assetBundleBuild  =  new AssetBundleBuild();
            assetBundleBuild.assetBundleName = assetBundleName;
            var assetNames = GetUnselectedScenePaths();
            assetBundleBuild.assetNames = assetNames;
            if (!Directory.Exists("AssetBundles"))
            {
                Directory.CreateDirectory("AssetBundles");
            }
            
            
            var isBuilt = BuildPipeline.BuildAssetBundles("AssetBundles", build, BuildAssetBundleOptions.None, EditorUserBuildSettings.activeBuildTarget);
            

        }


        public static string[] GetUnselectedScenePaths()
        {
            List<string> unSelectedPaths = new List<string>();
            EditorBuildSettingsScene[] selectedScenes = EditorBuildSettings.scenes;
            foreach (var scene in selectedScenes)
            {
                if (!scene.enabled)
                {
                    unSelectedPaths.Add(scene.path);
                }
            }

            return unSelectedPaths.ToArray();
        }

    }

   
}