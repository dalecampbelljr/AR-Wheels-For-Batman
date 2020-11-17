//#define LEGACY_REMAPPING

#if !DEBUG
#undef LEGACY_REMAPPING
#endif

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;

using VirtualEscapes.Common.Editor;

namespace VirtualEscapes.Common.Importers.LWO
{
    [CustomEditor(typeof(LWOImporter))]
    public class LWObject : ScriptedImporterEditor
    {
        private const string APPNAME = "LWO Importer";

        private GUIContent[] normalGenerationEnumNames =
        {
            new GUIContent("Weighted Average"),
            new GUIContent("Lightwave Style")
        };

        private GUIContent[] indexFormatEnumNames =
        {
            new GUIContent("16 bit"),
            new GUIContent("32 bit")
        };

        //readonly
        private SerializedProperty propertyHasBlendShapes, propertyHasSubdivSurfaces, propertyHasNormalMapping;

        //read/write
        private SerializedProperty propertyGenerateTangents, propertyImportBlendShapes,
            propertyCatmullClarkeIterations, propertyIndexFormat, propertyGenerateLWOHierarchy;

#if UNITY_2018_4_OR_NEWER
        private SerializedProperty propertyGenerateUV2;
        private SerializedProperty propertyUV2Separation;
#endif

        private SerializedProperty propertyNormalGenerationMethod, propertySmoothVaryingSurfaces,
            propertyUseSmoothingAngles, propertyTextureOverridesBaseValue;

        private SerializedProperty propertyLogTime, propertyWarnings;

        private SerializedProperty propertyMaterialsAutoRemappedCount;
        private SerializedProperty propertyAutoRemapMatchingNames;
        private SerializedProperty propertyRemappedMaterialList, propertyMaterialNames;

        private GUIContent contentHeadingMesh, contentHeadingNormals, contentHeadingLog, contentHeadingMaterials;

        private GUIContent contentGenerateTangents, contentNormalMappingFound;
        private GUIContent contentImportBlendShapes;
        private GUIContent contentCCIterations;
        private GUIContent contentIndexFormat;

#if UNITY_2018_4_OR_NEWER
        private GUIContent contentGenerateUV2;
        private GUIContent contentUV2Separation;
#endif

        private GUIContent contentGenerateLWOHierarchy;
        private GUIContent contentNormalMethod;
        private GUIContent contentSmoothOverVaryingSurfaces;
        private GUIContent contentUseSmoothingAngles;
        private GUIContent contentLogTime;
        private GUIContent contentWarningOptions;
        private GUIContent contentTextureOverridesBase;
        private GUIContent contentRemapMatchingNames;
        private GUIContent contentRemappedMaterialList;

        private LightWaveImporterFooter mFooter;

        private List<string> materialNameChoices;
        private bool mbMaterialChoicesNeedBuilding;

        private Vector2 lvRemappedMaterialsPosition;

        private GUIStyle styleInfoLabel;
        private GUIStyle styleErrorLabel;

        private static bool foldoutMesh = true, foldoutNormals = true, foldoutLog = false, foldoutMaterials = true;

        public override bool UseDefaultMargins() { return false; }

        public override bool RequiresConstantRepaint() { return true; }

