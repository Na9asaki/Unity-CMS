using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using CMS.Loaders;
using Source.CMS.CMSData;

namespace CMS.DevTools
{
    public class CMSJsonInspector : EditorWindow
    {
        private string filePath;
        private CMSRootData targetObject;
        private Type selectedType;
        private Vector2 scrollPos;

        private Type[] allTypes;

        [MenuItem("CMS/JSON Inspector")]
        public static void Open(string path, string loaderTypeName)
        {
            var window = GetWindow<CMSJsonInspector>("JSON Inspector");
            window.filePath = path;
            window.ResolveTypeFromLoader(loaderTypeName);
            window.LoadTypes();
            window.LoadJson();
        }
        private void OnEnable()
        {
            LoadTypes();
        }
        
        private void ResolveTypeFromLoader(string loaderTypeName)
        {
            var loaderType = Type.GetType(loaderTypeName);
            if (loaderType == null)
            {
                Debug.LogError($"Loader type not found: {loaderTypeName}");
                return;
            }

            var dataType = CMSLoaderReflection.GetDataTypeFromLoader(loaderType);
            if (dataType == null)
            {
                Debug.LogError($"Loader {loaderTypeName} has no CMSLoader<T>");
                return;
            }

            selectedType = dataType;
        }



        private void LoadTypes()
        {
            allTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsSubclassOf(typeof(CMSRootData)) && !t.IsAbstract)
                .ToArray();
        }

        private void InitTargetObject()
        {
            // Если выбран тип, создаём объект
            if (selectedType == null && allTypes.Length > 0)
                selectedType = allTypes[0];

            if (selectedType != null)
                LoadJson();
        }

        private void LoadJson()
        {
            if (selectedType == null) return;

            // Создаём экземпляр выбранного типа
            targetObject = (CMSRootData)Activator.CreateInstance(selectedType);

            if (File.Exists(filePath))
            {
                try
                {
                    string jsonText = File.ReadAllText(filePath);
                    if (!string.IsNullOrWhiteSpace(jsonText))
                        JsonUtility.FromJsonOverwrite(jsonText, targetObject);
                }
                catch
                {
                    Debug.LogWarning("Не удалось распарсить JSON, используются значения по умолчанию.");
                }
            }
        }

        private void OnGUI()
        {
            if (string.IsNullOrEmpty(filePath))
            {
                EditorGUILayout.LabelField("Файл не выбран");
                return;
            }

            EditorGUILayout.LabelField($"Editing JSON: {Path.GetFileName(filePath)}", EditorStyles.boldLabel);

            if (allTypes == null || allTypes.Length == 0)
            {
                EditorGUILayout.HelpBox("Нет доступных типов CMSRootData", MessageType.Warning);
                return;
            }

            string[] typeNames = allTypes.Select(t => t.FullName).ToArray();
            int selectedIndex = selectedType != null ? Array.IndexOf(allTypes, selectedType) : 0;
            int newIndex = EditorGUILayout.Popup("Select Type", selectedIndex, typeNames);
            if (newIndex != selectedIndex)
            {
                selectedType = allTypes[newIndex];
                LoadJson();
            }

            if (targetObject == null)
            {
                EditorGUILayout.HelpBox("Создаётся объект выбранного типа...", MessageType.Info);
                return;
            }

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            DrawFields(targetObject);

            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save"))
            {
                string json = JsonUtility.ToJson(targetObject, true);
                File.WriteAllText(filePath, json);
                AssetDatabase.Refresh();
            }

            if (GUILayout.Button("Reload"))
            {
                LoadJson();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawFields(object obj)
        {
            if (obj == null) return;

            FieldInfo[] fields = obj.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);

            foreach (var field in fields)
            {
                object value = field.GetValue(obj);
                Type fieldType = field.FieldType;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(field.Name, GUILayout.Width(150));

                if (fieldType == typeof(int))
                    field.SetValue(obj, EditorGUILayout.IntField((int)value));
                else if (fieldType == typeof(float))
                    field.SetValue(obj, EditorGUILayout.FloatField((float)value));
                else if (fieldType == typeof(string))
                    field.SetValue(obj, EditorGUILayout.TextField((string)value));
                else if (fieldType == typeof(bool))
                    field.SetValue(obj, EditorGUILayout.Toggle((bool)value));
                else if (fieldType.IsEnum)
                    field.SetValue(obj, EditorGUILayout.EnumPopup((Enum)value));
                else if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    DrawListField(obj, field, value);
                }
                else if (typeof(CMSRootData).IsAssignableFrom(fieldType))
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUI.indentLevel++;
                    DrawFields(value);
                    EditorGUI.indentLevel--;
                    continue;
                }
                else
                    EditorGUILayout.LabelField($"Unsupported type: {fieldType.Name}");

                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawListField(object parent, FieldInfo field, object value)
        {
            Type elementType = field.FieldType.GetGenericArguments()[0];
            var list = value as System.Collections.IList;

            if (list == null) return;

            EditorGUILayout.BeginVertical("box");
            for (int i = 0; i < list.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"[{i}]", GUILayout.Width(30));

                object elem = list[i];
                if (elementType == typeof(int))
                    list[i] = EditorGUILayout.IntField((int)elem);
                else if (elementType == typeof(float))
                    list[i] = EditorGUILayout.FloatField((float)elem);
                else if (elementType == typeof(string))
                    list[i] = EditorGUILayout.TextField((string)elem);
                else if (elementType == typeof(bool))
                    list[i] = EditorGUILayout.Toggle((bool)elem);
                else if (elementType.IsEnum)
                    list[i] = EditorGUILayout.EnumPopup((Enum)elem);
                else if (typeof(CMSRootData).IsAssignableFrom(elementType))
                {
                    EditorGUI.indentLevel++;
                    DrawFields(elem);
                    EditorGUI.indentLevel--;
                }
                else
                {
                    EditorGUILayout.LabelField($"Unsupported element type: {elementType.Name}");
                }

                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    list.RemoveAt(i);
                    i--;
                }

                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Add Element"))
            {
                list.Add(Activator.CreateInstance(elementType));
            }

            EditorGUILayout.EndVertical();
        }

    }
}
