using System.IO;
using UnityEditor;
using UnityEngine;

namespace GooglePlayInstant.Editor
{
    /// <summary>
    /// A handler for building an apk using quick deploy.
    /// </summary>
    public static class Il2cppBuilder
    {
        private const string SplashScenePath = "play-instant-loading-screen-scene.unity";
        private static string _apkPathName = Path.GetFullPath("base.apk");

        private static bool _projectIsUsingIl2Cpp =
            PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android) == ScriptingImplementation.IL2CPP;

        /// <summary>
        /// Whether or not the project is using IL2CPP as scripting backend. Ther return value of this is Updated by a
        /// call to Il2cppBuilder.UpdateScriptingBackendInformation
        /// </summary>
        public static bool ProjectIsUsingIl2cpp
        {
            get { return _projectIsUsingIl2Cpp; }
        }

        /// <summary>
        /// The specified path name of the target apk to be built.
        /// </summary>
        public static string ApkPathName
        {
            get { return _apkPathName; }
            set
            {
                if (!Directory.Exists(Path.GetDirectoryName(Path.GetFullPath(value))))
                {
                    Debug.LogErrorFormat("Invalid File path. Directory {0} does not exist",
                        Path.GetDirectoryName(value));
                    return;
                }

                _apkPathName = Path.GetFullPath(value);
            }
        }

        /// <summary>
        /// Reloads information about the current scripting backend. Useful for detecting changes in scripting backend
        /// information after player settings have changed.
        /// </summary>
        public static void UpdateScriptingBackendInformation()
        {
            _projectIsUsingIl2Cpp = PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android) ==
                                    ScriptingImplementation.IL2CPP;
        }

        /// <summary>
        /// Build an android apk with quick deploy. Produces a resulting apk that contains the splash scene
        /// and functionality that will load the game's asset bundle from the cloud at runtime.
        /// Logs success message to the console with built apk's path when building apk is complete.
        /// </summary>
        public static void BuildQuickDeployInstantGameApk()
        {
            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = new[] {SplashScenePath},
                locationPathName = ApkPathName,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            BuildPipeline.BuildPlayer(buildPlayerOptions);
            Debug.Log("Apk successfuly built at " + ApkPathName);
        }
    }
}