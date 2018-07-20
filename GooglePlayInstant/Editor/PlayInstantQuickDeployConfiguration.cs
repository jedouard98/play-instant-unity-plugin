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

using System;
using System.IO;
using UnityEngine;

namespace GooglePlayInstant.Editor
{
    public static class PlayInstantQuickDeployConfiguration
    {
        private static readonly string ConfigurationFilePath =
            Path.Combine("Library", "PlayInstantQuickDeployConfig.json");

        public static readonly Configuration Config = LoadConfiguration();

        public static void SaveConfiguration()
        {
            Debug.Log("I ran.");
            File.WriteAllText(ConfigurationFilePath, JsonUtility.ToJson(Config));
        }

        private static Configuration LoadConfiguration()
        {
            if (!File.Exists(ConfigurationFilePath))
            {
                return new Configuration();
            }

            var configurationJson = File.ReadAllText(ConfigurationFilePath);
            return JsonUtility.FromJson<Configuration>(configurationJson);
        }

        [Serializable]
        public class Configuration
        {
            public string assetbundleFileName;
            public string cloudStorageBucketName;
            public string cloudStorageFileName;
            public string cloudCredentials;
            public string assetBundleUrl;
            public string loadingScreenImageFileName;
            public string APKFileName;
        }
    }
}