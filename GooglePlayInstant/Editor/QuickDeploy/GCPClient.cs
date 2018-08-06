using System;
using System.Collections.Generic;
using System.IO;
using System.Security.AccessControl;
using System.Text;
using UnityEngine;

namespace GooglePlayInstant.Editor.QuickDeploy
{
    /// <summary>
    /// A class that implements the functionality for interacting with Google Cloud Storage API to deploying asset
    /// bundles.
    /// </summary>
    public static class GCPClient
    {
        private static QuickDeployConfig.Configuration _config = QuickDeployConfig.Config;

        /// <summary>
        /// Method signature for methods to be invoked on an WWW object representing a request that was made.
        /// </summary>
        private delegate void WwwHandler(WWW request);

        /// <summary>
        /// Executes all the steps required for deploying an Asset Bundle to Google Cloud Storage.
        /// First verifies if configured bucket exists, and creates the bucket if it does not exist. It then uploads
        /// asset bundle to Google Cloud Storage and sets the visibility of the asset bundle to public.
        /// </summary>
        /// <exception cref="Exception"></exception>
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


        /// <summary>
        /// Uploads asset bundle to Google Cloud storage and makes the bundle public.
        /// </summary>
        /// <exception cref="Exception">Exception thrown if there was an error uploading the bundle or setting the
        /// visibility of asset bundle to public.</exception>
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

                        Debug.Log("Visility of asset bundle was set to public.");
                    });
            });
        }

        /// <summary>
        /// Sends an HTTP request to Google Cloud Storage to upload the asset bundle according to configurations, and
        /// invokes the result handler on the response.
        /// Will first update access token before making this function call if valid acess token is not present.
        /// </summary>
        /// <param name="responseHandler"></param>
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

        /// <summary>
        /// Sends an HTTP to Google Cloud storage to create a bucket with the configured name, and invokes the result
        /// handler on the HTTP response.
        /// Will first update access token before making this function call if valid access token is not present.
        /// </summary>
        /// <param name="resultHandler"></param>
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
        /// Uses present access token to send HTTP request to Google cloud storage to change visibility of uploaded
        /// file to public, and invokes the result handler on the HTTP response.
        /// Will first update access token before making this function call if valid access token is not
        /// present.
        /// </summary>
        /// <param name="response">UploadBundleJsonResponse instance containing remote file and bucket name.</param>
        /// <param name="resultHandler">A delegate to handle the response that will be received from the server.</param>
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