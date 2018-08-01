using System;
using System.Collections.Generic;
using UnityEngine;

namespace GooglePlayInstant.Editor
{
    public static class AccessTokenGetter
    {
        // Members used for getting, retrieving and storing authorization code
        private const string GrantType = "authorization_code";
        private const string Scope = "https://www.googleapis.com/auth/devstorage.full_control";
        private static KeyValuePair<string, string>? _authorizationResponse;

        public delegate void AuthorizationCodeHandler(AuthorizationCode authorizationCode);

        public delegate void AuthorizationResponseHandler(KeyValuePair<string, string> response);

        private static AuthorizationResponseHandler _onOAuthResponseReceived;


        // Access token storage
        private static GcpAccessToken _accessToken;

        public static GcpAccessToken AccessToken
        {
            get { return _accessToken; }
            set { _accessToken = value; }
        }

        public delegate void AccessTokenHandler(GcpAccessToken accessToken);

        public static void OnGUI()
        {
            // Handle scheduled tasks for when authnorization code is received.
            if (_authorizationResponse.HasValue && _onOAuthResponseReceived != null)
            {
                _onOAuthResponseReceived.Invoke(_authorizationResponse.Value);
                _authorizationResponse = null;
                _onOAuthResponseReceived = null;
            }

            WwwRequestInProgress.NextState();
        }

        public static void ScheduleAuthCode(AuthorizationCodeHandler authorizationCodeHandler)
        {
            QuickDeployOAuth2Server server = new QuickDeployOAuth2Server(authorizationResponse =>
            {
                _authorizationResponse = authorizationResponse;
            });
            server.Start();
            var redirect_uri = server.CallbackEndpoint;

            AuthorizationResponseHandler onOAuthResponseReceived = responsePair =>
            {
                if (!string.Equals("code", responsePair.Key))
                {
                    throw new InvalidStateException("Could not receive required permissions");
                }

                AuthorizationCode authCode = new AuthorizationCode
                {
                    code = responsePair.Value,
                    redirect_uri = redirect_uri
                };
                if (authorizationCodeHandler != null)
                {
                    authorizationCodeHandler.Invoke(authCode);
                }
            };
            _onOAuthResponseReceived = onOAuthResponseReceived;
            // Now ask permissions from the server.
            GCPClientHelper.Oauth2Credentials credentials = GCPClientHelper.GetOauth2Credentials();
            string queryParams = "?scope=" + Scope + "&access_type=offline&include_granted_scopes=true" +
                                 "&redirect_uri=" + redirect_uri + "&response_type=code" + "&client_id=" +
                                 credentials.client_id;
            var authorizatonUrl = credentials.auth_uri + queryParams;
            Application.OpenURL(authorizatonUrl);
        }

        public static void ScheduleAccessToken(AuthorizationCode authCode, AccessTokenHandler onAccessTokenReceived)
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
                "Downloading access token");
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
                onAccessTokenReceived.Invoke(token);
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