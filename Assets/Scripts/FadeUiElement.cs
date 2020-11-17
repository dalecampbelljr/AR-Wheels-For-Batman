using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FadeUiElement : MonoBehaviour
{
    public float duration = 2f;
    public float delay = 2f;

   // Image image;
    public Text text;
    Button button;
    public bool useButton = true;

    Color targetImageCol;
    Color targetTextCol;

    public Image image;

    public delegate void FadeOutCompleteEvent(); // Used by instructions controller
    public FadeOutCompleteEvent OnFadeOutComplete;

    public delegate void FadeInCompleteEvent(); // Unused
    public FadeInCompleteEvent OnFadeInComplete;

    public bool fadeInOnAwake = true;

    string thisName; // For dbugging
    Coroutine crt;

    bool firstEnable = false;

    private void Start()
    {
        if (image)
            targetImageCol = image.color;
        if (text)
            targetTextCol = text.color;
    }

    private void OnEnable()
    {
        if (firstEnable)
            StartCoroutine(FadeIn());

        firstEnable = true;
    }

    private void Awake()
    {
        thisName = name;

        if (!text)
            text = GetComponentInChildren<Text>();

        if (!image)
            image = GetComponentInChildren<Image>();

        if (useButton)
            button = GetComponentInChildren<Button>();

        if (text)
        {
            targetTextCol = text.color;
            Color cText = targetTextCol;
            cText.a = 0f;
            text.color = cText;
        }

        if (fadeInOnAwake)
            crt = StartCoroutine(FadeIn());
    }

    public void FadeOutNow()
    {
        if (crt != null)
            StopCoroutine(crt);

        StartCoroutine(FadeOut(1f, false));
    }

    public void FadeOutNow(float startAlpha)
    {
        if (crt != null)
            StopCoroutine(crt);

        StartCoroutine(FadeOut(startAlpha, false));
    }

    public void FadeOutDisable()
    {
        if (crt != null)
            StopCoroutine(crt);

        StartCoroutine(FadeOut(1f, true));
    }

    IEnumerator FadeIn()
    {
        yield return null;

        if (Time.time < 1f)
            yield return new WaitForSeconds(1f);

        Color cImage = Color.white;
        if (image)
        {
            cImage = image.color;
            cImage.a = 0f;
            image.color = cImage;
        }

        Color cText = Color.white;;
        if (text)
        {
            cText = text.color;
            cText.a = 0f;
            text.color = cText;
        }

        if (button)
            button.interactable = false;

        yield return new WaitForSecondsRealtime(delay);

        if (button)
            button.interactable = true;

        float t = 0f;

        while (t < duration)
        {
            if (image)
            {
                cImage.a = t / duration * targetImageCol.a;
                image.color = cImage;
            }

            if (text)
            {
                cText.a = t / duration;
                text.color = cText;
            }

            t += Time.unscaledDeltaTime;

            yield return null;
        }

        OnFadeInComplete?.Invoke();
    }

    IEnumerator FadeOut(float startAlpha, bool disableOnComplete)
    {
        yield return null;

        Color cImage = Color.white;
        if (image)
        {
            cImage = image.color;
            cImage.a = 1f;
            image.color = cImage;
        }

        Color cText = Color.white;
        if (text)
        {
            cText = text.color;
            cText.a = 1f;
            text.color = cText;
        }

        if (button)
            button.interactable = false;

        yield return new WaitForSecondsRealtime(delay);
        
        float t = duration;

        while (t > 0)
        {
            if (image)
            {
                cImage.a = t / duration * targetImageCol.a;
                image.color = cImage;
            }

            if (text)
            {
                cText.a = t / duration;
                text.color = cText;
            }

            t -= Time.unscaledDeltaTime;

            yield return null;
        }

        OnFadeOutComplete?.Invoke();

        if (disableOnComplete)
            gameObject.SetActive(false);
    }

    float freq = 0.2f;

    IEnumerator SineFade()
    {
        yield return null;
           // Color cImage = image.color;
           // cImage.a = 0f;
           // image.color = cImage;
        

        Color cText = text.color;
        cText.a = 0f;
        text.color = cText;

        float t = 0f;

        t = Mathf.Asin(-1f) / (2 * Mathf.PI * freq);
        Debug.Log("sad " + Mathf.Sin(t * 2 * Mathf.PI * freq));
        float sinAlpha;

        while (true)
        {
            sinAlpha = (Mathf.Sin(t * 2 * Mathf.PI * freq) * 0.25f + 0.25f) * .33f;

          //  cImage.a = sinAlpha;
          //  image.color = cImage;

            cText.a = sinAlpha;
            text.color = cText;

            t += Time.unscaledDeltaTime;

            yield return null;
        }
    }

    private void OnDisable()
    {
        /*  if (image)
        {
            Color cImage = targetImageCol;
            cImage.a = 0f;
            image.color = cImage;
        }
        */

        if (text)
        {
            Color cText = targetTextCol;
            cText.a = 0f;
            text.color = cText;
        }

        if(image)
        {
            Color cImage= targetImageCol;
            cImage.a = 0f;
            image.color = cImage;
        }

        if (crt != null)
            StopCoroutine(crt);
    }
}
