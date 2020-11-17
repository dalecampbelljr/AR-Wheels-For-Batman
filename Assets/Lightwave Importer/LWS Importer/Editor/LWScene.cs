using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;

using VirtualEscapes.Common.Importers.LWS.Objects;

namespace VirtualEscapes.Common.Importers.LWS
{
    [CustomEditor(typeof(LWSImporter))]
    public class LWScene : ScriptedImporterEditor
    {
        private const string APPNAME = "LWS Importer";

        private SerializedObject mLWSDataObject;
        private SerializedProperty propertyLWSObjects;

        private SerializedProperty propertySavedScenePath;

        private SerializedProperty propertyPreviewRange, propertyRenderRange, propertyCustomRenderRange, propertyFPS;
        private SerializedProperty propertyHasAnimation, propertyCreateTimeline, propertyOptimiseKeyframes, propertyDrawBones;

        private SerializedProperty propertyFrameRangeType;

        private SerializedProperty propertyBuildErrorInvalidModelCount;
        private SerializedProperty propertyImportFilePathWarningCount;
        private SerializedProperty propertyImportFilePathErrorCount;

        private GUIContent contentHeadingImport;
        private GUIContent contentHeadingBuild;
        private GUIContent contentHeadingOutput;

        private GUIContent contentFrameRangeType;
        private GUIContent contentCreateTimeline;
        private GUIContent contentOptimiseKeyframes;
        private GUIContent contentDrawBones;

        private GUIContent contentOverwriteSavedScene;

        private LightWaveImporterFooter mFooter;

        private static bool foldoutImport = true, foldoutBuild = true, foldoutOutput = true;

        public override bool UseDefaultMargins() { return false; }

        public override bool RequiresConstantRepaint() { return true; }

        public override void OnEnable()
        {
            base.OnEnable();

            contentHeadingImport = new GUIContent("Import");
            contentHeadingBuild = new GUIContent("Build");
            contentHeadingOutput = new GUIContent("Output");

            contentCreateTimeline = new GUIContent("Create Timeline");
            contentOptimiseKeyframes = new GUIContent("Optimise Keyframes");
            contentDrawBones = new GUIContent("Draw Bones", "Enables drawing of connected bones in the scene.");
            contentFrameRangeType = new GUIContent("Use Frame Range From");

            mLWSDataObject = new SerializedObject(serializedObject.FindProperty("lwsData").objectReferenceValue);
            propertyLWSObjects = mLWSDataObject.FindProperty("lwsObjects");

            propertyPreviewRange = mLWSDataObject.FindProperty("previewRange");
            propertyRenderRange = mLWSDataObject.FindProperty("renderRange");
            propertyFPS = mLWSDataObject.FindProperty("framesPerSecond");
            propertyHasAnimation = mLWSDataObject.FindProperty("hasAnimation");

            propertyCreateTimeline = serializedObject.FindProperty("createTimeline");
            propertyOptimiseKeyframes = serializedObject.FindProperty("optimiseKeyframes");
            propertyDrawBones = serializedObject.FindProperty("drawBones");

            propertyFrameRangeType = serializedObject.FindProperty("frameRangeType");
            propertyCustomRenderRange = serializedObject.FindProperty("customFrameRange");

            propertySavedScenePath = serializedObject.FindProperty("savedScenePath");

            propertyBuildErrorInvalidModelCount = serializedObject.FindProperty("buildErrorInvalidModelCount");
            propertyImportFilePathWarningCount = serializedObject.FindProperty("importFilePathWarningCount");
            propertyImportFilePathErrorCount = serializedObject.FindProperty("importFilePathErrorCount");

            contentOverwriteSavedScene = new GUIContent("Overwrite Scene File", "Drag a Unity scene file here. This file will be overwritten when creating a scene from the imported lws file.");

            mFooter = new LightWaveImporterFooter(APPNAME, LWSVersion.major, LWSVersion.minor, LWSVersion.revision);
        }

