using System;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace GooglePlayInstant.Editor
{
    public static class Il2cppBuilder
    {
        public static void BuildIl2cppApk()
        {
            
            
            #if UNITY_EDITOR
            var backend = PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android);
            if (backend != ScriptingImplementation.IL2CPP)
            {
                Debug.LogError("If the scripting backend is not IL2CPP, There may be problems");
            }
            #endif
            
            Debug.Log("Going to buid IL2CPP APK");
            // Get filename.
            string path =  EditorUtility.SaveFilePanel("Coose file name", Application.dataPath, "myotherapk", "apk");
            string[] levels = new string[] {"Assets/Scenes/SampleScene.unity"};


            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();

            buildPlayerOptions.scenes = levels;
            buildPlayerOptions.locationPathName = path;
            buildPlayerOptions.target = BuildTarget.Android;
            buildPlayerOptions.options = BuildOptions.None;
            BuildPipeline.BuildPlayer(buildPlayerOptions);
            
        }
    }
}