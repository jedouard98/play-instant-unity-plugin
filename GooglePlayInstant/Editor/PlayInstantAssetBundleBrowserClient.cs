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
using UnityEditor;
using UnityEngine;


namespace GooglePlayInstant.Editor
{
    public static class AssetBundleBrowserClient
    {
        private const string AssetBundleBrowserMenuItem = "Window/AssetBundle Browser";
        private static bool? _assetBundleBrowserIsPresent;


        // Detects AssetBundleBrowser Namespace and the AssetBundleBrowserMain Class
        public static bool BundleBrowserIsPresent()
        {
            if (_assetBundleBrowserIsPresent.HasValue) return _assetBundleBrowserIsPresent.Value;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly == null) continue;
                foreach (var type in assembly.GetTypes())
                {
                    // Look for AssetBundleBrowserMain in the AssetBundleBrowser Namespace
                    if (type != null && type.Namespace != null && type.Namespace.Equals("AssetBundleBrowser") &&
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

        public static string BundleBrowserVersion => GetBrowserVersion(GetAssetBundlesBrowserPackageDotSONFiles()); 
        

        // Get package.json files related to AssetBundles Browser
        private static string[] GetAssetBundlesBrowserPackageDotSONFiles()
        {
            var assetsPath = Application.dataPath;
            var files = Directory.GetFiles(assetsPath, "package.json", SearchOption.AllDirectories);
            return files.Where(f => f.Contains("AssetBundle") && f.Contains("Browser")).ToArray();
        }

        // Extract the version from the package.json.
        // Goes line by line since there is no guarantee that any given file will be a valid well formatted JSON file
        private static string GetBrowserVersion(string[] filePaths)
        {
            foreach (var filePath in filePaths)
            {
                var allText = File.ReadAllText(filePath);
                if (!allText.ToLower().Contains("\"name\": \"com.unity.assetbundlebrowser\"")) continue;
                var fileStream = new StreamReader(filePath);
                const string versionMatcher = "version\":";
                string line;
                while ((line = fileStream.ReadLine()) != null)
                {
                    if (line.Contains(versionMatcher))
                    {
                        var versionBeginIndex = line.IndexOf(versionMatcher) + versionMatcher.Length;
                        var version = line.Substring(versionBeginIndex);
                        if (!version.Trim().Equals(""))
                        {
                            return version.Trim().Replace(",", "");
                        }
                    }
                }
            }

            return "not found";
        }
        
        // Displays AssetBundleBrowser
        public static void DisplayAssetBundleBrowser()
        {
            if (!BundleBrowserIsPresent())
            {
                throw new Exception("Cannot detect AssetBundleBrowser Context");
            }
            EditorApplication.ExecuteMenuItem(AssetBundleBrowserMenuItem);
        }
    }
}