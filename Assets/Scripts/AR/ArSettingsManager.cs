using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// Global AR settings
/// Copyright - Richard Beare. Free to edit. 2020-06-09
/// </summary>

public class ArSettingsManager : MonoBehaviour
{
    public static ArSettingsManager _instance;
    public static ArSettingsManager instance
    {
        get
        {
            if (!_instance)
            {
                _instance = FindObjectOfType<ArSettingsManager>();
            }
            return _instance;
        }
    }

    ARPlaneManager planeManager;

    public enum DetectionMode { None, Horizontal, Vertical, Everything }

    [SerializeField] public PlaneDetectionMode planeDetectionMode = PlaneDetectionMode.Horizontal | PlaneDetectionMode.Vertical;
    [SerializeField] public bool planesVisible = true;
    [SerializeField] public bool prefabPlacementAllowed = true;
    [SerializeField] public bool objectSelectionAllowed = true;
    [SerializeField] public bool objectMovementAllowed = true;
    [SerializeField] public bool objectScalingAllowed = true;
    [SerializeField] public bool objectRotateAllowed = true;
    [SerializeField] public bool debugClickInput = false;

    public Material shadowMat;

    public delegate void PlaneVisibilityChangedEvent(bool visible);
    public PlaneVisibilityChangedEvent OnPlaneVisibilityChanged;

    private void Awake()
    {
        planeManager = GetComponent<ARPlaneManager>();
    }

    public void EnableShadowShader()
    {
        foreach (ARPlane plane in planeManager.trackables)
            if(plane)
            {
                plane.GetComponent<MeshRenderer>().material = shadowMat;
            }
    }

    public void SetArPlaneVisibility(bool visible)
    {
        planesVisible = visible;

        OnPlaneVisibilityChanged?.Invoke(visible);
    }

    public void ToggleArPlaneVisibility()
    {
        planesVisible = !planesVisible;

        OnPlaneVisibilityChanged?.Invoke(planesVisible);
    }

    public void SetArPlacementPermission(bool allow)
    {
        prefabPlacementAllowed = allow;
    }

    public void SetObjectSelectionPermission(bool allow)
    {
        objectSelectionAllowed = allow;
    }

    public void SetObjectMovementPermission(bool allow)
    {
        objectMovementAllowed = allow;
    }

    public void SetObjectRotatePermission(bool allow)
    {
        objectRotateAllowed = allow;
    }

    public void SetObjectScalePermission(bool allow)
    {
        objectScalingAllowed = allow;
    }

    public void SetAllObjectManipulationPermissions(bool allow)
    {
        objectScalingAllowed = allow;
        objectRotateAllowed = allow;
        objectMovementAllowed = allow;
        objectSelectionAllowed = allow;
        prefabPlacementAllowed = allow;
    }

    public void ToggleAllObjectMovementPermissions()
    {
        objectRotateAllowed = !objectRotateAllowed;
        objectMovementAllowed = !objectMovementAllowed;
    }

    public void SetAllObjectMovementPermissions(bool allow)
    {
        objectRotateAllowed = allow;
        objectMovementAllowed = allow;
    }

    public void SetArPlaneDetectionMode(int mode) // Not using!
    {
        return; // Not using!

        //DetectionMode mode = DetectionMode.Everything;
        switch (mode)
        {
            case 0:
                planeDetectionMode = PlaneDetectionMode.None;
                SetPlaneDetection(false);
                break;
            case 1:
                planeDetectionMode = PlaneDetectionMode.Horizontal;
                SetPlaneDetection(true);
                break;
            case 2:
                planeDetectionMode = PlaneDetectionMode.Vertical;
                SetPlaneDetection(true);
                break;
            case 3:
                planeDetectionMode = PlaneDetectionMode.Horizontal | PlaneDetectionMode.Vertical;
                SetPlaneDetection(true);
                break;
        }

        planeManager.detectionMode = planeDetectionMode;
    }

    public void SetPlaneDetection(bool allowDetection)
    {
        if (planeManager.enabled != allowDetection)
        {
            planeManager.enabled = allowDetection;

            if (planeManager.enabled)
                SetAllPlanesActive(true);
            else
                SetAllPlanesActive(false);
        }
    }

    void SetAllPlanesActive(bool value)
    {
        foreach (var plane in planeManager.trackables)
            plane.gameObject.SetActive(value);
    }
}
