using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// AR scan, place (etc) instructions controller/sequencer
/// Copyright: Richard Beare
/// </summary>
public class ArInstruction : MonoBehaviour
{
    public UnityEvent InstructionEnabledEvent;
    public UnityEvent ActionCompletedEvent; // Optional, if want trigger some other method in some other script on action complete
    public ArAction actionRequired;
    public GameObject optionalPrefab;
    FadeUiElement fader;
    public bool debugForceComplete;
    public bool triggerInstructionAnimTransition;

    public float autoTransitionDuration = 1f;

    public bool overrideFadeOutTransition = false; // Timed event

    string thisName;

    public delegate void ForceStepCompleteEvent();
    public ForceStepCompleteEvent OnForceStepComplete;

    private void OnEnable()
    {
        thisName = name;
        debugForceComplete = false; // Just in case!!

        fader = GetComponent<FadeUiElement>();

        if (actionRequired == ArAction.None)
            StartCoroutine(AutoCompleteStep());

        if (triggerInstructionAnimTransition)
            ArInstructionsController.instance.instructionsAnimator.SetTrigger("NextStep");

        if(fader)
            fader.OnFadeOutComplete += DoneFadeOut;

        if (ArPrefabSpawner.instance)
            ArPrefabSpawner.instance.OnActionTaken += OnActionComplete;

        InstructionEnabledEvent.Invoke();
    }

    private void OnDisable()
    {
        if (actionRequired == ArAction.None)
            OnActionComplete(ArAction.None, null);

        if (ArPrefabSpawner.instance)
            ArPrefabSpawner.instance.OnActionTaken -= OnActionComplete;

        if (fader)
            fader.OnFadeOutComplete -= DoneFadeOut;

        OnForceStepComplete?.Invoke();
    }

    void OnActionComplete(ArAction actionTaken, GameObject prefabInstance)
    {
        if(prefabInstance && optionalPrefab)
        {
            if (prefabInstance != optionalPrefab)
                return;
        }

        if (actionTaken == actionRequired)
        {
            ActionCompletedEvent.Invoke();

            if (!overrideFadeOutTransition)
                fader.FadeOutNow();
            else
                DoneFadeOut();
        }
    }

    void DoneFadeOut()
    {
        ArInstructionsController.instance.TaskComplete();
    }

    private void Update()
    {
        if (debugForceComplete)
        {
            if (fader)
                fader.FadeOutNow();
            else
                ArInstructionsController.instance.TaskComplete();

            debugForceComplete = false;
        }
    }

    IEnumerator AutoCompleteStep()
    {
        yield return new WaitForSeconds(autoTransitionDuration);
        fader.FadeOutNow();
    }
}
