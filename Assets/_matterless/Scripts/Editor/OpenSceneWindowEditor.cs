using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Matterless.Floorcraft.Editor
{
    public class OpenSceneWindowEditor : EditorWindow
    {
        private const string UI_PREFABS_PATH = "/_matterless/Resources/UIPrefabs/";
        private const string ENTITIES_PREFAB_PATH = "/_matterless/Resources/NewEntities/";
        
        private static EditorWindow s_Window;

        private readonly List<string> m_UIPrefabs_paths = new List<string>();
        private readonly List<string> m_EntitiesPrefab_paths = new List<string>();
        private Vector2 m_ScrollPositionScenes;
        private Vector2 m_ScrollPositionUIPrefabs;
        private Vector2 m_ScrollPositionEntitiesPrefabs;

        [MenuItem("Matterless/Scenes & Prefabs...", false, 1)]
        static void ShowWindow()
        {
            s_Window = EditorWindow.GetWindow(typeof(OpenSceneWindowEditor));
            s_Window.titleContent = new GUIContent("Matterless Scenes");
            s_Window.Show();
        }

        void OnGUI()
        {
            ShowScenes();

            GUILayout.Label("Prefabs");
            var oldColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.green;

            if (GUILayout.Button("Refresh"))
            {
                RefreshPrefabPaths(UI_PREFABS_PATH, m_UIPrefabs_paths);
                RefreshPrefabPaths(ENTITIES_PREFAB_PATH, m_EntitiesPrefab_paths);
            }

            GUI.backgroundColor = oldColor;

            GUILayout.Space(10);
            GUILayout.Label("UI");
            m_ScrollPositionUIPrefabs = ShowPrefabs(UI_PREFABS_PATH, m_UIPrefabs_paths, m_ScrollPositionUIPrefabs);
            
            GUILayout.Space(10);
            GUILayout.Label("Entities");
            m_ScrollPositionEntitiesPrefabs = ShowPrefabs(ENTITIES_PREFAB_PATH, m_EntitiesPrefab_paths, m_ScrollPositionEntitiesPrefabs);
        }

        private void ShowScenes()
        {
            m_ScrollPositionScenes = GUILayout.BeginScrollView(m_ScrollPositionScenes, GUILayout.Height(100));

            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;

            for (int i = 0; i < scenes.Length; i++)
            {
                SceneAsset scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenes[i].path);

                if (GUILayout.Button(scene.name))
                {
                    EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                    EditorSceneManager.OpenScene(scenes[i].path, OpenSceneMode.Single);
                }
            }
            GUILayout.EndScrollView();
        }

        private Vector2 ShowPrefabs(string path, List<string> list, Vector2 scrollPosition)
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
            //GUILayout.Label ("", GUILayout.Width ( 380 ), GUILayout.Height ( 1500 ) );
            foreach (var name in list)
            {
                if (GUILayout.Button(name))
                {
                    OpenPrefab(path, name);
                }
            }
            GUILayout.EndScrollView();

            return scrollPosition;
        }

        private void RefreshPrefabPaths(string path, List<string> list)
        {
            list.Clear();
            var d = new DirectoryInfo(Application.dataPath+path);
            
            foreach (var file in d.GetFiles("*.prefab"))
            {
                list.Add(Path.GetFileNameWithoutExtension(file.Name));
            }
        }

        private void OpenPrefab(string path, string name)
            => AssetDatabase.OpenAsset(
                AssetDatabase.LoadAssetAtPath<GameObject>($"Assets{path}{name}.prefab"));
    }
}