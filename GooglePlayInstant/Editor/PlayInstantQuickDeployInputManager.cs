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

using UnityEditor;

namespace GooglePlayInstant.Editor
{
    /// <summary>
    /// Provides functionality for reading, storing and retrieving text field values from "Deploy Bundle" section.
    /// </summary>
    public static class DeployBundleTextFields
    {
        private const string DataStoragePrefix = "GooglePlayInstant.Editor.QuickDeploy.";
        private static string _oauth2CredentialsPath;
        private static string _localAssetBundlePath;
        private static string _remoteBucketName;
        private static string _remoteObjectName;

        public static string Oauth2CredentialsPath
        {
            get
            {
                if (_oauth2CredentialsPath != null)
                {
                    return _oauth2CredentialsPath;
                }

                UpdateFieldFromPrefs(ref _oauth2CredentialsPath, "oauth2CredentialsPath");
                return _oauth2CredentialsPath;
            }
            set
            {
                if (string.Equals(_oauth2CredentialsPath, value))
                {
                    return;
                }

                _oauth2CredentialsPath = value;
                UpdatePrefs("oauth2CredentialsPath", value);
            }
        }

        public static string LocalAssetBundlePath
        {
            get
            {
                if (_localAssetBundlePath != null)
                {
                    return _localAssetBundlePath;
                }

                UpdateFieldFromPrefs(ref _localAssetBundlePath, "localAssetBundlePath");
                return _localAssetBundlePath;
            }
            set
            {
                if (string.Equals(_localAssetBundlePath, value))
                {
                    return;
                }

                _localAssetBundlePath = value;
                UpdatePrefs("localAssetBundlePath", value);
            }
        }

        public static string RemoteBucketName
        {
            get
            {
                if (_remoteBucketName != null)
                {
                    return _remoteBucketName;
                }

                UpdateFieldFromPrefs(ref _remoteBucketName, "remoteBucketName");
                return _remoteBucketName;
            }
            set
            {
                if (string.Equals(_remoteBucketName, value))
                {
                    return;
                }

                _remoteBucketName = value;
                UpdatePrefs("remoteBucketName", value);
            }
        }

        public static string RemoteObjectName
        {
            get
            {
                if (_remoteObjectName != null)
                {
                    return _remoteObjectName;
                }

                UpdateFieldFromPrefs(ref _remoteObjectName, "remoteObjectName");
                return _remoteObjectName;
            }
            set
            {
                if (string.Equals(_remoteObjectName, value))
                {
                    return;
                }

                _remoteObjectName = value;
                UpdatePrefs("remoteObjectName", value);
            }
        }

        // Set DataStoragePrefix + key to value in EditorPrefs if value is not empty or null. Otherwise remove key.
        private static void UpdatePrefs(string key, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                EditorPrefs.DeleteKey(DataStoragePrefix + key);
            }
            else
            {
                EditorPrefs.SetString(DataStoragePrefix + key, value);
            }
        }

        // Set field to key's associated value from EditorPrefs. Set field to "" if key not present.
        private static void UpdateFieldFromPrefs(ref string field, string key)
        {
            field = EditorPrefs.HasKey(DataStoragePrefix + key) ? EditorPrefs.GetString(DataStoragePrefix + key) : "";
        }
    }
}