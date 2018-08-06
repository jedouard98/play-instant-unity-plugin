using System;
using System.Collections.Generic;
using System.IO;
using System.Security.AccessControl;
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
        public static void DeployAssetBundle()
        {
            VerifyBucketExistence(
                // To be executed if bucket exists.
                bucketExistsResponse => { UploadBundleAndMakeItPublic(); },
                // To be executed if bucket does not exist.
                bucketNotFoundResponse =>
                {
                    CreateBucket(bucketCreationResponse =>
                    {
                        var error = bucketCreationResponse.error;
                        if (!string.IsNullOrEmpty(error))
                        {
                            throw new Exception(string.Format("Got error attempting to create bucket: {0}\n{1}", error,
                                bucketCreationResponse.text));
                        }

                        Debug.Log("Google Cloud Storage bucket was successfully created.");
                        UploadBundleAndMakeItPublic();
                    });
                });
        }


        private static void UploadBundleAndMakeItPublic()
        {
            UploadBundle(uploadBundleWww =>
            {
                if (!string.IsNullOrEmpty(uploadBundleWww.error))
                {
                    throw new Exception(string.Format("Got error uploading bundle: {0}\n{1}", uploadBundleWww.error,
                        uploadBundleWww.text));
                }

                Debug.Log("Asset bundle was uploaded to Google Cloud Storage.");
                var response = JsonUtility.FromJson<UploadBundleJsonResponse>(uploadBundleWww.text);
                _config.assetBundleUrl = string.Format("https://storage.googleapis.com/{0}/{1}", response.bucket,
                    response.name);

                MakeBundlePublic(JsonUtility.FromJson<UploadBundleJsonResponse>(uploadBundleWww.text),
                    makeBundlePublicWww =>
                    {
                        var error = makeBundlePublicWww.error;
                        if (!string.IsNullOrEmpty(error))
                        {
                            throw new Exception(string.Format("Got error making bundle public: {0}\n{1}", error,
                                makeBundlePublicWww.text));
                        }

                        Debug.Log("Visility of asset bundle was stored to public.");
                    });
            });
        }

        private static void UploadBundle(WwwHandler responseHandler)
        {
            var token = AccessTokenGetter.AccessToken;
            if (token != null)
            {
                var assetBundleFileName = _config.assetBundleFileName;
                var cloudStorageBucketName = _config.cloudStorageBucketName;
                var cloudStorageFileName = _config.cloudStorageFileName;
                var uploadEndpoint =
                    string.Format("https://www.googleapis.com/upload/storage/v1/b/{0}/o?uploadType=media&name={1}",
                        cloudStorageBucketName, cloudStorageFileName);


                var bytes = File.ReadAllBytes(assetBundleFileName);
                var headers = new Dictionary<string, string>();
                headers.Add("Authorization", string.Format("Bearer {0}", token.access_token));
                headers.Add("Content-Type", "application/octet-stream");
                var request = HttpRequestHelper.SendHttpPostRequest(uploadEndpoint, bytes, headers);
                WwwRequestInProgress.TrackProgress(request,
                    "Uploading asset bundle to google cloud storage", responseHandler.Invoke);
            }
            else
            {
                AccessTokenGetter.UpdateAccessToken(() => UploadBundle(responseHandler));
            }
        }

        // Creates a bucket with the given bucket name. Assumed TokenUtility has a valid access token and that the bucket
        // currently does not exist
        private static void CreateBucket(WwwHandler resultHandler)
        {
            var token = AccessTokenGetter.AccessToken;
            if (token != null)
            {
                var credentials = OAuth2Credentials.GetCredentials();
                var createBucketEndPoint = string.Format("https://www.googleapis.com/storage/v1/b?project={0}",
                    credentials.project_id);
                var createBucketRequest = new CreateBucketRequest
                {
                    name = _config.cloudStorageBucketName
                };

                var jsonBytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(createBucketRequest));
                var headers = new Dictionary<string, string>();
                headers.Add("Authorization", string.Format("Bearer {0}", token.access_token));
                headers.Add("Content-Type", "application/json");
                var request = HttpRequestHelper.SendHttpPostRequest(createBucketEndPoint, jsonBytes, headers);

                WwwRequestInProgress.TrackProgress(request,
                    string.Format("Creating bucket with name \"{0}\"", createBucketRequest.name), resultHandler.Invoke);
            }
            else
            {
                AccessTokenGetter.UpdateAccessToken(() => CreateBucket(resultHandler));
            }
        }
        
        /// <summary>
        /// Changes visibility of uploaded asset bundle to public, and invokes the result handler on the http response
        /// </summary>
        /// <param name="response"></param>
        /// <param name="resultHandler"></param>
        private static void MakeBundlePublic(UploadBundleJsonResponse response, WwwHandler resultHandler)
        {
            var token = AccessTokenGetter.AccessToken;
            if (token != null)
            {
                var makePublicEndpoint = string.Format("https://www.googleapis.com/storage/v1/b/{0}/o/{1}/acl",
                    response.bucket, response.name);
                var requestJsonContents = JsonUtility.ToJson(new MakeBundlePublicRequest());
                var requestBytes = Encoding.UTF8.GetBytes(requestJsonContents);
                var requestHeaders = new Dictionary<string, string>();
                requestHeaders.Add("Authorization",
                    string.Format("Bearer {0} ", token.access_token));
                requestHeaders.Add("Content-Type", "application/json");
                var makeBundlePublicWww =
                    HttpRequestHelper.SendHttpPostRequest(makePublicEndpoint, requestBytes, requestHeaders);
                WwwRequestInProgress.TrackProgress(makeBundlePublicWww, "Making remote asset bundle public",
                    resultHandler.Invoke);
            }
            else
            {
                AccessTokenGetter.UpdateAccessToken(() => MakeBundlePublic(response, resultHandler));
            }
        }


        /// <summary>
        /// If access token is present, sends an HTTP request to GCP to verify whether the configured bucket name exists or
        /// not. Invokes delegates onBucketExists if bucket exists, or onBucketDoesNotExists if bucket does
        /// not exist.
        /// If access token is not present, uses access token getter to get a new acess token and invoke this method
        /// again.
        /// Throws an exception if the server responds with error when verifying bucket existence.
        /// </summary>
        /// <param name="onBucketExists">Delegate to be invoked on the received result when bucket exists.</param>
        /// <param name="onBucketDoesNotExist">Delegate to be invoked on the received result when bucket does not
        /// exist.</param>
        private static void VerifyBucketExistence(WwwHandler onBucketExists, WwwHandler onBucketDoesNotExist)
        {
            var token = AccessTokenGetter.AccessToken;
            if (token != null)
            {
                var bucketInfoUrl =
                    string.Format("https://www.googleapis.com/storage/v1/b/{0}", _config.cloudStorageBucketName);
                var headers = new Dictionary<string, string>();
                headers.Add("Authorization", string.Format("Bearer {0}", token.access_token));
                var request = HttpRequestHelper.SendHttpGetRequest(bucketInfoUrl, null, headers);
                WwwRequestInProgress.TrackProgress(request, "Checking whether bucket exists.",
                    completeRequest =>
                    {
                        var error = completeRequest.error;
                        if (!string.IsNullOrEmpty(error))
                        {
                            if (error.StartsWith("404"))
                            {
                                onBucketDoesNotExist(completeRequest);
                            }
                            else
                            {
                                throw new Exception(string.Format(
                                    "Got error when verifying bucket existence: {0} \n {1}", error,
                                    completeRequest.text));
                            }
                        }
                        else
                        {
                            onBucketExists(completeRequest);
                        }
                    });
            }
            else
            {
                AccessTokenGetter.UpdateAccessToken(() =>
                    VerifyBucketExistence(onBucketExists, onBucketDoesNotExist));
            }
        }

        /// <summary>
        /// A representation of the body of the JSON request sent to Google Cloud Storage to create a bucket.
        /// </summary>
        [Serializable]
        private class CreateBucketRequest
        {
            public string name;
        }

        /// <summary>
        /// A representation of the body of the JSON request sent to Google Cloud Storage to set asset bundle
        /// visibility to public. 
        /// </summary>
        [Serializable]
        private class MakeBundlePublicRequest
        {
            public string entity = "allUsers";
            public string role = "READER";
        }

        /// <summary>
        /// A representation of a JSON response received once the asset bundle has been successfully uploaded to
        /// Google cloud storage.
        /// </summary>
        [Serializable]
        private class UploadBundleJsonResponse
        {
            public string name;
            public string bucket;
        }
    }
}