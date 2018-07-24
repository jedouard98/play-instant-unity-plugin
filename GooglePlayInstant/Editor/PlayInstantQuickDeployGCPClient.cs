using System;
using System.Collections.Generic;
using UnityEngine;

namespace GooglePlayInstant.Editor
{
    public static class QuickDeployTokenUtility
    {
        private const string GrantType = "authorization_code";
        private const string Scope = "https://www.googleapis.com/auth/devstorage.read_write";
        public  static AuthorizationCode GetAuthCode()
        {
            QuickDeployOAuth2CallbackEndpointServer server = new QuickDeployOAuth2CallbackEndpointServer();
            server.Start();
            var redirect_uri = server.CallbackEndpoint;
            Oauth2Credential credentials =
                JsonUtility.FromJson<Oauth2Credential>(QuickDeployConfig.Config.cloudCredentialsFileName);
            string queryParams = "?scope=" + Scope +"&access_type=offline&include_granted_scopes=true" +
                                 "&redirect_uri="+redirect_uri+"&response_type=code"+"client_id="+credentials.client_id;
            var authorizatonUrl = credentials.auth_uri + queryParams;
            Application.OpenURL(authorizatonUrl);
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

            Oauth2Credential credential =
                JsonUtility.FromJson<Oauth2Credential>(QuickDeployConfig.Config.cloudCredentialsFileName);
            string tokenEndpiont = credential.token_uri;

            AuthorizationCode authCode = GetAuthCode();
            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("Content-Type", "application/x-www-form-urlencoded");
            
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
    }
    
    public abstract class QuickDeployCloudUtility
    {

        public void UploadBundle()
        {
            var config = QuickDeployConfig.Config;
            
        };

        public abstract void MakeBundlePublic();
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

    }
}