        public override void OnEnable()
        {
            base.OnEnable();

            //state properties - should not be modified, only read
            propertyHasBlendShapes = serializedObject.FindProperty("hasBlendShapes");
            propertyHasSubdivSurfaces = serializedObject.FindProperty("hasSubdivSurfaces");
            propertyHasNormalMapping = serializedObject.FindProperty("hasNormalMappedMaterials");

            //read-write properties
            propertyGenerateTangents = serializedObject.FindProperty("generateTangents");
            propertyImportBlendShapes = serializedObject.FindProperty("importBlendShapes");
            propertyCatmullClarkeIterations = serializedObject.FindProperty("catmullClarkeIterations");
            propertyIndexFormat = serializedObject.FindProperty("indexFormat");

#if UNITY_2018_4_OR_NEWER
            propertyGenerateUV2 = serializedObject.FindProperty("generateUV2");
            propertyUV2Separation = serializedObject.FindProperty("uv2Separation");
#endif

            propertyGenerateLWOHierarchy = serializedObject.FindProperty("generateLWOHierarchy");
            propertyNormalGenerationMethod = serializedObject.FindProperty("normalGenerationMethod");
            propertySmoothVaryingSurfaces = serializedObject.FindProperty("smoothVaryingSurfaces");
            propertyUseSmoothingAngles = serializedObject.FindProperty("useSmoothingAngles");
            propertyTextureOverridesBaseValue = serializedObject.FindProperty("textureOverridesBaseValue");

            propertyLogTime = serializedObject.FindProperty("showImportTime");
            propertyWarnings = serializedObject.FindProperty("warningMask");

            propertyMaterialsAutoRemappedCount = serializedObject.FindProperty("materialsAutoRemappedCount");
            propertyAutoRemapMatchingNames = serializedObject.FindProperty("autoRemapMatchingNames");
            propertyRemappedMaterialList = serializedObject.FindProperty("remappedMaterialsList");
            propertyMaterialNames = serializedObject.FindProperty("materialNames");

            contentRemapMatchingNames = new GUIContent("Remap Matching Material Names", "Automatically remap materials to existing materials in the project with the same name.");

            //GUIContent
            contentHeadingMesh = new GUIContent("Mesh");
            contentHeadingNormals = new GUIContent("Normals");
            contentHeadingLog = new GUIContent("Log Output");
            contentHeadingMaterials = new GUIContent("Materials");

            contentGenerateTangents = new GUIContent("Generate Tangents", "Generate Tangents for use with Normal-Mapped materials");
            contentNormalMappingFound = new GUIContent("Normal Mapped materials found.", "The imported model contains materials which have normal mapping applied.\nVertex tangents should be generated in order for the model to render correctly.");
            contentImportBlendShapes = new GUIContent("Import BlendShapes", "Choose whether to import Blendshapes (In Lightwave terminology: Endomorphs)");
            contentCCIterations = new GUIContent("Catmull-Clarke Iterations", "Higher values will result in a smoother, but higher-polygon mesh");
            contentIndexFormat = new GUIContent("Index Format", "Using '16 bit' will cause the generated Mesh to be split into multiple SubMeshes with vertex counts no greater than 65535.\nHowever this will ensure compatibility with a wider range of target hardware.");

#if UNITY_2018_4_OR_NEWER
            contentGenerateUV2 = new GUIContent("Generate Lightmap UVs", "Creates a second UV channel for Lightmapping.");
            contentUV2Separation = new GUIContent("UV Island Separation", "Maximum secondary UV channel island separation");
#endif

            contentGenerateLWOHierarchy = new GUIContent("Generate Scene Hierarchy Data", "Generates hierarchy and bone weight mapping data which is used by LWS Importer.\nEnable in order to recreate this object in a corresponding LightWave scene.\n\nIf LWS Importer is not being used then this can be disabled.");
            contentNormalMethod = new GUIContent("Normal Generation Method", "Determines how normals are calculated from the geometry. 'Weighted Average' will generally generate better looking results.");
            contentSmoothOverVaryingSurfaces = new GUIContent("Smooth Over Varying Surfaces", "Enables smoothing of normals between polygons with different materials. Lightwave default behaviour is 'Off'");
            contentUseSmoothingAngles = new GUIContent("Use Smoothing Angles", "Use the smoothing angle defined in the materials, or always smooth across shared vertices.");
            contentLogTime = new GUIContent("Processing Time", "Show how long it took import the LWO file.");
            contentWarningOptions = new GUIContent("Extra Warning Options", "Choose which extra warning options to show.");
            contentTextureOverridesBase = new GUIContent("Texture Overrides Base Value", "Texture colors are multiplied with the base color. When checked the materials base color will be overwritten with white.");
            contentRemappedMaterialList = new GUIContent("Remapped Materials", "A dynamic list of materials to remap.");

            mFooter = new LightWaveImporterFooter(APPNAME, Version.major, Version.minor, Version.revision);

            mbMaterialChoicesNeedBuilding = true;
        }

