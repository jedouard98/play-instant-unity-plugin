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
using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using NUnit.Framework.Constraints;
using UnityEngine;
using Random = System.Random;

[assembly: InternalsVisibleTo("GooglePlayInstant.Tests.Editor.QuickDeploy")]

namespace GooglePlayInstant.Editor
{
    /// <summary>
    /// A class representing a server with the sole purpose of providing an endpoint that listens for the autorization
    /// code from google's OAuth2 API's authorization page, on which the user grants an application access their data
    /// in a given scope.
    ///
    /// <see cref="https://developers.google.com/identity/protocols/OAuth2"/> for an overview of OAuth2 protocol for
    /// Google APIs.
    /// </summary>
    public class QuickDeployOAuth2CallbackEndpointServer
    {
        internal const string CallbackEndpointResponseOnSuccess =
            "<h1>Authorization successful. You may close this window</h1>";

        internal const string CallBackEndpointResponseOnError =
            "<h1>Authorization Failed. Could not get necessary permissions</h1>";

        private HttpListener _httpListener;
        private string _callbackEndpoint;
        private readonly Queue<KeyValuePair<string, string>> _responseQueue = new Queue<KeyValuePair<string, string>>();

        public string CallbackEndpoint
        {
            get
            {
                if (!IsListening())
                {
                    throw new InvalidStateException("Server is not running");
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
            var randomizer = new Random();
            return randomizer.Next(minimumPort, maximumPort).ToString();
        }

        // Helper method to compute an MD5 digest of a string
        internal static string GetMD5Hash(string input)
        {
            var inputAsBytes = Encoding.ASCII.GetBytes(input);
            var hashAsBytes = MD5.Create().ComputeHash(inputAsBytes);

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
                var endpointString = GetRandomEndpointString();
                try
                {
                    // Keep trying different ports until one works
                    var fullEndpoint =
                        string.Format("http://localhost:{0}/{1}/", GetRandomPortAsString(), endpointString);
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
            var responseThread = new Thread(HandleResponses);
            responseThread.Start();
        }


        // Handle concurrent requests with a thread pool.
        private void HandleResponses()
        {
            while (true)
            {
                ThreadPool.QueueUserWorkItem(ProcessContext, _httpListener.GetContext());
            }
        }

        // Process the current HttpListenerContext, retain code or error response and respond to the client.
        private void ProcessContext(object o)
        {
            var context = o as HttpListenerContext;
            var query = context.Request.Url.Query;
            if (query.StartsWith("?"))
            {
                query = query.Substring(1);
            }

            //var keyValuePairs = query.Split('&');
            var queryDictionary = new Dictionary<string, string>();
            foreach (var pair in query.Split('&'))
            {
                if (pair.Contains("="))
                {
                    var keyAndValue = pair.Split('=');
                    queryDictionary.Add(keyAndValue[0], keyAndValue[1]);
                }
            }

            context.Response.KeepAlive = false;

            // Only one query param is allowed, which is either ?code=auth_code or ?error=error_code.
            if (!queryDictionary.ContainsKey("code") && !queryDictionary.ContainsKey("error"))
            {
                context.Response.StatusCode = 404;
                context.Response.Close();
                return;
            }

            KeyValuePair<string, string> responsePair;
            foreach (var pair in queryDictionary)
            {
                if (string.Equals("code", pair.Key) || string.Equals("error", pair.Key))
                {
                    responsePair = pair;
                    _responseQueue.Enqueue(pair);
                    break;
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
        /// Stops the server. A future call of the Start() method on this instance will restart this server on a
        /// different endpoint.
        /// </summary>
        public void Stop()
        {
            _responseQueue.Clear();
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
        /// Find out whether the server has received any authorization code responses so far, including error responses.
        /// </summary>
        public bool HasOauth2AuthorizationResponse()
        {
            lock (_responseQueue)
            {
                return _responseQueue.Count > 0;
            }
        }

        /// <summary>
        /// Returns a KeyValuePair instance corresponding to one of the responses containing authorization code or error
        /// information that this server has received.
        /// </summary>
        /// <exception cref="InvalidStateException">This exception when the server hasn't received any authorization
        /// response yet.</exception>
        public KeyValuePair<string, string> getAuthorizationResponse()
        {
            KeyValuePair<string, string> response;
            lock (_responseQueue)
            {
                if (!HasOauth2AuthorizationResponse())
                {
                    throw new InvalidStateException("Server has not yet received any responses.");
                }

                response = _responseQueue.Dequeue();
            }

            return response;
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