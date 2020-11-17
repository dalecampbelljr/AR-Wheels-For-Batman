using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public enum ArAction { None, HorizPlaneDetected, VertPlaneDetected, PrefabPlaced, PrefabSelected, PrefabScaled, PrefabRotated, PrefabMoved, PrefabDeselected, PrefabChanged }

/// <summary>
/// Placement and manipulation of AR objects
/// Copyright - Richard Beare. Free to edit.
/// </summary>

[RequireComponent(typeof(AudioSource))]
public class ArPrefabSpawner : MonoBehaviour
{
    private static ArPrefabSpawner _instance;
    public static ArPrefabSpawner instance
    {
        get
        {
            if (!_instance)
            {
                _instance = FindObjectOfType<ArPrefabSpawner>();
            }
            return _instance;
        }
    }

    #region Vars

    ARRaycastManager raycastManager;

    //public List<GameObject> prefabs; // This might be the maze game (as a prefab)
    [SerializeField] List<string> prefabNames; // Loacted in Resources folder

    List<GameObject> prefabInstances = new List<GameObject>(); // Holds reference to the prefab that was instantiated (default is null)
    Transform selectedInst;
    public Transform grabPos;
    Rigidbody heldRb;

    ArAction lastAction = ArAction.None;

    public delegate void ActionTakenEvent(ArAction actionType, GameObject inst);
    public ActionTakenEvent OnActionTaken;

    enum Mode { Moving, Unselected, Selected, ScaleRotate }
    [SerializeField] Mode mode = Mode.Unselected;

    AudioSource source;
    public AudioClip selectClip, placeClip, moveClip, scaleClip;
    public float pitchModifier = 1f;

    ArPlanesInfo allPlanesInfo = new ArPlanesInfo();
    bool lockPlaneInfoUpdates = false;

    Vector3 farAwayPos = new Vector3(-1000f, -1000f, -1100f); // neg inf?

    public LayerMask raycastSelectMask;

    bool interactionsAllowed = true;

#if UNITY_EDITOR
    public bool debugPlace;
#endif

#endregion

    // Start is called before the first frame update
    void Start()
    {
        raycastManager = GetComponent<ARRaycastManager>(); // Automatically get it
        if (raycastManager)
            Debug.Log("got it raycast Mgr");
        source = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        GetComponent<ARPlaneManager>().planesChanged += OnPlanesChanged;
    }
    private void OnDisable()
    {
        GetComponent<ARPlaneManager>().planesChanged -= OnPlanesChanged;
    }

    public bool debugDestroyDimensioner;

    void Update()
    {
        //CheckPermissions();

        if(debugDestroyDimensioner)
        {
            ClearInstances();
            debugDestroyDimensioner = false;
        }

#if UNITY_EDITOR
        if(debugPlace)
        {
            debugPlace = false;
            InstantiateDebug();
        }
#endif
        if (interactionsAllowed)
        {
            if (Input.touchCount == 2)
            {
                if (selectedInst)
                {
                    if (scalerCrt == null && ArSettingsManager.instance.objectScalingAllowed)
                    {
                        Debug.Log("scale " + selectedInst.name);
                        scalerCrt = StartCoroutine(ScaleObject(selectedInst));
                        mode = Mode.ScaleRotate;
                    }
                    if (rotateCrt == null && ArSettingsManager.instance.objectRotateAllowed)
                    {
                        Debug.Log("rot " + selectedInst.name);
                        rotateCrt = StartCoroutine(RotateObject(selectedInst));
                        mode = Mode.ScaleRotate;
                    }
                }
            }
            else if (Input.touchCount == 1) // Need to check that player is touching screen
            {
                bool emptySpace; // pointing that way

                Vector2 touchPoint = Input.GetTouch(0).position; // Position on the screen of phone (2 dimensional)

                HitNormal hitNorm = GetArRaycastHitPosition(touchPoint);

                Vector3 arHitPos = hitNorm.hitPos;

                if (Input.GetTouch(0).phase == TouchPhase.Began)
                {
                    emptySpace = false;

                    Debug.Log("Single touch registered ");

                    if (mode != Mode.Moving) // *** Select or instantiate ***
                    {
                        Debug.Log("Not moving obj");

                        if (ArSettingsManager.instance.objectSelectionAllowed)
                        {
                            if (ObjectSelect(touchPoint)) // Attempt object selection
                            {
                                Debug.Log("Selected " + selectedInst.name);

                                mode = Mode.Selected;
                            }
                            else if (!hitNorm.hitValid)
                            {
                                emptySpace = true;
                            }
                        }
                        else
                        {
                            DeselectObject();
                        }

                        if (hitNorm.hitValid)
                        {
                            emptySpace = false;

                            if (mode == Mode.Unselected && prefabInstances.Count == 0 && ArSettingsManager.instance.prefabPlacementAllowed)
                            {
                                Debug.Log("Attempt start Inst method");
                                InstantiateAr(arHitPos, hitNorm.orientationOfNormal);
                            }
                        }

                        if (emptySpace)
                        {
                            DeselectObject();
                        }
                    }
                }
                else if (selectedInst != null && mode != Mode.ScaleRotate && ArSettingsManager.instance.objectMovementAllowed) // *** Move object ***
                {
                    if (selectedInst.tag != "SelectableNoMove")
                    {
                        if (hitNorm.hitValid)
                        {
                            AttemptArPlaneMove(arHitPos, hitNorm);
                        }
                        else if (!heldRb) // looking in space
                        {
                            AttemptGrabMove();
                        }

                        mode = Mode.Moving;

                        StartCoroutine(FireActionTakenEvent(ArAction.PrefabMoved, selectedInst.gameObject));


                        if (source.clip != moveClip)
                        {
                            PlaySoundRandomPitch(moveClip, false);
                        }
                    }
                }
            }
            else if (mode == Mode.Moving || mode == Mode.ScaleRotate) // *** Done moving object ***
            {
                StopScaleRot();
            }
        }
    }

