using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Lucky44.Placer
{
    public class LuckyPlacerWindow : EditorWindow
    {
        private Dictionary<string, string[]> categories;
        private bool[] open;

        //Completely open Methods
        [MenuItem("Tools/Lucky Placer")]
        public static void Open()
        {
            Type inspectorType = Type.GetType("UnityEditor.InspectorWindow,UnityEditor.dll");

            LuckyPlacerWindow window = GetWindow<LuckyPlacerWindow>(new Type[] { inspectorType });
            window.Show();
            window.Init();
        }

        public static void close()
        {
            LuckyPlacerWindow window = GetWindow<LuckyPlacerWindow>();
            window.Close();
        }

        //Class internal methods

        public void Init()
        {
            LoadPrefabs();
        }

        private void LoadPrefabs()
        {
            if (AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                categories = new Dictionary<string, string[]>();
                string[] subFolders = AssetDatabase.GetSubFolders("Assets/Prefabs");

                if (subFolders.Length <= 0)
                    return;

                foreach (string folder in subFolders)
                {
                    string[] prefabs = AssetDatabase.FindAssets("t:prefab", new string[] { folder });
                    categories[folder] = prefabs;
                }
                open = new bool[categories.Count];
            }
            else
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
                Debug.LogError("Could not find 'Prefab' folder, so one was created. Please put your Placeables into this folder, preferably into catogarized sub-folders");
                LoadPrefabs();
            }
        }

        private Vector2 scrollPos;
        private void OnGUI()
        {
            if (categories == null)
                return;

            int rowAmount = ((int)position.width - 20) / 100;

            GUIStyle noPrefabsFound = new GUIStyle(EditorStyles.boldLabel);


            if (categories.Count == 0)
            {
                center(() => { GUILayout.Label("NO CATEGORIES FOUND", noPrefabsFound); }, true, true);
                return;
            }

            scrollPos = GUILayout.BeginScrollView(scrollPos);

            for (int i = 0; i < categories.Count; i++)
            {
                string category = categories.Keys.ToArray()[i];
                string categoryName = category.Split('/')[2];

                if (open[i])
                {
                    if (GUILayout.Button($"Close {categoryName}"))
                    {
                        open[i] = false;
                    }

                    GUILayout.BeginVertical();
                    int counter = 0;
                    bool openCall = false;
                    foreach (string prefab in categories[category])
                    {
                        if (counter == 0)
                        {
                            GUILayout.BeginHorizontal();
                            openCall = true;
                        }

                        string path = AssetDatabase.GUIDToAssetPath(prefab);
                        GameObject g = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                        if (GUILayout.Button(new GUIContent(AssetPreview.GetAssetPreview(g), g.name), GUILayout.Width(100), GUILayout.Height(100)))
                        {
                            LuckyPlacer.Instance.setSelected(g);
                        }

                        counter++;
                        if (counter == rowAmount)
                        {
                            GUILayout.EndHorizontal();
                            counter = 0;
                            openCall = false;
                        }
                    }
                    if (openCall)
                        GUILayout.EndHorizontal();

                    GUILayout.EndVertical();
                }
                else
                {
                    if (GUILayout.Button($"Open {categoryName}"))
                    {
                        open[i] = true;
                    }
                }
            }

            GUILayout.EndScrollView();
        }

        private void center(Action executeInMiddle, bool horizontal, bool vertical)
        {
            if (vertical)
                GUILayout.FlexibleSpace();

            if (horizontal)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
            }

            executeInMiddle();

            if (horizontal)
            {
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            if (vertical)
                GUILayout.FlexibleSpace();
        }
    }
}
