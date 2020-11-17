using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadSceneOnVideoComplete : MonoBehaviour
{
    public VideoPlayer player;
    public string sceneToLoad;
    AsyncOperation asyncLoad;

    public float alphaTransitionTime = 1f;
    public CanvasGroup vidCg;

    public Text progressText;

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Prepping " + sceneToLoad);

        asyncLoad = SceneManager.LoadSceneAsync(sceneToLoad);
        asyncLoad.allowSceneActivation = false;

        player.loopPointReached += LoadSceneNow;

        player.Play();
    }

    public void LoadSceneNow(VideoPlayer vidPlayer)
    {
        Debug.Log("Vid complete - load scene");
        asyncLoad.allowSceneActivation = true;
        StartCoroutine(LerpVidTransparency());
    }

    IEnumerator LerpVidTransparency()
    {
        float t = alphaTransitionTime;

        while(t > 0f)
        {
            vidCg.alpha = t / alphaTransitionTime;

            t -= Time.deltaTime;
            yield return null;
        }

        vidCg.alpha = 0f;

        Destroy(this.gameObject);
    }

    public void LoadSceneNow()
    {
        Debug.Log("Force load scene");
        asyncLoad.allowSceneActivation = true;
    }

    IEnumerator LoadSceneProgressBar()
    {
        asyncLoad = SceneManager.LoadSceneAsync(sceneToLoad);
        asyncLoad.allowSceneActivation = false;

        while (asyncLoad.isDone == false)
        {
            progressText.text = "Loading... " + Mathf.RoundToInt(asyncLoad.progress * 100f) + "%";
            if (asyncLoad.progress >= 0.9f)
            {
                progressText.text = "Loading... 100%";
                asyncLoad.allowSceneActivation = true;
                progressText.gameObject.GetComponent<Animator>().SetTrigger("Fade Out");

                yield return new WaitForSeconds(0.33f); // Anim fadeout

                player.Play();
            }
            yield return null;
        }
    }
}
