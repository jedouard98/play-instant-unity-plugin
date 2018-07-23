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
        /// <summary>
        /// Determine whether or not the project is using IL2CPP as scripting backend.
        /// </summary>
        public static bool ProjectIsUsingIl2cpp()
        {
            return PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android) == ScriptingImplementation.IL2CPP;
        }

        /// <summary>
        /// Build an android apk with quick deploy. Produces a resulting apk that contains the splash scene
        /// and functionality that will load the game's asset bundle from the cloud at the game's runtime.
        /// Logs success message to the console with built apk's path when the apk is successfully built, otherwise logs
        /// error message.
        /// </summary>
        public static void BuildQuickDeployInstantGameApk()
        {
            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = new[] {PlayInstantLoadingScreenGenerator.LoadingSceneName + ".unity"},
                locationPathName = QuickDeployConfig.Config.apkFileName,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };
            if (PlayInstantBuilder.Build(buildPlayerOptions))
            {
                Debug.LogFormat("Apk successfully built at \"{0}\".", buildPlayerOptions.locationPathName);
            }
            else
            {
                Debug.LogError("Couldn't build apk.");
            }
        }
    }
}