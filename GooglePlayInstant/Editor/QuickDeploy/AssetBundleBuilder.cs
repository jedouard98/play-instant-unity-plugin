﻿using System;
using System.IO;
using UnityEditor;

namespace GooglePlayInstant.Editor.QuickDeploy
{
    /// <summary>
    /// Contains methods for building asset bundle to be deployed.
    /// </summary>
    public static class AssetBundleBuilder
    {
        /// <summary>
        /// Builds an AssetBundle containing scenes at given paths, and stores the AssetBundle at configured path.
        /// </summary>
        /// <param name="scenePaths">Paths to scenes to include in the AssetBundle. Should be relative to project directory.</param>
        public static void BuildQuickDeployAssetBundle(string[] scenePaths)
        {
            if (scenePaths.Length == 0)
            {
                throw new Exception("No scenes were selected. Please select scenes to include in AssetBundle.");
            }

            if (string.IsNullOrEmpty(QuickDeployConfig.AssetBundleFileName))
            {
                throw new Exception("Cannot build AssetBundle with invalid file name.");
            }

            var assetBundleBuild = new AssetBundleBuild();
            assetBundleBuild.assetBundleName = Path.GetFileName(QuickDeployConfig.AssetBundleFileName);
            assetBundleBuild.assetNames = scenePaths;
            var assetBundleDirectory = Path.GetDirectoryName(QuickDeployConfig.AssetBundleFileName);
            if (!Directory.Exists(assetBundleDirectory))
            {
                Directory.CreateDirectory(assetBundleDirectory);
            }

            var isBuilt = BuildPipeline.BuildAssetBundles(assetBundleDirectory, new[] {assetBundleBuild},
                BuildAssetBundleOptions.None, EditorUserBuildSettings.activeBuildTarget);

            if (!isBuilt)
            {
                throw new Exception(
                    "Could not build AssetBundle. Please ensure that you have properly configured AssetBundle to be buit" +
                    "by selecting scenes to include, and that you have choosen a valid path for AssetBundle to be stored.");
            }
        }
    }
}