using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace GooglePlayInstant.Editor
{
    public static class AccessTokenGetter
    {
        private const string GrantType = "authorization_code";
        private const string Scope = "https://www.googleapis.com/auth/devstorage.full_control";
        private static GcpAccessToken _accessToken;

        public delegate void AuthCodeReceivedCallback(AuthorizationCode authorizationCode);

        public delegate void TokenReceivedCallback(GcpAccessToken accessToken);

        private static AuthorizationResponseCallback oAuthCodeReceivedCallback;

        private static KeyValuePair<string, string>? AuthResponse;

        public static GcpAccessToken AccessToken
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
            GCPClientHelper.Oauth2Credentials credentials = GCPClientHelper.GetOauth2Credentials();
            string queryParams = "?scope=" + Scope + "&access_type=offline&include_granted_scopes=true" +
                                 "&redirect_uri=" + redirect_uri + "&response_type=code" + "&client_id=" +
                                 credentials.client_id;
            var authorizatonUrl = credentials.auth_uri + queryParams;
            Application.OpenURL(authorizatonUrl);
        }

        public static void ScheduleAccessToken(AuthorizationCode authCode, TokenReceivedCallback tokenReceivedCallback)
        {
            GCPClientHelper.Oauth2Credentials credentials = GCPClientHelper.GetOauth2Credentials();
            string tokenEndpiont = credentials.token_uri;
            Dictionary<string, string> formData = new Dictionary<string, string>();
            formData.Add("code", authCode.code);
            formData.Add("client_id", credentials.client_id);
            formData.Add("client_secret", credentials.client_secret);
            formData.Add("redirect_uri", authCode.redirect_uri);
            formData.Add("grant_type", GrantType);

            WwwRequestInProgress requestInProgress = new WwwRequestInProgress(
                QuickDeployHttpRequestHelper.SendHttpPostRequest(tokenEndpiont, formData, null),
                "Downloading access token",
                "Getting access token to use for uploading asset bundle");
            requestInProgress.TrackProgress();
            requestInProgress.ScheduleTaskOnDone(doneWww =>
            {
                string text = doneWww.text;
                var token = JsonUtility.FromJson<GcpAccessToken>(text);
                if (string.IsNullOrEmpty(token.access_token))
                {
                    throw new Exception(string.Format(
                        "Attempted to get access token and got response with code {0} and text {1}", doneWww.text,
                        doneWww.error));
                }

                tokenReceivedCallback.Invoke(token);
            });
        }

       
        
        [Serializable]
        public class AuthorizationCode
        {
            public string code;
            public string redirect_uri;
        }

        [Serializable]
        public class GcpAccessToken
        {
            public string access_token;
            public string refresh_token;
            public string token_type;
            public string expires_in;
        }
    }
}