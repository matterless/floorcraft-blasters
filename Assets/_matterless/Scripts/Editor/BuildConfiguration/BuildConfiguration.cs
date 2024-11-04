using System.Collections.Generic;
using UnityEngine;

namespace Matterless.Floorcraft.Editor
{
    [CreateAssetMenu(menuName = "Matterless/Build Config")]
    public class BuildConfiguration : ScriptableObject
    {
        #region Inspector
        [Header("App Settings")]
        [SerializeField] private string m_AppName;
        [SerializeField] private string m_AppVersion;
        [SerializeField] private int m_BuildNumber;
        [SerializeField] private string m_AppIdentifier;
        [SerializeField] private Object[] m_Scenes;
        [SerializeField] private string[] m_Defines;
        [Header("Output")] 
        [SerializeField] private string m_OutputFolder;
        [SerializeField] private string m_OutputFolderPostfix;
        #endregion

        public string appName => m_AppName;
        public string appIdentifier => m_AppIdentifier;
        public string appVersion => m_AppVersion;
        public int buildNumber => m_BuildNumber;
        public string fullVersion => $"{m_AppVersion}b{m_BuildNumber}{m_OutputFolderPostfix}";
        public string appBuildFolder => $"{m_OutputFolder}-{fullVersion}"; 
        public string[] defines => m_Defines;

        public void IncreaseBuildNumber() => m_BuildNumber++;
        
        
#if UNITY_EDITOR
        public List<UnityEditor.EditorBuildSettingsScene> scenes
        {
            get
            {
                List<UnityEditor.EditorBuildSettingsScene> scenesList = new List<UnityEditor.EditorBuildSettingsScene>();
                foreach (var sceneObject in m_Scenes)
                {
                    string pathToScene = UnityEditor.AssetDatabase.GetAssetPath(sceneObject);
                    Debug.Log(pathToScene);
                    scenesList.Add( new UnityEditor.EditorBuildSettingsScene(pathToScene, true));
                }
                return scenesList;
            }
        }

        public string[] scenePathArray
        {
            get
            {
                List<string> scenesList = new List<string>();
                foreach (var sceneObject in m_Scenes)
                {
                    string pathToScene = UnityEditor.AssetDatabase.GetAssetPath(sceneObject);
                    Debug.Log(pathToScene);
                    scenesList.Add(pathToScene);
                }
                return scenesList.ToArray();
            }
        }
#endif
    }
}