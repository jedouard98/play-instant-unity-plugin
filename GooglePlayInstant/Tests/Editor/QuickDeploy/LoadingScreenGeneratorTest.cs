using System;
using System.Collections.Generic;
using System.IO;
using GooglePlayInstant.Editor.QuickDeploy;
using GooglePlayInstant.LoadingScreen;
using NUnit.Framework;
using UnityEngine;

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

        [Test]
        public void TestGenerateLoadingScreenConfigFile()
        {
            var loadingScreeGameObject = new GameObject(LoadingScreenGenerator.LoadingScreenCanvasName);
            LoadingScreenGenerator.GenerateLoadingScreenConfigFile("test.co");
            
            

        }
    }
}