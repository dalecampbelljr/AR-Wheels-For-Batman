using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.Experimental.XR;
using System;

public class ArPlaneController : MonoBehaviour
{
    public float duration = 2f;

    Material planeMat;
    Material lineMat;

    float origAlpha;
    float curAlpha;
    float targetAlpha;

    float t = 0f;

    private void Awake()
    {
        planeMat = gameObject.GetComponent<MeshRenderer>().material;
        lineMat = gameObject.GetComponent<LineRenderer>().material;

        origAlpha = planeMat.color.a;
        curAlpha = origAlpha;

        targetAlpha = origAlpha;

        if (!ArSettingsManager.instance.planesVisible)
        {
            targetAlpha = 0f;

            Color tempPlaneCol = planeMat.color;
            tempPlaneCol.a = targetAlpha;
            planeMat.color = tempPlaneCol;

            Color tempLineCol = lineMat.color;
            tempLineCol.a = targetAlpha;
            lineMat.color = tempLineCol;
        }

        ArSettingsManager.instance.OnPlaneVisibilityChanged += SetPlaneVisibility;

        StartCoroutine(ControlVisibility());
    }

    IEnumerator ControlVisibility()
    {
        float a;
        Color tempPlaneCol = planeMat.color;
        Color tempLineCol = lineMat.color;

        curAlpha = planeMat.color.a;

        while (true)
        {
            a = Mathf.Lerp(curAlpha, targetAlpha, t / duration);

            if (planeMat != null && lineMat != null)
            {
                tempPlaneCol.a = a;
                planeMat.color = tempPlaneCol;

                tempLineCol.a = a;
                lineMat.color = tempLineCol;
            }

            t += Time.deltaTime;

            yield return null;
        }
    }

    public void SetPlaneVisibility(bool visible)
    {
        t = 0f;

        curAlpha = planeMat.color.a;

        if (visible)
            targetAlpha = origAlpha;
        else
            targetAlpha = 0f;
    }
}

