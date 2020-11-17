using UnityEngine.Timeline; //if this line is throwing an error in Unity 2018 then you need to import the TimeLine Package using the PackageManager

using System.Collections.Generic;
using System.IO;

using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

using VirtualEscapes.Common.Importers.LWS.Objects;
using VirtualEscapes.Common.Editor;

namespace VirtualEscapes.Common.Importers.LWS
{
    public static class LWSceneBuilder
    {
        internal static void Build(List<LWSObject> lwsObjects, string assetPath, int firstFrame, int lastFrame, int fps, bool hasAnimation, bool drawBones, string overwritePath)
        {
            bool lbHasOverwritePath = !string.IsNullOrEmpty(overwritePath);

            HashSet<LWSGameObject> laAnimatedObjects = new HashSet<LWSGameObject>();

            Scene lScene = LWSceneBuilderFunctions.BuildScene(lwsObjects, laAnimatedObjects, firstFrame, fps, drawBones);

            ProgressBar lProgressBar = null;
            if (hasAnimation)
            {
                int liAnimationCurvesCount = 0;
                foreach (LWSGameObject animatedLWSGameObject in laAnimatedObjects)
                {
                    liAnimationCurvesCount += animatedLWSGameObject.animationCurveCount;
                }

                lProgressBar = new ProgressBar("Creating Scene", new ProgressBar.Stage[]
                {
                    new ProgressBar.Stage("Building Timeline", liAnimationCurvesCount),
                    new ProgressBar.Stage("Saving Assets to AssetDatabase", liAnimationCurvesCount) //saving assets takes at least as long as building the timeline
                
                }, false);

                lProgressBar.Update();

                buildTimeline(laAnimatedObjects, assetPath, firstFrame, lastFrame, fps, lProgressBar);

                lProgressBar.IncrementStage();
                lProgressBar.Update(true);
            }
            

            if (lbHasOverwritePath)
            {
                EditorSceneManager.SaveScene(lScene, overwritePath);
            }
            else
            {
                EditorSceneManager.MarkSceneDirty(lScene);
            }

            AssetDatabase.SaveAssets();

            if (hasAnimation)
            {
                lProgressBar.IncrementStage();
                lProgressBar.Update();

                lProgressBar.Clear();
            }
        }

        private static GameObject buildTimeline(HashSet<LWSGameObject> animatedObjects, string assetPath, int firstFrame, int lastFrame, int fps, ProgressBar progressBar)
        {
            string lsPath = Path.GetDirectoryName(assetPath);
            string lsFileName = Path.GetFileNameWithoutExtension(assetPath);
            TimelineAsset timeline = ScriptableObject.CreateInstance<TimelineAsset>();

            AssetDatabase.CreateAsset(timeline, lsPath + "/" + lsFileName + "_Timeline.playable");

            timeline.durationMode = TimelineAsset.DurationMode.FixedLength;
            int liTotalFrames = lastFrame - firstFrame;
            float lfDurationSeconds = ((float)liTotalFrames) / fps;

            timeline.fixedDuration = lfDurationSeconds;
            timeline.editorSettings.fps = fps;

            //create a new GameObject that will store our timeline component
            GameObject lDirectorGO = new GameObject("Timeline");
            PlayableDirector director = lDirectorGO.AddComponent<PlayableDirector>();
            director.playableAsset = timeline;

            PlayableGraph graph = PlayableGraph.Create();

            foreach (LWSGameObject animatedLWSGameObject in animatedObjects)
            {
                GameObject lGameObject = animatedLWSGameObject.gameObject;
                Animator lAnimator = lGameObject.AddComponent<Animator>();

                AnimationTrack lTrack = timeline.CreateTrack<AnimationTrack>(null, lGameObject.name + "_track");

                //CreateRecordableClip is used rather than CreateClip - it automatically adds the track as a subasset of the timeline
                TimelineClip lClip = lTrack.CreateRecordableClip(lGameObject.name + "_clip");

                lClip.duration = animatedLWSGameObject.animationDuration;

                AnimationPlayableAsset lAnimationPlayableAsset = lClip.asset as AnimationPlayableAsset;

                animatedLWSGameObject.AddCurvesToClip(lAnimationPlayableAsset.clip, progressBar); //can be slow

                AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, lAnimationPlayableAsset.name, lAnimator);
                AnimationClipPlayable playable = AnimationClipPlayable.Create(graph, lAnimationPlayableAsset.clip);
                output.SetSourcePlayable(playable);

                director.SetGenericBinding(lTrack, lGameObject);
            }

            return lDirectorGO;
        }
    }
}