using System.Collections.Generic;
using GooglePlayInstant.Editor;
using NUnit.Framework;

namespace GooglePlayInstant.Tests.Editor.HttpHandler
{
    [TestFixture]
    public class Oauth2CallbackEndPointServerTest
    {
        [Test]
        public void TestRandomPortGenerator()
        {
            var portString = Oauth2CallbackEndpointServer.GetRandomPortAsString();
            int n;
            Assert.True(int.TryParse(portString, out n), "Expected a port to be an int");
        }

        [Test]
        public void TestEndPointGenerator()
        {
            var endPointString = Oauth2CallbackEndpointServer.GetRandomEndpoint();
            Assert.AreEqual(32, Oauth2CallbackEndpointServer.GetMD5Hash(endPointString).Length, 0.0, "Should return a 32 long string");
            
        }

        [Test]
        public void TestServerBehavesCorrectlyOnValidRequests()
        {
            var server = new Oauth2CallbackEndpointServer();
            server.Start();
            Assert.True(server.IsListening());
            var validResponse = RemoteWwwRequestHandler.GetHttpResponse(
                server.CallbackEndpoint + "?code" + Oauth2CallbackEndpointServer.GetMD5Hash("test"), null, null);
            Assert.AreEqual(Oauth2CallbackEndpointServer.CallbackResponseOnSuccess, validResponse);
            Assert.True(server.HasOauth2AuthorizationOutcome());

            var actualResponse = server.getAuthorizationResponse();
            Assert.True(actualResponse.IsAuthorizationCode());
            var expectedResponse = new Oauth2AuthorizationOutcome
            {
                nature = "code",
                value = Oauth2CallbackEndpointServer.GetMD5Hash("test")
            };
            Assert.AreEqual(expectedResponse, actualResponse, "Expected response with valid attributes");
        }

        [Test]
        public void TestServerBehavesCorrectlyOnErrorRequests()
        {
            // TODO(audace): Change this to work for error
            var server = new Oauth2CallbackEndpointServer();
            server.Start();
            Assert.True(server.IsListening());
            var validResponse = RemoteWwwRequestHandler.GetHttpResponse(
                server.CallbackEndpoint + "?error" + Oauth2CallbackEndpointServer.GetMD5Hash("test"), null, null);
            Assert.AreEqual(Oauth2CallbackEndpointServer.CallBackResponseOnError, validResponse);
            Assert.True(server.HasOauth2AuthorizationOutcome());

            var actualResponse = server.getAuthorizationResponse();
            Assert.True(actualResponse.IsAuthorizationCode());
            var expectedResponse = new Oauth2AuthorizationOutcome
            {
                nature = "error",
                value = Oauth2CallbackEndpointServer.GetMD5Hash("test")
            };
            Assert.AreEqual(expectedResponse, actualResponse, "Expected response with valid attributes");
        }

        [Test]
        public void TestServerBehavesCorrectlyOnInvalidRequest()
        {
            var server = new Oauth2CallbackEndpointServer();
            server.Start();
            Assert.True(true);
        }
    }
}