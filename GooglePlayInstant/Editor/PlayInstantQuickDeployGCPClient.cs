using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection.Emit;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Boo.Lang;
using GooglePlayInstant.Editor.GooglePlayServices;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

namespace GooglePlayInstant.Editor
{
    public delegate void AuthorizationResponseCallback(KeyValuePair<string, string> response);

    public abstract class QuickDeployGCPClient
    {
        private static QuickDeployConfig.Configuration _config = QuickDeployConfig.Config;

        private delegate void WwwHandler(WWW request);

        /// <summary>
        /// Upload bundle to the cloud, assuming TokenUtility has a valid access token.
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

            Debug.Log("Came back here to do the checking");
            IfBucketExists(_config.cloudStorageBucketName,
                doneWWW =>
                {
                    UploadBundle(resp => { ScheduleMakeBundlePublic(www => { Debug.Log("Response: " + www.text); }); });
                },
                doneWWW =>
                {
                    ScheduleCreateBucket(_config.cloudStorageBucketName,
                        bucketCreationResponse => { UploadBundle(www => { Debug.Log("Response: " + www.text); }); });
                });
        }

        private static void UploadBundle(WwwHandler responseHandler)
        {
            Debug.LogWarning("Bundle Upload starting right now");
            var uploadEndpoint =
                string.Format("https://www.googleapis.com/upload/storage/v1/b/{0}/o?uploadType=media&name={1}",
                    _config.cloudStorageBucketName, _config.cloudStorageFileName);


            byte[] bytes = File.ReadAllBytes(_config.assetBundleFileName);
            var headers = new Dictionary<string, string>();
            headers.Add("Authorization", string.Format("Bearer {0}", AccessTokenGetter.AccessToken.access_token));
            var result = QuickDeployHttpRequestHelper.SendHttpPostRequest(uploadEndpoint, bytes, headers);
            var requestInProgress = new WwwRequestInProgress(result, "Uploading bundle to cloud",
                "Please wait while your bundle is being uploaded to the cloud");
            requestInProgress.ScheduleTaskOnDone(www =>
            {
                Debug.Log("Got response: " + www.text + " After upload");
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
            WWW request = QuickDeployHttpRequestHelper.SendHttpPostRequest(createBucketEndPoint, jsonBytes, headers);

            WwwRequestInProgress requestInProgress = new WwwRequestInProgress(request,
                string.Format("Creating bucket with name {0}", bucketName),
                "You have specified a bucket that does not exist yet. Please wait while it is being created");
            Debug.Log("Bucket creation request created.");
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
            var objectName = _config.cloudStorageFileName;
            string makePublicEndpoint = string.Format("https://www.googleapis.com/storage/v1/b/{0}/o/{1}/acl",
                bucketName, objectName);
            string jsonContents = JsonUtility.ToJson(new MakeBucketPublicRequest());
            byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonContents);
            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("Authorization", string.Format("Bearer {0} ", AccessTokenGetter.AccessToken.access_token));
            headers.Add("Content-Type", "application/json");
            WWW request = QuickDeployHttpRequestHelper.SendHttpPostRequest(makePublicEndpoint, jsonBytes, headers);
            Debug.Log("Request to make bucket public was sent");
            WwwRequestInProgress requestInProgress = new WwwRequestInProgress(request, "Making object public",
                "Sending request to make object public");
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
        private static void IfBucketExists(string bucketName, WwwHandler onTrue, WwwHandler onFalse)
        {
            Debug.Log("CHECKED BUCKET EXISTENCE");
            string bucketInfoUrl =
                string.Format("https://www.googleapis.com/storage/v1/b/{0}", bucketName);
            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("Authorization", string.Format("Bearer {0}", AccessTokenGetter.AccessToken.access_token));
            var result = QuickDeployHttpRequestHelper.SendHttpGetRequest(bucketInfoUrl, null, headers);
            WwwRequestInProgress requestInProgress =
                new WwwRequestInProgress(result, "Checking whether bucket exists", "");
            requestInProgress.TrackProgress();
            Debug.Log("Going to schedule the tasks");
            requestInProgress.ScheduleTaskOnDone(wwwResult =>
            {
                var text = wwwResult.text;
                Debug.Log("Bucket Exists Error : " + text);
                if (text.Contains("error"))
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
        public class MakeBucketPublicRequest
        {
            public string entity = "allUsers";
            public string role = "READER";
        }
        
        
    }

}