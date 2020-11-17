//#define LEGACY_REMAPPING
//#define DISABLE_IMPORTING

#if !DEBUG
#undef LEGACY_REMAPPING
#undef DISABLE_IMPORTING
#endif

using System.IO;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.Experimental.AssetImporters;

using VirtualEscapes.Common.IFF;

namespace VirtualEscapes.Common.Importers.LWO
{

#if DISABLE_IMPORTING
    [ScriptedImporter(1, "lwoDisabled")]
#else
    [ScriptedImporter(1, "lwo")]
#endif

    public class LWOImporter : ScriptedImporter
    {

#region Properties to share with custom editor

        //mesh
        public bool generateTangents;
        public bool importBlendShapes = true;
        public int catmullClarkeIterations = 1;

        public IndexFormat indexFormat = IndexFormat.UInt16;

        public bool generateUV2 = false;
        public float uv2Separation = 0.02f;

        public bool generateLWOHierarchy = true;

        //normals
        public NormalGenerationMethod normalGenerationMethod = NormalGenerationMethod.WeightedAverage;
        public bool smoothVaryingSurfaces = false; //default LW behaviour is to create hard edges between different surfaces
        public bool useSmoothingAngles = true;

        //log
        public bool showImportTime = false;
        public int warningMask = (int)ExtraWarningFlags.InsufficientVerticesInPolygons | (int)ExtraWarningFlags.BadTexturePaths;

        //materials
        public bool textureOverridesBaseValue = true;

        //legacy
        public RemappedMaterial[] remappedMaterials;

#region material remapping
        public bool autoRemapMatchingNames;
        public int materialsAutoRemappedCount;
        public List<RemappedMaterial> remappedMaterialsList = new List<RemappedMaterial>();
        public string[] materialNames;
        #endregion

        //state properties
        public int importCount = 0;
        public bool hasBlendShapes = false;
        public bool hasSubdivSurfaces = false;
        public bool hasNormalMappedMaterials = false;
#endregion

        public override void OnImportAsset(AssetImportContext ctx)
        {

#if !UNITY_2018_4_OR_NEWER
            generateUV2 = false;
#endif

            LWOImporterLogManager lLogManager = new LWOImporterLogManager(ctx.assetPath, showImportTime);
            Model lModel = new Model(ctx);

            using (BinaryReaderEndian reader = new BinaryReaderEndian(File.Open(ctx.assetPath, FileMode.Open), BinaryReaderEndian.Endianness.Big))
            {
                Block.Initialise(reader);
                Block lBlock = new Block();

                switch (lBlock.type)
                {
                    case Block.Type.Form:
                    {
                        switch (lBlock.formID)
                        {
                            case "LWO2":
                            case "LWO3":
                            lModel.Read(lBlock, normalGenerationMethod, catmullClarkeIterations, warningMask);
                            hasSubdivSurfaces = lModel.hasSubdivSurfaces;
                            hasBlendShapes = lModel.hasBlendShapes;
                            break;

                            default:
                            LWOImporterLogManager.AddWarning("Unsupported object format: " + lBlock.formID);
                            lModel = null;
                            break;
                        }

                        break;
                    }

                    default:
                    //Expected file to begin with a FORM block. Bad file format.
                    LWOImporterLogManager.AddWarning("Unsupported file format.");
                    lModel = null;
                    break;
                }

                lBlock.Close();
            }

            if (lModel != null)
            {
                int liBadTexturePathFlag = (int)ExtraWarningFlags.BadTexturePaths;
                int liDegeneratePolygonFlag = (int)ExtraWarningFlags.DegeneratePolygons;
                bool lbBadTexturePathWarning = (warningMask & liBadTexturePathFlag) == liBadTexturePathFlag;
                bool lbDegeneratePolygonWarning = (warningMask & liDegeneratePolygonFlag) == liDegeneratePolygonFlag;

                lModel.BuildMaterials(textureOverridesBaseValue,
                                      ctx,
                                      lbBadTexturePathWarning,
                                      ref remappedMaterialsList,
                                      ref remappedMaterials,
                                      out materialNames,
                                      out hasNormalMappedMaterials);

                if (importCount == 0)
                {
                    //first import
                    generateTangents = hasNormalMappedMaterials;
                }
                else if (hasNormalMappedMaterials && !generateTangents)
                {
                    LWOImporterLogManager.AddWarning("Normal mapping found. Enable 'Generate Tangents' in the model inspector for correct material rendering.");
                }

                lModel.BuildLayers(generateTangents,
                                   importBlendShapes,
                                   smoothVaryingSurfaces,
                                   useSmoothingAngles,
                                   lbDegeneratePolygonWarning,
                                   indexFormat,
                                   generateUV2,
                                   uv2Separation);

                GameObject lRoot = lModel.BuildGameObjectHierarchy(generateLWOHierarchy);
                ctx.AddObjectToAsset(lRoot.name, lRoot);
                ctx.SetMainObject(lRoot);

                importCount++;
            }

#if !LEGACY_REMAPPING
            //ensures we use the new remapped material list
            remappedMaterials = null;
#endif

            lLogManager.OutputToLog();
        } 
    }
}