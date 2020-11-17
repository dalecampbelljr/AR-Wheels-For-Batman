using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArInstructionsController : MonoBehaviour
{
    private static ArInstructionsController _instance;
    public static ArInstructionsController instance
    {
        get
        {
            if (!_instance)
            {
                _instance = FindObjectOfType<ArInstructionsController>();
            }
            return _instance;
        }
    }

    [SerializeField]
    List<ArInstruction> instructions;
    int currentInstruction = 0;
    public Animator instructionsAnimator;
    public bool debugForceComplete;
    public List<ArInstruction> reselectDimensionerInstructions = new List<ArInstruction>();

    bool debugAllowed = true;

    private void Update()
    {
        if(debugForceComplete && debugAllowed) 
        {
            TaskComplete();
            debugForceComplete = false;
        }
    }

    void Start()
    {
        debugForceComplete = false; // Just in case 

        if (instructions.Count == 0)
        {
            instructions = new List<ArInstruction>();

            foreach (Transform t in transform)
            {
                ArInstruction instruction = t.GetComponent<ArInstruction>();
                if (instruction)
                {
                    instructions.Add(instruction);
                    instruction.gameObject.SetActive(false);
                }
            }
        }

        instructions[currentInstruction].gameObject.SetActive(true);
    }

    private void OnDisable()
    {
        ArSettingsManager.instance.SetAllObjectManipulationPermissions(true);
        ArSettingsManager.instance.SetArPlaneVisibility(false);
    }

    public void TaskComplete()
    {
        instructions[currentInstruction].gameObject.SetActive(false);

        currentInstruction++;

        if (currentInstruction < instructions.Count)
            instructions[currentInstruction].gameObject.SetActive(true);
        else
            OnAllStepsComplete();        
    }

    public void OnAllStepsComplete()
    {
        debugAllowed = false;
    }

    public void ReselectDimensioner()
    {
        debugAllowed = true;
        instructions = reselectDimensionerInstructions;
        RestartInstructions();
    }

    void RestartInstructions()
    {
        debugAllowed = true;
        currentInstruction = 0;
        instructions[currentInstruction].gameObject.SetActive(true);
    }
}
