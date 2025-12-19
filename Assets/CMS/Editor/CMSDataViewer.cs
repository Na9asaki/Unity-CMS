using System.Collections.Generic;
using System.IO;
using CMS.Loaders;
using UnityEditor;
using UnityEngine;

namespace CMS.DevTools
{
    public class CMSDataViewer : EditorWindow
    {
        private Vector2 scrollPos;
        private List<ManifestDto> allManifests = new List<ManifestDto>();
        private Texture2D folderIcon;
        private Texture2D prefabIcon;

        [MenuItem("CMS/Content Viewer")]
        public static void OpenWindow()
        {
            GetWindow<CMSDataViewer>("Content Viewer");
        }

        private void OnEnable()
        {
            // Заменяем иконки на более “контентные”
            folderIcon = EditorGUIUtility.IconContent("Folder Icon").image as Texture2D; // папка → префаб
            prefabIcon = EditorGUIUtility.IconContent("Prefab Icon").image as Texture2D;        // файл → префаб

            RefreshData();
        }

        private void RefreshData()
        {
            allManifests.Clear();
            string cmsRoot = Path.Combine(Application.dataPath, "Resources/CMS");

            if (Directory.Exists(cmsRoot))
            {
                var manifestFiles = Directory.GetFiles(cmsRoot, "manifest.json", SearchOption.AllDirectories);
                foreach (var path in manifestFiles)
                {
                    string json = File.ReadAllText(path);
                    var manifest = JsonUtility.FromJson<ManifestDto>(json);
                    if (manifest != null)
                        allManifests.Add(manifest);
                }
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Content Viewer", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            foreach (var manifest in allManifests)
            {
                EditorGUILayout.BeginVertical("box");

                // Папка / категория ассетов
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(folderIcon, GUILayout.Width(20), GUILayout.Height(20));
                EditorGUILayout.LabelField($"Категория: {manifest.Id}", EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.LabelField($"Локация: {manifest.Path}");

                if (manifest.Data is { Count: > 0 })
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Контент:");
                    EditorGUI.indentLevel++;
                    foreach (var entry in manifest.Data)
                    {
                        EditorGUILayout.BeginHorizontal("box");
                        GUILayout.Label(prefabIcon, GUILayout.Width(20), GUILayout.Height(20));
                        EditorGUILayout.LabelField(entry.Key); // Имя ассета

                        // Кнопка Open → открывает отдельное окно JSON Editor
                        if (GUILayout.Button("Редактировать", GUILayout.Width(100)))
                        {
                            string jsonPath = Path.Combine(Application.dataPath, "Resources/CMS", manifest.Path, entry.Key + ".json");
                            CMSJsonInspector.Open(jsonPath, entry.Loader);
                        }

                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }

            if (GUILayout.Button("Обновить"))
            {
                RefreshData();
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