    void DeselectObject()
    {
        if (selectedInst)
        {
            Debug.Log("Deselecting inst: " + selectedInst.name);
            StartCoroutine(FireActionTakenEvent(ArAction.PrefabDeselected, selectedInst.gameObject));
        }
        else
            Debug.Log("selectedInst is null");


        selectedInst = null;
        selectedChildInst = null;

        mode = Mode.Unselected;
        Debug.Log("Deselected");
    }

    HitNormal GetArRaycastHitPosition(Vector2 touchPoint)
    {
        List<ARRaycastHit> hitsInfo = new List<ARRaycastHit>(); // Stores info about what was hit

        HitNormal hitNorm = new HitNormal(farAwayPos, Quaternion.identity, false);

        float maxDist = 25f;

        if (raycastManager.Raycast(touchPoint, hitsInfo, UnityEngine.XR.ARSubsystems.TrackableType.PlaneWithinBounds)) // Returns true if plane is hit
        {
            if (Vector3.Distance(hitsInfo[0].pose.position, Camera.main.transform.position) < maxDist)
            {
                hitNorm.hitPos = hitsInfo[0].pose.position;
                hitNorm.orientationOfNormal = hitsInfo[0].pose.rotation;
                hitNorm.hitValid = true;
            }
        }

        return hitNorm;
    }

    void InstantiateAr(Vector3 hitPos, Quaternion hitRot)
    {
        Debug.Log("Attempt inst");

        GameObject prefab = Resources.Load(prefabNames[prefabInstances.Count]) as GameObject;

        GameObject newInst = Instantiate(prefab, hitPos, prefab.transform.rotation);

        StartCoroutine(FireActionTakenEvent(ArAction.PrefabPlaced, prefab));

        prefabInstances.Add(newInst);

        RotateFacePlayer(newInst);

        PlaySoundRandomPitch(placeClip, true);

        ArSettingsManager.instance.SetArPlaneVisibility(false);
    }

#if UNITY_EDITOR
    public void InstantiateDebug()
    {
        Debug.Log("Attempt inst");

        GameObject prefab = Resources.Load(prefabNames[prefabInstances.Count]) as GameObject;

        GameObject newInst = Instantiate(prefab, Vector3.zero, prefab.transform.rotation);

        StartCoroutine(FireActionTakenEvent(ArAction.PrefabPlaced, prefab));

        prefabInstances.Add(newInst);

        RotateFacePlayer(newInst);

        PlaySoundRandomPitch(placeClip, true);

        ArSettingsManager.instance.SetArPlaneVisibility(false);
    }
#endif

