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
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace GooglePlayInstant.Editor
{
    public static class AssetBundleBrowserClient
    {
        private const string AssetBundleBrowserName = "com.unity.assetbundlebrowser";
        private const string AssetBundleBrowserMenuItem = "Window/AssetBundle Browser";
        private static bool? _assetBundleBrowserIsPresent;
        private static string _assetBundleBrowserVersion;

        /// <summary>
        /// Whether or not Asset Bundle Browser is present
        /// </summary>
        public static bool AssetBundleBrowserIsPresent
        {
            get { return BundleBrowserIsPresent(); }
        }

        /// <summary>
        /// The detected version of Asset Bundle Browser.
        /// </summary>
        public static string AssetBundleBrowserVersion
        {
            get { return GetBrowserVersion(); }
        }


        // Detects AssetBundleBrowser Namespace and the AssetBundleBrowserMain Class
        private static bool BundleBrowserIsPresent(bool useCurrentValueIfPresent = true)
        {
            if (useCurrentValueIfPresent && _assetBundleBrowserIsPresent.HasValue)
            {
                return _assetBundleBrowserIsPresent.Value;
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                // Checking that assembly != null since we do not want to rely on iteration through
                // AppDomain.CurrentDomain.GetAssemblies() to yield non-null items for this function to work
                if (assembly == null)
                {
                    continue;
                }

                foreach (var type in assembly.GetTypes())
                {
                    // Look for AssetBundleBrowserMain in the AssetBundleBrowser Namespace
                    if (type != null && string.Equals(type.Namespace, "AssetBundleBrowser") &&
                        type.Name.Equals("AssetBundleBrowserMain"))
                    {
                        _assetBundleBrowserIsPresent = true;
                        return _assetBundleBrowserIsPresent.Value;
                    }
                }
            }

            _assetBundleBrowserIsPresent = false;
            return false;
        }

        // Evaluates whether a folder has "AssetBundles-Browser" in its name, making it
        // a candidate for the Asset Bundles Browser Folder
        private static bool IsAssetBundleBrowserFolder(string folderName)
        {
            var regex = new Regex(@"AssetBundles-Browser", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            return regex.Matches(folderName).Count > 0;
        }

        // Extracts AssetBundleBrowser version name from the Asset Bundle Browser package.json file
        private static string GetBrowserVersion(bool useCurrentValueIfPresent = true)
        {
            if (useCurrentValueIfPresent && _assetBundleBrowserVersion != null)
            {
                return _assetBundleBrowserVersion;
            }

            var assetsPath = Application.dataPath;
            var assetBundleBrowserFolderPaths =
                Directory.GetDirectories(assetsPath).ToArray().Where(IsAssetBundleBrowserFolder);

            foreach (var folderPath in assetBundleBrowserFolderPaths)
            {
                var expectedPackageDotJsonPath = Path.Combine(folderPath, "package.json");
                if (!File.Exists(expectedPackageDotJsonPath))
                {
                    continue;
                }

                var data = File.ReadAllText(expectedPackageDotJsonPath);
                try
                {
                    var json = JsonUtility.FromJson<PackageDotJSsonContent>(data);

                    if (string.Equals(json.name, AssetBundleBrowserName))
                    {
                        _assetBundleBrowserVersion = json.version;
                        return _assetBundleBrowserVersion;
                    }
                }
                catch (ArgumentException e)
                {
                    Debug.LogWarning(string.Format(
                        "Unable to read Asset Bundle Browser version contents from {0}. \n {1}",
                        expectedPackageDotJsonPath, e.Message));
                }
            }

            _assetBundleBrowserVersion = "not found";
            return _assetBundleBrowserVersion;
        }

        /// <summary>
        /// Display the Asset Bundle Browser Window
        /// </summary>
        public static void DisplayAssetBundleBrowser()
        {
            if (!BundleBrowserIsPresent())
            {
                Debug.LogError("Cannot detect Unity Asset Bundle Browser");
            }

            EditorApplication.ExecuteMenuItem(AssetBundleBrowserMenuItem);
        }


        // CallBack method to re-update static values when this tab is re-opened
        /// <summary>
        /// Reload and update Asset Bundle Browser information
        /// </summary>
        public static void ReloadAndUpdateBrowserInfo()
        {
            _assetBundleBrowserIsPresent = BundleBrowserIsPresent(false);
            _assetBundleBrowserVersion = GetBrowserVersion(false);
        }

        // Represents name and version fields from the package.json file of the Asset Bundle Browser project:
        // https://github.com/Unity-Technologies/AssetBundles-Browser/blob/master/package.json
        // Suppress warnings about non-initialization of fields.
#pragma warning disable CS0649 
        [Serializable]
        private class PackageDotJSsonContent
        {
            public string name;
            public string version;
        }
    }
}