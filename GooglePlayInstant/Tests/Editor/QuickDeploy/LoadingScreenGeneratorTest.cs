using System.Collections;
using System.IO;
using GooglePlayInstant.Editor.QuickDeploy;
using GooglePlayInstant.LoadingScreen;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
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
                "A canvas component should have been added.");
            Assert.IsNotNull(loadingScreenGameObject.GetComponent<Image>(),
                "An image component should have been added.");
        }

        [UnityTest]
        public IEnumerator TestGenerateLoadingScreenConfigFileWithString()
        {
            const string testUrl = "test.co";

            Directory.CreateDirectory(LoadingScreenGenerator.LoadingScreenResourcesPath);

            LoadingScreenGenerator.GenerateLoadingScreenConfigFile(testUrl);

            var loadingScreenJsonPath = Path.Combine(LoadingScreenGenerator.LoadingScreenResourcesPath,
                LoadingScreenGenerator.LoadingScreenJsonFileName);

            while (!File.Exists(loadingScreenJsonPath))
            {
                yield return null;
            }

            var loadingScreenConfigJson =
                AssetDatabase.LoadAssetAtPath(loadingScreenJsonPath, typeof(TextAsset)).ToString();


            var loadingScreenConfig = JsonUtility.FromJson<LoadingScreenConfig>(loadingScreenConfigJson);

            AssetDatabase.DeleteAsset(loadingScreenJsonPath);
            FileUtil.DeleteFileOrDirectory(LoadingScreenGenerator.LoadingScreenResourcesPath);
            FileUtil.DeleteFileOrDirectory(LoadingScreenGenerator.LoadingScreenScenePath);

            Assert.AreEqual(testUrl, loadingScreenConfig.assetBundleUrl);
        }

        [UnityTest]
        public IEnumerator TestGenerateLoadingScreenConfigFileWithEmptyString()
        {
            Directory.CreateDirectory(LoadingScreenGenerator.LoadingScreenResourcesPath);

            LoadingScreenGenerator.GenerateLoadingScreenConfigFile("");

            var loadingScreenJsonPath = Path.Combine(LoadingScreenGenerator.LoadingScreenResourcesPath,
                LoadingScreenGenerator.LoadingScreenJsonFileName);

            while (!File.Exists(loadingScreenJsonPath))
            {
                yield return null;
            }

            var loadingScreenConfigJson =
                AssetDatabase.LoadAssetAtPath(loadingScreenJsonPath, typeof(TextAsset)).ToString();


            var loadingScreenConfig = JsonUtility.FromJson<LoadingScreenConfig>(loadingScreenConfigJson);

            Assert.AreEqual("", loadingScreenConfig.assetBundleUrl);

            AssetDatabase.DeleteAsset(loadingScreenJsonPath);
            FileUtil.DeleteFileOrDirectory(LoadingScreenGenerator.LoadingScreenResourcesPath);
            FileUtil.DeleteFileOrDirectory(LoadingScreenGenerator.LoadingScreenScenePath);
        }
    }
}