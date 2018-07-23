using System.IO;
using UnityEditor;
using UnityEngine;

namespace GooglePlayInstant.Editor
{
    /// <summary>
    /// A handler for building apks using quick deploy.
    /// </summary>
    public static class QuickDeployBuilder
    {
        private static string _apkPathName = Path.GetFullPath("base.apk");


        /// <summary>
        /// Whether or not the project is using IL2CPP as scripting backend. Ther return value of this is Updated by a
        /// call to Il2cppBuilder.UpdateScriptingBackendInformation
        /// </summary>
        public static bool ProjectIsUsingIl2cpp
        {
            get
            {
                return PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android) == ScriptingImplementation.IL2CPP;
            }
        }

        // TODO(audace): This soon needs to be removed once the changes to config have been approved, then you won't actually be needing this.
        /// <summary>
        /// The specified path name of the target apk to be built.
        /// </summary>
        public static string ApkPathName
        {
            get { return _apkPathName; }
            set
            {
                var fullApkPath = Path.GetFullPath(value);
                if (!Directory.Exists(Path.GetDirectoryName(fullApkPath)))
                {
                    Debug.LogErrorFormat("Invalid File path. Directory \"{0}\" does not exist.",
                        Path.GetDirectoryName(value));
                    return;
                }

                _apkPathName = fullApkPath;
            }
        }

        /// <summary>
        /// Build an android apk with quick deploy. Produces a resulting apk that contains the splash scene
        /// and functionality that will load the game's asset bundle from the cloud at the game's runtime.
        /// Logs success message to the console with built apk's path when building apk is complete,
        /// otherwise logs an error
        /// </summary>
        public static void BuildQuickDeployInstantGameApk()
        {
            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = new[] {PlayInstantLoadingScreenGenerator.LoadingSceneName+".unity"},
                locationPathName = ApkPathName,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };
            if (PlayInstantBuilder.Build(buildPlayerOptions))
            {
                Debug.LogFormat("Apk successfully built at \"{0}\".", ApkPathName);
            }
            else
            {
                Debug.LogError("Couldn't build apk.");
            }
        }
    }
}