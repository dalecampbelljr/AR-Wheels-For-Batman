using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlashCamera : MonoBehaviour
{
    private static FlashCamera _instance;
    public static FlashCamera instance
    {
        get
        {
            if (!_instance)
            {
                _instance = FindObjectOfType<FlashCamera>();
            }
            return _instance;
        }
    }

    public float flashAlpa = 0.6f;
    public float decayDuration = 1f;
    public float maxBrightnessDuration = 1f;

    public bool testFlashNow;

    private void Update()
    {
        if(testFlashNow)
        {
            CameraFlashNow();
            testFlashNow = false;
        }
    }

    public float CameraFlashNow()
    {
        StartCoroutine(FlashEffect());

        return decayDuration + maxBrightnessDuration;
    }

    public float TotalDuration()
    {
        return maxBrightnessDuration + decayDuration;
    }

    IEnumerator FlashEffect()
    {
        float t = decayDuration;
        MeshRenderer rend = GetComponent<MeshRenderer>();
        Color col = rend.material.color;
        col.a = flashAlpa;
        rend.material.color = col;

        yield return new WaitForSeconds(maxBrightnessDuration);

        while (t > 0f)
        {
            col.a = t / decayDuration * flashAlpa;
            rend.material.color = col;

            t -= Time.deltaTime;
            yield return null;
        }
    }
}
