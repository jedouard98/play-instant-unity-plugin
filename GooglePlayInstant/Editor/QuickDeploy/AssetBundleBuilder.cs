﻿// Copyright 2018 Google LLC
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
using System.IO;
using UnityEditor;

namespace GooglePlayInstant.Editor.QuickDeploy
{
    /// <summary>
    /// Contains methods for building AssetBundle to be deployed.
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

            // TODO: Update AssetBundle manifest path in PlayInstantBuildConfiguration.(Requires some refactoring)
            var builtAssetBundleManifest = BuildPipeline.BuildAssetBundles(assetBundleDirectory,
                new[] {assetBundleBuild}, BuildAssetBundleOptions.None, EditorUserBuildSettings.activeBuildTarget);
            // Returned AssetBundleManifest will be null if there was error in building assetbundle.
            if (builtAssetBundleManifest == null)
            {
                throw new Exception(
                    "Could not build AssetBundle. Please ensure that you have properly configured AssetBundle to be buit " +
                    "by selecting scenes to include and choosing a valid path for AssetBundle to be stored.");
            }
        }
    }
}