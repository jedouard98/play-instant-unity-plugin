using System.Collections;
using System.IO;
using GooglePlayInstant.Editor.QuickDeploy;
using GooglePlayInstant.LoadingScreen;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace GooglePlayInstant.Tests.Editor.QuickDeploy
{
    /// <summary>
    /// Contains tests for LoadingScreenGenerator methods.
    /// </summary>
    [TestFixture]
    public class LoadingScreenGeneratorTest
    {
        [Test]
        public void TestAddLoadingScreenScript()
        {
            var loadingScreeGameObject = new GameObject(LoadingScreenGenerator.LoadingScreenCanvasName);
            LoadingScreenGenerator.AddLoadingScreenScript(loadingScreeGameObject);
            Assert.IsNotNull(loadingScreeGameObject.GetComponent<LoadingScreenScript>(),
                "A script should be attached to the loading screen object.");
        }

        [Test]
        public void TestAddLoadingScreenImage()
        {
        }

        [UnityTest]
        public IEnumerator TestGenerateLoadingScreenConfigFile()
        {
            LoadingScreenGenerator.GenerateLoadingScreenConfigFile("test.co");
            
            var locationOfAsset = Path.Combine(LoadingScreenGenerator.LoadingScreenResourcesPath, LoadingScreenGenerator.LoadingScreenJsonFileName);

            while (!File.Exists(locationOfAsset))
            {
                yield return null;
            }

            var loadingScreenConfigJson = AssetDatabase.LoadAssetAtPath(locationOfAsset, typeof(TextAsset)).ToString();

            
            var loadingScreenConfig = JsonUtility.FromJson<LoadingScreenConfig>(loadingScreenConfigJson);

            Assert.AreEqual("test.co", loadingScreenConfig.assetBundleUrl);

            AssetDatabase.DeleteAsset(locationOfAsset);
            FileUtil.DeleteFileOrDirectory(LoadingScreenGenerator.LoadingScreenResourcesPath);
            FileUtil.DeleteFileOrDirectory(LoadingScreenGenerator.LoadingScreenScenePath);
            
        }
    }
}