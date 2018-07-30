//using System.Net;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Web;
using GooglePlayInstant.Editor;
using NUnit.Framework;
using UnityEditor.PackageManager.Requests;
using UnityEditor.U2D;
using UnityEngine;
using Random = System.Random;

namespace GooglePlayInstant.Tests.Editor.QuickDeploy
{
    /// <summary>
    /// Tests for Http Request Helper methods.
    /// </summary>
    [TestFixture]
    public class QuickDeployHttpRequestHelperTest
    {
        /// <summary>
        /// Handler for Http listener contexts.
        /// </summary>
        /// <param name="context">A reference to the HttpListenerContext instance to be handled.</param>
        private delegate void HttpListenerContextHandler(HttpListenerContext context);

        /// <summary>
        /// Tests for SendHttpGetRequest method with no params or headers.
        /// </summary>
        [Test]
        public void TestSendGetRequestNoGetParamsOrHeaders()
        {
            const string expectedResponse = "Request Received";
            HttpListenerContextHandler handler = context =>
            {
                var responseArray = Encoding.UTF8.GetBytes(expectedResponse);
                var outputStream = context.Response.OutputStream;
                outputStream.Write(responseArray, 0, responseArray.Length);
                outputStream.Flush();
                outputStream.Close();
                context.Response.Close();
            };

            var server = new TestServer(handler);
            var wwwObject = QuickDeployHttpRequestHelper.SendHttpGetRequest(server.EndPoint, null, null);
            // request shouldn't take long since the server is running on localhost.
            Thread.Sleep(1000);
            Assert.AreEqual(expectedResponse, wwwObject.text);
            server.Stop();
        }

        /// <summary>
        /// Tests for SendHttpGetRequest method with params but no headers.
        /// </summary>
        [Test]
        public void TestSendGetRequestWithGetParamsNoHeaders()
        {
            var getParams = QuickDeployHttpTestHelper.GetKeyValueDict(10);
            HttpListenerContextHandler handler = context =>
            {
                var outputStream = context.Response.OutputStream;
                // The response is
                var responseArray = Encoding.UTF8.GetBytes(context.Request.Url.Query);
                outputStream.Write(responseArray, 0, responseArray.Length);
                outputStream.Flush();
                outputStream.Close();
                context.Response.Close();
            };

            var server = new TestServer(handler);
            var wwwObject = QuickDeployHttpRequestHelper.SendHttpGetRequest(server.EndPoint, getParams, null);
            Thread.Sleep(1000);
            Assert.AreEqual(QuickDeployHttpTestHelper.GetUrlQueryFromDict(getParams), wwwObject.text);
            server.Stop();
        }

        /// <summary>
        /// Tests for SendHttpGetRequest with headers but no params.
        /// </summary>
        [Test]
        public void TestSendGetRequestWithHeadersNoGetParams()
        {
            var sentHeaders = QuickDeployHttpTestHelper.GetKeyValueDict(10);
            HttpListenerContextHandler handler = context =>
            {
                var outputStream = context.Response.OutputStream;
                var headersDict = new Dictionary<string, string>();
                foreach (var key in context.Request.Headers.AllKeys)
                {
                    headersDict.Add(key, context.Request.Headers[key]);
                }

                // Respond with the URI query string corresponding to all the sent headers.
                var responseArray = Encoding.UTF8.GetBytes(QuickDeployHttpTestHelper.GetUrlQueryFromDict(headersDict));
                outputStream.Write(responseArray, 0, responseArray.Length);
                outputStream.Flush();
                outputStream.Close();
                context.Response.Close();
            };

            var server = new TestServer(handler);
            var wwwObject = QuickDeployHttpRequestHelper.SendHttpGetRequest(server.EndPoint, null, sentHeaders);
            Thread.Sleep(1000);
            var receivedHeaders = QuickDeployHttpTestHelper.GetDictFromUrlQuery(wwwObject.text);
            Assert.IsTrue(!sentHeaders.Except(receivedHeaders).Any());
            server.Stop();
        }

        /// <summary>
        /// Tests for SendHttpPostRequest with form and no headers.
        /// </summary>
        [Test]
        public void TestSendPostRequestWithFormAndNoHeaders()
        {
            var formDict = QuickDeployHttpTestHelper.GetKeyValueDict(10);
            HttpListenerContextHandler handler = context =>
            {
                var formData = new StreamReader(context.Request.InputStream).ReadToEnd();
                var outputStream = context.Response.OutputStream;
                var responseArray = Encoding.UTF8.GetBytes(formData);
                outputStream.Write(responseArray, 0, responseArray.Length);
                outputStream.Flush();
                outputStream.Close();
                context.Response.Close();
            };

            var server = new TestServer(handler);
            var wwwObject = QuickDeployHttpRequestHelper.SendHttpPostRequest(server.EndPoint, formDict, null);
            // request shouldn't take long since the server is running on localhost.
            Thread.Sleep(1000);
            Assert.True(QuickDeployHttpTestHelper.DictsAreEqual(formDict,
                QuickDeployHttpTestHelper.GetDictFromUrlQuery("?" + wwwObject.text)));
            server.Stop();
        }

