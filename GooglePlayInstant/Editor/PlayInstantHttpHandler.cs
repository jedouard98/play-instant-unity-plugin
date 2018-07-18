using System.Collections.Generic;
using System.Net;
using System.Security.Policy;
using UnityEngine;

namespace GooglePlayInstant.Editor
{
    /// <summary>
    /// General handler for operations related to the WWW class, especially post and get requests
    /// </summary>
    public static class RemoteWwwRequestHandler
    {
        public static List<WWW> wwwList = new List<WWW>();

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
                Debug.Log("Progress: "+ (www.progress*100).ToString() + "/100 completed");
            }

            return www.text;
        }
    }


    public static class Ouauth2LocalHostServer
    {

        public static void Main()
        {
            Debug.Log(RemoteWwwRequestHandler.GetHttpPostResponse(null, null, null));
            Debug.Log(RemoteWwwRequestHandler.GetHttpResponse(null, null, null));
        }
    }

}