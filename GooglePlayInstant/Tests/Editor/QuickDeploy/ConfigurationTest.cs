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

using System.IO;
using GooglePlayInstant.Editor.QuickDeploy;
using GooglePlayInstant.LoadingScreen;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GooglePlayInstant.Tests.Editor.QuickDeploy
{
    public class ConfigurationTest
    {
        private static readonly string TestConfigurationPath =
            Path.Combine("Assets", LoadingScreenConfig.EngineConfigurationFileName);

        // Dispose of temporarily created file.  
        [TearDown]
        public void Cleanup()
        {
            AssetDatabase.DeleteAsset(TestConfigurationPath);
        }
        
        [Test]
        public void TestSavingConfigOnCreateBundle()
        {
            QuickDeployConfig.EditorConfiguration inputConfig = new QuickDeployConfig.EditorConfiguration();
            QuickDeployConfig.SaveEditorConfiguration(QuickDeployWindow.ToolBarSelectedButton.CreateBundle, inputConfig, TestConfigurationPath);
            
            var outputConfigurationJson = File.ReadAllText(TestConfigurationPath);
            QuickDeployConfig.EditorConfiguration outputConfig = JsonUtility.FromJson<QuickDeployConfig.EditorConfiguration>(outputConfigurationJson);
            
            Assert.AreEqual(outputConfig.assetBundleFileName, QuickDeployConfig.AssetBundleFileName);
        }
        
        [Test]
        public void TestSavingConfigOnDeployBundle()
        {
            QuickDeployConfig.EditorConfiguration inputConfig = new QuickDeployConfig.EditorConfiguration();
            QuickDeployConfig.SaveEditorConfiguration(QuickDeployWindow.ToolBarSelectedButton.DeployBundle, inputConfig, TestConfigurationPath);
            
            var outputConfigurationJson = File.ReadAllText(TestConfigurationPath);
            QuickDeployConfig.EditorConfiguration outputConfig = JsonUtility.FromJson<QuickDeployConfig.EditorConfiguration>(outputConfigurationJson);
            
            Assert.AreEqual(outputConfig.cloudCredentialsFileName, QuickDeployConfig.CloudCredentialsFileName);
            Assert.AreEqual(outputConfig.assetBundleFileName, QuickDeployConfig.AssetBundleFileName);
            Assert.AreEqual(outputConfig.cloudStorageBucketName, QuickDeployConfig.CloudStorageBucketName);
            Assert.AreEqual(outputConfig.cloudStorageObjectName, QuickDeployConfig.CloudStorageObjectName);
        }
        
        [Test]
        public void TestSavingConfigOnLoadingScreen()
        {
            LoadingScreenConfig.EngineConfiguration inputConfig = new LoadingScreenConfig.EngineConfiguration();
            QuickDeployConfig.SaveEngineConfiguration(QuickDeployWindow.ToolBarSelectedButton.LoadingScreen, inputConfig, TestConfigurationPath);
            
            var outputConfigurationJson = File.ReadAllText(TestConfigurationPath);
            LoadingScreenConfig.EngineConfiguration outputConfig = JsonUtility.FromJson<LoadingScreenConfig.EngineConfiguration>(outputConfigurationJson);
            
            Assert.AreEqual(outputConfig.assetBundleUrl, QuickDeployConfig.AssetBundleUrl);
        }
    }
}