        /// <summary>
        /// Tests for SendHttpPostRequest method with form and headers.
        /// </summary>
        [Test]
        public void TestSendPostRequestWithFormAndHeaders()
        {
            var formDict = QuickDeployHttpTestHelper.GetKeyValueDict(10);
            var sentHeaders = QuickDeployHttpTestHelper.GetKeyValueDict(10);
            var receivedHeaders = new Dictionary<string, string>();
            HttpListenerContextHandler handler = context =>
            {
                // Headers first
                foreach (var key in context.Request.Headers.AllKeys)
                {
                    receivedHeaders.Add(key, context.Request.Headers[key]);
                }

                // Now form
                var formData = new StreamReader(context.Request.InputStream).ReadToEnd();
                var outputStream = context.Response.OutputStream;
                var responseArray = Encoding.UTF8.GetBytes(formData);
                outputStream.Write(responseArray, 0, responseArray.Length);
                outputStream.Flush();
                outputStream.Close();
                context.Response.Close();
            };

            var server = new TestServer(handler);
            var wwwObject = QuickDeployHttpRequestHelper.SendHttpPostRequest(server.EndPoint, formDict, sentHeaders);
            Thread.Sleep(1000);
            Assert.IsTrue(!sentHeaders.Except(receivedHeaders).Any());
            Assert.IsTrue(QuickDeployHttpTestHelper.DictsAreEqual(formDict,
                QuickDeployHttpTestHelper.GetDictFromUrlQuery("?" + wwwObject.text)));
            server.Stop();
        }

        /// <summary>
        /// Tests SendHttpPostRequest method with bytes
        /// </summary>
        [Test]
        public void TestSendPostRequestWithBytes()
        {
            var dict = QuickDeployHttpTestHelper.GetKeyValueDict(10);
            var sentBytes = Encoding.UTF8.GetBytes(QuickDeployHttpTestHelper.GetUrlQueryFromDict(dict).Substring(1));
            //byte[] receivedBytes = null;
            HttpListenerContextHandler handler = context =>
            {
                var receivedBytes = Encoding.UTF8.GetBytes(new StreamReader(context.Request.InputStream).ReadToEnd());
                var outputStream = context.Response.OutputStream;
                outputStream.Write(receivedBytes, 0, receivedBytes.Length);
                outputStream.Flush();
                outputStream.Close();
                context.Response.Close();
            };

            var server = new TestServer(handler);
            var wwwObject = QuickDeployHttpRequestHelper.SendHttpPostRequest(server.EndPoint, sentBytes, null);
            // request shouldn't take long since the server is running on localhost.
            Thread.Sleep(1000);
            Assert.AreEqual(sentBytes, Encoding.UTF8.GetBytes(wwwObject.text));
            server.Stop();
        }


        /// <summary>
        /// A server to be used by test methods for handling and inspecting http requests and responses.
        /// </summary>
        private class TestServer
        {
            private HttpListenerContextHandler _contextHandler;
            private readonly HttpListener _httpListener;
            private readonly string _endPoint;

            internal string EndPoint
            {
                get { return _endPoint; }
            }

            /// <summary>
            /// Constructs and returns an instance of a server that handles all HttpListenerContexts by invoking the
            /// delegate passed in as a context handler. The server will be listening to an endpoint that can be
            /// accessed by the EndPoint property after the server has been instantiated.
            /// </summary>
            /// <param name="contextHandler">A handler for HttpListenerContexts corresponding to request-response pairs
            /// that this server is going to process. Must not be null.</param>
            internal TestServer(HttpListenerContextHandler contextHandler)
            {
                _contextHandler = contextHandler;
                // Allowed ports range from minimumPort to maximumPort
                const int minimumPort = 1024;
                const int maximumPort = 65535;
                while (true)
                {
                    var endPoint = string.Format("http://localhost:{0}/test/",
                        new Random().Next(minimumPort, maximumPort).ToString());
                    _httpListener = new HttpListener();
                    try
                    {
                        _httpListener.Prefixes.Add(endPoint);
                        _endPoint = endPoint;
                        break;
                    }
                    catch (HttpListenerException)
                    {
                        if (_httpListener != null)
                        {
                            _httpListener.Close();
                        }
                    }
                }

                _httpListener.Start();
                var responseHandler = new Thread(() =>
                {
                    while (true)
                    {
                        ThreadPool.QueueUserWorkItem(
                            o => { _contextHandler.Invoke(o as HttpListenerContext); },
                            _httpListener.GetContext());
                    }
                });
                responseHandler.Start();
            }

            /// <summary>
            /// Stop the server from handling further requests.
            /// </summary>
            internal void Stop()
            {
                _httpListener.Stop();
            }
        }
    }
}