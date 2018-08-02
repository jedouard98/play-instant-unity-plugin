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
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

[assembly: InternalsVisibleTo("GooglePlayInstant.Tests.Editor.QuickDeploy")]

namespace GooglePlayInstant.Editor
{
    /// <summary>
    /// A class representing a server that provides an endpoint to use for getting authorization code from Google's
    /// OAuth2 API's authorization page, on which the user grants the application to access their data on Google
    /// Cloud platform.
    ///
    /// <see cref="https://developers.google.com/identity/protocols/OAuth2#installed"/> for an overview of OAuth2
    /// protocol for installed applications that interact with Google APIs.
    /// </summary>
    public class QuickDeployOAuth2Server
    {
        internal const string CloseTabScript = "<script>window.close();</script>";
        internal HttpListener _httpListener;
        private string _callbackEndpoint;

        /// <summary>
        /// A delegate that will handle the KeyValuePair that represents the authorization response.
        /// </summary>
        /// <param name="response">A response received through a GET request that is sent to the server.</param>
        public delegate void ResponseHandler(KeyValuePair<string, string> response);

        private readonly ResponseHandler _responseHandler;

        /// <summary>
        /// An endpoint that on which the server is listening.
        /// </summary>
        public string CallbackEndpoint
        {
            get { return _callbackEndpoint; }
        }

        /// <summary>
        /// An instance of a server that will run locally to retrieve authorization code. The server will stop running
        /// once the first response gets received.
        /// </summary>
        /// <param name="responseHandler">A response handler to be invoked on the key-value pair representing the first
        /// response that will be caught by the server. Note that that the invocation of this handler will not be done
        /// on the main thread, therefore the handler should only operations that can be run off the main thread.
        /// No invocation is done if responseHandler is null.
        /// </param>
        public QuickDeployOAuth2Server(ResponseHandler responseHandler)
        {
            _responseHandler = responseHandler;
        }

        internal static string GetRandomEndpoint()
        {
            return GetMD5Hash(new Random().Next(int.MinValue, int.MaxValue).ToString());
        }

        internal static int GetRandomPort()
        {
            const int minimumPort = 1024;
            const int maximumPort = 65535;
            return new Random().Next(minimumPort, maximumPort);
        }

        // Helper method to compute an MD5 digest of a string
        internal static string GetMD5Hash(string input)
        {
            var hashAsBytes = MD5.Create().ComputeHash(Encoding.ASCII.GetBytes(input));
            // Convert to hex string
            var sb = new StringBuilder();
            for (var i = 0; i < hashAsBytes.Length; i++)
            {
                sb.Append(hashAsBytes[i].ToString("X2"));
            }

            return sb.ToString();
        }


        /// <summary>
        /// Starts this server to make it listen for requests containing authorization code or error data that are
        /// forwarded from google's OAuth2 authorization url.
        ///
        /// After a call to this method, the CallBackEndpoint property of this instance will provide the endpoint
        /// at which this server is listening.
        /// </summary>
        public void Start()
        {
            // Pick an available port and then break.
            while (true)
            {
                try
                {
                    var fullEndpoint =
                        string.Format("http://localhost:{0}/{1}/", GetRandomPort(), GetRandomEndpoint());
                    _httpListener = new HttpListener();
                    _httpListener.Prefixes.Add(fullEndpoint);
                    _callbackEndpoint = fullEndpoint;
                    _httpListener.Start();
                    break;
                }
                // thrown when port/endpoint is busy
                catch (HttpListenerException)
                {
                    // Debug.Log("Got this error");
                    if (_httpListener != null)
                    {
                        _httpListener.Close();
                        // Set the reference to null for garbage collection.
                        _httpListener = null;
                    }
                }
            }

            // Handle incoming requests with another thread.
            new Thread(() =>
            {
                while (true)
                {
                    ThreadPool.QueueUserWorkItem(ProcessContext, _httpListener.GetContext());
                }
            }).Start();
        }

        /// <summary>
        /// Processes the object as an HttpListenerContext instancce and retrieves authorization response. Invokes
        /// the response handler on the response if response handler is not null, and responds to request with a
        /// string corresponding to a script that will close the browser.
        /// </summary>
        /// <param name="o"></param>
        private void ProcessContext(object o)
        {
            var context = o as HttpListenerContext;
            context.Response.KeepAlive = false;
            if (!UriContainsValidQueryParams(context.Request.Url))
            {
                context.Response.StatusCode = 404;
                context.Response.Close();
                Stop();
                return;
            }

            Dictionary<string, string> queryDictionary = null;
            KeyValuePair<string, string> responsePair;
            foreach (var pair in GetQueryParamsFromUri(context.Request.Url))
            {
                if (string.Equals("code", pair.Key) || string.Equals("error", pair.Key))
                {
                    responsePair = pair;
                    break;
                }
            }

            if (_responseHandler != null)
            {
                _responseHandler.Invoke(responsePair);
            }

            var responseArray = Encoding.UTF8.GetBytes(CloseTabScript);
            var outputStream = context.Response.OutputStream;
            outputStream.Write(responseArray, 0, responseArray.Length);
            outputStream.Flush();
            outputStream.Close();
            context.Response.Close();
            Stop();
        }

        /// <summary>
        /// Inspect the URI and determine whether it contains valid params according to the following policies:
        ///   1. URI query must include exactly one of either "code" or "error" as param keys.
        ///   2. "code" and "error" can not be present at the same time.
        ///   3. No other keys apart from "code", "error" and "scope" are allowed.
        ///   4. "scope" should only be present when there is "code".
        /// </summary>
        internal static bool UriContainsValidQueryParams(Uri uri)
        {
            var allowedQueries = new[] {"code", "error", "scope"};
            var queryParams = GetQueryParamsFromUri(uri);

            Predicate<Dictionary<string, string>> policy1 = queryParamsDict =>
                queryParamsDict.ContainsKey("code") || queryParamsDict.ContainsKey("error");

            Predicate<Dictionary<string, string>> policy2 = queryParamsDict =>
                !(queryParamsDict.ContainsKey("error") && queryParamsDict.ContainsKey("code"));

            Predicate<Dictionary<string, string>> policy3 = queryParamsDict =>
                queryParamsDict.Where(kvp => !allowedQueries.Contains(kvp.Key)).ToArray().Length == 0;

            Predicate<Dictionary<string, string>> policy4 = queryParamsDict =>
                !(queryParams.ContainsKey("scope") && !queryParams.ContainsKey("code"));

            return policy1.Invoke(queryParams) && policy2.Invoke(queryParams) && policy3.Invoke(queryParams) &&
                   policy4.Invoke(queryParams);
        }

        /// <summary>
        /// Processes URI, extracts query params, puts them into a dictionary returns the dictionary.
        /// </summary>
        private static Dictionary<string, string> GetQueryParamsFromUri(Uri uri)
        {
            var result = new Dictionary<string, string>();
            foreach (var pair in uri.Query.Substring(1).Split('&'))
            {
                if (pair.Contains("="))
                {
                    var keyAndValue = pair.Split('=');
                    result.Add(keyAndValue[0], keyAndValue[1]);
                }
            }

            return result;
        }

        /// <summary>
        /// Stops the server. A future call of the Start() method on this instance will restart this server at a
        /// different endpoint.
        /// </summary>
        internal void Stop()
        {
            _httpListener.Close();
            // Assign to null so that it can be garbage collected.
            _httpListener = null;
        }
    }
}