        public override void OnInspectorGUI()
        {

#if UNITY_2019_2_OR_NEWER
            serializedObject.Update();
#endif

            setStyles();

            if (EditorHelper.Foldout(ref foldoutMesh, contentHeadingMesh))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(propertyGenerateTangents, contentGenerateTangents);
                if (propertyHasNormalMapping.boolValue)
                {
                    EditorGUILayout.LabelField(contentNormalMappingFound, propertyGenerateTangents.boolValue ? styleInfoLabel : styleErrorLabel);
                }
                EditorGUILayout.EndHorizontal();

                beginDisableGroupIfPropertyIsFalse(propertyHasBlendShapes);
                EditorGUILayout.PropertyField(propertyImportBlendShapes, contentImportBlendShapes);
                EditorGUI.EndDisabledGroup();

                beginDisableGroupIfPropertyIsFalse(propertyHasSubdivSurfaces);
                addIntSlider(propertyCatmullClarkeIterations, contentCCIterations, 0, 4);
                EditorGUI.EndDisabledGroup();

                addPopup(propertyIndexFormat, contentIndexFormat, indexFormatEnumNames);

#if UNITY_2018_4_OR_NEWER
                EditorGUILayout.PropertyField(propertyGenerateUV2, contentGenerateUV2);
                if (propertyGenerateUV2.boolValue)
                {
                    addFloatSlider(propertyUV2Separation, contentUV2Separation, 0, 1);
                }
#endif
                EditorGUILayout.PropertyField(propertyGenerateLWOHierarchy, contentGenerateLWOHierarchy);

                EditorGUILayout.Space();
            }


            if (EditorHelper.Foldout(ref foldoutNormals, contentHeadingNormals))
            {
                addPopup(propertyNormalGenerationMethod, contentNormalMethod, normalGenerationEnumNames);
                EditorGUILayout.PropertyField(propertySmoothVaryingSurfaces, contentSmoothOverVaryingSurfaces);
                EditorGUILayout.PropertyField(propertyUseSmoothingAngles, contentUseSmoothingAngles);

                EditorGUILayout.Space();
            }


            if (EditorHelper.Foldout(ref foldoutLog, contentHeadingLog))
            {
                EditorGUILayout.PropertyField(propertyLogTime, contentLogTime);
                propertyWarnings.intValue = (int)(ExtraWarningFlags)EditorGUILayout.EnumFlagsField(contentWarningOptions, (ExtraWarningFlags)propertyWarnings.intValue);

                EditorGUILayout.Space();
            }


            if (EditorHelper.Foldout(ref foldoutMaterials, contentHeadingMaterials))
            {
                EditorGUILayout.PropertyField(propertyTextureOverridesBaseValue, contentTextureOverridesBase);

                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(propertyAutoRemapMatchingNames, contentRemapMatchingNames);
                if (propertyAutoRemapMatchingNames.boolValue)
                {
                    int liMaterialCount = propertyMaterialNames.arraySize;
                    int liMaterialsAutoRemappedCount = propertyMaterialsAutoRemappedCount.intValue;
                    if (liMaterialsAutoRemappedCount > 0)
                    {
                        EditorGUILayout.LabelField(liMaterialsAutoRemappedCount + " / " + liMaterialCount + " matched.");
                    }
                    else
                    {
                        EditorGUILayout.LabelField("No matching names found.");
                    }
                }
                if (EditorGUI.EndChangeCheck())
                {
                    if (propertyAutoRemapMatchingNames.boolValue)
                    {
                        propertyMaterialsAutoRemappedCount.intValue = addAutoRemappedMaterials();
                    }
                    else
                    {
                        removeAutoRemappedMaterials();
                        propertyMaterialsAutoRemappedCount.intValue = 0;
                    }
                }
                EditorGUILayout.EndHorizontal();


                EditorGUILayout.Space();
                EditorGUILayout.LabelField(contentRemappedMaterialList);


#if LEGACY_REMAPPING
            addMaterialArray("remappedMaterials", serializedObject);
#else
                addMaterialList();
#endif

            }


#if UNITY_2019_2_OR_NEWER
            serializedObject.ApplyModifiedProperties();
#endif

            ApplyRevertGUI();

            mFooter.GUILayoutDraw();
        }

        private void setStyles()
        {
            EditorHelper.SetStyles();

            styleInfoLabel = new GUIStyle(GUI.skin.label);
            styleInfoLabel.fontSize = 10;
            styleInfoLabel.normal.textColor = Color.gray;

            styleErrorLabel = new GUIStyle(GUI.skin.label);
            styleErrorLabel.fontSize = 10;
            styleErrorLabel.normal.textColor = Color.red;
        }

        private void beginDisableGroupIfPropertyIsFalse(SerializedProperty serializedProperty)
        {
            EditorGUI.BeginDisabledGroup(!serializedProperty.boolValue);
        }

        private void addMaterialArray(string arrayName, SerializedObject obj)
        {
            int liCount = obj.FindProperty(arrayName + ".Array.size").intValue;
            for (int i = 0; i < liCount; i++)
            {
                SerializedProperty remappedMaterialProperty = obj.FindProperty(string.Format("{0}.Array.data[{1}]", arrayName, i));
                GUIContent labelContent = new GUIContent(remappedMaterialProperty.FindPropertyRelative("name").stringValue);
                EditorGUILayout.PropertyField(remappedMaterialProperty.FindPropertyRelative("material"), labelContent);
            }
        }

