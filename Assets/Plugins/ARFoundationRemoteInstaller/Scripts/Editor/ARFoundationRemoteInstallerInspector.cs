#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;


namespace ARFoundationRemote.Editor {
    [CustomEditor(typeof(ARFoundationRemoteInstaller), true), CanEditMultipleObjects]
    public class ARFoundationRemoteInstallerInspector : UnityEditor.Editor {
        public override void OnInspectorGUI() {
            if (EditorApplication.isCompiling) {
                return;
            }
            
            DrawDefaultInspector();
            showMethodsInInspector(targets);
        }

        static void showMethodsInInspector(params Object[] targets) {
            var target = targets.First() as ARFoundationRemoteInstaller;
            Assert.IsNotNull(target);

            if (isInstalled) {
                GUILayout.Space(16);
                GUILayout.Label("AR Companion app", EditorStyles.boldLabel);
                if (GUILayout.Button("Install AR Companion App", new GUIStyle(GUI.skin.button) {fontStyle = FontStyle.Bold})) {
                    AutoARFoundationFixes.Enabled = true;
                    #if AR_FOUNDATION_REMOTE_INSTALLED
                    if (FixesForEditorSupport.Apply()) {
                        Debug.LogError($"{ARFoundationRemoteInstaller.displayName}: applying AR Foundation fixes... Please retry the AR Companion app installation.");
                        return;
                    }
                    #endif
                
                    execute(() => CompanionAppInstaller.BuildAndRun(target.optionalCompanionAppExtension, target.modifyAppId));
                }

                if (GUILayout.Button("Build AR Companion and show in folder", new GUIStyle(GUI.skin.button))) {
                    execute(() => CompanionAppInstaller.Build(target.optionalCompanionAppExtension, target.modifyAppId));
                }

                if (GUILayout.Button("Open Plugin Settings")) {
                    #if AR_FOUNDATION_REMOTE_INSTALLED
                        Selection.activeObject = Runtime.Settings.Instance;
                    #endif
                }

                if (GUILayout.Button("Delete AR Companion app build folder")) {
                    execute(CompanionAppInstaller.DeleteCompanionAppBuildFolder);
                }

                GUILayout.Space(16);
                if (GUILayout.Button("Apply AR Foundation fixes")) {
                    AutoARFoundationFixes.Enabled = true;
                    #if AR_FOUNDATION_REMOTE_INSTALLED
                        FixesForEditorSupport.Apply(true);
                    #endif
                }
                
                GUILayout.Space(16);
                GUILayout.Label(ARFoundationRemoteInstaller.displayName, EditorStyles.boldLabel);
                if (GUILayout.Button("Un-install Plugin", new GUIStyle(GUI.skin.button) {normal = {textColor = Color.red}})) {
                    AutoARFoundationFixes.Enabled = false;
                    ARFoundationRemoteInstaller.UnInstallPlugin();
                }
            } else {
                if (GUILayout.Button("Install Plugin")) {
                    ARFoundationRemoteInstaller.InstallPlugin(true);
                }    
            }
        }

        static bool isInstalled {
            get {
                return
                #if AR_FOUNDATION_REMOTE_INSTALLED
                    true;
                #else
                    false;
                #endif
            }
        }
        
        static void execute(Action action) {
            action();
        }
    }
}
#endif // UNITY_EDITOR
