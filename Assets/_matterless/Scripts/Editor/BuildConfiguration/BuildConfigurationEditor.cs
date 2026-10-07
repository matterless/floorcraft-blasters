using System;
using System.Collections.Generic;
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
        // Android store identity and signing come from .env (or the process environment), so the
        // tracked Player Settings keep neutral values. See Docs/Secrets.md.
        private const string EnvPackageName = "ANDROID_PACKAGE_NAME";
        private const string EnvKeystorePath = "ANDROID_KEYSTORE_PATH";
        private const string EnvKeystoreAlias = "ANDROID_KEYSTORE_ALIAS";
        private const string EnvKeystorePass = "ANDROID_KEYSTORE_PASS";
        private const string EnvKeyaliasPass = "ANDROID_KEYALIAS_PASS";

        public override void OnInspectorGUI()
        {
            var config = (BuildConfiguration)target;
            string what = BuildLabel(config);

            GUILayout.Label("Builds");

            if (GUILayout.Button("Set App in Editor"))
                SetAppEditorSettings(config);

            if (GUILayout.Button($"Build {what}"))
                Build(config, BuildOptions.None);

            if (GUILayout.Button($"Build {what} (Dev Build)"))
                Build(config, BuildOptions.Development);

            GUILayout.Space(6);

            string releasePath = OutputPath(config, BuildOptions.None);
            string devPath = OutputPath(config, BuildOptions.Development);
            string existing = Exists(releasePath) ? releasePath : Exists(devPath) ? devPath : null;
            EditorGUILayout.HelpBox(existing != null
                ? $"Last build: {existing}"
                : $"No build yet. Output: {releasePath}", MessageType.None);
            if (GUILayout.Button(existing != null ? "Reveal Build in Finder" : "Open Builds Folder"))
                EditorUtility.RevealInFinder(existing ?? BuildsFolder());

            GUILayout.Space(10);

            base.OnInspectorGUI();
        }

        private static string BuildsFolder() =>
            Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Builds");

        /// <summary>Where a build of this config ends up: an Xcode project folder for iOS, a single file for Android.</summary>
        private static string OutputPath(BuildConfiguration config, BuildOptions buildOptions)
        {
            string path = Path.Combine(BuildsFolder(), config.appBuildFolder);

            if (buildOptions == BuildOptions.Development)
                path += "_dev";

            if (TargetOf(config) == BuildTarget.Android)
                path += config.androidExtension;

            return path;
        }

        private static bool Exists(string path) => File.Exists(path) || Directory.Exists(path);

        private static string BuildLabel(BuildConfiguration config)
        {
            if (config.platform == BuildConfiguration.Platform.Android)
                return config.androidAppBundle ? "Android App Bundle" : "Android APK";
            return "Xcode Project";
        }

        private static BuildTarget TargetOf(BuildConfiguration config) =>
            config.platform == BuildConfiguration.Platform.Android ? BuildTarget.Android : BuildTarget.iOS;

        private static NamedBuildTarget NamedTargetOf(BuildConfiguration config) =>
            config.platform == BuildConfiguration.Platform.Android ? NamedBuildTarget.Android : NamedBuildTarget.iOS;

        private static void SetAppEditorSettings(BuildConfiguration config)
        {
            Debug.LogFormat("Set application: {0} ({1})", config.appName, config.platform);
            var namedTarget = NamedTargetOf(config);

            // product name
            PlayerSettings.productName = config.appName;
            // application identifier
            PlayerSettings.SetApplicationIdentifier(namedTarget, config.appIdentifier);
            // version
            PlayerSettings.bundleVersion = config.appVersion;
            if (config.platform == BuildConfiguration.Platform.Android)
                PlayerSettings.Android.bundleVersionCode = config.buildNumber;
            else
                PlayerSettings.iOS.buildNumber = config.buildNumber.ToString();
            // scenes
            EditorBuildSettings.scenes = config.scenes.ToArray();
            // symbols
            PlayerSettings.SetScriptingDefineSymbols(namedTarget, config.defines);

            AssetDatabase.Refresh();

            Debug.Log("***********************");
            Debug.Log($"   VER: {config.fullVersion}");
            Debug.Log("***********************");
        }

        // This function is used both from the manual button press, and from continuous integration builds (see BuilderForCI.cs)
        internal static BuildReport Build(BuildConfiguration config, BuildOptions buildOptions, bool interactive = true)
        {
            BuildReport buildReport = null;

            // Taken before SetAppEditorSettings writes the config's version code, so an Android
            // build can put the tracked Player Settings back afterwards.
            var neutral = AndroidIdentity.Capture();
            SetAppEditorSettings(config);

            var target = TargetOf(config);
            string what = BuildLabel(config);

            if (!Directory.Exists(BuildsFolder()))
                Directory.CreateDirectory(BuildsFolder());

            string path = OutputPath(config, buildOptions);

            if (!interactive || EditorUtility.DisplayDialog($"Build {what}", $"Build {what} at:\n\"{path}\" ?", "Build", "Nope!"))
            {
                //config.IncreaseBuildNumber();
                SetAppEditorSettings(config);

                if (target == BuildTarget.Android)
                {
                    // Apply the store identity for the duration of the build only, so the tracked
                    // Player Settings keep their neutral values afterwards.
                    try
                    {
                        PrepareAndroid(config);
                        buildReport = BuildPipeline.BuildPlayer(config.scenePathArray, path, target, buildOptions);
                    }
                    finally
                    {
                        neutral.Restore();
                    }
                }
                else
                {
                    buildReport = BuildPipeline.BuildPlayer(config.scenePathArray, path, target, buildOptions);
                }

                if (interactive)
                {
                    // show path
                    EditorUtility.RevealInFinder(path);
                }
            }
            else
            {
                if (target == BuildTarget.Android)
                    neutral.Restore();
                Debug.Log("Build Canceled");
            }

            return buildReport;
        }

        /// <summary>
        /// Android specifics: app bundle toggle, native symbols, package name and release signing from .env.
        /// Keystore passwords may also be typed into Player Settings for the editor session.
        /// Configs with debug signing enabled skip the keystore entirely.
        /// </summary>
        private static void PrepareAndroid(BuildConfiguration config)
        {
            EditorUserBuildSettings.buildAppBundle = config.androidAppBundle;

            // Store builds write a .symbols.zip next to the bundle; upload it with the release so
            // Play can symbolicate native crashes and ANRs. Debug-signed test builds skip it.
            EditorUserBuildSettings.androidCreateSymbols = config.androidDebugSigning
                ? AndroidCreateSymbols.Disabled
                : AndroidCreateSymbols.Public;

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                Debug.LogWarning("[Build] Active platform is not Android; Unity switches platform as part of this build, which takes a while.");

            var env = SecretsSync.ReadValues();

            string packageName = SecretsSync.Get(env, EnvPackageName) ?? config.appIdentifier;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, packageName);
            Debug.Log($"[Build] Android package name: {packageName}" +
                      (SecretsSync.Get(env, EnvPackageName) != null ? $" (from {EnvPackageName})" : " (from the build config)"));

            if (config.androidDebugSigning)
            {
                PlayerSettings.Android.useCustomKeystore = false;
                Debug.Log("[Build] Android build is signed with the debug keystore (test build).");
                return;
            }

            string keystorePath = SecretsSync.Get(env, EnvKeystorePath);
            if (keystorePath != null)
            {
                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = keystorePath;
                PlayerSettings.Android.keyaliasName = SecretsSync.Get(env, EnvKeystoreAlias) ?? PlayerSettings.Android.keyaliasName;
            }

            if (!PlayerSettings.Android.useCustomKeystore)
            {
                Debug.LogWarning($"[Build] Android build uses the debug keystore. Google Play rejects it; set {EnvKeystorePath} and {EnvKeystoreAlias} in .env.");
                return;
            }

            string keystorePass = SecretsSync.Get(env, EnvKeystorePass);
            string keyaliasPass = SecretsSync.Get(env, EnvKeyaliasPass);
            if (keystorePass != null)
                PlayerSettings.Android.keystorePass = keystorePass;
            if (keyaliasPass != null)
                PlayerSettings.Android.keyaliasPass = keyaliasPass;

            if (string.IsNullOrEmpty(PlayerSettings.Android.keystorePass) || string.IsNullOrEmpty(PlayerSettings.Android.keyaliasPass))
                Debug.LogWarning($"[Build] Keystore passwords are not set. Enter them in Player Settings > Publishing Settings for this session, or set {EnvKeystorePass} and {EnvKeyaliasPass} in .env.");
        }

        /// <summary>The Android Player Settings that a store build overrides, so they can be put back.</summary>
        private struct AndroidIdentity
        {
            private string m_Identifier;
            private bool m_UseCustomKeystore;
            private string m_KeystoreName;
            private string m_KeyaliasName;
            private int m_VersionCode;

            /// <summary>Call before SetAppEditorSettings, which writes the config's version code.</summary>
            public static AndroidIdentity Capture() => new AndroidIdentity
            {
                m_Identifier = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android),
                m_UseCustomKeystore = PlayerSettings.Android.useCustomKeystore,
                m_KeystoreName = PlayerSettings.Android.keystoreName,
                m_KeyaliasName = PlayerSettings.Android.keyaliasName,
                m_VersionCode = PlayerSettings.Android.bundleVersionCode,
            };

            public void Restore()
            {
                // Only write back what the build changed; assigning an unchanged empty keystore
                // name still dirties ProjectSettings.asset.
                if (PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android) != m_Identifier)
                    PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, m_Identifier);
                if (PlayerSettings.Android.useCustomKeystore != m_UseCustomKeystore)
                    PlayerSettings.Android.useCustomKeystore = m_UseCustomKeystore;
                if (PlayerSettings.Android.keystoreName != m_KeystoreName)
                    PlayerSettings.Android.keystoreName = m_KeystoreName;
                if (PlayerSettings.Android.keyaliasName != m_KeyaliasName)
                    PlayerSettings.Android.keyaliasName = m_KeyaliasName;
                // store version codes are Auki-internal; keep them out of ProjectSettings.asset
                if (PlayerSettings.Android.bundleVersionCode != m_VersionCode)
                    PlayerSettings.Android.bundleVersionCode = m_VersionCode;
                AssetDatabase.SaveAssets();
            }
        }
    }
}