    public void ClearInstances()
    {
        GameObject[] instancesCopy = new GameObject[prefabInstances.Count];
        prefabInstances.CopyTo(instancesCopy);

        for (int i = 0; i < instancesCopy.Length; i++)
        {
            Destroy(instancesCopy[i]);
        }

        prefabInstances = new List<GameObject>();

        /*
        foreach (GameObject instance in prefabInstances)
        {
            prefabInstances.Remove(instance);
            Destroy(instance);
        }*/
    }

    public void ChangePrefab(string prefab)
    {
        prefabNames[0] = prefab;
        StartCoroutine(FireActionTakenEvent(ArAction.PrefabChanged, Resources.Load(prefab) as GameObject));
    }

    Transform selectedChildInst;

    bool ObjectSelect(Vector2 touchPoint)
    {
        bool selected = false;

        selectedChildInst = null;

        Ray ray = Camera.main.ScreenPointToRay(new Vector3(touchPoint.x, touchPoint.y, 0f), Camera.MonoOrStereoscopicEye.Mono);

        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, raycastSelectMask))
        {
            Transform hitTransform = hit.transform;

            if (prefabInstances.Contains(hitTransform.gameObject) || hitTransform.tag == "SelectableNoMove")
            {
                selectedInst = hitTransform;

                selected = true;
            }
            else if (hitTransform.tag == "Selectable")
            {
                if (prefabInstances.Contains(hitTransform.root.gameObject))
                {
                    selectedInst = hitTransform.root;

                    selectedChildInst = hitTransform;

                    selected = true;
                }
            }
        }

        if (selected)
        {
            Debug.Log("Selected " + selectedInst.name);
            PlaySoundRandomPitch(selectClip, true);
            StartCoroutine(FireActionTakenEvent(ArAction.PrefabSelected, selectedInst.gameObject));
            return true;
        }
        else
            return false;
    }

#region Ar Planes

    public void DetectPlanesDeltaChange(bool lockPlaneInfoUpdates)
    {
        this.lockPlaneInfoUpdates = lockPlaneInfoUpdates; // Prevent changing plane info, so can analyze change in size in OnPlanesChanged (for tutorial plane-scanning HorizPlaneDetected event)
    }

    void OnPlanesChanged(ARPlanesChangedEventArgs eventArgs)
    {
        foreach (ARPlane plane in eventArgs.added)
        {
            if (!allPlanesInfo.ContainsPlane(plane))
                allPlanesInfo.AddPlane(plane, plane.size.x, plane.size.y);
        }

        foreach (ARPlane updatedPlane in eventArgs.updated)
        {
            ArPlaneInfo planeInfo = allPlanesInfo.GetPlaneInfo(updatedPlane);

            if (planeInfo != null) // It containts the plane that was updated, and is that same plane
            {
                if (updatedPlane.size.x > planeInfo.xSize + 0.2f && updatedPlane.size.y > planeInfo.ySize + 0.2f) // Some min size
                {
                    if (updatedPlane.alignment == UnityEngine.XR.ARSubsystems.PlaneAlignment.HorizontalUp)
                        StartCoroutine(FireActionTakenEvent(ArAction.HorizPlaneDetected, null));

                    else if (updatedPlane.alignment == UnityEngine.XR.ARSubsystems.PlaneAlignment.Vertical)
                        StartCoroutine(FireActionTakenEvent(ArAction.VertPlaneDetected, null));
                }

                if (!lockPlaneInfoUpdates)
                {
                    planeInfo.xSize = updatedPlane.size.x;
                    planeInfo.ySize = updatedPlane.size.y;
                }
            }
        }
    }

