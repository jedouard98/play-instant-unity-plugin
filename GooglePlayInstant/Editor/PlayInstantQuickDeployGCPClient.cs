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

    public static class QuickDeployTokenUtility
    {
        private const string GrantType = "authorization_code";
        private const string Scope = "https://www.googleapis.com/auth/devstorage.full_control";
        private static AccessToken _accessToken;

        public delegate void AuthCodeReceivedCallback(AuthorizationCode authorizationCode);

        public delegate void TokenReceivedCallback(AccessToken accessToken);

        public static AuthorizationResponseCallback oAuthCodeReceivedCallback;

        public static KeyValuePair<string, string>? AuthResponse;

        public static AccessToken AccessToken
        {
            get { return _accessToken; }
            set { _accessToken = value; }
        }


        public static void OnGUI()
        {
            // Handle Authorization code tasks
            if (oAuthCodeReceivedCallback != null & AuthResponse.HasValue)
            {
                oAuthCodeReceivedCallback.Invoke(AuthResponse.Value);
                oAuthCodeReceivedCallback = null;
            }

            // Display statuses for the requests in progress
            WwwRequestInProgress.UpdateState();
            //WwwRequestInProgress.DisplayProgressForTrackedRequests();
        }

        public static void ScheduleAuthCode(AuthCodeReceivedCallback authCodeReceivedCallback)
        {
            QuickDeployOAuth2Server server = new QuickDeployOAuth2Server(response => { AuthResponse = response; });
            server.Start();
            var redirect_uri = server.CallbackEndpoint;


            AuthorizationResponseCallback responseCallback = responsePair =>
            {
                if (!string.Equals("code", responsePair.Key))
                {
                    throw new InvalidStateException("Could not receive needed permissions");
                }

                AuthorizationCode authCode = new AuthorizationCode
                {
                    code = responsePair.Value,
                    redirect_uri = redirect_uri
                };
                if (authCodeReceivedCallback != null)
                {
                    authCodeReceivedCallback.Invoke(authCode);
                }
            };
            oAuthCodeReceivedCallback = responseCallback;
            // Now ask permissions from the server.
            Oauth2Credentials credentials = ReadOauth2CredentialsFile();
            string queryParams = "?scope=" + Scope + "&access_type=offline&include_granted_scopes=true" +
                                 "&redirect_uri=" + redirect_uri + "&response_type=code" + "&client_id=" +
                                 credentials.client_id;
            var authorizatonUrl = credentials.auth_uri + queryParams;
            Application.OpenURL(authorizatonUrl);
        }

        public static void ScheduleAccessToken(AuthorizationCode authCode, TokenReceivedCallback tokenReceivedCallback)
        {
            Oauth2Credentials credentials = ReadOauth2CredentialsFile();

            string tokenEndpiont = credentials.token_uri;
            Dictionary<string, string> headers = new Dictionary<string, string>();

            //headers.Add("Content-Type", "application/x-www-form-urlencoded");

            Dictionary<string, string> formData = new Dictionary<string, string>();
            formData.Add("code", authCode.code);
            formData.Add("client_id", credentials.client_id);
            formData.Add("client_secret", credentials.client_secret);
            formData.Add("redirect_uri", authCode.redirect_uri);
            formData.Add("grant_type", GrantType);

            WwwRequestInProgress requestInProgress = new WwwRequestInProgress(
                QuickDeployHttpRequestHelper.SendHttpPostRequest(tokenEndpiont, formData, headers),
                "Downloading access token",
                "Getting access token to use for uploading asset bundle");
            requestInProgress.TrackProgress();
            requestInProgress.ScheduleTaskOnDone(doneWww =>
            {
                string text = doneWww.text;
                File.WriteAllText("output-token.txt", "Token Text: " + text);
                var token = JsonUtility.FromJson<AccessToken>(text);
                if (string.IsNullOrEmpty(token.access_token))
                {
                    throw new Exception(string.Format(
                        "Attempted to get access token and got response with code {0} and text {1}", doneWww.text,
                        doneWww.error));
                }

                tokenReceivedCallback.Invoke(token);
            });
        }

        public static Oauth2Credentials ReadOauth2CredentialsFile()
        {
            string path = QuickDeployConfig.Config.cloudCredentialsFileName;
            Debug.Log("Path is " + path);
            string allText = File.ReadAllText(QuickDeployConfig.Config.cloudCredentialsFileName);
            Debug.Log("All Text: " + allText);
            var file = JsonUtility.FromJson<Oauth2File>(allText);
            Oauth2Credentials installed = file.installed;
            return installed;
        }
    }

    public abstract class QuickDeployGCPClient
    {
        private static QuickDeployConfig.Configuration _config = QuickDeployConfig.Config;

        private static AccessToken AccessToken
        {
            get { return QuickDeployTokenUtility.AccessToken; }
        }

        private delegate void WwwHandler(WWW request);

        /// <summary>
        /// Upload bundle to the cloud, assuming TokenUtility has a valid access token.
        /// </summary>
        public static void CreateBucketIfNotExistsAndUploadBundle()
        {
            QuickDeployConfig.SaveConfiguration();
            if (AccessToken == null)
            {
                QuickDeployTokenUtility.ScheduleAuthCode(code =>
                {
                    QuickDeployTokenUtility.ScheduleAccessToken(code, token =>
                    {
                        QuickDeployTokenUtility.AccessToken = token;
                        CreateBucketIfNotExistsAndUploadBundle();
                    });
                });
                return;
            }

            // TODO(audace): Split this into two tasks, one to carry out when the bucket exists, and one to carry out when the bucket happens to not exist
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
            headers.Add("Authorization", string.Format("Bearer {0}", AccessToken.access_token));
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
            Oauth2Credentials credentials = QuickDeployTokenUtility.ReadOauth2CredentialsFile();
            string createBucketEndPoint = string.Format("https://www.googleapis.com/storage/v1/b?project={0}",
                credentials.project_id);
            string jsonContents = JsonUtility.ToJson(new CreateBucketRequest
            {
                name = bucketName
            });

            byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonContents);
            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("Authorization", string.Format("Bearer {0} ", AccessToken.access_token));
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
            headers.Add("Authorization", string.Format("Bearer {0} ", AccessToken.access_token));
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
            headers.Add("Authorization", string.Format("Bearer {0}", AccessToken.access_token));
            var result = QuickDeployHttpRequestHelper.SendHttpGetRequest(bucketInfoUrl, null, headers);
            WwwRequestInProgress requestInProgress =
                new WwwRequestInProgress(result, "Checking whether bucket exists", "");
            requestInProgress.TrackProgress();
            Debug.Log("Going to schedule the tasks");
            requestInProgress.ScheduleTaskOnDone(wwwResult =>
            {
                var text = wwwResult.text;
                Debug.Log("Bucket Exists Error : "+text);
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
    }

    public class AuthorizationCode
    {
        public string code;
        public string redirect_uri;
    }

    [Serializable]
    public class AccessToken
    {
        public string access_token;
        public string refresh_token;
        public string token_type;
        public string expires_in;
    }


    [Serializable]
    public class Oauth2Credentials
    {
        public string client_id;
        public string client_secret;
        public string auth_uri;
        public string token_uri;
        public string project_id;
    }

    [Serializable]
    public class Oauth2File
    {
        public Oauth2Credentials installed;
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