        private void addMaterialField(SerializedProperty serializedProperty, GUIContent content)
        {
            Material lMaterial = serializedProperty.objectReferenceValue as Material;
            Material lNewMaterial = (Material)EditorGUILayout.ObjectField(content, lMaterial, typeof(Material), false);

            if (lMaterial != lNewMaterial)
            {
                //validate
                if (lNewMaterial == null)
                {
                    serializedProperty.objectReferenceValue = null;
                }
                else
                {
                    bool isNotEditable = (lNewMaterial.hideFlags & HideFlags.NotEditable) == HideFlags.NotEditable;
                    if (isNotEditable)
                    {
                        Debug.LogError("Materials from prefabs can not be assigned.");
                    }
                    else
                    {
                        serializedProperty.objectReferenceValue = lNewMaterial;
                    }
                }
            }
        }

        private void addFloatSlider(SerializedProperty serializedProperty, GUIContent content, float leftValue, float rightValue)
        {
            Rect ourRect = EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginProperty(ourRect, GUIContent.none, serializedProperty);
            EditorGUI.BeginChangeCheck();

            float selectionFromInspector = serializedProperty.floatValue;
            float selectedValue = EditorGUILayout.Slider(content, selectionFromInspector, leftValue, rightValue);
            serializedProperty.floatValue = selectedValue;

            EditorGUI.EndProperty();
            EditorGUILayout.EndHorizontal();
        }

        private void addIntSlider(SerializedProperty serializedProperty, GUIContent content, int leftValue, int rightValue)
        {
            Rect ourRect = EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginProperty(ourRect, GUIContent.none, serializedProperty);
            EditorGUI.BeginChangeCheck();

            int selectionFromInspector = serializedProperty.intValue;
            int selectedValue = EditorGUILayout.IntSlider(content, selectionFromInspector, leftValue, rightValue);
            serializedProperty.intValue = selectedValue;

            EditorGUI.EndProperty();
            EditorGUILayout.EndHorizontal();
        }

        private void addPopup(SerializedProperty serializedProperty, GUIContent content, GUIContent[] enumNames)
        {
            Rect ourRect = EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginProperty(ourRect, GUIContent.none, serializedProperty);
            EditorGUI.BeginChangeCheck();

            int selectionFromInspector = serializedProperty.intValue;

#if UNITY_2017_1_OR_NEWER
            int selectedValue = EditorGUILayout.Popup(content, selectionFromInspector, enumNames);
#else
            int[] laValues = new int[enumNames.Length];
            for (int i = 0; i < laValues.Length; i++)
            {
                laValues[i] = i;
            }
            EditorGUILayout.PrefixLabel(content);
            int selectedValue = EditorGUILayout.IntPopup(selectionFromInspector, enumNames, laValues);
#endif

            serializedProperty.intValue = selectedValue;

            EditorGUI.EndProperty();
            EditorGUILayout.EndHorizontal();
        }

        private void addMaterialList()
        {
            float lfItemHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            int liMaxItems = 10;

            int liMaterialNamesCount = propertyMaterialNames.arraySize;
            int liRemappedMaterialsCount = propertyRemappedMaterialList.arraySize;
            bool lbLegacyMaterialRemapping = liMaterialNamesCount == 0;

            if (lbLegacyMaterialRemapping)
            {
                //imported object is using the old material remapping system. The object needs to be reimported before we can use the new system
                EditorGUILayout.BeginVertical("Box");
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Reimport asset to enable")) ApplyAndImport();

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();

                EditorGUI.BeginDisabledGroup(true);
                addMaterialArray("remappedMaterials", serializedObject);
                EditorGUI.EndDisabledGroup();
                return;
            }

            if (propertyMaterialNames == null) return;

            if (materialNameChoices == null)
            {
                //collate the available material choices - these are the ones that have not been assigned for remapping
                materialNameChoices = new List<string>();
                mbMaterialChoicesNeedBuilding = true;
            }

            if (mbMaterialChoicesNeedBuilding)
            {
                buildMaterialNameChoices(liMaterialNamesCount, liRemappedMaterialsCount);
            }

            GUIStyle remappingAddButtonStyle = new GUIStyle(EditorStyles.miniButton)
            {
                fontStyle = FontStyle.Bold
            };

            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.MaxHeight(liMaxItems * lfItemHeight));