#endregion

    void PlaySoundRandomPitch(AudioClip clip, bool useRandPitch)
    {
        if (source && clip)
        {
            source.clip = clip;

            if (useRandPitch)
                source.pitch = Random.Range(0.75f, 1.25f) * pitchModifier;
            else
                source.pitch = 1f;

            source.Play();
        }
    }

    IEnumerator FireActionTakenEvent(ArAction actionMade, GameObject inst)
    {
        bool isObjectManipulateAction = (actionMade == ArAction.PrefabMoved || actionMade == ArAction.PrefabScaled || actionMade == ArAction.PrefabRotated);

        if (actionMade == lastAction && isObjectManipulateAction) // same object, same thing, don't fire event
            yield break;

        if (inst)
            Debug.Log("Action invoked: " + actionMade.ToString() + " on " + inst.name);
        else
            Debug.Log("Action invoked: " + actionMade.ToString());

        OnActionTaken?.Invoke(actionMade, inst); // Inform whoever's listening (e.g. ArInstructionController)

        lastAction = actionMade;

        yield return null;
    }

    void StopScaleRot()
    {
        Debug.Log("Stop scale rot");
        selectedInst.transform.parent = null;

        DeselectObject();
        source.clip = null;

        if (heldRb)
        {
            heldRb.isKinematic = false;
            heldRb = null;
        }

        if (scalerCrt != null)
            StopCoroutine(scalerCrt);

        if (rotateCrt != null)
            StopCoroutine(rotateCrt);

        scalerCrt = null;
        rotateCrt = null;
        Debug.Log("scale rot stopped");
    }

    public void EnableInteractions()
    {
        interactionsAllowed = true;
    }

    public void DisableInteractions()
    {
        interactionsAllowed = false;
    }

    #region Object Manipulation

    public void LockUnlockObjects(bool locked)
    {
        if (locked)
        {
            Debug.Log("Locked");
            ArSettingsManager.instance.SetAllObjectManipulationPermissions(true);

            if (mode == Mode.Moving || mode == Mode.ScaleRotate) // Not sure this would ever happen, but just in case...
                StopScaleRot();
            else
                DeselectObject();
        }
        else
        {
            ArSettingsManager.instance.SetAllObjectManipulationPermissions(false);
            Debug.Log("UnLocked");
        }
    }

    void AttemptArPlaneMove(Vector3 arHitPos, HitNormal arHitnormal)
    {
        if (heldRb) // Revert to plane movement
        {
            heldRb.isKinematic = false;
            heldRb = null;
        }

        Vector3 offset = Vector3.zero;
        if (selectedChildInst != null)
        {
            offset = selectedInst.transform.position - selectedChildInst.transform.position;
            offset.y = 0f;
        }
        selectedInst.transform.position = arHitPos + offset;
    }

    void AttemptGrabMove()
    {
        if (selectedChildInst)
        {
            heldRb = selectedChildInst.GetComponent<Rigidbody>();
        }
        else
            heldRb = selectedInst.GetComponent<Rigidbody>();

        if (heldRb)
        {
            heldRb.isKinematic = true;
            selectedInst.transform.parent = grabPos;
            selectedInst.transform.position = grabPos.position;
        }
    }



    IEnumerator GrabLerp()
    {
        while (selectedInst && heldRb)
        {
            //        overshoot = 
            selectedInst.transform.position += grabPos.position - selectedInst.position;

            yield return null;
        }
    }

    Coroutine scalerCrt = null;

    IEnumerator ScaleObject(Transform toScale)
    {
        float startFingerDist = Vector2.Distance(Input.GetTouch(0).position, Input.GetTouch(1).position);
        float fingerDist = startFingerDist;

        Vector3 startScale = toScale.localScale;

        PlaySoundRandomPitch(scaleClip, false);
        bool actionFired = false;

        while (Input.touchCount > 1)
        {
            toScale.localScale = startScale * fingerDist / startFingerDist;

            fingerDist = Vector2.Distance(Input.GetTouch(0).position, Input.GetTouch(1).position);

            if (Mathf.Abs(toScale.localScale.magnitude - startScale.magnitude) / startScale.magnitude > 0.1f && !actionFired)
            {
                StartCoroutine(FireActionTakenEvent(ArAction.PrefabScaled, toScale.gameObject));
                actionFired = true;
            }

            yield return null;
        }
    }

    bool rotationActive;
    float relativeAngle;
    float maxRelativeAngle = 0f;
    Coroutine rotateCrt = null;

    private IEnumerator RotateObject(Transform inst)
    {
        rotationActive = false;

        Vector2 startDoubleTouchVector = Input.GetTouch(0).position - Input.GetTouch(1).position;
        float startDoubleTouchAngle = Mathf.Atan2(startDoubleTouchVector.y, startDoubleTouchVector.x) * 360f / Mathf.PI;
        Vector3 startInstEulerAngles = inst.transform.eulerAngles;

        Vector2 currentDoubleTouchVector = startDoubleTouchVector;
        float currentDoubleTouchAngle = startDoubleTouchAngle;
        float lastDoubleTouchAngle = startDoubleTouchAngle;
        float totalRot = 0f;

        bool actionFired = false;

        while (Input.touchCount > 1)
        {
            relativeAngle = -(currentDoubleTouchAngle - lastDoubleTouchAngle);

            if (rotationActive)
            {
                Vector3 pivot;

                if (selectedChildInst)
                    pivot = selectedChildInst.position;
                else
                    pivot = inst.position;

                pivot.y = 0f;
                inst.transform.RotateAround(pivot, Vector3.up, relativeAngle);
                lastDoubleTouchAngle = currentDoubleTouchAngle;
                totalRot += relativeAngle;

                if (Mathf.Abs(totalRot) > 15f && !actionFired)
                {
                    StartCoroutine(FireActionTakenEvent(ArAction.PrefabRotated, inst.gameObject));
                    actionFired = true;
                }
            }

            if (Mathf.Abs(relativeAngle) > 15f && !rotationActive)
            {
                lastDoubleTouchAngle = currentDoubleTouchAngle;

                rotationActive = true;
            }

            currentDoubleTouchVector = Input.GetTouch(0).position - Input.GetTouch(1).position;
            currentDoubleTouchAngle = Mathf.Atan2((currentDoubleTouchVector.y), (currentDoubleTouchVector.x)) * 360f / Mathf.PI;

            yield return null;
        }
    }

