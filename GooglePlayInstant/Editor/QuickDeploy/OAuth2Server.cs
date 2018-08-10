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
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using UnityEngine;

[assembly: InternalsVisibleTo("GooglePlayInstant.Tests.Editor.QuickDeploy")]

namespace GooglePlayInstant.Editor.QuickDeploy
{
    /// <summary>
    /// A class representing a server that provides an endpoint to use for getting authorization code from Google's
    /// OAuth2 API's authorization page. On that page, the user grants the application to access their data on Google
    /// Cloud platform. The server attempt to open and start listening at a fixed port. The server announces
    /// its chosen endpoint with the CallbackEndpoint property. This server will run until it receives the first
    /// request, which it will process to retrieve the authorization response and handle the by invoking on the response
    /// the handler passed to the server during instatiation. The server will then stop listening for further requests.
    ///
    /// <see cref="https://developers.google.com/identity/protocols/OAuth2#installed"/> for an overview of OAuth2
    /// protocol for installed applications that interact with Google APIs.
    /// </summary>
    public class OAuth2Server
    {
        private const string CloseTabText = "You may close this tab.";
        internal const string InvalidQueryExceptionMessage = "Uri query is not valid";

        // Arbitrarily chosen port to listen for the authorization callback.
        private const int ServerPort = 2806;

        private readonly HttpListener _httpListener;
        private readonly string _callbackEndpoint;
        private readonly Action<KeyValuePair<string, string>> _onResponseAction;

        /// <summary>
        /// An instance of a server that will run locally to retrieve authorization code. The server will stop running
        /// once the first response gets received.
        /// </summary>
        /// <param name="onResponseAction">An action to be invoked on the key-value pair representing the first
        /// response that will be caught by the server. Note that the invocation of this action will not be done
        /// on Unity's main thread, therefore the action should only be performing operations that can be run off the
        /// main thread. 
        /// </param>
        public OAuth2Server(Action<KeyValuePair<string, string>> onResponseAction)
        {
            _onResponseAction = onResponseAction;
            _callbackEndpoint = string.Format("http://localhost:{0}/{1}/", ServerPort, Path.GetRandomFileName());
            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add(_callbackEndpoint);
        }

        /// <summary>
        /// The callback endpoint on which the server is listening.
        /// </summary>
        public string CallbackEndpoint
        {
            get { return _callbackEndpoint; }
        }

        /// <summary>
        /// Allow this instance to start listening for incoming requests containing authorization code or error data
        /// from OAuth2. Server will stop after processing the first request.
        /// </summary>
        public void Start()
        {
            _httpListener.Start();
            // Server will only respond to the first request, therefore one new thread should handle it just fine.
            new Thread(() => { ProcessContext(_httpListener.GetContext()); }).Start();
        }

        /// <summary>
        /// Processes the object as an HttpListenerContext instance and retrieves authorization response. Invokes the
        /// response handler action on the response, responds to request with a string asking the user to close tab,
        /// and stops the server from listening for future incoming requests.
        /// </summary>
        private void ProcessContext(HttpListenerContext context)
        {
            var authorizationResponse = GetAuthorizationResponse(context.Request.Url);
            _onResponseAction(authorizationResponse);
            context.Response.KeepAlive = false;
            var responsebBytes = Encoding.UTF8.GetBytes(CloseTabText);
            var outputStream = context.Response.OutputStream;
            outputStream.Write(responsebBytes, 0, responsebBytes.Length);
            outputStream.Flush();
            outputStream.Close();
            context.Response.Close();
            Stop();
        }

        /// <summary>
        /// Returns a key value pair corresponding to the authorization response sent from OAuth2 authorization page.
        /// Logs error message and throws ArgumentException if the uri contains invalid params.
        /// </summary>
        /// <param name="uri">The uri of the incoming request</param>
        /// <exception cref="ArgumentException">Exception thrown if the uri contains invalid params.</exception>
        internal static KeyValuePair<string, string> GetAuthorizationResponse(Uri uri)
        {
            if (!UriContainsValidQueryParams(uri))
            {
                const string errorMessage = "Uri query is not valid";
                Debug.LogError(errorMessage);
                throw new ArgumentException(errorMessage);
            }

            return GetQueryParamsFromUri(uri).ToArray()[0];
        }

        /// <summary>
        /// Inspect the URI and determine whether it contains valid params according to the following policies:
        ///   1. URI query must include "code" or "error" in param keys.
        ///   2. "code" and "error" cannot be present at the same time.
        ///   3. No other keys apart from "code", "error" are allowed.
        /// </summary>
        internal static bool UriContainsValidQueryParams(Uri uri)
        {
            var allowedQueries = new[] {"code", "error"};
            var queryParams = GetQueryParamsFromUri(uri);

            Predicate<Dictionary<string, string>> codeOrErrorIsPresent = queryParamsDict =>
                queryParamsDict.ContainsKey("code") || queryParamsDict.ContainsKey("error");

            Predicate<Dictionary<string, string>> notBothCodeAndErrorArePresent = queryParamsDict =>
                !(queryParamsDict.ContainsKey("error") && queryParamsDict.ContainsKey("code"));

            Predicate<Dictionary<string, string>> noOtherKeysArePresent = queryParamsDict =>
                queryParamsDict.Where(kvp => !allowedQueries.Contains(kvp.Key)).ToArray().Length == 0;
            return codeOrErrorIsPresent(queryParams) && notBothCodeAndErrorArePresent(queryParams) &&
                   noOtherKeysArePresent(queryParams);
        }

        /// <summary>
        /// Processes Uri, extracts query params, puts them into a dictionary returns the dictionary.
        /// Uri must not be null.
        /// </summary>
        internal static Dictionary<string, string> GetQueryParamsFromUri(Uri uri)
        {
            var result = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(uri.Query))
            {
                return result;
            }

            // Uri's Query string always starts with "?" so skip the first character.
            var query = uri.Query.Substring(1);
            foreach (var pair in query.Split('&'))
            {
                var keyAndValue = pair.Split('=');
                result.Add(Uri.UnescapeDataString(keyAndValue[0]), Uri.UnescapeDataString(keyAndValue[1]));
            }

            return result;
        }

        /// <summary>
        /// Stops the server from listening from incoming requests.
        /// </summary>
        private void Stop()
        {
            _httpListener.Stop();
        }
    }
}