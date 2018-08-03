using System;
using System.IO;
using UnityEngine;

namespace GooglePlayInstant.Editor.QuickDeploy
{
    public static class GCPClientHelper
    {
        public static Oauth2Credentials GetOauth2Credentials()
        {
            var allText = File.ReadAllText(QuickDeployConfig.Config.cloudCredentialsFileName);
            return JsonUtility.FromJson<Oauth2File>(allText).installed;
        }
        
        [Serializable]
        public class Oauth2Credentials
        {
            public string client_id;
            public string client_secret;
            public string auth_uri;
            public string token_uri;
            public string project_id;
        }

        [Serializable]
        public class Oauth2File
        {
            public Oauth2Credentials installed;
        }
        
    }
}