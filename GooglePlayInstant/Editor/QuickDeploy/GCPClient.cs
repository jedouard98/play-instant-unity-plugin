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
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace GooglePlayInstant.Editor.QuickDeploy
{
    /// <summary>
    /// A class that implements the functionality for interacting with Google Cloud Storage API to deploy asset
    /// bundles.
    /// </summary>
    public static class GCPClient
    {
        private static QuickDeployConfig.Configuration _config = QuickDeployConfig.Config;

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
                // see https://cloud.google.com/storage/docs/access-public-data on accessing public cloud objects.
                _config.assetBundleUrl = string.Format("https://storage.googleapis.com/{0}/{1}", response.bucket,
                    response.name);

                MakeBundlePublic(response,
                    makeBundlePublicWww =>
                    {
                        var error = makeBundlePublicWww.error;
                        if (!string.IsNullOrEmpty(error))
                        {
                            throw new Exception(string.Format("Got error making bundle public: {0}\n{1}", error,
                                makeBundlePublicWww.text));
                        }

                        Debug.Log("Visibility of asset bundle was set to public.");
                    });
            });
        }

        /// <summary>
        /// Sends an HTTP request to Google Cloud Storage to upload the asset bundle according to configurations, and
        /// invokes the result handler on the response.
        /// Will first update access token before making this function call if valid acess token is not present.
        /// </summary>
        /// <param name="onUploadBundleResponseAction">An action to be invoked on the www instance holding the http request
        /// once the response to the request to upload the bundle is available</param>
        private static void UploadBundle(Action<WWW> onUploadBundleResponseAction)
        {
            var token = AccessTokenGetter.AccessToken;
            if (token != null)
            {
                var assetBundleFileName = _config.assetBundleFileName;
                var cloudStorageBucketName = _config.cloudStorageBucketName;
                var cloudStorageFileName = _config.cloudStorageFileName;
                // see https://cloud.google.com/storage/docs/uploading-objects on uploading objects.
                var uploadEndpoint =
                    string.Format("https://www.googleapis.com/upload/storage/v1/b/{0}/o?uploadType=media&name={1}",
                        cloudStorageBucketName, cloudStorageFileName);
                var request = SendAuthenticatedPostRequest(uploadEndpoint, File.ReadAllBytes(assetBundleFileName),
                    "application/octet-stream", token.access_token);
                WwwRequestInProgress.TrackProgress(request,
                    "Uploading asset bundle to google cloud storage", onUploadBundleResponseAction);
            }
            else
            {
                AccessTokenGetter.UpdateAccessToken(() => UploadBundle(onUploadBundleResponseAction));
            }
        }

        /// <summary>
        /// Sends an HTTP to Google Cloud storage to create a bucket with the configured name, and invokes the result
        /// handler on the HTTP response.
        /// Will first update access token before making this function call if valid access token is not present.
        /// </summary>
        /// <param name="onCreateBucketResponseAction">An action to be invoked on the www instance holding the HTTP request
        /// once the response to the request to create bucket is available</param>
        private static void CreateBucket(Action<WWW> onCreateBucketResponseAction)
        {
            var token = AccessTokenGetter.AccessToken;
            if (token != null)
            {
                var credentials = OAuth2Credentials.GetCredentials();
                // see https://cloud.google.com/storage/docs/creating-buckets on creating buckets.
                var createBucketEndPoint = string.Format("https://www.googleapis.com/storage/v1/b?project={0}",
                    credentials.project_id);
                var createBucketRequest = new CreateBucketRequest
                {
                    name = _config.cloudStorageBucketName
                };

                var jsonBytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(createBucketRequest));
                var createBucketWww = SendAuthenticatedPostRequest(createBucketEndPoint, jsonBytes, "application/json",
                    token.access_token);
                WwwRequestInProgress.TrackProgress(createBucketWww,
                    string.Format("Creating bucket with name \"{0}\"", createBucketRequest.name),
                    onCreateBucketResponseAction);
            }
            else
            {
                AccessTokenGetter.UpdateAccessToken(() => CreateBucket(onCreateBucketResponseAction));
            }
        }

        /// <summary>
        /// Uses present access token to send HTTP request to Google cloud storage to change visibility of uploaded
        /// file to public, and invokes the result handler action on the HTTP response.
        /// Will first update access token before making this function call if valid access token is not present.
        /// </summary>
        /// <param name="response">UploadBundleJsonResponse instance containing remote file and bucket name.</param>
        /// <param name="onMakeBundlePublicResponseAction">An action to be invoked on the WWW instance holding the HTTP
        /// request once the response to the request to make the bundle public is available.</param>
        private static void MakeBundlePublic(UploadBundleJsonResponse response,
            Action<WWW> onMakeBundlePublicResponseAction)
        {
            var token = AccessTokenGetter.AccessToken;
            if (token != null)
            {
                // see https://cloud.google.com/storage/docs/access-control/making-data-public on making data public.
                var makePublicEndpoint = string.Format("https://www.googleapis.com/storage/v1/b/{0}/o/{1}/acl",
                    response.bucket, response.name);
                var requestJsonContents = JsonUtility.ToJson(new MakeBundlePublicRequest());
                var makeBundlePublicWww = SendAuthenticatedPostRequest(makePublicEndpoint,
                    Encoding.UTF8.GetBytes(requestJsonContents), "application/json", token.access_token);
                WwwRequestInProgress.TrackProgress(makeBundlePublicWww, "Making remote asset bundle public",
                    onMakeBundlePublicResponseAction);
            }
            else
            {
                AccessTokenGetter.UpdateAccessToken(() => MakeBundlePublic(response, onMakeBundlePublicResponseAction));
            }
        }

        /// <summary>
        /// If access token is present, sends an HTTP request to GCP to verify whether the configured bucket name exists
        /// or not. Invokes action onBucketExists if bucket exists, or action onBucketDoesNotExists if bucket does
        /// not exist.
        /// Will first update access token before making this function call if valid access token is not present.
        /// Throws an exception if the server responds with error when verifying bucket existence.
        /// </summary>
        /// <param name="onBucketExistsResponseAction">An action to be invoked on the WWW instance holding the HTTP request
        /// when bucket exists.</param>
        /// <param name="onBucketDoesNotExistResponseAction">An action to be invoked on the WWW instance holding the HTTP request
        /// when the bucket does not exist.</param>
        private static void VerifyBucketExistence(Action<WWW> onBucketExistsResponseAction,
            Action<WWW> onBucketDoesNotExistResponseAction)
        {
            var token = AccessTokenGetter.AccessToken;
            if (token != null)
            {
                // see https://cloud.google.com/storage/docs/getting-bucket-information on getting bucket information.
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
                                onBucketDoesNotExistResponseAction(completeRequest);
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
                            onBucketExistsResponseAction(completeRequest);
                        }
                    });
            }
            else
            {
                AccessTokenGetter.UpdateAccessToken(() =>
                    VerifyBucketExistence(onBucketExistsResponseAction, onBucketDoesNotExistResponseAction));
            }
        }

        /// <summary>
        /// Helps send an authenticated HTTP post request to Google Cloud Storage.
        /// </summary>
        /// <param name="endpoint">endpoint to which the request is going.</param>
        /// <param name="content">Content bytes to be put in the body of the request.</param>
        /// <param name="contentType">Type of content to be used in headers.</param>
        /// <param name="accessToken">OAuth2 Access token to be used in the headers.</param>
        /// <returns></returns>
        private static WWW SendAuthenticatedPostRequest(string endpoint, byte[] content, string contentType,
            string accessToken)
        {
            var requestHeaders = new Dictionary<string, string>();
            requestHeaders.Add("Authorization",
                string.Format("Bearer {0}", accessToken));
            requestHeaders.Add("Content-Type", contentType);
            return HttpRequestHelper.SendHttpPostRequest(endpoint, content, requestHeaders);
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
#pragma warning disable CS0649
        [Serializable]
        private class UploadBundleJsonResponse
        {
            public string name;
            public string bucket;
        }
    }
}