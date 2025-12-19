using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using CMS.Loaders;

namespace CMS.DevTools
{
    public class CMSEditorWindow : EditorWindow
    {
        private Vector2 scrollPos;

        private List<string> manifestPaths = new();
        private ManifestDto currentManifest;
        private string currentManifestPath;

        private List<Type> allLoaders;
        private string[] loaderNames;

        [MenuItem("CMS/Editor")]
        public static void OpenWindow()
        {
            GetWindow<CMSEditorWindow>("CMS Editor");
        }

        private void OnEnable()
        {
            LoadAllManifests();
            LoadAllLoaders();
        }

        #region Loaders & Manifests discovery

        private void LoadAllManifests()
        {
            manifestPaths.Clear();

            string cmsRoot = Path.Combine(Application.dataPath, "Resources/CMS");
            if (!Directory.Exists(cmsRoot))
                return;

            manifestPaths.AddRange(
                Directory.GetFiles(cmsRoot, "manifest.json", SearchOption.AllDirectories)
            );
        }

        private void LoadAllLoaders()
        {
            allLoaders = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t =>
                    typeof(CMSBaseLoader).IsAssignableFrom(t) &&
                    !t.IsAbstract &&
                    !t.IsInterface
                )
                .ToList();

            loaderNames = allLoaders.Select(t => t.FullName).ToArray();
        }

        #endregion

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();

            DrawManifestsPanel();
            DrawEditorPanel();

