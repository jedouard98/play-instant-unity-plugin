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
            var inputConfig = new QuickDeployConfig.EditorConfiguration();
            QuickDeployConfig.SaveEditorConfiguration(QuickDeployWindow.ToolBarSelectedButton.CreateBundle, inputConfig,
                TestConfigurationPath);

            var outputConfigurationJson = File.ReadAllText(TestConfigurationPath);
            var outputConfig =
                JsonUtility.FromJson<QuickDeployConfig.EditorConfiguration>(outputConfigurationJson);

            Assert.AreEqual(outputConfig.assetBundleFileName, QuickDeployConfig.AssetBundleFileName);
        }

        [Test]
        public void TestSavingConfigOnDeployBundle()
        {
            var inputConfig = new QuickDeployConfig.EditorConfiguration();
            QuickDeployConfig.SaveEditorConfiguration(QuickDeployWindow.ToolBarSelectedButton.DeployBundle, inputConfig,
                TestConfigurationPath);

            var outputConfigurationJson = File.ReadAllText(TestConfigurationPath);
            var outputConfig =
                JsonUtility.FromJson<QuickDeployConfig.EditorConfiguration>(outputConfigurationJson);

            Assert.AreEqual(outputConfig.cloudCredentialsFileName, QuickDeployConfig.CloudCredentialsFileName);
            Assert.AreEqual(outputConfig.assetBundleFileName, QuickDeployConfig.AssetBundleFileName);
            Assert.AreEqual(outputConfig.cloudStorageBucketName, QuickDeployConfig.CloudStorageBucketName);
            Assert.AreEqual(outputConfig.cloudStorageObjectName, QuickDeployConfig.CloudStorageObjectName);
        }

        [Test]
        public void TestSavingConfigOnLoadingScreen()
        {
            var inputConfig = new LoadingScreenConfig.EngineConfiguration();
            QuickDeployConfig.SaveEngineConfiguration(QuickDeployWindow.ToolBarSelectedButton.LoadingScreen,
                inputConfig, TestConfigurationPath);

            var outputConfigurationJson = File.ReadAllText(TestConfigurationPath);
            var outputConfig =
                JsonUtility.FromJson<LoadingScreenConfig.EngineConfiguration>(outputConfigurationJson);

            Assert.AreEqual(outputConfig.assetBundleUrl, QuickDeployConfig.AssetBundleUrl);
        }

        [Test]
        public void TestLoadingEditorConfiguration()
        {
            var inputConfig = new QuickDeployConfig.EditorConfiguration()
            {
                cloudCredentialsFileName = "testcredentials",
                assetBundleFileName = "testbundle",
                cloudStorageBucketName = "testbucket",
                cloudStorageObjectName = "testobject"
            };

            File.WriteAllText(TestConfigurationPath, JsonUtility.ToJson(inputConfig));

            var outputConfig = QuickDeployConfig.LoadEditorConfiguration(TestConfigurationPath);

            Assert.AreEqual(inputConfig.cloudCredentialsFileName, outputConfig.cloudCredentialsFileName);
            Assert.AreEqual(inputConfig.assetBundleFileName, outputConfig.assetBundleFileName);
            Assert.AreEqual(inputConfig.cloudStorageBucketName, outputConfig.cloudStorageBucketName);
            Assert.AreEqual(inputConfig.cloudStorageObjectName, outputConfig.cloudStorageObjectName);
        }

        [Test]
        public void TestLoadingEngineConfiguration()
        {
            var inputConfig = new LoadingScreenConfig.EngineConfiguration {assetBundleUrl = "testurl"};

            File.WriteAllText(TestConfigurationPath, JsonUtility.ToJson(inputConfig));

            var outputConfig = QuickDeployConfig.LoadEngineConfiguration(TestConfigurationPath);

            Assert.AreEqual(inputConfig.assetBundleUrl, outputConfig.assetBundleUrl);
        }
    }
}