        public override void OnInspectorGUI()
        {

#if UNITY_2019_2_OR_NEWER
            serializedObject.Update();
#endif

            EditorHelper.SetStyles();

            bool lbHasAnimation = propertyHasAnimation.boolValue;

            SerializedProperty lPropertyFrameRange = getFrameRange((LWSImporter.FrameRangeTypes)propertyFrameRangeType.intValue);

            if (EditorHelper.Foldout(ref foldoutImport, contentHeadingImport))
            {
                EditorGUI.BeginDisabledGroup(!lbHasAnimation);
                EditorGUILayout.PropertyField(propertyOptimiseKeyframes, contentOptimiseKeyframes);
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.Space();
            }

            if (EditorHelper.Foldout(ref foldoutBuild, contentHeadingBuild))
            {
                if (lbHasAnimation)
                {
                    EditorGUI.BeginDisabledGroup(!lbHasAnimation);

                    EditorGUILayout.PropertyField(propertyCreateTimeline, contentCreateTimeline);

                    if (propertyCreateTimeline.boolValue)
                    {
                        EditorGUILayout.PropertyField(propertyFrameRangeType, contentFrameRangeType);
                        EditorGUILayout.PropertyField(lPropertyFrameRange);
                    }

                    EditorGUI.EndDisabledGroup();
                }

                EditorGUILayout.Space();

                EditorGUILayout.PropertyField(propertyDrawBones, contentDrawBones);

                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PrefixLabel(contentOverwriteSavedScene);

                SceneAsset lSceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(propertySavedScenePath.stringValue);
                var newScene = EditorGUILayout.ObjectField(lSceneAsset, typeof(SceneAsset), false) as SceneAsset;

                if (EditorGUI.EndChangeCheck())
                {
                    propertySavedScenePath.stringValue = AssetDatabase.GetAssetPath(newScene);
                }
                EditorGUILayout.EndHorizontal();

                string lsSavedScenePath = propertySavedScenePath.stringValue;
                bool lbScenePathValid = lSceneAsset != null;

                string lsButtonLabel = lbScenePathValid ? "Overwrite Unity scene: " + Path.GetFileNameWithoutExtension(lsSavedScenePath) : "Create New Unity Scene";

                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical("Box");
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button(lsButtonLabel, GUILayout.Height(40)))
                {
                    List<LWSObject> laLWSObjects = new List<LWSObject>();
                    int liCount = propertyLWSObjects.arraySize;
                    for (int i = 0; i < liCount; i++)
                    {
                        SerializedProperty lLWSObjectProperty = propertyLWSObjects.GetArrayElementAtIndex(i);
                        LWSObject lWSObject = (LWSObject)lLWSObjectProperty.objectReferenceValue;
                        laLWSObjects.Add(lWSObject);
                    }

                    bool lbBuildScene;
                    if (lbScenePathValid)
                    {
                        lbBuildScene = EditorUtility.DisplayDialog("Create Scene", "This will overwrite the existing scene:\n" + lsSavedScenePath, "Overwrite", "Cancel");
                    }
                    else
                    {
                        lbBuildScene = true;
                    }

                    if (lbBuildScene)
                    {
                        Dictionary<string, LWSPrefab> laPrefabDictionary = new Dictionary<string, LWSPrefab>();
                        propertyBuildErrorInvalidModelCount.intValue = validateAndBuildPrefabs(laLWSObjects, laPrefabDictionary);
                        serializedObject.ApplyModifiedProperties();

                        LWSceneBuilder.Build(laLWSObjects,
                                                mLWSDataObject.FindProperty("savedAssetPath").stringValue,
                                                lPropertyFrameRange.FindPropertyRelative("_start").intValue,
                                                lPropertyFrameRange.FindPropertyRelative("_end").intValue,
                                                propertyFPS.intValue,
                                                propertyHasAnimation.boolValue,
                                                propertyDrawBones.boolValue,
                                                lbScenePathValid ? lsSavedScenePath : null);

                        GUIUtility.ExitGUI();
                    }
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space();
            }

            if (EditorHelper.Foldout(ref foldoutOutput, contentHeadingOutput))
            {
                displayOutputLog(lbHasAnimation);
                EditorGUILayout.Space();
            }

#if UNITY_2019_2_OR_NEWER
            serializedObject.ApplyModifiedProperties();
#endif

            ApplyRevertGUI();

            mFooter.GUILayoutDraw();

        }

