using System;
using System.Collections.Generic;
using UnityEngine;

namespace GooglePlayInstant.Editor.QuickDeploy
{
    public static class AccessTokenGetter
    {
        // Members used for getting, retrieving and storing authorization code
        private const string GrantType = "authorization_code";
        private const string Scope = "https://www.googleapis.com/auth/devstorage.full_control";
        private static KeyValuePair<string, string>? _authorizationResponse;

        /// <summary>
        /// Signature for methods to handle authorization response from the server. An authorization response could be
        /// either "code" or "error" response.
        /// </summary>
        /// <param name="response">A KeyValuePair instance corresponding to the authorization response received.</param>
        private delegate void AuthorizationResponseHandler(KeyValuePair<string, string> response);

        /// <summary>
        /// Signature for methods that handle the authorization code.
        /// </summary>
        /// <param name="authorizationCode">An Authorizationcode instance corresponding to the authorization code
        /// received from OAuth2.</param>
        public delegate void AuthorizationCodeHandler(AuthorizationCode authorizationCode);

        /// <summary>
        /// Signature for methods to be called after access token has been received.
        /// </summary>
        public delegate void PostTokenAction();

        private static AuthorizationResponseHandler _onOAuthResponseReceived;

        // Access token storage
        private static GCPAccessToken _accessToken;

        /// <summary>
        /// Get Access token to use for API calls if available. Returns null if access token is not available.
        /// </summary>
        public static GCPAccessToken AccessToken
        {
            get { return _accessToken; }
            private set { _accessToken = value; }
        }

        /// <summary>
        /// Check whether a new authorization code has been received and execute scheduled tasks accordingly.
        /// </summary>
        public static void Update()
        {
            // Handle scheduled tasks for when authorization code is received.
            if (_authorizationResponse.HasValue && _onOAuthResponseReceived != null)
            {
                _onOAuthResponseReceived(_authorizationResponse.Value);
                _authorizationResponse = null;
                _onOAuthResponseReceived = null;
            }
        }


        /// <summary>
        /// Get new access token if access token is expired or is not available, and execute the action when the access
        /// token is available.
        /// </summary>
        /// <param name="postTokenAction">Action to be executed when valid access token is avalable.</param>
        public static void UpdateAccessToken(PostTokenAction postTokenAction)
        {
            if (AccessToken == null)
            {
                GetAuthCode(code => RequestAndStoreAccessToken(code, postTokenAction));
            }
            else
            {
                postTokenAction();
            }
        }


        /// <summary>
        /// Instantiate the OAuth2 flow to retrieve authorization code for google cloud storage, and schedule invocation of
        /// the code handler on the received authorization code once it is available or throw an exception once there is
        /// a failure to get the authorization code.
        /// </summary>
        /// <param name="authorizationCodeHandler"></param>
        /// <exception cref="Exception">Exception thrown when required authorization code cannot be received
        /// from OAuth2 flow.</exception>
        public static void GetAuthCode(AuthorizationCodeHandler authorizationCodeHandler)
        {
            var server = new OAuth2Server(authorizationResponse => { _authorizationResponse = authorizationResponse; });
            server.Start();

            var redirect_uri = server.CallbackEndpoint;

            _onOAuthResponseReceived = authorizationResponse =>
            {
                if (!string.Equals("code", authorizationResponse.Key))
                {
                    throw new Exception("Could not receive required permissions");
                }

                var authCode = new AuthorizationCode
                {
                    code = authorizationResponse.Value,
                    redirect_uri = redirect_uri
                };

                if (authorizationCodeHandler != null)
                {
                    authorizationCodeHandler(authCode);
                }
            };

            // Now ask permissions from the server.
            var credentials = OAuth2Credentials.GetCredentials();
            var queryParams = "?scope=" + Scope + "&access_type=offline&include_granted_scopes=true" +
                              "&redirect_uri=" + redirect_uri + "&response_type=code" + "&client_id=" +
                              credentials.client_id;
            var authorizatonUrl = credentials.auth_uri + queryParams;
            Application.OpenURL(authorizatonUrl);
        }

        /// <summary>
        /// Sends an HTTP request to retrieve an access token from the token uri from user's OAuth2 credentials file.
        /// Schedules a delegate to store the access token once the token is received from the server or to throw an exception once there is a failure to retrieve
        /// the token, and to invoke the post token action passed as an argument to this function once the token has been received and stored.
        /// </summary>
        /// <param name="authCode"></param>
        /// <param name="postTokenAction"></param>
        /// <exception cref="Exception"></exception>
        private static void RequestAndStoreAccessToken(AuthorizationCode authCode, PostTokenAction postTokenAction)
        {
            var credentials = OAuth2Credentials.GetCredentials();
            var tokenEndpiont = credentials.token_uri;
            var formData = new Dictionary<string, string>();
            formData.Add("code", authCode.code);
            formData.Add("client_id", credentials.client_id);
            formData.Add("client_secret", credentials.client_secret);
            formData.Add("redirect_uri", authCode.redirect_uri);
            formData.Add("grant_type", GrantType);

            WwwRequestInProgress.TrackProgress(
                HttpRequestHelper.SendHttpPostRequest(tokenEndpiont, formData, null),
                "Requesting access token",
                doneWww =>
                {
                    var token = JsonUtility.FromJson<GCPAccessToken>(doneWww.text);
                    if (string.IsNullOrEmpty(token.access_token))
                    {
                        throw new Exception(string.Format(
                            "Attempted to get access token and got response with code {0} and text {1}", doneWww.text,
                            doneWww.error));
                    }

                    AccessToken = token;
                    postTokenAction();
                });
        }

        /// <summary>
        /// Represents authorization code received from OAuth2 Protocol when the user authorizes the application, and
        /// is used to get an access token used for making API requests.
        /// </summary>
        [Serializable]
        public class AuthorizationCode
        {
            public string code;
            public string redirect_uri;
        }

        /// <summary>
        /// Represents a Google Cloud Platform access token used for making API requests.
        /// </summary>
        [Serializable]
        public class GCPAccessToken
        {
            public string access_token;
            public string refresh_token;
            public string token_type;
            public int expires_in;
        }
    }
}