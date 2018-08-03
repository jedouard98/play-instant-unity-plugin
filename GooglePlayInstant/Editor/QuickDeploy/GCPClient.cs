using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace GooglePlayInstant.Editor.QuickDeploy
{
    abstract class GCPClient
    {
        private static QuickDeployConfig.Configuration _config = QuickDeployConfig.Config;

        private delegate void WwwHandler(WWW request);

        /// <summary>
        /// Creates bucket if not exists, and uploads asset bundle file to the cloud.
        /// </summary>
        public static void CreateBucketIfNotExistsAndUploadBundle()
        {
            
            if (AccessTokenGetter.AccessToken == null)
            {
                AccessTokenGetter.GetAuthCode(code =>
                {
                    AccessTokenGetter.GetAccessToken(code, token =>
                    {
                        CreateBucketIfNotExistsAndUploadBundle();
                    });
                });
                return;
            }
            
            Debug.Log("Starting now");

            Debug.Log("Access token expires in "+AccessTokenGetter.AccessToken.expires_in);
            CheckWhetherBucketExists(_config.cloudStorageBucketName,
                bucketExistsResponse => { UploadBundle(resp => { MakeBundlePublic(www => { }); }); },
                bucketNotFoundResponse =>
                {
                    CreateBucket(_config.cloudStorageBucketName,
                        bucketCreationResponse => { UploadBundle(resp => {MakeBundlePublic(www => { }); }); });
                });
        }

        private static void UploadBundle(WwwHandler responseHandler)
        {
            Debug.Log("going to upload bundle");
            var assetBundleFileName = _config.assetBundleFileName;
            var cloudStorageBucketName = _config.cloudStorageBucketName;
            var cloudStorageFileName = _config.cloudStorageFileName;
            var uploadEndpoint =
                string.Format("https://www.googleapis.com/upload/storage/v1/b/{0}/o?uploadType=media&name={1}",
                    cloudStorageBucketName, cloudStorageFileName);


            var bytes = File.ReadAllBytes(assetBundleFileName);
            var headers = new Dictionary<string, string>();
            headers.Add("Authorization", string.Format("Bearer {0}", AccessTokenGetter.AccessToken.access_token));
            var request = HttpRequestHelper.SendHttpPostRequest(uploadEndpoint, bytes, headers);
            WwwRequestInProgress.TrackProgress(request,
                "Uploading asset bundle to google cloud storage",
                www =>
                {
                    if (responseHandler != null)
                    {
                        responseHandler.Invoke(www);
                    }
                });
        }

        // Creates a bucket with the given bucket name. Assumed TokenUtility has a valid access token and that the bucket
        // currently does not exist
        private static void CreateBucket(string bucketName, WwwHandler resultHandler)
        {
            var credentials = GCPClientHelper.GetOauth2Credentials();
            var createBucketEndPoint = string.Format("https://www.googleapis.com/storage/v1/b?project={0}",
                credentials.project_id);
            var jsonContents = JsonUtility.ToJson(new CreateBucketRequest
            {
                name = bucketName
            });

            var jsonBytes = Encoding.UTF8.GetBytes(jsonContents);
            var headers = new Dictionary<string, string>();
            headers.Add("Authorization", string.Format("Bearer {0}", AccessTokenGetter.AccessToken.access_token));
            headers.Add("Content-Type", "application/json");
            var request = HttpRequestHelper.SendHttpPostRequest(createBucketEndPoint, jsonBytes, headers);

            WwwRequestInProgress.TrackProgress(request,
                string.Format("Creating bucket with name \"{0}\"", bucketName),
                wwwResult =>
                {
                    if (resultHandler != null)
                    {
                        resultHandler.Invoke(wwwResult);
                    }
                });
        }

        private static void MakeBundlePublic(WwwHandler resultHandler)
        {
            var bucketName = _config.cloudStorageBucketName;
            var remoteAssetBundleName = _config.cloudStorageFileName;
            var makePublicEndpoint = string.Format("https://www.googleapis.com/storage/v1/b/{0}/o/{1}/acl",
                bucketName, remoteAssetBundleName);
            var requestJsonContents = JsonUtility.ToJson(new MakeBundlePublicRequest());
            var requestBytes = Encoding.UTF8.GetBytes(requestJsonContents);
            var requestHeaders = new Dictionary<string, string>();
            requestHeaders.Add("Authorization",
                string.Format("Bearer {0} ", AccessTokenGetter.AccessToken.access_token));
            requestHeaders.Add("Content-Type", "application/json");
            var request =
                HttpRequestHelper.SendHttpPostRequest(makePublicEndpoint, requestBytes, requestHeaders);

            WwwRequestInProgress.TrackProgress(request, "Making remote asset bundle public",
                wwwResult =>
                {
                    if (resultHandler != null)
                    {
                        resultHandler.Invoke(wwwResult);
                    }
                });
        }

        // Checks whether the bucket with the name bucketName exists. Assumes access token valid.
        private static void CheckWhetherBucketExists(string bucketName, WwwHandler onTrue, WwwHandler onFalse)
        {
            var bucketInfoUrl =
                string.Format("https://www.googleapis.com/storage/v1/b/{0}", bucketName);
            var headers = new Dictionary<string, string>();
            headers.Add("Authorization", string.Format("Bearer {0}", AccessTokenGetter.AccessToken.access_token));
            var result = HttpRequestHelper.SendHttpGetRequest(bucketInfoUrl, null, headers);
            WwwRequestInProgress.TrackProgress(result, "Checking whether bucket exists.",
                wwwResult =>
                {
                    if (!string.IsNullOrEmpty(wwwResult.error))
                    {
                        onFalse.Invoke(wwwResult);
                    }
                    else
                    {
                        onTrue.Invoke(wwwResult);
                    }
                });
        }

        [Serializable]
        public class CreateBucketRequest
        {
            public string name;
        }

        [Serializable]
        public class MakeBundlePublicRequest
        {
            public string entity = "allUsers";
            public string role = "READER";
        }
    }
}