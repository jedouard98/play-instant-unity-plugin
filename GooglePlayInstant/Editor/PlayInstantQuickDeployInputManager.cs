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
    /// <summary>
    /// Provides functionality for reading, storing and retrieving text field values from "Deploy Bundle" section.
    /// </summary>
    public static class DeployBundleTextFields
    {
        private const string dataStorageFilePath =".google_play_instant_quick_deploy_inputs.json";

        private static string _oauth2CredentialsPath;
        private static string _localAssetBundlePath;
        private static string _remoteBucketName;
        private static string _remoteObjectName;

        public static string Oauth2CredentialsPath
        {
            get { return getUpdatedValue(ref _oauth2CredentialsPath); }
            set { updateNewValue(ref _oauth2CredentialsPath, value); }
        }

        public static string LocalAssetBundlePath
        {
            get { return getUpdatedValue(ref _localAssetBundlePath); }
            set { updateNewValue(ref _localAssetBundlePath, value); }
        }

        public static string RemoteBucketName
        {
            get { return getUpdatedValue(ref _remoteBucketName); }
            set { updateNewValue(ref _remoteBucketName, value); }
        }

        public static string RemoteObjectName
        {
            get { return getUpdatedValue(ref _remoteObjectName); }
            set { updateNewValue(ref _remoteObjectName, value); }
        }


        private static string getUpdatedValue(ref string field)
        {
            if (string.Equals(field, null))
            {
                RetrieveTextFieldInputs();
            }

            return field;
        }

        // Do nothing if value has not changed, otherwise update the value of the static field and write to file.
        private static void updateNewValue(ref string field, string value)
        {
            if (string.Equals(field, value))
            {
                return;
            }

            field = value;
            StoreTextFieldInputs();
        }

        private static void StoreTextFieldInputs()
        {
            var fieldsData = new TextFieldInputFileContent
            {
                oauth2CredentialsPath = _oauth2CredentialsPath?? "path_to_google_cloud_project_oauth2_credentials",
                localAssetBundlePath = _localAssetBundlePath ?? "local_path_to_assetbundle",
                remoteBucketName = _remoteBucketName ?? "remote_bucket_name",
                remoteObjectName = _remoteObjectName ?? "remote_object_name",
            };
            File.WriteAllText(dataStorageFilePath, JsonUtility.ToJson(fieldsData));
        }

        private static void RetrieveTextFieldInputs()
        {
            if (!File.Exists(dataStorageFilePath))
            {
                StoreTextFieldInputs();
            }

            var fieldInputObject =
                JsonUtility.FromJson<TextFieldInputFileContent>(File.ReadAllText(dataStorageFilePath));
            _oauth2CredentialsPath = fieldInputObject.oauth2CredentialsPath;
            _localAssetBundlePath = fieldInputObject.localAssetBundlePath;
            _remoteBucketName = fieldInputObject.remoteBucketName;
            _remoteObjectName = fieldInputObject.remoteObjectName;
        }
        
        /// <summary>
        /// Represents the contents of the file used to store text field inputs.
        /// </summary>
        [Serializable]
        private class TextFieldInputFileContent
        {
            public string oauth2CredentialsPath;
            public string localAssetBundlePath;
            public string remoteBucketName;
            public string remoteObjectName;
        }
    }
}