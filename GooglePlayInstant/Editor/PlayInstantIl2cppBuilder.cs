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
        private static string _apkPathName = Path.GetFullPath("base.apk");

        private static ScriptingImplementation _scriptingImplementation =
            PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android);

        private const string SplashScenePath = "play-instant-loading-screen-scene.unity";

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
        /// Determine whether the current project is using IL2CPP as the scripting backend.
        /// </summary>
        public static bool ProjectIsUsingIl2cpp()
        {
            return _scriptingImplementation == ScriptingImplementation.IL2CPP;
        }

        /// <summary>
        /// Refreshe information about the current scripting backend. Useful for detecting changes in scripting backend
        /// information after player settings have changed.
        /// </summary>
        public static void ReloadAndUpdateScriptingBackendInformation()
        {
            _scriptingImplementation = PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android);
        }

        /// <summary>
        /// Build an android apk with quick deploy. Produces a resulting apk that contains the splash scene
        /// and functioanality that will load the game's asset bundle from the cloud atruntime.
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