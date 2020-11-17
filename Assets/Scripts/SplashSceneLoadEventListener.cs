using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SplashSceneLoadEventListener : MonoBehaviour
{
    public LoadSceneOnVideoComplete sceneLoader;
    public AudioFadeOut musicFader;

    public void AnimTriggerLoadScene()
    {
        sceneLoader.LoadSceneNow();

        if(musicFader)
            musicFader.FadeAudioNow();
    }

    public void AnimTriggerDestroyCanvas()
    {
        Destroy(transform.root.gameObject);
    }
}
