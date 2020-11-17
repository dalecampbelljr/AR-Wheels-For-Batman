using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class UiPanelController : MonoBehaviour
{
    Animator anim;
    public  UnityEvent OnHide, OnReveal;

    private void Start()
    {
        anim = GetComponent<Animator>();
    }

    public void RevealPanel()
    {
        anim.SetBool("Reveal", true);
        OnReveal?.Invoke();
    }

    public void HidePanel()
    {
        anim.SetBool("Reveal", false);
        OnHide?.Invoke();
    }
}
