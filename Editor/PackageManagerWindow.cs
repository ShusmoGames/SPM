using System.Collections.Generic;
using System.Collections;
using UnityEditor;
using UnityEngine;
using Unity.EditorCoroutines.Editor;
using UnityEngine.Networking;

namespace ShusmoAPI.Editor
{
    public class PackageManagerWindow : EditorWindow
    {
        private List<Package> packages = new List<Package>();
        private bool isOperationInProgress = false;
        private string operationMessage = "";

        [MenuItem("Shusmo/SPM")]
        public static void ShowWindow()
        {
            GetWindow<PackageManagerWindow>("SPM");
        }

        private void OnEnable()
        {
            FetchPackages();
        }

        private void OnGUI()
        {
            if (isOperationInProgress)
            {
                EditorGUILayout.HelpBox(operationMessage, MessageType.Info);
                GUILayout.Label("Operation in progress. Please wait...");
            }
            else
            {
                if (GUILayout.Button("Refresh Packages", GUILayout.Height(30)))
                {
                    FetchPackages();
                }

                EditorGUILayout.Space(10);

                foreach (var package in packages)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"{package.name}          {package.version}", GUILayout.Width(300));

                    if (package.status == PackageStatus.NotInstalled)
                    {
                        if (GUILayout.Button("Install", GUILayout.Width(100)))
                        {
                            InstallPackage(package);
                        }
                    }
                    else if (package.status == PackageStatus.Outdated)
                    {
                        if (GUILayout.Button("Update", GUILayout.Width(100)))
                        {
                            UpdatePackage(package);
                        }
                    }
                    else
                    {
                        GUILayout.Label("Up to date", GUILayout.Width(100));
                    }

                    GUILayout.EndHorizontal();
                    EditorGUILayout.Space();
                }
            }
        }

        private void FetchPackages()
        {
            // Start the coroutine to fetch the package data
            EditorCoroutineUtility.StartCoroutine(FetchPackageDataCoroutine(), this);
        }

        private IEnumerator FetchPackageDataCoroutine()
        {
            string url = "https://shusmo.io/SPM/packages.json"; // Replace with your actual URL

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("Failed to fetch packages: " + request.error);
                }
                else
                {
                    try
                    {
                        var json = request.downloadHandler.text;
                        PackageList packageList = JsonUtility.FromJson<PackageList>(json);
                        packages = packageList.packages;

                        // Update the package statuses based on the installed packages
                        UpdatePackageStatuses();
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError("JSON parse error: " + ex.Message);
                    }
                }
            }
        }

        private void UpdatePackageStatuses()
        {
            if (isOperationInProgress)
            {
                Debug.LogWarning("Another operation is already in progress.");
                return;
            }

            isOperationInProgress = true;
            operationMessage = "Updating package statuses...";

            var request = UnityEditor.PackageManager.Client.List();
            float timeout = 60f; // 60 seconds timeout
            float startTime = Time.realtimeSinceStartup;

            void EditorUpdate()
            {
                if (request.IsCompleted || (Time.realtimeSinceStartup - startTime > timeout))
                {
                    if (request.IsCompleted && request.Status == UnityEditor.PackageManager.StatusCode.Success)
                    {
                        HashSet<string> installedPackageNames = new HashSet<string>();

                        foreach (var installedPackage in request.Result)
                        {
                            installedPackageNames.Add(installedPackage.name);

                            var package = packages.Find(p => p.name == installedPackage.name);
                            if (package != null)
                            {
                                package.status = installedPackage.version == package.version ? PackageStatus.UpToDate : PackageStatus.Outdated;
                            }
                        }

                        foreach (var package in packages)
                        {
                            if (!installedPackageNames.Contains(package.name))
                            {
                                package.status = PackageStatus.NotInstalled;
                            }
                        }
                    }
                    else if (request.Status >= UnityEditor.PackageManager.StatusCode.Failure)
                    {
                        Debug.LogError("Failed to list installed packages: " + request.Error.message);
                    }

                    EditorApplication.update -= EditorUpdate;
                    isOperationInProgress = false;

                    // Force the GUI to refresh
                    Repaint();
                }
            }

            EditorApplication.update += EditorUpdate;
        }

        private void InstallPackage(Package package)
        {
            if (isOperationInProgress)
            {
                Debug.LogWarning("Another operation is already in progress.");
                return;
            }

            isOperationInProgress = true;
            operationMessage = $"Installing {package.name}...";

            var request = UnityEditor.PackageManager.Client.Add($"git+{package.url}");
            float timeout = 60f; // 60 seconds timeout
            float startTime = Time.realtimeSinceStartup;

            void EditorUpdate()
            {
                if (request.IsCompleted || (Time.realtimeSinceStartup - startTime > timeout))
                {
                    if (request.IsCompleted && request.Status == UnityEditor.PackageManager.StatusCode.Success)
                    {
                        Debug.Log($"Successfully installed {package.name}");
                    }
                    else if (request.Status >= UnityEditor.PackageManager.StatusCode.Failure)
                    {
                        Debug.LogError($"Failed to install {package.name}: {request.Error.message}");
                    }

                    EditorApplication.update -= EditorUpdate;
                    isOperationInProgress = false;

                    // Refresh package statuses after installation
                    FetchPackages();
                }
            }

            EditorApplication.update += EditorUpdate;
        }

        private void UpdatePackage(Package package)
        {
            if (isOperationInProgress)
            {
                Debug.LogWarning("Another operation is already in progress.");
                return;
            }

            isOperationInProgress = true;
            operationMessage = $"Updating {package.name}...";

            var request = UnityEditor.PackageManager.Client.Add($"git+{package.url}");
            float timeout = 60f; // 60 seconds timeout
            float startTime = Time.realtimeSinceStartup;

            void EditorUpdate()
            {
                if (request.IsCompleted || (Time.realtimeSinceStartup - startTime > timeout))
                {
                    if (request.IsCompleted && request.Status == UnityEditor.PackageManager.StatusCode.Success)
                    {
                        Debug.Log($"Successfully updated {package.name}");
                    }
                    else if (request.Status >= UnityEditor.PackageManager.StatusCode.Failure)
                    {
                        Debug.LogError($"Failed to update {package.name}: {request.Error.message}");
                    }

                    EditorApplication.update -= EditorUpdate;
                    isOperationInProgress = false;

                    // Refresh package statuses after updating
                    FetchPackages();
                }
            }

            EditorApplication.update += EditorUpdate;
        }

        [System.Serializable]
        public class Package
        {
            public string name;
            public string version;
            public string url;
            public PackageStatus status;

            public Package(string name, string version, string url, PackageStatus status = PackageStatus.Unknown)
            {
                this.name = name;
                this.version = version;
                this.url = url;
                this.status = status;
            }
        }

        [System.Serializable]
        public class PackageList
        {
            public List<Package> packages;
        }

        public enum PackageStatus
        {
            NotInstalled,
            Outdated,
            UpToDate,
            Unknown
        }
    }
}