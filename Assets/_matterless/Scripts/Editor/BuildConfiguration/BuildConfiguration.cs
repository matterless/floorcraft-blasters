using System.Collections.Generic;
using UnityEngine;

namespace Matterless.Floorcraft.Editor
{
    [CreateAssetMenu(menuName = "Matterless/Build Config")]
    public class BuildConfiguration : ScriptableObject
    {
        public enum Platform
        {
            iOS = 0,
            Android = 1,
        }

        #region Inspector
        [Header("Platform")]
        [SerializeField] private Platform m_Platform = Platform.iOS;
        [Tooltip("Android only: build a Google Play app bundle (.aab) instead of an .apk")]
        [SerializeField] private bool m_AndroidAppBundle = true;
        [Header("App Settings")]
        [SerializeField] private string m_AppName;
        [SerializeField] private string m_AppVersion;
        [Tooltip("iOS build number, or Android version code")]
        [SerializeField] private int m_BuildNumber;
        [SerializeField] private string m_AppIdentifier;
        [SerializeField] private Object[] m_Scenes;
        [SerializeField] private string[] m_Defines;
        [Header("Output")] 
        [SerializeField] private string m_OutputFolder;
        [SerializeField] private string m_OutputFolderPostfix;
        #endregion

        public Platform platform => m_Platform;
        public bool androidAppBundle => m_AndroidAppBundle;
        public string appName => m_AppName;
        public string appIdentifier => m_AppIdentifier;
        public string appVersion => m_AppVersion;
        public int buildNumber => m_BuildNumber;
        public string fullVersion => $"{m_AppVersion}b{m_BuildNumber}{m_OutputFolderPostfix}";
        /// <summary>Name under the Builds folder: a directory for the Xcode project, a file stem for Android.</summary>
        public string appBuildFolder => $"{m_OutputFolder}-{fullVersion}"; 
        public string androidExtension => m_AndroidAppBundle ? ".aab" : ".apk";
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
