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
            QuickDeployConfig.SaveConfiguration();
            if (AccessTokenGetter.AccessToken == null)
            {
                AccessTokenGetter.ScheduleAuthCode(code =>
                {
                    AccessTokenGetter.ScheduleAccessToken(code, token =>
                    {
                        AccessTokenGetter.AccessToken = token;
                        CreateBucketIfNotExistsAndUploadBundle();
                    });
                });
                return;
            }

            CheckWhetherBucketExists(_config.cloudStorageBucketName,
                bucketExistsResponse => { UploadBundle(resp => { ScheduleMakeBundlePublic(www => { }); }); },
                bucketNotFoundResponse =>
                {
                    ScheduleCreateBucket(_config.cloudStorageBucketName,
                        bucketCreationResponse => { UploadBundle(resp => { ScheduleMakeBundlePublic(www => { }); }); });
                });
        }

        private static void UploadBundle(WwwHandler responseHandler)
        {
            var assetBundleFileName = _config.assetBundleFileName;
            var cloudStorageBucketName = _config.cloudStorageBucketName;
            var cloudStorageFileName = _config.cloudStorageFileName;
            var uploadEndpoint =
                string.Format("https://www.googleapis.com/upload/storage/v1/b/{0}/o?uploadType=media&name={1}",
                    cloudStorageBucketName, cloudStorageFileName);


            byte[] bytes = File.ReadAllBytes(assetBundleFileName);
            var headers = new Dictionary<string, string>();
            headers.Add("Authorization", string.Format("Bearer {0}", AccessTokenGetter.AccessToken.access_token));
            var result = HttpRequestHelper.SendHttpPostRequest(uploadEndpoint, bytes, headers);
            var requestInProgress = new WwwRequestInProgress(result,
                "Uploading asset bundle to google cloud storage");
            requestInProgress.TrackProgress();
            requestInProgress.ScheduleTaskOnDone(www =>
            {
                if (responseHandler != null)
                {
                    responseHandler.Invoke(www);
                }
            });
        }

        // Creates a bucket with the given bucket name. Assumed TokenUtility has a valid access token and that the bucket
        // currently does not exist
        private static void ScheduleCreateBucket(string bucketName, WwwHandler resultHandler)
        {
            GCPClientHelper.Oauth2Credentials credentials = GCPClientHelper.GetOauth2Credentials();
            string createBucketEndPoint = string.Format("https://www.googleapis.com/storage/v1/b?project={0}",
                credentials.project_id);
            string jsonContents = JsonUtility.ToJson(new CreateBucketRequest
            {
                name = bucketName
            });

            byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonContents);
            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("Authorization", string.Format("Bearer {0} ", AccessTokenGetter.AccessToken.access_token));
            headers.Add("Content-Type", "application/json");
            WWW request = HttpRequestHelper.SendHttpPostRequest(createBucketEndPoint, jsonBytes, headers);

            WwwRequestInProgress requestInProgress = new WwwRequestInProgress(request,
                string.Format("Creating bucket with name \"{0}\"", bucketName));
            requestInProgress.TrackProgress();
            requestInProgress.ScheduleTaskOnDone(wwwResult =>
            {
                if (resultHandler != null)
                {
                    resultHandler.Invoke(wwwResult);
                }
            });
        }

        private static void ScheduleMakeBundlePublic(WwwHandler resultHandler)
        {
            var bucketName = _config.cloudStorageBucketName;
            var remoteAssetBundleName = _config.cloudStorageFileName;
            string makePublicEndpoint = string.Format("https://www.googleapis.com/storage/v1/b/{0}/o/{1}/acl",
                bucketName, remoteAssetBundleName);
            string requestJsonContents = JsonUtility.ToJson(new MakeBundlePublicRequest());
            byte[] requestBytes = Encoding.UTF8.GetBytes(requestJsonContents);
            Dictionary<string, string> requestHeaders = new Dictionary<string, string>();
            requestHeaders.Add("Authorization",
                string.Format("Bearer {0} ", AccessTokenGetter.AccessToken.access_token));
            requestHeaders.Add("Content-Type", "application/json");
            WWW request =
                HttpRequestHelper.SendHttpPostRequest(makePublicEndpoint, requestBytes, requestHeaders);

            WwwRequestInProgress requestInProgress =
                new WwwRequestInProgress(request,"Making remote asset bundle public");
            requestInProgress.TrackProgress();
            requestInProgress.ScheduleTaskOnDone(wwwResult =>
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
            string bucketInfoUrl =
                string.Format("https://www.googleapis.com/storage/v1/b/{0}", bucketName);
            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("Authorization", string.Format("Bearer {0}", AccessTokenGetter.AccessToken.access_token));
            var result = HttpRequestHelper.SendHttpGetRequest(bucketInfoUrl, null, headers);
            WwwRequestInProgress requestInProgress =
                new WwwRequestInProgress(result, "Checking whether bucket exists.");
            requestInProgress.TrackProgress();
            requestInProgress.ScheduleTaskOnDone(wwwResult =>
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