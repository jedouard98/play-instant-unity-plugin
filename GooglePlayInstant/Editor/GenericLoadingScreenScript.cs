using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;

public class GenericLoadingScreenScript : MonoBehaviour {
    private AssetBundle bundle;
    IEnumerator Start () 
    {
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
        SceneManager.LoadScene(bundle.GetAllScenePaths()[0]);
    }
}