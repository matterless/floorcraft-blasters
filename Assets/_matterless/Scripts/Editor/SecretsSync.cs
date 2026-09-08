using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Matterless.Floorcraft.Editor
{
    /// <summary>
    /// Generates Assets/_matterless/Resources/AppSecrets.asset from the project's .env file.
    /// Process environment variables with the same names take precedence, which is what CI uses.
    /// Runs when scripts reload, before every build, and from the Matterless > Secrets menu.
    /// The build tooling reads Android identity and signing values through <see cref="ReadValues"/>.
    /// See Docs/Secrets.md.
    /// </summary>
    [InitializeOnLoad]
    public class SecretsSync : IPreprocessBuildWithReport
    {
        private const string AssetPath = "Assets/_matterless/Resources/AppSecrets.asset";
        private const string EnvFileName = ".env";

        // Environment variable name -> serialized field on AppSecrets. Required ones are warned about when empty.
        private static readonly (string env, string field, bool required)[] s_Keys =
        {
            ("AUKI_APP_KEY", "m_AukiAppKey", true),
            ("AUKI_APP_SECRET", "m_AukiAppSecret", true),
            ("AMPLITUDE_API_KEY", "m_AmplitudeApiKey", true),
            ("REOWN_PROJECT_ID", "m_ReownProjectId", true),
            ("ALCHEMY_API_KEY", "m_AlchemyApiKey", true),
            ("AUKI_DOMAIN_ID", "m_AukiDomainId", false),
            ("AUKI_EDITOR_SESSION_ID", "m_AukiEditorSessionId", false),
        };

        public int callbackOrder => -100;

        static SecretsSync()
        {
            EditorApplication.delayCall += () => Sync(verbose: false);
        }

        public void OnPreprocessBuild(BuildReport report) => Sync(verbose: true);

        [MenuItem("Matterless/Secrets/Sync from .env")]
        public static void SyncFromMenu() => Sync(verbose: true);

        private static string EnvPath => Path.GetFullPath(Path.Combine(Application.dataPath, "..", EnvFileName));

        /// <summary>All values from .env, with process environment variables overriding the file.</summary>
        internal static Dictionary<string, string> ReadValues()
        {
            var values = ReadEnvFile(EnvPath);
            foreach (string name in Environment.GetEnvironmentVariables().Keys)
            {
                var fromProcess = Environment.GetEnvironmentVariable(name);
                if (!string.IsNullOrEmpty(fromProcess))
                {
                    values[name] = fromProcess;
                }
            }
            return values;
        }

        /// <summary>One value from .env or the process environment, or null when unset or empty.</summary>
        internal static string Get(Dictionary<string, string> values, string name)
        {
            return values.TryGetValue(name, out var v) && !string.IsNullOrEmpty(v) ? v : null;
        }

        private static void Sync(bool verbose)
        {
            var values = ReadValues();

            var anyValue = false;
            foreach (var (env, _, _) in s_Keys)
            {
                anyValue |= Get(values, env) != null;
            }

            var asset = AssetDatabase.LoadAssetAtPath<AppSecrets>(AssetPath);
            if (!anyValue)
            {
                if (asset == null)
                {
                    Debug.LogWarning($"[Secrets] No {EnvFileName} at {EnvPath} and no environment variables set, so " +
                                     $"{AssetPath} was not generated. Copy .env.example to .env and fill it in. See Docs/Secrets.md.");
                }
                return;
            }

            if (asset == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(AssetPath));
                asset = ScriptableObject.CreateInstance<AppSecrets>();
                AssetDatabase.CreateAsset(asset, AssetPath);
            }

            var serialized = new SerializedObject(asset);
            var changed = false;
            var missing = new List<string>();
            foreach (var (env, field, required) in s_Keys)
            {
                var value = Get(values, env) ?? string.Empty;
                if (required && value.Length == 0)
                {
                    missing.Add(env);
                }
                var property = serialized.FindProperty(field);
                if (property.stringValue != value)
                {
                    property.stringValue = value;
                    changed = true;
                }
            }

            if (changed)
            {
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssetIfDirty(asset);
                Debug.Log($"[Secrets] {AssetPath} updated from {EnvFileName}.");
            }

            if (verbose && missing.Count > 0)
            {
                Debug.LogWarning($"[Secrets] Missing values for: {string.Join(", ", missing)}. The related features will be disabled.");
            }
        }

        /// <summary>Parses KEY=VALUE lines. Supports comments, blank lines, "export KEY=", and quoted values.</summary>
        private static Dictionary<string, string> ReadEnvFile(string path)
        {
            var result = new Dictionary<string, string>();
            if (!File.Exists(path))
            {
                return result;
            }

            foreach (var raw in File.ReadAllLines(path))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#"))
                {
                    continue;
                }
                if (line.StartsWith("export "))
                {
                    line = line.Substring("export ".Length).TrimStart();
                }
                var eq = line.IndexOf('=');
                if (eq <= 0)
                {
                    continue;
                }
                var key = line.Substring(0, eq).Trim();
                var value = line.Substring(eq + 1).Trim();
                if (value.Length >= 2 && (value[0] == '"' || value[0] == '\'') && value[value.Length - 1] == value[0])
                {
                    value = value.Substring(1, value.Length - 2);
                }
                result[key] = value;
            }
            return result;
        }
    }
}
