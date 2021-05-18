using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.Assertions;
using Debug = UnityEngine.Debug;


namespace ARFoundationRemote.Editor {
    public class ARFoundationRemoteInstaller : ScriptableObject {
        [Tooltip("Use this field if your platform require additional extension when making a build.")]
        [SerializeField] public string optionalCompanionAppExtension = "";
        [Tooltip("By default, the plugin modifies the package name while making the AR Companion build so it doesn't override your real app on your AR device.")]
        [SerializeField] public bool modifyAppId = true;

        public const string pluginId = "com.kyrylokuzyk.arfoundationremote";
        public const string displayName = "AR Foundation Editor Remote";
        const string packagesFolderName = "Packages";
        static readonly Dictionary<string, string> minDependencies = new Dictionary<string, string> {
            {"com.unity.xr.arfoundation", "3.0.1"},
            {"com.unity.xr.arsubsystems", "3.0.0"},
            {"com.unity.xr.arcore", "3.0.1"},
            {"com.unity.xr.arkit", "3.0.1"},
            {"com.unity.xr.arkit-face-tracking", "3.0.1"},
        };

        static DirectoryInfo dataPathParent => Directory.GetParent(Application.dataPath);
        static char slash => Path.DirectorySeparatorChar;
        static string sourceFolderName => $"{dataPathParent}{slash}Assets{slash}Plugins{slash}ARFoundationRemoteInstaller{slash}{pluginId}";
        static string destinationFolderName => $"{dataPathParent}{slash}{packagesFolderName}{slash}{pluginId}";


        public static void UnInstallPlugin() {
            #if AR_FOUNDATION_REMOTE_INSTALLED
                #if AR_FOUNDATION_REMOTE_4_12_0_OR_NEWER
                if (EditorUtility.IsDirty(Runtime.SessionRecordings.Instance)) {
                    Debug.LogError($"{displayName}: please save the SessionRecordings.asset before uninstall to prevent file corruption.");
                    return;
                }
                #endif
            
            if (FixesForEditorSupport.Undo()) {
                Debug.LogError($"{displayName}: undoing AR Foundation fixes... Please press the 'Un-install Plugin' button again.");
                return;
            }
            #endif
            
            var listRequest = Client.List(true, false);
            runRequest(listRequest, () => {
                Assert.AreEqual(StatusCode.Success, listRequest.Status);
                var plugin = listRequest.Result.SingleOrDefault(_ => _.name == pluginId);
                Assert.IsNotNull(plugin);
                if (plugin.source == PackageSource.Embedded) {
                    moveFolder(destinationFolderName, sourceFolderName);
                    logUninstallSuccess();
                } else {
                    Debug.LogError("Removing plugin via Package Manager. This error should not be visible in production!");
                    var removeRequest = Client.Remove(pluginId);
                    runRequest(removeRequest, () => {
                        if (removeRequest.Status == StatusCode.Success) {
                            logUninstallSuccess();
                        } else {
                            Debug.LogError($"removeRequest failed {removeRequest.Error}");
                        }
                    });
                }    
            });
            
            void logUninstallSuccess() {
                Debug.Log($"{displayName} was uninstalled from Packages folder. To uninstall the plugin completely, please delete the ARFoundationRemoteInstaller folder.");
            }
        }

        static void checkDependencies(Action<bool> callback) {
            var listRequest = Client.List(true, true);
            runRequest(listRequest, () => {
                callback(checkVersions(listRequest.Result));
            });
        }

        static bool checkVersions(PackageCollection packages) {
            var result = true;
            foreach (var package in packages) {
                var packageName = package.name;
                var currentVersion = parseUnityPackageManagerVersion(package.version);
                if (minDependencies.TryGetValue(packageName, out string dependency)) {
                    //Debug.Log(packageName);
                    var minRequiredVersion = new Version(dependency);
                    if (currentVersion < minRequiredVersion) {
                        result = false;
                        Debug.LogError("Please update this package to the required version via Window -> Package Manager: " + packageName + ":" + minRequiredVersion);
                    }
                }
            }

            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS) {
                if (packages.All(_ => _.name != "com.unity.xr.arkit-face-tracking")) {
                    Debug.Log("To enable iOS face tracking, install ARKit Face Tracking 3.0.1 via Package Manager.");
                }
            }
            
            return result;
        }

