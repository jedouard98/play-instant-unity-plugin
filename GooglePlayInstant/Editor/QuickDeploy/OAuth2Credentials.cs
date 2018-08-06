using System;
using System.IO;
using UnityEngine;

namespace GooglePlayInstant.Editor.QuickDeploy
{
    public static class OAuth2Credentials
    {
        public static Credentials GetCredentials()
        {
            var allText = File.ReadAllText(QuickDeployConfig.Config.cloudCredentialsFileName);
            return JsonUtility.FromJson<CredentialsFile>(allText).installed;
        }
        
        [Serializable]
        public class Credentials
        {
            public string client_id;
            public string client_secret;
            public string auth_uri;
            public string token_uri;
            public string project_id;
        }

        [Serializable]
        public class CredentialsFile
        {
            public Credentials installed;
        }
        
    }
}