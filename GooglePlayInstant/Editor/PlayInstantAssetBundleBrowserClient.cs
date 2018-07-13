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
        private static bool? _assetBundleBrowserIsPresent;
        private const string AssetBundleBrowserName = "com.unity.assetbundlebrowser";
        private const string AssetBundleBrowserMenuItem = "Window/AssetBundle Browser";
        public static readonly string AssetBundleBrowserVersion = GetBrowserVersion();

        // Detects AssetBundleBrowser Namespace and the AssetBundleBrowserMain Class
        public static bool BundleBrowserIsPresent()
        {
            if (_assetBundleBrowserIsPresent.HasValue)
            {
                return _assetBundleBrowserIsPresent.Value;
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly == null) continue;
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
            const string pattern = @"AssetBundles-Browser";
            var rx = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var matches = rx.Matches(folderName);
            return matches.Count > 0;
        }

        // Extracts AssetBundleBrowser version name from the Asset Bundle Browser package.json file
        private static string GetBrowserVersion()
        {
            var assetsPath = Application.dataPath;
            var assetBundleBrowserFolderPaths =
                Directory.GetDirectories(assetsPath).ToArray().Where(IsAssetBundleBrowserFolder);

            foreach (var folderPath in assetBundleBrowserFolderPaths)
            {
                var expectedPackageDotJsonPath = Path.Combine(folderPath, "package.json");
                if (!File.Exists(expectedPackageDotJsonPath)) continue;
                var data = File.ReadAllText(expectedPackageDotJsonPath);
                try
                {
                    var json = JsonUtility.FromJson<PackageDotJSsonContents>(data);

                    if (string.Equals(json.name, AssetBundleBrowserName))
                    {
                        return json.version;
                    }
                }
                catch (Exception)
                {
                }
            }
            return "not found";
        }

        // Displays AssetBundleBrowser Window
        public static void DisplayAssetBundleBrowser()
        {
            if (!BundleBrowserIsPresent())
            {
                throw new Exception("Cannot detect Unity Asset Bundle Browser");
            }
            EditorApplication.ExecuteMenuItem(AssetBundleBrowserMenuItem);
        }

        // Allows to create a serializable object from containing only name and version attributes from package.json
        // Suppress warnings about non-initialization of fields
#pragma warning disable CS0649 
        [Serializable]
        private class PackageDotJSsonContents
        {
            public string version;
            public string name;
        }
    }
}