            lvRemappedMaterialsPosition = EditorGUILayout.BeginScrollView(lvRemappedMaterialsPosition);
            int liFirstIndex = (int)(lvRemappedMaterialsPosition.y / lfItemHeight);
            liFirstIndex = Mathf.Clamp(liFirstIndex, 0, Mathf.Max(0, liRemappedMaterialsCount - liMaxItems));
            GUILayout.Space(liFirstIndex * lfItemHeight);

            if (liRemappedMaterialsCount == 0)
            {
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField("List is Empty");

                GUILayout.FlexibleSpace();

                if (liRemappedMaterialsCount < liMaterialNamesCount && addRemappingAddButton(remappingAddButtonStyle))
                {
                    insertRemappedMaterialAtIndex(liRemappedMaterialsCount);
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                int liArrayIndexToDelete = -1;

                for (int i = liFirstIndex; i < Mathf.Min(liRemappedMaterialsCount, liFirstIndex + liMaxItems); i++)
                {
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                    SerializedProperty lItem = propertyRemappedMaterialList.GetArrayElementAtIndex(i);

                    bool lbIsAutoMapped = lItem.FindPropertyRelative("isAutoMapped").boolValue;

                    EditorGUI.BeginDisabledGroup(lbIsAutoMapped);

                    string lsName = lItem.FindPropertyRelative("name").stringValue;

                    GUIStyle lPopupStyle = new GUIStyle(EditorStyles.popup);
                    bool lbIsMissing = lItem.FindPropertyRelative("isMissing").boolValue;
                    if (lbIsMissing)
                    {
                        lsName += " (Missing)";
                        lPopupStyle.normal.textColor = Color.red;
                        lPopupStyle.active.textColor = Color.red;
                        lPopupStyle.focused.textColor = Color.red;
                    }

                    GUIContent labelContent = new GUIContent("", lsName);

                    string[] laChoices = new string[materialNameChoices.Count + 1];
                    laChoices[0] = lsName;
                    for (int p = 1; p < laChoices.Length; p++)
                    {
                        laChoices[p] = materialNameChoices[p - 1];
                    }

                    int liNewIndex = EditorGUILayout.Popup(0, laChoices, lPopupStyle);
                    if (liNewIndex > 0)
                    {
                        lItem.FindPropertyRelative("name").stringValue = laChoices[liNewIndex];
                        lItem.FindPropertyRelative("isMissing").boolValue = false;
                        mbMaterialChoicesNeedBuilding = true;
                    }

                    addMaterialField(lItem.FindPropertyRelative("material"), labelContent);

                    GUIStyle removeButtonStyle = new GUIStyle(EditorStyles.miniButton)
                    {
                        fontStyle = FontStyle.Bold
                    };

                    if (!lbIsAutoMapped && GUILayout.Button("-", removeButtonStyle, GUILayout.Width(18)))
                    {
                        liArrayIndexToDelete = i;
                    }

                    EditorGUI.EndDisabledGroup();

                    EditorGUILayout.EndHorizontal();
                }

                GUILayout.Space(Mathf.Max(0, (liRemappedMaterialsCount - liFirstIndex - liMaxItems) * lfItemHeight));

                if (liArrayIndexToDelete > -1)
                {
                    propertyRemappedMaterialList.DeleteArrayElementAtIndex(liArrayIndexToDelete);
                    mbMaterialChoicesNeedBuilding = true;
                }

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (liRemappedMaterialsCount < liMaterialNamesCount && addRemappingAddButton(remappingAddButtonStyle))
                {
                    insertRemappedMaterialAtIndex(liRemappedMaterialsCount);
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private bool addRemappingAddButton(GUIStyle remappingAddButtonStyle)
        {
            return GUILayout.Button("+", remappingAddButtonStyle, GUILayout.Width(22));
        }

        /// <summary>
        /// Builds the material name choices List.
        /// </summary>
        /// <param name="materialNamesCount">Material names count.</param>
        private void buildMaterialNameChoices(int materialNamesCount, int remappedMaterialsCount)
        {
            //Debug.Log("Building material choices");
            materialNameChoices.Clear();
            for (int i = 0; i < materialNamesCount; i++)
            {
                bool lbNameFound = false;
                string lsNameToMatch = propertyMaterialNames.GetArrayElementAtIndex(i).stringValue;
                for (int p = 0; p < remappedMaterialsCount; p++)
                {
                    SerializedProperty lItem = propertyRemappedMaterialList.GetArrayElementAtIndex(p);
                    string lsName = lItem.FindPropertyRelative("name").stringValue;
                    if (lsNameToMatch == lsName)
                    {
                        lbNameFound = true;
                        break;
                    }
                }
                if (!lbNameFound)
                {
                    materialNameChoices.Add(lsNameToMatch);
                }
            }
            mbMaterialChoicesNeedBuilding = false;
        }

        private void insertRemappedMaterialAtIndex(int index)
        {
            propertyRemappedMaterialList.InsertArrayElementAtIndex(index);
            SerializedProperty lItem = propertyRemappedMaterialList.GetArrayElementAtIndex(index);
            SerializedProperty nameProperty = lItem.FindPropertyRelative("name");
            //use first available material name
            nameProperty.stringValue = materialNameChoices[0];
            lItem.FindPropertyRelative("material").objectReferenceValue = null;
            lItem.FindPropertyRelative("isMissing").boolValue = false;
            lItem.FindPropertyRelative("isAutoMapped").boolValue = false;

            mbMaterialChoicesNeedBuilding = true;
        }

        /// <summary>
        /// Adds any materials that match the name of existing materials in the project, unless they already exist in the MaterialNames List
        /// </summary>
        /// <returns>The number of auto remapped materials.</returns>
        private int addAutoRemappedMaterials()
        {
            int liMatches = 0;
            for (int m = 0; m < propertyMaterialNames.arraySize; m++)
            {
                string lsMaterialName = propertyMaterialNames.GetArrayElementAtIndex(m).stringValue;

                //look for a pre-existing material with this name
                string[] laGUIDMatches = AssetDatabase.FindAssets(lsMaterialName);
                if (laGUIDMatches.Length > 0)
                {
                    //only add matches that are exact
                    foreach (string guid in laGUIDMatches)
                    {
                        string lsPath = AssetDatabase.GUIDToAssetPath(guid);
                        bool lbHasMaterialExtension = Path.GetExtension(lsPath) == ".mat";

                        if (lbHasMaterialExtension && Path.GetFileNameWithoutExtension(lsPath) == lsMaterialName)
                        {
                            //it's an exact match, and the file looks like a material, but is it really a material?
                            Material lMaterial = AssetDatabase.LoadAssetAtPath<Material>(lsPath) as Material;
                            if (lMaterial != null)
                            {
                                //check we don't already have it
                                bool lbExists = false;
                                //foreach (RemappedMaterial remappedMaterial in remappedMaterials)
                                for (int i = 0; i < propertyRemappedMaterialList.arraySize; i++)
                                {
                                    SerializedProperty lItem = propertyRemappedMaterialList.GetArrayElementAtIndex(i);
                                    if (lItem.FindPropertyRelative("name").stringValue == lsMaterialName)
                                    {
                                        lbExists = true;
                                        break;
                                    }
                                }

                                if (!lbExists)
                                {
                                    int liIndex = propertyRemappedMaterialList.arraySize;
                                    propertyRemappedMaterialList.InsertArrayElementAtIndex(liIndex);
                                    SerializedProperty lItem = propertyRemappedMaterialList.GetArrayElementAtIndex(liIndex);

                                    lItem.FindPropertyRelative("name").stringValue = lsMaterialName;
                                    lItem.FindPropertyRelative("material").objectReferenceValue = lMaterial;
                                    lItem.FindPropertyRelative("isAutoMapped").boolValue = true;
                                    liMatches++;
                                }
                                break; //break out of the gui matches, continue through the tags
                            }
                        }
                    }
                }
            }
            mbMaterialChoicesNeedBuilding = true;
            return liMatches;
        }

        private void removeAutoRemappedMaterials()
        {
            List<int> laIndicesToDelete = new List<int>();

            for (int i = 0; i < propertyRemappedMaterialList.arraySize; i++)
            {
                SerializedProperty lItem = propertyRemappedMaterialList.GetArrayElementAtIndex(i);
                if (lItem.FindPropertyRelative("isAutoMapped").boolValue)
                {
                    laIndicesToDelete.Insert(0, i);
                }
            }

            foreach (int d in laIndicesToDelete)
            {
                propertyRemappedMaterialList.DeleteArrayElementAtIndex(d);
            }

            mbMaterialChoicesNeedBuilding = true;
        }
    }
}
 