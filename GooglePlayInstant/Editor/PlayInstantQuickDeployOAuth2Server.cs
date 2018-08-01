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
        /// on the main thread, therefore the handler should only operations that can be run off the main thread.</param>
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
                    // Keep trying different ports until one works
                    var fullEndpoint =
                        string.Format("http://localhost:{0}/{1}/", GetRandomPort(), GetRandomEndpoint());
                    _httpListener = new HttpListener();
                    _httpListener.Prefixes.Add(fullEndpoint);
                    _callbackEndpoint = fullEndpoint;
                    break;
                }
                // thrown when port/endpoint is busy
                catch (HttpListenerException)
                {
                    if (_httpListener != null)
                    {
                        _httpListener.Close();
                        _httpListener = null;
                    }
                }
            }

            _httpListener.Start();

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
        /// the response handler on the response, and responds to request with a script that will close the browser.
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
                return;
            }

            Dictionary<string, string> queryDictionary = null;
            KeyValuePair<string, string> responsePair;
            foreach (var pair in GetQueryParamsFromUri(context.Request.Url, ref queryDictionary))
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

            var responseArray = Encoding.UTF8.GetBytes("<script>window.close();</script>");
            var outputStream = context.Response.OutputStream;
            outputStream.Write(responseArray, 0, responseArray.Length);
            outputStream.Flush();
            outputStream.Close();
            context.Response.Close();
            Stop();
        }

        /// <summary>
        /// Inspect the uri and determine whether it contains valid params according to the following policies:
        ///   1. URI query must not be empty.
        ///   2. URI query must include exactly one of either "code" or "error" as param keys.
        ///   3. Only other key that is allowed in the param keys is "scope". 
        /// </summary>
        private bool UriContainsValidQueryParams(Uri uri)
        {
            var allowedQueries = new[] {"code", "error", "scope"};
            Dictionary<string, string> queryParams = null;
            var uriRespectsPolicies = uri.Query.StartsWith("?") &&
                                      (GetQueryParamsFromUri(uri, ref queryParams).ContainsKey("code") ||
                                       queryParams.ContainsKey("error")) &&
                                      !(queryParams.ContainsKey("error") && queryParams.ContainsKey("code")) &&
                                      queryParams.Where(kvp => !allowedQueries.Contains(kvp.Key)).ToArray().Length == 0;
            return uriRespectsPolicies;
        }

        /// <summary>
        /// Process uri, extract query params, put them into a dictionary and assign result's reference to the
        /// dictionary and return result if result's reference is null. Otherwise just return result.
        /// </summary>
        private static Dictionary<string, string> GetQueryParamsFromUri(Uri uri, ref Dictionary<string, string> result)
        {
            if (result != null)
            {
                return result;
            }

            result = new Dictionary<string, string>();
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
        private void Stop()
        {
            _httpListener.Close();
        }
    }
}