            EditorGUILayout.EndHorizontal();
        }

        #region Left panel

        private void DrawManifestsPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(240));
            EditorGUILayout.LabelField("CMS Manifests", EditorStyles.boldLabel);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            foreach (var path in manifestPaths)
            {
                if (GUILayout.Button(Path.GetDirectoryName(path)))
                    LoadManifest(path);
            }
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("Add New Folder"))
                CreateNewFolder();

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Right panel

        private void DrawEditorPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));

            if (currentManifest == null)
            {
                EditorGUILayout.HelpBox("Select a manifest to edit", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.LabelField($"Editing Manifest: {currentManifest.Id}", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            currentManifest.Id = EditorGUILayout.TextField("Id", currentManifest.Id);
            currentManifest.Path = EditorGUILayout.TextField("Path", currentManifest.Path);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Files", EditorStyles.boldLabel);

            if (currentManifest.Data == null)
                currentManifest.Data = new List<ManifestDataEntry>();

            for (int i = 0; i < currentManifest.Data.Count; i++)
            {
                var entry = currentManifest.Data[i];

                EditorGUILayout.BeginHorizontal("box");

                entry.Key = EditorGUILayout.TextField(entry.Key);

                int loaderIndex = Mathf.Max(0, Array.IndexOf(loaderNames, entry.Loader));
                int newIndex = EditorGUILayout.Popup(loaderIndex, loaderNames);
                if (newIndex != loaderIndex)
                    entry.Loader = loaderNames[newIndex];

                if (GUILayout.Button("X", GUILayout.Width(22)))
                {
                    DeleteDataFile(entry.Key);
                    currentManifest.Data.RemoveAt(i);
                    i--;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Add File"))
            {
                currentManifest.Data.Add(new ManifestDataEntry
                {
                    Key = "newFile",
                    Loader = loaderNames.Length > 0 ? loaderNames[0] : null
                });
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Save Manifest"))
                SaveCurrentManifest();

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region File operations

        private void CreateDataFile(ManifestDataEntry entry)
        {
            if (string.IsNullOrEmpty(entry.Loader))
                return;

            var loaderType = Type.GetType(entry.Loader);
            if (loaderType == null)
            {
                Debug.LogError($"CMS: Loader not found: {entry.Loader}");
                return;
            }

            var dataType = CMSLoaderReflection.GetDataTypeFromLoader(loaderType);
            if (dataType == null)
            {
                Debug.LogError($"CMS: Loader {entry.Loader} has no CMSLoader<T>");
                return;
            }

            object instance = Activator.CreateInstance(dataType);

            string dir = Path.Combine(
                Application.dataPath,
                "Resources/CMS",
                currentManifest.Path
            );

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string filePath = Path.Combine(dir, entry.Key + ".json");

            if (File.Exists(filePath))
            {
                Debug.LogWarning($"CMS: File already exists: {filePath}");
                return;
            }

            File.WriteAllText(filePath, JsonUtility.ToJson(instance, true));
            AssetDatabase.Refresh();

            Debug.Log($"CMS: Created {filePath}");
        }

        private void DeleteDataFile(string key)
        {
            string path = Path.Combine(
                Application.dataPath,
                "Resources/CMS",
                currentManifest.Path,
                key + ".json"
            );

            if (File.Exists(path))
            {
                File.Delete(path);
                AssetDatabase.Refresh();
                Debug.Log($"CMS: Deleted {path}");
            }
        }

        #endregion

        #region Manifest IO

        private void LoadManifest(string path)
        {
            currentManifestPath = path;
            currentManifest = JsonUtility.FromJson<ManifestDto>(File.ReadAllText(path));
        }

        private void SaveCurrentManifest()
        {
            if (string.IsNullOrEmpty(currentManifestPath))
                return;

            string dataDir = Path.Combine(
                Application.dataPath,
                "Resources/CMS",
                currentManifest.Path
            );

            if (!Directory.Exists(dataDir))
                Directory.CreateDirectory(dataDir);

            // все файлы на диске
            var existingFiles = Directory.Exists(dataDir)
                ? Directory.GetFiles(dataDir, "*.json")
                    .Select(f => Path.GetFileNameWithoutExtension(f))
                    .ToHashSet()
                : new HashSet<string>();

            // все ключи из manifest
            var manifestKeys = currentManifest.Data
                .Select(d => d.Key)
                .ToHashSet();

            // ➕ создать недостающие
            foreach (var entry in currentManifest.Data)
            {
                if (existingFiles.Contains(entry.Key))
                    continue;

                CreateDataFileFromEntry(entry, dataDir);
            }

            // ❌ удалить лишние
            foreach (var file in existingFiles)
            {
                if (!manifestKeys.Contains(file))
                {
                    File.Delete(Path.Combine(dataDir, file + ".json"));
                }
            }

            // сохранить manifest
            File.WriteAllText(
                currentManifestPath,
                JsonUtility.ToJson(currentManifest, true)
            );

            AssetDatabase.Refresh();
            Debug.Log("CMS: Manifest saved & synced");
        }
        
        private void CreateDataFileFromEntry(ManifestDataEntry entry, string dataDir)
        {
            if (string.IsNullOrEmpty(entry.Loader))
                return;

            var loaderType = Type.GetType(entry.Loader);
            var dataType = CMSLoaderReflection.GetDataTypeFromLoader(loaderType);

            if (dataType == null)
            {
                Debug.LogError($"CMS: Cannot resolve data type for {entry.Loader}");
                return;
            }

            object instance = Activator.CreateInstance(dataType);
            string path = Path.Combine(dataDir, entry.Key + ".json");

            File.WriteAllText(path, JsonUtility.ToJson(instance, true));
        }

        private void CreateNewFolder()
        {
            string cmsRoot = Path.Combine(Application.dataPath, "Resources/CMS");
            Directory.CreateDirectory(cmsRoot);

            string baseName = "NewDataFolder";
            string folderPath = Path.Combine(cmsRoot, baseName);
            int counter = 1;

            while (Directory.Exists(folderPath))
            {
                folderPath = Path.Combine(cmsRoot, baseName + counter);
                counter++;
            }

            Directory.CreateDirectory(folderPath);

            var manifest = new ManifestDto
            {
                Id = Path.GetFileName(folderPath),
                Path = $"CMS/{Path.GetFileName(folderPath)}",
                Data = new List<ManifestDataEntry>()
            };

            string manifestPath = Path.Combine(folderPath, "manifest.json");
            File.WriteAllText(manifestPath, JsonUtility.ToJson(manifest, true));

            AssetDatabase.Refresh();

            LoadAllManifests();
            LoadManifest(manifestPath);
        }

        #endregion
    }
}
