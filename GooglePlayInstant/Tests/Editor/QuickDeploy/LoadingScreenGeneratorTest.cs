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
using UnityEngine.UI;

namespace GooglePlayInstant.Tests.Editor.QuickDeploy
{
    /// <summary>
    /// Contains unit tests for LoadingScreenGenerator methods.
    /// </summary>
    [TestFixture]
    public class LoadingScreenGeneratorTest
    {
        [Test]
        public void TestAddLoadingScreenScript()
        {
            var loadingScreenGameObject = new GameObject();
            LoadingScreenGenerator.AddLoadingScreenScript(loadingScreenGameObject);
            Assert.IsNotNull(loadingScreenGameObject.GetComponent<LoadingScreenScript>(),
                "A script should be attached to the loading screen object.");
        }

        [Test]
        public void TestAddLoadingScreenImage()
        {
            const string testImage = "example.png";

            File.Create(testImage).Dispose();

            var loadingScreenGameObject = new GameObject();

            LoadingScreenGenerator.AddLoadingScreenImageToScene(loadingScreenGameObject, testImage);

            AssetDatabase.DeleteAsset(testImage);

            Assert.IsNotNull(loadingScreenGameObject.GetComponent<Canvas>(),
                "A canvas component should have been added to the loading screen game object.");
            Assert.IsNotNull(loadingScreenGameObject.GetComponent<Image>(),
                "An image component should have been added to the loading screen game object.");
        }

        [Test]
        public void TestGenerateLoadingScreenConfigFileWithString()
        {
            const string testUrl = "test.co";

            Directory.CreateDirectory(LoadingScreenGenerator.LoadingScreenResourcesPath);

            LoadingScreenGenerator.GenerateLoadingScreenConfigFile(testUrl);

            var loadingScreenJsonPath = Path.Combine(LoadingScreenGenerator.LoadingScreenResourcesPath,
                LoadingScreenGenerator.LoadingScreenJsonFileName);

            var loadingScreenConfigJson =
                AssetDatabase.LoadAssetAtPath(loadingScreenJsonPath, typeof(TextAsset)).ToString();


            var loadingScreenConfig = JsonUtility.FromJson<LoadingScreenConfig>(loadingScreenConfigJson);

            AssetDatabase.DeleteAsset(loadingScreenJsonPath);
            FileUtil.DeleteFileOrDirectory(LoadingScreenGenerator.LoadingScreenResourcesPath);
            FileUtil.DeleteFileOrDirectory(LoadingScreenGenerator.LoadingScreenScenePath);

            Assert.AreEqual(testUrl, loadingScreenConfig.assetBundleUrl,
                string.Format("AssetBundle Url from Config file should be {0}", testUrl));
        }

        [Test]
        public void TestGenerateLoadingScreenConfigFileWithEmptyString()
        {
            Directory.CreateDirectory(LoadingScreenGenerator.LoadingScreenResourcesPath);

            LoadingScreenGenerator.GenerateLoadingScreenConfigFile("");

            var loadingScreenJsonPath = Path.Combine(LoadingScreenGenerator.LoadingScreenResourcesPath,
                LoadingScreenGenerator.LoadingScreenJsonFileName);

            var loadingScreenConfigJson =
                AssetDatabase.LoadAssetAtPath(loadingScreenJsonPath, typeof(TextAsset)).ToString();


            var loadingScreenConfig = JsonUtility.FromJson<LoadingScreenConfig>(loadingScreenConfigJson);

            Assert.IsEmpty(loadingScreenConfig.assetBundleUrl, "AssetBundle Url from Config file should be empty.");

            AssetDatabase.DeleteAsset(loadingScreenJsonPath);
            FileUtil.DeleteFileOrDirectory(LoadingScreenGenerator.LoadingScreenResourcesPath);
            FileUtil.DeleteFileOrDirectory(LoadingScreenGenerator.LoadingScreenScenePath);
        }
    }
}