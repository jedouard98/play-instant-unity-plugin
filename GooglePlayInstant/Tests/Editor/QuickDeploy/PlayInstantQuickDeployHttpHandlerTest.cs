//using System.Net;

using System.Net;
using  System.Web;
using NUnit.Framework;
using UnityEditor.U2D;
using UnityEngine;
using Random = System.Random;

namespace GooglePlayInstant.Tests.Editor.QuickDeploy
{
    [TestFixture]
    public class QuickDeployWwwRequestHandlerTest
    {
        private delegate void ContextHandler(HttpListenerContext context);
        [Test]
        public void TestSendGetRequestNoGetParamsOrHeaders()
        {
            // TODO request to a blank url, no params or headers
        }

        [Test]
        public void TestSendGetRequestWithGetParamsNoHeaders()
        {
        }

        [Test]
        public void TestSendGetRequestWithHeadersNoGetParams()
        {
        }

        [Test]
        public void TestSendGetRequestWithHeadersAndGetParams()
        {
        }

        [Test]
        public void TestSendPostRequestWithFormAndNoHeaders()
        {
        }

        [Test]
        public void TestSendPostRequestWithFormAndHeaders()
        {
        }

        [Test]
        public void TestSendPostRequestWithBytes()
        {
        }

        private class TestServer
        {
            private ContextHandler _contextHandler;
            private HttpListener _httpListener;
            public TestServer(ContextHandler contextHandler)
            {
                _contextHandler = contextHandler;
                while (true)
                {
                    const int minimumPort = 1024;
                    const int maximumPort = 65535;
                    string endPoint = string.Format("http://localhost:{0}/test", new Random(mini).);
                }
            }
        }
    }
}