// Copyright 2018 Google LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

/// <summary>
/// A GenericLoadingScreenScript that is
/// </summary>
public class GenericLoadingScreenScript : MonoBehaviour
{
    private AssetBundle _bundle;

    private IEnumerator Start()
    {
        yield return StartCoroutine(GetAssetBundle());
        SceneManager.LoadScene(_bundle.GetAllScenePaths()[0]);
    }

    private IEnumerator GetAssetBundle()
    {
        #if UNITY_2018_2_OR_NEWER
            var www = UnityWebRequestAssetBundle.GetAssetBundle("__ASSETBUNDLEURL__");
            
        #else
            var www = UnityWebRequest.GetAssetBundle("__ASSETBUNDLEURL__");
    
        #endif
        
        yield return www.SendWebRequest();
        
        if (www.isNetworkError || www.isHttpError)
        {
            Debug.Log(www.error);
        }
        else
        {
            _bundle = DownloadHandlerAssetBundle.GetContent(www);
        }
    }
}