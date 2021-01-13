using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HyperlinkButton : MonoBehaviour
{
    public string appUrl;
    public string browserUrl;
    public string oldUrlTesting;
    bool leftApp = false;
    bool noUrl = false;
    int index;

    private void Start()
    {
        if (appUrl.Length == 0)
            noUrl = true;
    }

    private void GetUrl()
    {
        switch (index)
        {
            case 0:
                appUrl = "https://youtu.be/oHg5SJYRHA0";
                break;
            case 1:
                appUrl = "https://youtu.be/wAu_fYHZKLs";
                break;
            case 2:
                appUrl = "https://youtu.be/QXZv-HiMs90";
                break;
        }

        index = (index + 1) % 3;
    }

    public void OpenUrl()
    {
        StartCoroutine(OpenUrlRoutine());
    }

    IEnumerator OpenUrlRoutine()
    {
        Application.OpenURL(appUrl);
        yield return new WaitForSeconds(0.75f);
        if(!leftApp)
        {
            Application.OpenURL(browserUrl);
        }
    }

    private void OnApplicationPause(bool pause)
    {
        leftApp = pause;
    }

    void OnApplicationFocus(bool hasFocus)
    {
        leftApp = !hasFocus;
    }

    /*public void OpenURL() // OLD implementation
    {
        if (noUrl)
            GetUrl();

        Application.OpenURL(appUrl);
        Debug.Log("is this working?");
    }*/
}