        static Version parseUnityPackageManagerVersion(string version) {
            var versionNumbersStrings = version.Split('.', '-');
            const int numOfVersionComponents = 3;
            Assert.IsTrue(versionNumbersStrings.Length >= numOfVersionComponents);
            var numbers = new List<int>();
            for (int i = 0; i < numOfVersionComponents; i++) {
                var str = versionNumbersStrings[i];
                if (int.TryParse(str, out int num)) {
                    numbers.Add(num);
                } else {
                    throw new Exception("cant parse " + str + " in " + version);
                }
            }

            return new Version(numbers[0], numbers[1], numbers[2]);
        }

        static Action requestCompletedCallback;
        static Request currentRequest;

        public static void runRequest(Request request, Action callback) {
            if (currentRequest != null) {
                Debug.Log(currentRequest.GetType().Name + " is already running, skipping new " + request.GetType().Name);
                return;
            }
        
            Assert.IsNull(requestCompletedCallback);
            Assert.IsNull(currentRequest);
            currentRequest = request;
            requestCompletedCallback = callback;
            EditorApplication.update += editorUpdate;
        }

        static void editorUpdate() {
            Assert.IsNotNull(currentRequest);
            if (currentRequest.IsCompleted) {
                EditorApplication.update -= editorUpdate;
                currentRequest = null;
                var cachedCallback = requestCompletedCallback;
                requestCompletedCallback = null;
                cachedCallback();
            }
        }

        [Conditional("_")]
        public static void log(string msg) {
            Debug.Log(msg);
        }
        
        public static void InstallPlugin(bool verbose) {
            AutoARFoundationFixes.Enabled = true;
            checkDependencies(success => {
                if (success) {
                    if (!Directory.Exists(sourceFolderName)) {
                        if (verbose) {
                            Debug.LogError($"{displayName}: please re-import the plugin or buy the additional license if you're trying to install the plugin on different development machine.");
                        }

                        return;
                    }

                    if (Directory.Exists(destinationFolderName)) {
                        Directory.Delete(destinationFolderName, true);
                    }

                    moveFolder(sourceFolderName, destinationFolderName);
                    addGitIgnore();
                    Debug.Log(displayName + " installed successfully. Please read DOCUMENTATION located at Assets/Plugins/ARFoundationRemoteInstaller/DOCUMENTATION.txt");
                } else {
                    Debug.LogError(displayName + " installation failed. Please fix errors and press 'Installer-Install Plugin'");
                }
            });
        }

        static void moveFolder(string source, string dest) {
            Assert.IsTrue(Directory.Exists(source));
            Assert.IsFalse(Directory.Exists(dest));
            Directory.Move(source, dest);
            File.Delete($"{source}.meta");
            #if UNITY_2020_1_OR_NEWER
            Client.Resolve();
            #elif UNITY_2019_4_OR_NEWER
            var method = typeof(Client).GetMethod("Resolve", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            Assert.IsNotNull(method);
            method.Invoke(null, null);
            #else
            AssetDatabase.Refresh();
            #endif
        }

        static void addGitIgnore() {
            var currentFolder = dataPathParent;
            while (true) {
                var path = $"{currentFolder}{slash}.gitignore";
                if (File.Exists(path)) {
                    log($"gitignore found at path {path}");
                    var text = File.ReadAllText(path);
                    if (!text.Contains(pluginId)) {
                        text += $"\n{pluginId}";
                        File.WriteAllText(path, text);
                    }   
                    
                    return;
                }

                currentFolder = currentFolder.Parent;
                if (currentFolder == null) {
                    log("gitignore not found in any parent folder");
                    return;
                }
            }
        }

        public static void OnImport() {
            log("OnImport");
            InstallPlugin(false);
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        static void didReloadScripts() {
            #if AR_FOUNDATION_REMOTE_INSTALLED
            if (AutoARFoundationFixes.Enabled) {
                FixesForEditorSupport.Apply();
            } else {
                log("skipping fixes");
            }
            #endif
        }
    }
    
    
    public static class AutoARFoundationFixes {
        const string key = "ARFondationRemote_auto_apply_fixes";
        
        public static bool Enabled {
            get => EditorPrefs.GetBool(key, true);
            set {
                ARFoundationRemoteInstaller.log($"{key}: {value}");
                EditorPrefs.SetBool(key, value);
            }
        }
    }
}
