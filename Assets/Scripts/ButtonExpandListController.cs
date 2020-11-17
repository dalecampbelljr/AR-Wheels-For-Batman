using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonExpandListController : MonoBehaviour
{
    public List<ExpandableButton> buttons;
    public float expandSpeed = 100f; // Normalize to resolution

    void Start()
    {
        
    }

    public void Expand()
    {
        foreach(ExpandableButton button in buttons)
        {
            StartCoroutine(MoveButton(button));
        }
    }

    public void Contract()
    {

    }

    IEnumerator MoveButton(ExpandableButton button)
    {
        Transform buttonT = button.transform;
        Vector3 targetPos = button.targetPosition.position;

        while (true)
        {
            buttonT.position += (targetPos - buttonT.position).normalized * Time.deltaTime * expandSpeed;

            yield return null;
        }
    }
}

[SerializeField]
public class ExpandableButton : MonoBehaviour
{
    public Transform targetPosition;
    public GameObject buttonPrefab;
}