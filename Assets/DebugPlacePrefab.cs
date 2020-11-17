using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugPlacePrefab : MonoBehaviour
{
#if UNITY_EDITOR

    void OnEnable()
    {
        GetComponent<ArInstruction>().OnForceStepComplete += SpawnPrefabDebug;
    }

    void OnDisable()
    {
        GetComponent<ArInstruction>().OnForceStepComplete -= SpawnPrefabDebug;
    }

    void SpawnPrefabDebug()
    {
        ArPrefabSpawner.instance.InstantiateDebug();
    }
    
#endif
}
