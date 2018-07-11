using System.Net;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEditor;
using UnityEngine.Assertions;
using System.IO;
using System.Runtime.Serialization.Json;
using System;
using System.Threading;
using UnityEngine.Networking;
using Object = System.Object;


// The class provides persistence needed to store developer inputs accross different
// sessions of unity without having to reenter the fields every time

namespace GooglePlayInstant.Deployer
{
    public static class DeveloperFieldInputs
    {
        
        private static string _dataStorageFilePath = ".google_cloud_play.json";
        private static string _credentialsPath = "";
        private static string _localAssetBundlePath = "";
        private static string _remoteBucketName = "";
        private static string _remoteObjectName = "";
        private static string _remoteProjectId = "";
        
        private static Object lockObject = new Object();
        
        public static string CredentialsPath
        {
            get
            {
                if (_credentialsPath.Equals(""))
                {
                    RetrieveDeveloperInputs();
                    
                }
                return _credentialsPath;
            }
            set
            {
                if (value.Equals(_credentialsPath)) return;
                _credentialsPath = value;
                StoreDeveloperInputs();
            }
        }

        public static string LocalAssetBundlePath
        {
            get
            {
                if (_localAssetBundlePath.Equals(""))
                {
                    RetrieveDeveloperInputs();
                }

                return _localAssetBundlePath;
            }
            set
            {
                if (value.Equals(_localAssetBundlePath)) return;
                _localAssetBundlePath = value;
                StoreDeveloperInputs();
            }
        }

        public static string RemoteBucketName
        {
            get
            {
                if (_remoteBucketName.Equals(""))
                {
                    RetrieveDeveloperInputs();
                }

                return _remoteBucketName;
            }
            set
            {
                if (value.Equals(_remoteBucketName)) return;
                _remoteBucketName = value;
                StoreDeveloperInputs();
            }
        }

        public static string RemoteObjectName
        {
            get
            {
                if (_remoteObjectName.Equals(""))
                {
                    RetrieveDeveloperInputs();
                }

                return _remoteObjectName;
            }
            set
            {
                if (value.Equals(_remoteObjectName)) return;
                _remoteObjectName = value;
                StoreDeveloperInputs();
            }
        }

        public static string RemoteProjectId
        {
            get
            {
                if (_remoteProjectId.Equals(""))
                {
                    RetrieveDeveloperInputs();
                }

                return _remoteProjectId;
            }
            set
            {
                if (value.Equals(_remoteProjectId)) return;
                _remoteProjectId = value;
                StoreDeveloperInputs();
            }
        }


        public static void StoreDeveloperInputs()
        {
            //return;
            FieldsDataObject fieldsData = new FieldsDataObject
            {
                credentialsPath = _credentialsPath.Length > 0 ? _credentialsPath : "path_to_credentials.json",
                localAssetBundlePath = _localAssetBundlePath.Length > 0 ? _localAssetBundlePath : "path_to_assetbundle",
                remoteBucketName = _remoteBucketName.Length > 0 ? _remoteBucketName : "remote_bucket_name",
                remoteObjectName = _remoteObjectName.Length > 0 ? _remoteObjectName : "remote_object_name",
                remoteProjectId = _remoteProjectId.Length > 0 ? _remoteProjectId : "remote_project_id"
            };

            MemoryStream memoryStream = new MemoryStream();
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(FieldsDataObject));

            serializer.WriteObject(memoryStream, fieldsData);

            memoryStream.Seek(0, SeekOrigin.Begin);

            lock (lockObject)
            {
                using (FileStream fs = new FileStream(_dataStorageFilePath, FileMode.OpenOrCreate))
                {
                    fs.Position = 0;
                    fs.SetLength(memoryStream.Length);
                    memoryStream.CopyTo(fs);
                    fs.Flush();
                    fs.Close();
                    fs.Dispose();
                }
            }

        }

        public static void RetrieveDeveloperInputs()
        {
            if (!File.Exists(_dataStorageFilePath))
            {
                StoreDeveloperInputs();
            }
            
            /*
            _credentialsPath = _credentialsPath.Length > 0 ? _credentialsPath : "path_to_credentials.json";
            _localAssetBundlePath = _localAssetBundlePath.Length > 0 ? _localAssetBundlePath : "path_to_assetbundle";
            _remoteBucketName = _remoteBucketName.Length > 0 ? _remoteBucketName : "remote_bucket_name";
            _remoteObjectName = _remoteObjectName.Length > 0 ? _remoteObjectName : "remote_object_name";
            _remoteProjectId = _remoteProjectId.Length > 0 ? _remoteProjectId : "remote_project_id";

            return; */
            FieldsDataObject fieldsDataObject;

            lock (lockObject)
            {
                byte[] data = File.ReadAllBytes(_dataStorageFilePath);
                MemoryStream memorystream = new MemoryStream(data);
            
            
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(FieldsDataObject));
                memorystream.Position = 0;
                fieldsDataObject  =  (FieldsDataObject) serializer.ReadObject(memorystream);
                memorystream.Close();
                memorystream.Dispose();
            }
            
            _credentialsPath = fieldsDataObject.credentialsPath;
            _localAssetBundlePath = fieldsDataObject.localAssetBundlePath;
            _remoteBucketName = fieldsDataObject.remoteBucketName;
            _remoteObjectName = fieldsDataObject.remoteObjectName;
            _remoteProjectId = fieldsDataObject.remoteProjectId;
        }
    }

    [DataContract]
    internal class FieldsDataObject
    {
        [DataMember]
        internal string credentialsPath;
        [DataMember]
        internal string localAssetBundlePath;
        [DataMember]
        internal string remoteBucketName;
        [DataMember]
        internal string remoteObjectName;
        [DataMember]
        internal string remoteProjectId;
    }


}