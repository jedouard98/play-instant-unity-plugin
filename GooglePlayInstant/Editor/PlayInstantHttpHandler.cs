using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Web;
using UnityEngine;
using Random = System.Random;
using NUnit.Framework;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor.Experimental.UIElements;

[assembly:System.Runtime.CompilerServices.InternalsVisibleTo("GooglePlayInstant.Tests.Editor.HttpHandler")]
namespace GooglePlayInstant.Editor
{
    /// <summary>
    /// General handler for operations related to the WWW class, especially post and get requests
    /// </summary>
    public static class RemoteWwwRequestHandler
    {
        /// <summary>
        /// Sends a general post request to the provided endpoint, along with the data provided in the form and headers
        /// parameters
        /// </summary>
        /// <param name="endpoint">Endpoint to which the data should be sent </param>
        /// <param name="postForm">A dictionary representing a set of key-value pairs to be sent in the request body</param>
        /// <param name="postHeaders"> A dictionary representing a set of key-value pairs to be added to the request headers</param>
        /// <returns></returns>
        public static string GetHttpPostResponse(string endpoint, Dictionary<string, string> postForm,
            Dictionary<string, string> postHeaders)
        {
            var form = new WWWForm();

            if (postForm != null)
            {
                foreach (var pair in postForm)
                {
                    form.AddField(pair.Key, pair.Value);
                }
            }

            if (postHeaders != null)
            {
                foreach (var pair in postHeaders)
                {
                    form.headers.Add(pair.Key, pair.Value);
                }
            }

            var www = new WWW(endpoint, form.data, form.headers);
            return GetResultWhenDone(www);
        }

        /// <summary>
        /// Sends a general get request to the specified endpoint along with specified parameters and headers
        /// </summary>
        /// <param name="endpoint">The endpoint to with the get request should be sent</param>
        /// <param name="getParams"> A dictionary representing ke-value pairs that should be attached to the endpoint as GET parameters</param>
        /// <param name="getHeaders">A dictionary representing key-value pairs that constitude the data to be added to the request headers</param>
        /// <returns></returns>
        public static string GetHttpResponse(string endpoint, Dictionary<string, string> getParams,
            Dictionary<string, string> getHeaders)
        {
            var fullEndpoint = endpoint + "?";
            var form = new WWWForm();
            if (getHeaders != null)
            {
                foreach (var pair in getHeaders)
                {
                    form.headers.Add(pair.Key, pair.Value);
                }
            }

            if (getParams != null)
            {
                foreach (var pair in getParams)
                {
                    fullEndpoint += pair.Key + "=" + pair.Value + "&";
                }
            }

            var www = new WWW(fullEndpoint, null, form.headers);
            return GetResultWhenDone(www);
        }


        private static string GetResultWhenDone(WWW www)
        {
            while (!www.isDone)
            {
                Debug.Log("Progress: " + (www.progress * 100).ToString() + "/100 completed");
            }

            return www.text;
        }
    }


    public class Oauth2CallbackEndpointServer
    {
        internal const string CallbackResponseOnSuccess = "<h1>Authorization successful. You may close this window</h1>";

        internal const string CallBackResponseOnError =
            "<h1>Authorization Failed. Could not get necessary permissions</h1>";

        private string _endpointString;
        private int? _port;
        private HttpListener _httpListener;
        private string _callbackEndpoint;

        //private List<Dictionary<string, string>> _responseList = new List<Dictionary<string, string>>();
        private readonly Queue<Oauth2AuthorizationOutcome> _responseQueue = new Queue<Oauth2AuthorizationOutcome>();


        /*
         * Abstraction function:
         * 
         * Rep Invariant:
         * Thread Safety Argument:
         */

        public static void CheckRep()
        {
        }

        internal static string GetRandomEndpoint()
        {
            return GetMD5Hash(new Random().Next(int.MinValue, int.MaxValue).ToString());
        }


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


