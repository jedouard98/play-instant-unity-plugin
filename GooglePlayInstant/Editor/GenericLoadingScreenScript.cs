using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using System.Threading;
using System.IO;

public class GenericLoadingScreenScript : MonoBehaviour {
    private AssetBundle bundle;
    IEnumerator Start () 
    {
        Debug.Log("Looking for Resources");
        yield return StartCoroutine(GetAssetBundle());
        GoToGame();
    }

    IEnumerator GetAssetBundle() 
    {
        UnityWebRequest www = UnityWebRequestAssetBundle.GetAssetBundle("__ASSETBUNDLEURL__");

        yield return www.SendWebRequest();
        if(www.isNetworkError || www.isHttpError) {
            Debug.Log(www.error);
        }
        else {
            bundle = DownloadHandlerAssetBundle.GetContent(www);
        }
    }

    void GoToGame()
    {
        string[] scenePaths = bundle.GetAllScenePaths();
        SceneManager.LoadScene(scenePaths[0]);
    }
}