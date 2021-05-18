using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.XR.ARFoundation;

public class LightEstimation : MonoBehaviour
{
    [SerializeField]
    private ARCameraManager arCameraManager;

    private Light currentLight;

    public float brightnessModifier;

    [Range(0f,1f)]
    public float brightnessDebug;

    private void Awake()
    {
        currentLight = GetComponent<Light>();
    }

    private void OnEnable()
    {
        arCameraManager.frameReceived += FrameUpdated;
    }

    private void OnDisable()
    {
        arCameraManager.frameReceived -= FrameUpdated;
    }

    private void Update()
    {
    #if UNITY_EDITOR
        RenderSettings.ambientIntensity = brightnessDebug;
    #endif
    }

    bool dbg1, dbg2, dbg3;

    private void FrameUpdated(ARCameraFrameEventArgs args)
    {
        if (args.lightEstimation.averageBrightness.HasValue)
        {
            currentLight.intensity = args.lightEstimation.averageBrightness.Value;
            RenderSettings.ambientIntensity = args.lightEstimation.averageBrightness.Value;
            if (!dbg1)
            {
                Debug.Log("averageBrightness.Value " + args.lightEstimation.averageBrightness.Value);
                dbg1 = !dbg1;
            }
        }

        if (args.lightEstimation.averageColorTemperature.HasValue)
        {
            currentLight.colorTemperature = args.lightEstimation.averageColorTemperature.Value;
            RenderSettings.ambientLight = Mathf.CorrelatedColorTemperatureToRGB(args.lightEstimation.averageColorTemperature.Value);
            if (!dbg2)
            {
                Debug.Log("averageColorTemperature.Value " + args.lightEstimation.averageColorTemperature.Value);
                dbg2 = !dbg2;
            }
        }

        if (args.lightEstimation.colorCorrection.HasValue)
        {
            currentLight.color = args.lightEstimation.colorCorrection.Value;
            if (!dbg3)
            {
                Debug.Log("colorCorrection.Value g " + args.lightEstimation.colorCorrection.Value.g);
                dbg3 = !dbg3;
            }
        }

        /*
        if(args.lightEstimation.mainLightDirection.HasValue)
        {
            transform.rotation = Quaternion.LookRotation(args.lightEstimation.mainLightDirection.Value);
        }

        if (args.lightEstimation.mainLightColor.HasValue)
        {
            currentLight.color = args.lightEstimation.mainLightColor.Value;
        }

        // Ref: https://github.com/Unity-Technologies/arfoundation-samples/blob/master/Assets/Scripts/LightEstimation.cs
        if (args.lightEstimation.mainLightIntensityLumens.HasValue)
        {
            // mainLightIntensityLumens = args.lightEstimation.mainLightIntensityLumens;
            currentLight.intensity = args.lightEstimation.averageMainLightBrightness.Value;
        }

        if (args.lightEstimation.ambientSphericalHarmonics.HasValue)
        {
            // sphericalHarmonics = args.lightEstimation.ambientSphericalHarmonics;
            RenderSettings.ambientMode = AmbientMode.Skybox;
            RenderSettings.ambientProbe = args.lightEstimation.ambientSphericalHarmonics.Value;
        }
        */
    }
}