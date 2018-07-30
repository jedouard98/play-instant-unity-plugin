using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection.Emit;
using System.Security.Cryptography;
using System.Threading;
using GooglePlayInstant.Editor.GooglePlayServices;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace GooglePlayInstant.Editor
{
    public static class QuickDeployTokenUtility
    {
        private const string GrantType = "authorization_code";
        private const string Scope = "https://www.googleapis.com/auth/devstorage.read_write";
        private static AccessToken _accessToken;

        public delegate void AuthCodeReceivedCallback(AuthorizationCode authorizationCode);
        public delegate void TokenReceivedCallback(AccessToken accessToken);

        public static AccessToken AccessToken
        {
            get { return _accessToken; }
        }


        public static void ScheduleAuthCode(AuthCodeReceivedCallback authCodeReceivedCallback)
        {
            QuickDeployOAuth2Server server = new QuickDeployOAuth2Server();
            server.Start();
            var redirect_uri = server.CallbackEndpoint;
            Oauth2Credentials credentials = ReadOauth2CredentialsFile();
            string queryParams = "?scope=" + Scope + "&access_type=offline&include_granted_scopes=true" +
                                 "&redirect_uri=" + redirect_uri + "&response_type=code" + "&client_id=" +
                                 credentials.client_id;
            var authorizatonUrl = credentials.auth_uri + queryParams;
            Process.Start(authorizatonUrl);
            while (!server.HasOauth2AuthorizationResponse())
            {
                Debug.Log("No response yet");
                Thread.Sleep(1);
            }

            KeyValuePair<string, string> response = server.GetAuthorizationResponse();
            if (!string.Equals("code", response.Key))
            {
                throw new InvalidStateException("Could not receive needed permissions");
            }

            AuthorizationCode authCode = new AuthorizationCode
            {
                code = response.Value,
                redirect_uri = redirect_uri
            };
            if (authCodeReceivedCallback != null)
            {
                authCodeReceivedCallback.Invoke(authCode);
            }
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

            WWW www = QuickDeployWwwRequestHandler.SendHttpPostRequest(tokenEndpiont, formData, headers);
            WwwRequestInProgress requestInProgress = new WwwRequestInProgress(www, "Downloading access token",
                "Getting access token to use for uploading asset bundle");
            requestInProgress.TrackProgress();
            requestInProgress.ScheduleTaskOnDone(doneWww =>
            {
                var token = JsonUtility.FromJson<AccessToken>(doneWww.text);
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
            return JsonUtility.FromJson<Oauth2File>(
                File.ReadAllText(QuickDeployConfig.Config.cloudCredentialsFileName)).installed;
        }

        public static void AssignToken(AccessToken accessToken)
        {
            _accessToken = accessToken;
        }
    }

    public abstract class QuickDeployCloudUtility
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
        public static void UploadBundle()
        {
            // TODO(audace): Split this into two tasks, one to carry out when the bucket exists, and one to carry out when the bucket happens to not exist
            if (!IfBucketExists(_config.cloudStorageBucketName, null, null))
            {
                CreateBucket(_config.cloudStorageBucketName, doneWww => { UploadBundle();});
            }

            if (AlwaysTrue())
            {
                return;
            }

            var uploadEndpoint =
                string.Format("https://www.googleapis.com/upload/storage/v1/b/{0}/o?uploadType=media&name={1}",
                    _config.cloudStorageBucketName, _config.cloudStorageFileName);


            byte[] bytes = File.ReadAllBytes(_config.assetBundleFileName);
            var headers = new Dictionary<string, string>();
            headers.Add("Authorization", "Bearer " + AccessToken.access_token);
            headers.Add("Content-Length", bytes.Length.ToString());
            var result = QuickDeployWwwRequestHandler.SendHttpPostRequest(uploadEndpoint, bytes, headers);
            
        }

        private static bool AlwaysTrue()
        {
            return true;
        }

        // Creates a bucket with the given bucket name. Assumed TokenUtility has a valid access token and that the bucket
        // currently does not exist
        private static void CreateBucket(string bucketName, WwwHandler resultHandler)
        {
            Oauth2Credentials credentials = QuickDeployTokenUtility.ReadOauth2CredentialsFile();
            string createBucketEndPoint = string.Format("https://www.googleapis.com/storage/v1/b?project={0}",
                credentials.project_id);
            Dictionary<string, string> form = new Dictionary<string, string>();
            form.Add("name", bucketName);
            WWW request = QuickDeployWwwRequestHandler.SendHttpPostRequest(createBucketEndPoint, form, null);
            WwwRequestInProgress requestInProgress = new WwwRequestInProgress(request,
                string.Format("Creating bucket with name {0}", bucketName),
                "You have specified a bucket that does not exist yet. Please wait while it is being created");
            requestInProgress.TrackProgress();
            requestInProgress.ScheduleTaksOnDone(wwwResult =>
            {
                if (resultHandler != null)
                {
                    resultHandler.Invoke(wwwResult);
                }

                
            });
        }

        // Checks whether the bucket with the name bucketName exists. Assumes access token valid.
        // TODO(audace): Implement different paths to be executed on true and on false
        private static bool IfBucketExists(string bucketName, WwwHandler onTrue, WwwHandler onFalse)
        {
            string bucketInfoUrl =
                string.Format("https://www.googleapis.com/storage/v1/b/{0}?fields=location", bucketName);
            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("Authorization", string.Format("Bearer {0}", AccessToken.access_token));
            var result = QuickDeployWwwRequestHandler.SendHttpGetRequest(bucketInfoUrl, null, headers);
            Debug.LogFormat("Bucket exists result is {0} ", result);
            return false;
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
}