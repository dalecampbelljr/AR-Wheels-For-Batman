using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraViewCollider : MonoBehaviour
{
    public static bool isColliding;
    [SerializeField] bool _isColliding;

    private void OnTriggerEnter(Collider other)
    {
        isColliding = true;
        _isColliding = isColliding;
    }

    private void OnTriggerExit(Collider other)
    {
        isColliding = false;
        _isColliding = isColliding;
    }
}
