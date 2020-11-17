using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioFadeOut : MonoBehaviour
{
    AudioSource source;
    public float duration = 1f;

    private void Start()
    {
        source = GetComponent<AudioSource>();
    }

    public void FadeAudioNow()
    {
        StartCoroutine(FadeAudio());
    }

    IEnumerator FadeAudio()
    {
        float vol = source.volume;
        float startVol = source.volume;

        while (vol > 0)
        {
            vol -= Time.deltaTime * startVol / duration;
            source.volume = vol;
            yield return null;
        }

        source.Stop();
    }
}