        private int PortNumber
        {
            get
            {
                if (!_httpListener.IsListening)
                {
                    throw new InvalidStateException("Server is not running");
                }

                return _port.Value;
            }
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


        public void Start()
        {
            // Keep trying available ports until you find one that works, and then break
            while (true)
            {
                var endpointString = GetRandomEndpoint();
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
                catch (HttpListenerException e)
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


        
        // Handle concurrent responses with a thread pool.
        private void HandleResponses()
        {
            while (true)
            {
                ThreadPool.QueueUserWorkItem(ProcessContext, _httpListener.GetContext());
            }
        }


        // Process the current HttpListenerContext, update values and respond to the client;
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
                    var keyValuePair = pair.Split('=');
                    queryDictionary.Add(keyValuePair[0], keyValuePair[1]);
                }
            }

            context.Response.KeepAlive = false;
            if (!queryDictionary.ContainsKey("code") && !queryDictionary.ContainsKey("error") ||
                queryDictionary.Count != 1)
            {
                context.Response.StatusCode = 404;
                context.Response.Close();
                return;
            }

           
            var enumerator = queryDictionary.GetEnumerator();
            enumerator.MoveNext();
            var current = enumerator.Current;
            var response = new Oauth2AuthorizationOutcome
            {
                nature = current.Key,
                value = current.Value
            };
            
            _responseQueue.Enqueue(response);


            var responseArray = Encoding.UTF8.GetBytes(queryDictionary.ContainsKey("code")
                ? CallbackResponseOnSuccess
                : CallBackResponseOnError);
            var outputStream = context.Response.OutputStream;
            outputStream.Write(responseArray, 0, responseArray.Length);
            outputStream.Flush();
            outputStream.Close();
            context.Response.Close();

            Debug.Log("Respone given to a request.");
        }


        /// <summary>
        /// Stops the server's listener. A next call to the Start() method will restart this server as a new instance
        /// on a different endpoint.
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
        /// Find out whether the server has received any authorization code responses so far. Error responses are also considered.
        /// </summary>
        /// <returns></returns>
        public bool HasOauth2AuthorizationOutcome()
        {
            lock (_responseQueue)
            {
                return _responseQueue.Count >= 0;
            }
        }

        /// <summary>
        /// Returns a single code response dictionary from any responses that might have been picked up by the server.
        /// A response is represented as a dictionary corresponding to query params with either code or error as keys that
        /// has been picked up by the server
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidStateException"></exception>
        public Oauth2AuthorizationOutcome getAuthorizationResponse()
        {
            Oauth2AuthorizationOutcome response;
            lock (_responseQueue)
            {
                if (!HasOauth2AuthorizationOutcome())
                {
                    throw new InvalidStateException("Server has not received any responses yet");
                }

                response = _responseQueue.Dequeue();
            }

            return response;
        }
        
        public static void launchServer()
        {
            var server = new Oauth2CallbackEndpointServer();
            server.Start();
            var failResponse = RemoteWwwRequestHandler.GetHttpResponse(server.CallbackEndpoint, null, null);
            Debug.Log("Fail Response: "+failResponse);
            Debug.Assert(failResponse.Equals(CallBackResponseOnError));
            Debug.Log("Endpoint: " + server.CallbackEndpoint);
            while (!server.HasOauth2AuthorizationOutcome())
            {
                
            }

            var authResponse = server.getAuthorizationResponse();
            Debug.Log(authResponse.ToString());
            //server.Stop();
        }

        private static string PrettifyDict(Dictionary<string, string> dict)
        {
            return "{" + string.Join(" , ", dict.Select(pair => pair.Key + " = "+pair.Value).ToArray()) + "}";
        }
    }
    
    /// <summary>
    /// Represents an authorization outcome received on this endpoint from Google Oauth's endpoint;
    /// </summary>

    [Serializable]
    public class Oauth2AuthorizationOutcome
    {
        public string nature;
        public string value;

        public override string ToString()
        {
            return string.Format("{0}={1}", nature, value);
        }

        public bool IsAuthorizationCode()
        {
            return string.Equals(nature, "code");
        }

        public override bool Equals(object other)
        {
            var outcome = other as Oauth2AuthorizationOutcome;
            return outcome != null && isSimilarTo(outcome);
        }

        private bool isSimilarTo(Oauth2AuthorizationOutcome other)
        {
            return string.Equals(nature, other.nature) && string.Equals(value, other.value);
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