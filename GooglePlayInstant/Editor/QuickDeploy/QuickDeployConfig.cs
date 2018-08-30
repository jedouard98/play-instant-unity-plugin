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
using GooglePlayInstant.LoadingScreen;
using UnityEngine;

namespace GooglePlayInstant.Editor.QuickDeploy
{
    /// <summary>
    /// Contains a set of operations for storing and retrieving quick deploy configurations.
    /// </summary>
    public static class QuickDeployConfig
    {
        private static readonly string EditorConfigurationFilePath =
            Path.Combine("Library", "PlayInstantQuickDeployEditorConfig.json");

        private static readonly string ResourcesDirectoryPath = Path.Combine(LoadingScreenGenerator.SceneDirectoryPath, "Resources");

        private static readonly string EngineConfigurationFilePath =
            Path.Combine(ResourcesDirectoryPath, LoadingScreenConfig.EngineConfigurationFileName);

        /// <summary>
        /// The Configuration singleton that should be used to read and modify Quick Deploy configuration.
        /// Modified values are persisted by calling SaveConfiguration.
        /// </summary>
        private static readonly EditorConfiguration EditorConfig = LoadEditorConfiguration();

        private static readonly LoadingScreenConfig.EngineConfiguration EngineConfig = LoadEngineConfiguration();

        public static string CloudCredentialsFileName = EditorConfig.cloudCredentialsFileName;
        public static string AssetBundleFileName = EditorConfig.assetBundleFileName;
        public static string CloudStorageBucketName = EditorConfig.cloudStorageBucketName;
        public static string CloudStorageObjectName = EditorConfig.cloudStorageObjectName;
        public static string AssetBundleUrl = EngineConfig.assetBundleUrl;

        /// <summary>
        /// Store configuration from the current quick deploy tab to persistent storage.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if tab shouldn't have input fields.</exception>
        public static void SaveConfiguration(QuickDeployWindow.ToolBarSelectedButton currentTab)
        {
            switch (currentTab)
            {
                case QuickDeployWindow.ToolBarSelectedButton.CreateBundle:
                    SaveEditorConfiguration(QuickDeployWindow.ToolBarSelectedButton.CreateBundle);
                    break;
                case QuickDeployWindow.ToolBarSelectedButton.DeployBundle:
                    SaveEditorConfiguration(QuickDeployWindow.ToolBarSelectedButton.DeployBundle);
                    break;
                case QuickDeployWindow.ToolBarSelectedButton.LoadingScreen:
                    SaveEngineConfiguration(QuickDeployWindow.ToolBarSelectedButton.LoadingScreen);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("currentTab", currentTab, "Can't save from this tab.");
            }

            // Shouldn't hurt to write to persistent storage as long as SaveConfiguration(currentTab) is only called
            // when a major action happens.
            File.WriteAllText(EngineConfigurationFilePath, JsonUtility.ToJson(EngineConfig));
        }

        /// <summary>
        /// Store configuration from the current quick deploy tab to persistent storage.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if tab shouldn't have input fields.</exception>
        private static void SaveEditorConfiguration(QuickDeployWindow.ToolBarSelectedButton currentTab)
        {
            switch (currentTab)
            {
                case QuickDeployWindow.ToolBarSelectedButton.CreateBundle:
                    EditorConfig.assetBundleFileName = AssetBundleFileName;
                    break;
                case QuickDeployWindow.ToolBarSelectedButton.DeployBundle:
                    EditorConfig.cloudCredentialsFileName = CloudCredentialsFileName;
                    EditorConfig.assetBundleFileName = AssetBundleFileName;
                    EditorConfig.cloudStorageBucketName = CloudStorageBucketName;
                    EditorConfig.cloudStorageObjectName = CloudStorageObjectName;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("currentTab", currentTab, "Can't save editor configurations " +
                                                                                    "from this tab.");
            }
        }

        /// <summary>
        /// De-serialize configuration file contents into Configuration instance if the file exists exists, otherwise
        /// return Configuration instance with empty fields.
        /// </summary>
        private static EditorConfiguration LoadEditorConfiguration()
        {
            if (!File.Exists(EditorConfigurationFilePath))
            {
                return new EditorConfiguration();
            }

            var configurationJson = File.ReadAllText(EditorConfigurationFilePath);
            return JsonUtility.FromJson<EditorConfiguration>(configurationJson);
        }

        /// <summary>
        /// Store configuration from the current quick deploy tab to persistent storage.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if tab shouldn't have input fields.</exception>
        private static void SaveEngineConfiguration(QuickDeployWindow.ToolBarSelectedButton currentTab)
        {
            switch (currentTab)
            {
                case QuickDeployWindow.ToolBarSelectedButton.LoadingScreen:
                    EngineConfig.assetBundleUrl = AssetBundleUrl;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("currentTab", currentTab, "Can't save engine configurations " + 
                                                                                    "from this tab.");
            }

            // Shouldn't hurt to write to persistent storage as long as SaveConfiguration(currentTab) is only called
            // when a major action happens.
            File.WriteAllText(EngineConfigurationFilePath, JsonUtility.ToJson(EngineConfig));
        }

        /// <summary>
        /// De-serialize configuration file contents into Configuration instance if the file exists exists, otherwise
        /// return Configuration instance with empty fields.
        /// </summary>
        private static LoadingScreenConfig.EngineConfiguration LoadEngineConfiguration()
        {
            if (!File.Exists(EngineConfigurationFilePath))
            {
                return new LoadingScreenConfig.EngineConfiguration();
            }

            var configurationJson = File.ReadAllText(EngineConfigurationFilePath);
            return JsonUtility.FromJson<LoadingScreenConfig.EngineConfiguration>(configurationJson);
        }
    }
}