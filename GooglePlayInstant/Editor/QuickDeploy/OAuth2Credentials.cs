using System;
using System.IO;
using UnityEngine;

namespace GooglePlayInstant.Editor.QuickDeploy
{
    /// <summary>
    /// Holds functionality for reading developer's OAuth2 Client Credentials.
    /// </summary>
    public static class OAuth2Credentials
    {
        /// <summary>
        /// Returns a Credentials instance containing credentials from the path specified in Quick Deploy
        /// configurations. Throws an exception if the file cannot be parsed as a valid OAuth2 client ID file.
        /// <see cref="https://console.cloud.google.com/apis/credentials"/>
        /// </summary>
        /// <exception cref="Exception"></exception>
        public static Credentials GetCredentials()
        {
            var credentialsFilePath = QuickDeployConfig.Config.cloudCredentialsFileName;
            var allText = File.ReadAllText(credentialsFilePath);
            var credentialsFile = JsonUtility.FromJson<CredentialsFile>(allText);
            if (credentialsFile == null || credentialsFile.installed == null)
            {
                throw new Exception(string.Format(
                    "File at {0} is not a valid OAuth 2.0 credentials file for installed application. Please visit " +
                    "https://console.cloud.google.com/apis/credentials to create a valid OAuth 2.0 credentials file " +
                    "for your project",
                    credentialsFilePath));
            }

            return credentialsFile.installed;
        }

        /// <summary>
        /// Class representation of the JSON contents of OAuth2 Credentials.
        /// </summary>
        [Serializable]
        public class Credentials
        {
            public string client_id;
            public string client_secret;
            public string auth_uri;
            public string token_uri;
            public string project_id;
        }

        /// <summary>
        /// Class representation of the JSON file containing OAuth2 credentials.
        /// </summary>
        [Serializable]
        public class CredentialsFile
        {
            public Credentials installed;
        }
    }
}