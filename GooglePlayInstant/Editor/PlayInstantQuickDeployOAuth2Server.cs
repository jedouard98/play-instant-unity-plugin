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
    /// A class representing a server with the sole purpose of providing an endpoint that listens for the autorization
    /// code from google's OAuth2 API's authorization page, on which the user grants an application access their data
    /// in a given scope.
    ///
    /// <see cref="https://developers.google.com/identity/protocols/OAuth2#installed"/> for an overview of OAuth2
    /// protocol for installed applications that interact with Google APIs.
    /// </summary>
    public class QuickDeployOAuth2Server
    {
        internal const string CallbackEndpointResponseOnSuccess =
            "<h1>Authorization successful. You may close this window</h1>";

        internal const string CallBackEndpointResponseOnError =
            "<h1>Authorization Failed. Could not get necessary permissions</h1>";

        private HttpListener _httpListener;

        private string _callbackEndpoint;

        private KeyValuePair<string, string>? _response;

        public string CallbackEndpoint
        {
            get
            {
                if (!IsListening())
                {
                    throw new InvalidStateException("Server is not running.");
                }

                return _callbackEndpoint;
            }
        }

        internal static string GetRandomEndpointString()
        {
            return GetMD5Hash(new Random().Next(int.MinValue, int.MaxValue).ToString());
        }

        internal static string GetRandomPortAsString()
        {
            const int minimumPort = 1024;
            const int maximumPort = 65535;
            return new Random().Next(minimumPort, maximumPort).ToString();
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
            // Keep trying available ports until you find one that works, and then break
            while (true)
            {
                try
                {
                    // Keep trying different ports until one works
                    var fullEndpoint =
                        string.Format("http://localhost:{0}/{1}/", GetRandomPortAsString(), GetRandomEndpointString());
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
            var responseThread = new Thread(() =>
            {
                while (true)
                {
                    ThreadPool.QueueUserWorkItem(ProcessContext, _httpListener.GetContext());
                }
            });
            responseThread.Start();
        }

        /// <summary>
        /// Process the context and retrieve authorization response.
        /// </summary>
        /// <param name="o"></param>
        private void ProcessContext(object o)
        {
            var context = o as HttpListenerContext;
            context.Response.KeepAlive = false;
            if (UriContainsValidQueryParams(context.Request.Url))
            {
                context.Response.StatusCode = 404;
                context.Response.Close();
                return;
            }

            KeyValuePair<string, string> responsePair;
            Dictionary<string, string> queryDictionary = null;
            foreach (var pair in GetQueryParamsFromUri(context.Request.Url, ref queryDictionary))
            {
                if (string.Equals("code", pair.Key) || string.Equals("error", pair.Key))
                {
                    _response = pair;
                }
            }

            var responseArray = Encoding.UTF8.GetBytes(string.Equals("code", responsePair.Key)
                ? CallbackEndpointResponseOnSuccess
                : CallBackEndpointResponseOnError);
            var outputStream = context.Response.OutputStream;
            outputStream.Write(responseArray, 0, responseArray.Length);
            outputStream.Flush();
            outputStream.Close();
            context.Response.Close();
        }

        /// <summary>
        /// Inspect the uri and determine whether it contains valid params to be considered.
        /// </summary>
        private bool UriContainsValidQueryParams(Uri uri)
        {
            // Policy 1: URI must contain query params (query starts with "?").
            // Policy 2: The only acceptable query params "code", "error", and "scope".

            var allowedQueries = new[] {"code", "error", "scope"};
            Dictionary<string, string> queryParams = null;
            var uriRespectsPolicies = uri.Query.StartsWith("?") &&
                                      (GetQueryParamsFromUri(uri, ref queryParams).ContainsKey("code") ||
                                       queryParams.ContainsKey("error")) &&
                                      queryParams.Where(kvp => !allowedQueries.Contains(kvp.Key)).ToArray().Length == 0;
            return uriRespectsPolicies;
        }

        /// <summary>
        /// Process uri, extract query params and return them in a dictionary. Provided uri must contain query params.
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
        /// Stops the server. A future call of the Start() method on this instance will restart this server on a
        /// different endpoint.
        /// </summary>
        public void Stop()
        {
            if (!IsListening())
            {
                return;
            }

            if (_httpListener != null)
            {
                _httpListener.Close();
                _httpListener = null;
            }
        }

        internal bool IsListening()
        {
            return _httpListener != null && _httpListener.IsListening;
        }

        /// <summary>
        /// Find out whether the server has received any authorization code or error response.
        /// </summary>
        public bool HasOauth2AuthorizationResponse()
        {
            return _response.HasValue;
        }

        /// <summary>
        /// Returns a KeyValuePair instance corresponding to the response received from authorizing the application.
        /// </summary>
        public KeyValuePair<string, string> GetAuthorizationResponse()
        {
            return _response.Value;
        }
    }

    /// <summary>
    /// Represents an exception that should be thrown when there are any inconsistencies between methods being executed
    /// values being acceesed and the current state.
    /// </summary>
    internal class InvalidStateException : Exception
    {
        public InvalidStateException(string message) : base(message)
        {
        }
    }
}