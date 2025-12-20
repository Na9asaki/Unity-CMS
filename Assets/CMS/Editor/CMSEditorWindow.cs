using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using CMS.CMSData;
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

        // --- Новая часть для генерации классов ---
        private string generatePath = "Assets/CMSGenerated";

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
            manifestPaths.AddRange(Directory.GetFiles(cmsRoot, "manifest.json", SearchOption.AllDirectories));
        }

        private void LoadAllLoaders()
        {
            allLoaders = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => typeof(CMSBaseLoader).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
                .ToList();

            loaderNames = allLoaders.Select(t => t.FullName).ToArray();
        }

        #endregion

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            DrawManifestsPanel();
            DrawEditorPanel();
            DrawGeneratorPanel(); // моя часть
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

            if (GUILayout.Button("New Manifest"))
                CreateNewManifest();

            if (GUILayout.Button("Refresh Manifests"))
                LoadAllManifests();

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Right panel (редактирование манифеста)

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

        #region Generator panel (новая часть)

        private void DrawGeneratorPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(300));
            EditorGUILayout.LabelField("Generate Static CMS Class", EditorStyles.boldLabel);

            generatePath = EditorGUILayout.TextField("Save Path", generatePath);

            if (GUILayout.Button("Generate CMSEntityIds"))
            {
                GenerateCMSEntityIdsClass(generatePath);
            }

            EditorGUILayout.EndVertical();
        }

        private void GenerateCMSEntityIdsClass(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            string className = "CMSEntityIds";
            string classPath = Path.Combine(path, className + ".cs");

            // Если файл уже существует — удаляем, чтобы перезаписать
            if (File.Exists(classPath))
                File.Delete(classPath);

            HashSet<string> allIds = new HashSet<string>();

            foreach (var manifestPath in manifestPaths)
            {
                var manifest = JsonUtility.FromJson<ManifestDto>(File.ReadAllText(manifestPath));
                if (manifest.Data == null) continue;

                string folderDir = Path.Combine(Application.dataPath, "Resources/CMS", manifest.Path);

                foreach (var entry in manifest.Data)
                {
                    string jsonFile = Path.Combine(folderDir, entry.Key + ".json");
                    if (!File.Exists(jsonFile)) continue;

                    var loaderType = ResolveLoaderType(entry.Loader);
                    var dataType = CMSLoaderReflection.GetDataTypeFromLoader(loaderType);
                    if (dataType == null) continue;

                    CMSRootData obj = (CMSRootData)Activator.CreateInstance(dataType);
                    try
                    {
                        string jsonText = File.ReadAllText(jsonFile);
                        if (!string.IsNullOrWhiteSpace(jsonText))
                            JsonUtility.FromJsonOverwrite(jsonText, obj);
                    }
                    catch
                    {
                        Debug.LogWarning($"Не удалось прочитать JSON: {jsonFile}");
                        continue;
                    }

                    if (!string.IsNullOrEmpty(obj.Id))
                        allIds.Add(obj.Id);
                }
            }

            // Перезаписываем файл
            using (var writer = new StreamWriter(classPath, false)) // false = overwrite
            {
                writer.WriteLine("public static class " + className);
                writer.WriteLine("{");
                foreach (var id in allIds)
                {
                    string constName = id.Replace(" ", "_").Replace("-", "_");
                    writer.WriteLine($"\tpublic const string {constName} = \"{id}\";");
                }
                writer.WriteLine("}");
            }

            AssetDatabase.Refresh();
            Debug.Log($"Generated static class with all IDs from JSONs: {classPath}");
        }




        #endregion

        #region File & Manifest operations (твоя логика)

        private void LoadManifest(string path)
        {
            currentManifestPath = path;
            currentManifest = JsonUtility.FromJson<ManifestDto>(File.ReadAllText(path));
        }

        private void SaveCurrentManifest()
        {
            if (currentManifest == null)
                return;

            string cmsRoot = Path.Combine(Application.dataPath, "Resources/CMS");
            string folderPath = Path.Combine(cmsRoot, currentManifest.Path);

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            currentManifestPath = Path.Combine(folderPath, "manifest.json");

            // Сохраняем манифест
            File.WriteAllText(currentManifestPath, JsonUtility.ToJson(currentManifest, true));
            AssetDatabase.Refresh();

            // Создаём недостающие файлы через твой метод
            foreach (var entry in currentManifest.Data)
            {
                CreateDataFile(entry); // твой старый метод, не трогаем
            }

            Debug.Log("CMS: Manifest saved & synced");
        }

        private void CreateDataFile(ManifestDataEntry entry)
        {
            if (string.IsNullOrEmpty(entry.Loader)) return;

            var loaderType = ResolveLoaderType(entry.Loader);
            var dataType = CMSLoaderReflection.GetDataTypeFromLoader(loaderType);
            if (dataType == null) return;

            object instance = Activator.CreateInstance(dataType);
            string dir = Path.Combine(Application.dataPath, "Resources/CMS", currentManifest.Path);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            string filePath = Path.Combine(dir, entry.Key + ".json");
            if (File.Exists(filePath)) return;

            File.WriteAllText(filePath, JsonUtility.ToJson(instance, true));
            AssetDatabase.Refresh();
        }

        private Type ResolveLoaderType(string loaderFullName)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.FullName == loaderFullName);
        }

        private void DeleteDataFile(string key)
        {
            string path = Path.Combine(Application.dataPath, "Resources/CMS", currentManifest.Path, key + ".json");
            if (File.Exists(path))
            {
                File.Delete(path);
                AssetDatabase.Refresh();
                Debug.Log($"CMS: Deleted {path}");
            }
        }

        private void CreateNewManifest()
        {
            currentManifest = new ManifestDto
            {
                Id = Guid.NewGuid().ToString(),
                Path = "NewFolder",
                Data = new List<ManifestDataEntry>()
            };
            currentManifestPath = null;
        }

        #endregion
    }
}
