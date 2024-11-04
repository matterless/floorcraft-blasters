using UnityEngine;
using UnityEditor;
using UEditor = UnityEditor.Editor;
using System.IO;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Matterless.Floorcraft.Editor
{
    [CustomEditor(typeof(BuildConfiguration))]
    public class BuildConfigurationEditor : UEditor
    {
        const string VERSIONING_CLASS = "namespace Matterless.MatterlessApp { public static class Versioning { public const string version = \"[version]\";}}";
        const string VERSIONING_CLASS_PATH = "_matterless/Scripts/Runtime/Versioning.cs";

        public override void OnInspectorGUI()
        {
            GUILayout.Label("Builds");

            if (GUILayout.Button("Set App in Editor"))
                SetAppEditorSettings((BuildConfiguration)target);

            if (GUILayout.Button("Build XCode Project"))
                Build((BuildConfiguration)target, BuildOptions.None);

            if (GUILayout.Button("Build XCode Project (Dev Build)"))
                Build((BuildConfiguration)target, BuildOptions.Development);

            GUILayout.Space(10);

            base.OnInspectorGUI();
        }

        private static void SetAppEditorSettings(BuildConfiguration config)
        {
            Debug.LogFormat("Set application: {0}", config.appName);

            // product name
            PlayerSettings.productName = config.appName;
            // application identifier
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, config.appIdentifier);
            // version
            PlayerSettings.bundleVersion = config.appVersion;
            PlayerSettings.iOS.buildNumber = config.buildNumber.ToString();
            // full version
            //File.WriteAllText(Path.Combine(Application.dataPath, VERSIONING_CLASS_PATH), VERSIONING_CLASS.Replace("[version]", config.fullVersion));
            // scenes
            EditorBuildSettings.scenes = config.scenes.ToArray();
            // symbols
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.iOS, config.defines);

            AssetDatabase.Refresh();

            Debug.Log("***********************");
            Debug.Log($"   VER: {config.fullVersion}");
            Debug.Log("***********************");
        }

        // This function is used both from the manual button press, and from continuous integration builds (see BuilderForCI.cs)
        internal static BuildReport Build(BuildConfiguration config, BuildOptions buildOptions, bool interactive = true)
        {
            BuildReport buildReport = null;

            SetAppEditorSettings(config);

            string path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Builds");

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            path = Path.Combine(path, config.appBuildFolder);

            if (buildOptions == BuildOptions.Development)
                path += "_dev";

            if (!interactive || EditorUtility.DisplayDialog("Build Xcode Project", $"Build Xcode project at:\n\"{path}\" ?", "Build", "Nope!"))
            {
                //config.IncreaseBuildNumber();
                SetAppEditorSettings(config);

                // Build player
                buildReport = BuildPipeline.BuildPlayer(config.scenePathArray, path, BuildTarget.iOS, buildOptions);

                if (interactive)
                {
                    // show path
                    EditorUtility.RevealInFinder(path);
                }
            }
            else
            {
                Debug.Log("Build Canceled");
            }

            return buildReport;
        }
    }
}