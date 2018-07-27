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
    [TestFixture]
    public class QuickDeployWwwRequestHandlerTest
    {
        private delegate void ContextHandler(HttpListenerContext context, ref Thread handlingThread);

        [Test]
        public void TestSendGetRequestNoGetParamsOrHeaders()
        {
            const string expectedResponse = "Request Received";
            ContextHandler handler = (HttpListenerContext context, ref Thread handlingThread) =>
            {
                var outputStream = context.Response.OutputStream;
                var responseArray = Encoding.UTF8.GetBytes(expectedResponse);
                outputStream.Write(responseArray, 0, responseArray.Length);
                outputStream.Flush();
                outputStream.Close();
                context.Response.Close();
                handlingThread.Abort();
            };

            var server = new TestServer(handler);
            var wwwObject = QuickDeployWwwRequestHandler.SendHttpGetRequest(server.EndPoint, null, null);
            // request shouldn't take long since the server is running on localhost.
            Thread.Sleep(1000);
            Assert.AreEqual(expectedResponse, wwwObject.text);
            server.Stop();
        }

        [Test]
        public void TestSendGetRequestWithGetParamsNoHeaders()
        {
            var getParams = QuickDeployHttpTestHelper.GetKeyValueDict(10);
            ContextHandler handler = (HttpListenerContext context, ref Thread handlingThread) =>
            {
                var outputStream = context.Response.OutputStream;
                // The response is
                var responseArray = Encoding.UTF8.GetBytes(context.Request.Url.Query);
                outputStream.Write(responseArray, 0, responseArray.Length);
                outputStream.Flush();
                outputStream.Close();
                context.Response.Close();
                handlingThread.Abort();
            };

            var server = new TestServer(handler);
            var wwwObject = QuickDeployWwwRequestHandler.SendHttpGetRequest(server.EndPoint, getParams, null);
            Thread.Sleep(1000);
            Assert.AreEqual(QuickDeployHttpTestHelper.GetUriQueryFromDict(getParams), wwwObject.text);
            server.Stop();
        }

        [Test]
        public void TestSendGetRequestWithHeadersNoGetParams()
        {
            var sentHeaders = QuickDeployHttpTestHelper.GetKeyValueDict(10);
            ContextHandler handler = (HttpListenerContext context, ref Thread handlingThread) =>
            {
                var outputStream = context.Response.OutputStream;
                var headersDict = new Dictionary<string, string>();
                foreach (var key in context.Request.Headers.AllKeys)
                {
                    headersDict.Add(key, context.Request.Headers[key]);
                }

                // Respond with the URI query string corresponding to all the sent headers.
                var responseArray = Encoding.UTF8.GetBytes(QuickDeployHttpTestHelper.GetUriQueryFromDict(headersDict));
                outputStream.Write(responseArray, 0, responseArray.Length);
                outputStream.Flush();
                outputStream.Close();
                context.Response.Close();
                handlingThread.Abort();
            };

            var server = new TestServer(handler);
            var wwwObject = QuickDeployWwwRequestHandler.SendHttpGetRequest(server.EndPoint, null, sentHeaders);
            Thread.Sleep(1000);
            var receivedHeaders = QuickDeployHttpTestHelper.GetDictFromUriQuery(wwwObject.text);
            Assert.IsTrue(!sentHeaders.Except(receivedHeaders).Any());
            server.Stop();
        }

        [Test]
        public void TestSendPostRequestWithFormAndNoHeaders()
        {
            var formDict = QuickDeployHttpTestHelper.GetKeyValueDict(10);
            ContextHandler handler = (HttpListenerContext context, ref Thread handlingThread) =>
            {
                var formData = new StreamReader(context.Request.InputStream).ReadToEnd();
                var outputStream = context.Response.OutputStream;
                var responseArray = Encoding.UTF8.GetBytes(formData);
                outputStream.Write(responseArray, 0, responseArray.Length);
                outputStream.Flush();
                outputStream.Close();
                context.Response.Close();
                handlingThread.Abort();
            };

            var server = new TestServer(handler);
            var wwwObject = QuickDeployWwwRequestHandler.SendHttpPostRequest(server.EndPoint, formDict, null);
            // request shouldn't take long since the server is running on localhost.
            Thread.Sleep(1000);
            Assert.True(QuickDeployHttpTestHelper.DictsAreEqual(formDict,
                QuickDeployHttpTestHelper.GetDictFromUriQuery("?" + wwwObject.text)));
            server.Stop();
        }

        [Test]
        public void TestSendPostRequestWithFormAndHeaders()
        {
            var formDict = QuickDeployHttpTestHelper.GetKeyValueDict(10);
            var sentHeaders = QuickDeployHttpTestHelper.GetKeyValueDict(10);
            var receivedHeaders = new Dictionary<string, string>();
            ContextHandler handler = (HttpListenerContext context, ref Thread handlingThread) =>
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
                handlingThread.Abort();
            };

            var server = new TestServer(handler);
            var wwwObject = QuickDeployWwwRequestHandler.SendHttpPostRequest(server.EndPoint, formDict, sentHeaders);
            Thread.Sleep(1000);
            Assert.IsTrue(!sentHeaders.Except(receivedHeaders).Any());
            Assert.IsTrue(QuickDeployHttpTestHelper.DictsAreEqual(formDict,
                QuickDeployHttpTestHelper.GetDictFromUriQuery("?" + wwwObject.text)));
            server.Stop();
        }

        [Test]
        public void TestSendPostRequestWithBytes()
        {
            var dict = QuickDeployHttpTestHelper.GetKeyValueDict(10);
            var sentBytes = Encoding.UTF8.GetBytes(QuickDeployHttpTestHelper.GetUriQueryFromDict(dict).Substring(1));
            //byte[] receivedBytes = null;
            ContextHandler handler = (HttpListenerContext context, ref Thread handlingThread) =>
            {
                var receivedBytes = Encoding.UTF8.GetBytes(new StreamReader(context.Request.InputStream).ReadToEnd());
                var outputStream = context.Response.OutputStream;
                outputStream.Write(receivedBytes, 0, receivedBytes.Length);
                outputStream.Flush();
                outputStream.Close();
                context.Response.Close();
                handlingThread.Abort();
            };

            var server = new TestServer(handler);
            var wwwObject = QuickDeployWwwRequestHandler.SendHttpPostRequest(server.EndPoint, sentBytes, null);
            // request shouldn't take long since the server is running on localhost.
            Thread.Sleep(1000);
            Assert.AreEqual(sentBytes, Encoding.UTF8.GetBytes(wwwObject.text));
            server.Stop();
        }


        private class TestServer
        {
            private ContextHandler _contextHandler;
            private HttpListener _httpListener;
            private string _endPoint;

            internal string EndPoint
            {
                get { return _endPoint; }
            }

            internal TestServer(ContextHandler contextHandler)
            {
                _contextHandler = contextHandler;
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

                Thread responseHandler = null;
                responseHandler = new Thread(() =>
                {
                    while (true)
                    {
                        ThreadPool.QueueUserWorkItem(
                            o => { _contextHandler.Invoke(o as HttpListenerContext, ref responseHandler); },
                            _httpListener.GetContext());
                    }
                });
                responseHandler.Start();
            }

            internal void Stop()
            {
                _httpListener.Stop();
            }
        }
    }
}