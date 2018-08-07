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
using UnityEngine;

namespace GooglePlayInstant.Editor.QuickDeploy
{
    public static class AccessTokenGetter
    {
        private const string OAuth2GrantType = "authorization_code";

        /// <summary>
        /// Full control scope is required, since the application needs to read, write as well as change access
        /// permissions of buckets and asset bundles.
        /// </summary>
        private const string OAuth2Scope = "https://www.googleapis.com/auth/devstorage.full_control";

        private static KeyValuePair<string, string>? _authorizationResponse;
        
        private static Action<KeyValuePair<string, string>> _onOAuthResponseReceived;

        // Use to store access token.
        private static GCPAccessToken _accessToken;

        /// <summary>
        /// Get Access token to use for API calls if available. Returns null if access token is not available.
        /// </summary>
        public static GCPAccessToken AccessToken
        {
            //TODO(audace): Implement functionality for tokens to be reused in future unity sessions without re-doing the OAuth2 Flow.
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
        public static void UpdateAccessToken(Action postTokenAction)
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
        /// Instantiate the OAuth2 flow to retrieve authorization code for google cloud storage, and schedule
        /// invocation of the code handler on the received authorization code once it is available or throw an exception
        /// once there is a failure to get the authorization code.
        /// </summary>
        /// <param name="authorizationCodeHandler"An action to invoke on the authorization code instance when it is
        /// available.></param>
        /// <exception cref="Exception">Exception thrown when required authorization code cannot be received
        /// from OAuth2 flow.</exception>
        public static void GetAuthCode(Action<AuthorizationCode> onAuthorizationCodeAction)
        {
            var server = new OAuth2Server(authorizationResponse => { _authorizationResponse = authorizationResponse; });
            server.Start();

            var redirectUri = server.CallbackEndpoint;

            _onOAuthResponseReceived = authorizationResponse =>
            {
                if (!string.Equals("code", authorizationResponse.Key))
                {
                    throw new Exception("Could not receive required permissions");
                }

                var authCode = new AuthorizationCode
                {
                    code = authorizationResponse.Value,
                    redirect_uri = redirectUri
                };

                if (onAuthorizationCodeAction != null)
                {
                    onAuthorizationCodeAction(authCode);
                }
            };

            // Take the user to the authorization page to authorize the application.
            var credentials = OAuth2Credentials.GetCredentials();
            var authorizationUrl =
                string.Format("{0}?scope={1}&access_type=offline&redirect_uri={2}&response_type=code&client_id={3}",
                    credentials.auth_uri, OAuth2Scope, redirectUri, credentials.client_id);

            Application.OpenURL(authorizationUrl);
        }

        /// <summary>
        /// Sends an HTTP request to retrieve an access token from the token uri in developer's OAuth2 credentials file.
        /// Schedules an action to store the access token once the token is received from the server or to throw an
        /// exception once there is a failure to retrieve the token, and to invoke the post token action passed as an
        /// argument to this function once the token has been received and stored.
        /// </summary>
        /// <param name="authCode">Authorization code received from OAuth2 to be used to fetch access token.</param>
        /// <param name="postTokenAction">An action to invoke once the token has been received and stored.</param>
        /// <exception cref="Exception">Exception thrown when there is a failure to retrieve access token.</exception>
        private static void RequestAndStoreAccessToken(AuthorizationCode authCode, Action postTokenAction)
        {
            var credentials = OAuth2Credentials.GetCredentials();
            var tokenEndpiont = credentials.token_uri;
            var formData = new Dictionary<string, string>();
            formData.Add("code", authCode.code);
            formData.Add("client_id", credentials.client_id);
            formData.Add("client_secret", credentials.client_secret);
            formData.Add("redirect_uri", authCode.redirect_uri);
            formData.Add("grant_type", OAuth2GrantType);

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
        /// Represents authorization code received from OAuth2 Protocol when the user authorizes the application to
        /// access the cloud, and is used to get an access token used for making API requests.
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
            public int expires_in;
        }
    }
}