using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

[RequireComponent(typeof(CanvasGroup))]
public class FadeCanvasGroup : MonoBehaviour
{
	CanvasGroup canvas;
	public float fadeDuration = 1f;
	public bool disabledOnStart = false;

	public UnityEvent OnFadeOutComplete, OnFadeInComplete;

    private void Start()
    {
		canvas = GetComponent<CanvasGroup>();


		if (disabledOnStart)
        {
			canvas.interactable = false;
            canvas.blocksRaycasts = false;
            canvas.alpha = 0f;
        }
    }


    public void EnableCanvasGroup()
    {
		StartCoroutine(FadeIn());
    }

	public void DisableCanvasGroup()
	{
		StartCoroutine(FadeOut());
	}

	IEnumerator FadeIn()
    {
		float t = 0f;
        canvas.blocksRaycasts = true;

        while (t < fadeDuration)
		{
			canvas.alpha = t / fadeDuration;
			//previewImage.canvasRenderer.SetColor(col);

			t += Time.deltaTime;
			yield return null;
		}

		canvas.alpha = 1f;

		canvas.interactable = true;

        OnFadeInComplete?.Invoke();
	}
	IEnumerator FadeOut()
	{
		canvas.interactable = false;

        float t = fadeDuration;

		while (t > 0f)
		{
			canvas.alpha = t / fadeDuration;

			t -= Time.deltaTime;
			yield return null;
		}

		canvas.alpha = 0f;
        canvas.blocksRaycasts = false;

        OnFadeOutComplete?.Invoke();
	}
}
