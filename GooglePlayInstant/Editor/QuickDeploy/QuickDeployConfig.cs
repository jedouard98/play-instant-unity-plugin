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
using UnityEditor.VersionControl;
using UnityEngine;

namespace GooglePlayInstant.Editor.QuickDeploy
{
    /// <summary>
    /// Contains set of operations for storing and retrieving quick deploy configurations.
    /// </summary>
    public static class QuickDeployConfig
    {
        private static readonly string ConfigurationFilePath =
            Path.Combine("Library", "PlayInstantQuickDeployConfig.json");

        /// <summary>
        /// The Configuration singleton that should be used to read and modify Quick Deploy configuration.
        /// Modified values are persisted by calling SaveConfiguration.
        /// </summary>
        private static readonly Configuration _config = LoadConfiguration();
        
        public static string CloudCredentialsFileName;
        public static string AssetBundleFileName;
        public static string CloudStorageBucketName;
        public static string CloudStorageObjectName;
        public static string AssetBundleUrl;
        public static string ApkFileName;


        public static void SaveConfiguration(QuickDeployWindow.ToolBarSelectedButton currentTab)
        {
            switch (currentTab)
            {
                case QuickDeployWindow.ToolBarSelectedButton.DeployBundle:
                    _config.cloudCredentialsFileName = CloudCredentialsFileName;
                    _config.assetBundleFileName = AssetBundleFileName;
                    _config.cloudStorageBucketName = CloudStorageBucketName;
                    _config.cloudStorageObjectName = CloudStorageObjectName;
                    break;
                case QuickDeployWindow.ToolBarSelectedButton.LoadingScreen:
                    _config.assetBundleUrl = AssetBundleUrl;
                    break;
                case QuickDeployWindow.ToolBarSelectedButton.Build:
                    _config.apkFileName = ApkFileName;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("currentTab", currentTab, "Can't save from this tab.");
            }

            File.WriteAllText(ConfigurationFilePath, JsonUtility.ToJson(_config));
        }

        private static Configuration LoadConfiguration()
        {
            if (!File.Exists(ConfigurationFilePath))
            {
                return new Configuration();
            }

            var configurationJson = File.ReadAllText(ConfigurationFilePath);
            return JsonUtility.FromJson<Configuration>(configurationJson);
        }

        /// <summary>
        /// Represents the contents of the quick deploy configuration file.
        /// </summary>
        [Serializable]
        private class Configuration
        {
            public string cloudCredentialsFileName;
            public string assetBundleFileName;
            public string cloudStorageBucketName;
            public string cloudStorageObjectName;
            public string assetBundleUrl;
            public string apkFileName;
        }
    }
}