        private SerializedProperty getFrameRange(LWSImporter.FrameRangeTypes frameRangeType)
        {
            switch (frameRangeType)
            {
                default:
                case LWSImporter.FrameRangeTypes.Preview:
                    return propertyPreviewRange;

                case LWSImporter.FrameRangeTypes.Render:
                    return propertyRenderRange;

                case LWSImporter.FrameRangeTypes.Custom:
                    return propertyCustomRenderRange;
            }
        }

        private int validateAndBuildPrefabs(List<LWSObject> lwsObjects, Dictionary<string, LWSPrefab> prefabDictionary)
        {
            foreach (LWSModel lwsModel in lwsObjects.OfType<LWSModel>())
            {
                lwsModel.BuildPrefab(prefabDictionary);
            }

            //count invalid prefabs
            int liInvalidPrefabsCount = 0;
            foreach (KeyValuePair<string, LWSPrefab> kvp in prefabDictionary)
            {
                if (!kvp.Value.isValid)
                {
                    liInvalidPrefabsCount++;
                    Debug.LogWarning(kvp.Key + " needs importing using the latest LWO Importer.");
                }
            }

            return liInvalidPrefabsCount;
        }

        private void displayOutputLog(bool hasAnimation)
        {
            if (!hasAnimation)
            {
                EditorGUILayout.LabelField("No animation data found.");
            }

            int liBuildErrorInvalidModelCount = propertyBuildErrorInvalidModelCount.intValue;
            int liImportWarningFilePathCount = propertyImportFilePathWarningCount.intValue;
            int liImportErrorFilePathCount = propertyImportFilePathErrorCount.intValue;

            bool lbHasImportErrorsOrWarnings = liImportWarningFilePathCount + liImportErrorFilePathCount > 0;
            bool lbHasBuildErrorsOrWarnings = liBuildErrorInvalidModelCount > 0;

            if (lbHasImportErrorsOrWarnings || lbHasBuildErrorsOrWarnings)
            {
                if (lbHasImportErrorsOrWarnings)
                {
                    if (liImportWarningFilePathCount > 0)
                    {
                        string lsPlural = liImportWarningFilePathCount == 1 ? "" : "s";
                        EditorGUILayout.HelpBox(liImportWarningFilePathCount + " file path warning"+ lsPlural + ". Ensure:\n• The Content Directory is set correctly in LightWave.\n• The file structure relative to the Content Directory is identical between the Unity Project and the LightWave project.\nSee Console Log for more information.", MessageType.Warning);
                    }
                    if (liImportErrorFilePathCount > 0)
                    {
                        string lsPlural = liImportErrorFilePathCount == 1 ? "" : "s";
                        EditorGUILayout.HelpBox(liImportErrorFilePathCount + " file path error" + lsPlural + ". Ensure:\n• The Content Directory is set correctly in LightWave.\n• The file structure relative to the Content Directory is identical between the Unity Project and the LightWave project.\nSee Console Log for more information.", MessageType.Error);
                    }
                }

                if (lbHasBuildErrorsOrWarnings)
                {
                    if (liBuildErrorInvalidModelCount > 0)
                    {
                        string lsPlural = liBuildErrorInvalidModelCount == 1 ? "" : "s";
                        EditorGUILayout.HelpBox(liBuildErrorInvalidModelCount + " Invalid model" + lsPlural + " found. Ensure models are imported using the latest version of LWO Importer", MessageType.Warning);
                    }
                }
            }
            else
            {
                
                EditorGUILayout.LabelField("No issues detected.");
            }
        }
    }
}