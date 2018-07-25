using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection.Emit;
using System.Security.Cryptography;
using GooglePlayInstant.Editor.GooglePlayServices;
using UnityEditor.U2D;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace GooglePlayInstant.Editor
{
    public static class QuickDeployTokenUtility
    {
        private const string GrantType = "authorization_code";
        private const string Scope = "https://www.googleapis.com/auth/devstorage.read_write";


        public static AuthorizationCode GetAuthCode()
        {
            QuickDeployOAuth2CallbackEndpointServer server = new QuickDeployOAuth2CallbackEndpointServer();
            server.Start();
            var redirect_uri = server.CallbackEndpoint;
            Oauth2Credential credentials = ReadOauth2CredentialsFile();
            string queryParams = "?scope=" + Scope + "&access_type=offline&include_granted_scopes=true" +
                                 "&redirect_uri=" + redirect_uri + "&response_type=code" + "&client_id=" +
                                 credentials.client_id;
            var authorizatonUrl = credentials.auth_uri + queryParams;
            Process.Start(authorizatonUrl);
            while (server.HasOauth2AuthorizationResponse())
            {
            }

            KeyValuePair<string, string> response = server.getAuthorizationResponse();
            if (!string.Equals("code", response.Key))
            {
                throw new InvalidStateException("Could not receive needed permissions");
            }

            return new AuthorizationCode
            {
                code = response.Key,
                redirect_uri = redirect_uri
            };
        }

        public static AccessToken GetAccessToken()
        {
            Oauth2Credential credential = ReadOauth2CredentialsFile();

            string tokenEndpiont = credential.token_uri;

            AuthorizationCode authCode = GetAuthCode();
            Dictionary<string, string> headers = new Dictionary<string, string>();

            //headers.Add("Content-Type", "application/x-www-form-urlencoded");

            Dictionary<string, string> formData = new Dictionary<string, string>();
            formData.Add("code", authCode.code);
            formData.Add("client_id", credential.client_id);
            formData.Add("client_secret", credential.client_secret);
            formData.Add("redirect_uri", authCode.redirect_uri);
            formData.Add("grant_type", GrantType);

            AccessToken token =
                JsonUtility.FromJson<AccessToken>(
                    QuickDeployWwwRequestHandler.SendHttpPostRequest(tokenEndpiont, formData, headers));
            if (string.IsNullOrEmpty(token.access_token))
            {
                throw new Exception("Error retrieving the access token");
            }

            return token;
        }

        public static Oauth2Credential ReadOauth2CredentialsFile()
        {
            var filePrefix = "{\"installed\":";
            var fileSuffix = "}";
            string fileContents = File.ReadAllText(QuickDeployConfig.Config.cloudCredentialsFileName);
            if (!fileContents.StartsWith(filePrefix) || !fileContents.EndsWith(fileSuffix))
            {
                throw new Exception("The file is not well formatted. Exiting");
            }

            Debug.Log("File contents are " + fileContents);

            var toPrint = "string length: " + fileContents.Length.ToString() + " prefix length " +
                          filePrefix.Length.ToString() + " suffixLength " + fileSuffix.Length.ToString();
            Debug.Log(toPrint);

            var startIndex = filePrefix.Length-1;
            var length = fileContents.Length - filePrefix.Length - fileSuffix.Length-1;
            Debug.Log("StartIndex: " + startIndex.ToString() + " length: " + length.ToString());

            var jsonContents = fileContents.Substring(filePrefix.Length + 1, length);
            Debug.Log("Json Contents are: {" + jsonContents);


            Oauth2Credential credential =  JsonUtility.FromJson<Oauth2Credential>("{"+jsonContents);
            Debug.Log("Auth uri: "+credential.auth_uri);
            return credential;
        }
    }

    public abstract class QuickDeployCloudUtility
    {
        private static AccessToken _accessToken;

        public static AccessToken AccessTokenValue
        {
            get
            {
                if (_accessToken != null)
                {
                    return _accessToken;
                }

                return QuickDeployTokenUtility.GetAccessToken();
            }
        }

        private static readonly QuickDeployConfig.Configuration Config = QuickDeployConfig.Config;

        public static void UploadBundle()
        {
            _accessToken = QuickDeployTokenUtility.GetAccessToken();
            if (!BucketExists(Config.cloudStorageBucketName))
            {
                CreateBucket(Config.cloudStorageBucketName);
            }

            if (AlwaysTrue())
            {
                return;
            }

            var uploadEndpoint =
                string.Format("https://www.googleapis.com/upload/storage/v1/b/{0}/o?uploadType=media&name={1}",
                    Config.cloudStorageBucketName, Config.cloudStorageFileName);


            byte[] bytes = File.ReadAllBytes(Config.assetBundleFileName);
            var headers = new Dictionary<string, string>();
            headers.Add("Authorization", "Bearer " + _accessToken.access_token);
            headers.Add("Content-Length", bytes.Length.ToString());
            var result = QuickDeployWwwRequestHandler.SendHttpPostRequest(uploadEndpoint, bytes, headers);
            Debug.Log("Our result was: " + result);
        }

        private static bool AlwaysTrue()
        {
            return true;
        }

        private static void CreateBucket(string bucketName)
        {
            Oauth2Credential credential = QuickDeployTokenUtility.ReadOauth2CredentialsFile();
            string createBucketEndPoint = string.Format("https://www.googleapis.com/storage/v1/b?project={0}",
                credential.project_id);
            Dictionary<string, string> form = new Dictionary<string, string>();
            form.Add("name", bucketName);
            string result = QuickDeployWwwRequestHandler.SendHttpPostRequest(createBucketEndPoint, form, null);
            Debug.Log(result);
        }

        private static bool BucketExists(string bucketName)
        {
            string bucketInfoUrl =
                string.Format("https://www.googleapis.com/storage/v1/b/{0}?fields=location", bucketName);
            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("Authorization", string.Format("Bearer {0}", AccessTokenValue.access_token));
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
    public class Oauth2Credential
    {
        public string client_id;
        public string client_secret;
        public string auth_uri;
        public string token_uri;
        public string project_id;
    }
}