#endregion

#region Auto Snap

    void RotateFacePlayer(GameObject toRotate)
    {
        Vector3 playerDirection = Camera.main.transform.position - toRotate.transform.position; // Direction pointing from the mirror prefab to the phone/player (camera)

        Quaternion rotationTowardsPlayer = Quaternion.FromToRotation(toRotate.transform.forward, playerDirection); // Get a "rotation" (quaternion, which describes a rotation) that we can apply to the mirror

        Vector3 verticalAngularRotation = new Vector3(toRotate.transform.eulerAngles.x, rotationTowardsPlayer.eulerAngles.y, toRotate.transform.eulerAngles.z); // Creating a Vector3 that contains the angeles in degrees (i.e. euler angles) 

        toRotate.transform.rotation = Quaternion.Euler(verticalAngularRotation); // Finally applying the new rotation to the prefab instance
    }

    void RotateFaceNormal(GameObject toRotate, Vector3 normal)
    {
        Quaternion rotationTowardsNormal = Quaternion.FromToRotation(toRotate.transform.forward, normal); // Get a "rotation" (quaternion, which describes a rotation) that we can apply to the mirror

        Vector3 verticalAngularRotation = new Vector3(toRotate.transform.eulerAngles.x, rotationTowardsNormal.eulerAngles.y, toRotate.transform.eulerAngles.z); // Creating a Vector3 that contains the angeles in degrees (i.e. euler angles) 

        toRotate.transform.rotation = Quaternion.Euler(verticalAngularRotation); // Finally applying the new rotation to the prefab instance
    }

#endregion
}

public struct HitNormal
{
    public Vector3 hitPos;
    public Quaternion orientationOfNormal;
    public bool hitValid;

    public HitNormal(Vector3 hitPos, Quaternion orientationOfNormal, bool hitValid)
    {
        this.hitPos = hitPos;
        this.orientationOfNormal = orientationOfNormal;
        this.hitValid = hitValid;
    }
}

class ArPlanesInfo
{
    public List<ArPlaneInfo> planes = new List<ArPlaneInfo>();

    public void AddPlane(ARPlane plane, float xSize, float ySize)
    {
        planes.Add(new ArPlaneInfo(plane, xSize, ySize));
    }

    public bool ContainsPlane(ARPlane plane)
    {
        for (int i = 0; i < planes.Count; i++)
        {
            if (planes[i].plane == plane)
                return true;
        }

        return false;
    }

    public ArPlaneInfo GetPlaneInfo(ARPlane plane)
    {
        for (int i = 0; i < planes.Count; i++)
        {
            if (planes[i].plane == plane)
                return planes[i];
        }

        return null;
    }
}

class ArPlaneInfo
{
    public ARPlane plane;
    public float xSize, ySize;

    public ArPlaneInfo(ARPlane plane, float xSize, float ySize)
    {
        this.plane = plane;
        this.xSize = xSize;
        this.ySize = ySize;
    }
}