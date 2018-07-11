using System;
using System.Text;
using Google.Cloud.Storage.V1;
using Object = Google.Apis.Storage.v1.Data.Object;
using Google.Apis.Storage.v1.Data;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using Google.Apis.Storage.v1;
using Debug = UnityEngine.Debug;

namespace GooglePlayInstant.Deployer
{
    public static class CloudClient
    {
        private static void UpdateEnvironmentVariables()
        {
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", DeveloperFieldInputs.CredentialsPath);
        }

        // TODO(audace): Take this out before pushing
        private static bool AlwaysTrue()
        {
            return true;
        }

        // Upload the currently selected bundle to the cloud
        public static void DeployBundle()
        {
            Debug.Log("Starting deploy process");

            UpdateEnvironmentVariables();
            CreateBucketIfNotExists(DeveloperFieldInputs.RemoteBucketName);
            UploadFile(DeveloperFieldInputs.RemoteBucketName, DeveloperFieldInputs.LocalAssetBundlePath, DeveloperFieldInputs.RemoteObjectName);
            MakePublic(DeveloperFieldInputs.RemoteBucketName, DeveloperFieldInputs.RemoteObjectName);
            Debug.Log($"AssetBundle deployer to the cloud as {DeveloperFieldInputs.RemoteObjectName}");
        }

        // Create a bucket, throw an error if the bucket already exists
        private static void CreateBucketIfNotExists(string bucketName)
        {
            string projectId = DeveloperFieldInputs.RemoteProjectId;
            StorageClient storageClient = StorageClient.Create();

            try
            {
                storageClient.CreateBucket(projectId, bucketName);
            }
            catch (Google.GoogleApiException e)
                when (e.Error.Code == 409)
            {
                // bucket already exists.
                Debug.Log($"The bucket {bucketName} already esists");
            }
            catch (Google.GoogleApiException e)
                when (e.Error.Code == 400)
            {
                // Bad request
                Debug.Log($"Got an error {e.Error.Code.ToString()} {e.Error.Message}");
            }

            catch (Google.GoogleApiException e)
            when (e.Error.Code == 401)
            {
                // Unauthorized request
                Debug.Log($"Got an error {e.Error.Code.ToString()} {e.Error.Message}");
            }

        }
        
        
        // Uploads a new object to the bucket
        private static void UploadFile(string bucketName, string localPath, string objectName = null)
        {
            var storage = StorageClient.Create();
            using (var f = File.OpenRead(localPath))
            {
                objectName = objectName ?? Path.GetFileName(localPath);
                storage.UploadObject(bucketName, objectName, null, f);
                Debug.Log($"Uploaded {objectName}");
            }
        }
        
        // Makes an object in the cloud plublic
        private static void MakePublic(string bucketName, string objectName)
        {
            var storage = StorageClient.Create();
            var storageObject = storage.GetObject(bucketName, objectName);
            storageObject.Acl = storageObject.Acl ?? new List<ObjectAccessControl>();
            storage.UpdateObject(storageObject, new UpdateObjectOptions
            {
                PredefinedAcl = PredefinedObjectAcl.PublicRead
            });
            Debug.Log($"{objectName} is now public and can now be fetched from {storageObject.MediaLink}");
        }
    }
}