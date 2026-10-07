using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Matterless.Floorcraft.Editor
{
    /// <summary>
    /// Stops Android builds that would miss Java classes the game looks up by name at runtime.
    /// The Android Resolver (External Dependency Manager) downloads those libraries into the gitignored
    /// Assets/Plugins/Android, so when it deletes them the build still succeeds and the app crashes at
    /// startup (Amplitude.Instance cannot find its Java plugin class). Before each Android build this
    /// checks for the classes, forces a resolve once if one is missing, and fails the build if it still is.
    /// The resolver's automatic resolve is off (ProjectSettings/GvhProjectSettings.xml): it deleted the
    /// libraries before every attempt, and the attempts failed for lack of a usable Java (see UseUnityJdk).
    /// </summary>
    [InitializeOnLoad]
    public class AndroidDependencyCheck : IPreprocessBuildWithReport
    {
        private const string PluginsFolder = "Assets/Plugins/Android";

        // Java class the C# side needs -> the dependency that provides it
        private static readonly (string classFile, string dependency)[] s_RequiredClasses =
        {
            ("com/amplitude/unity/plugins/AmplitudePlugin.class", "com.amplitude:android-sdk"),
        };

        public int callbackOrder => -50;

        static AndroidDependencyCheck()
        {
            UseUnityJdk();
        }

        // The resolver runs Gradle with JAVA_HOME. Its lookup of Unity's bundled JDK predates Unity 2022's
        // layout, so an editor started from Unity Hub finds no Java at all, and a shell's newer JDK (17)
        // is too new for its Gradle. Point this editor process at Unity's own JDK instead.
        private static void UseUnityJdk()
        {
            string jdk;
            try
            {
                jdk = Path.Combine(BuildPipeline.GetPlaybackEngineDirectory(BuildTarget.Android, BuildOptions.None), "OpenJDK");
            }
            catch (Exception)
            {
                return; // Android support is not installed
            }

            if (File.Exists(Path.Combine(jdk, "bin", "java")) || File.Exists(Path.Combine(jdk, "bin", "java.exe")))
                Environment.SetEnvironmentVariable("JAVA_HOME", jdk);
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.Android)
                return;

            var missing = FindMissing();
            if (missing.Length == 0)
                return;

            Debug.LogWarning($"[AndroidDependencyCheck] {string.Join(", ", missing)} missing from {PluginsFolder}; running the Android Resolver");
            ForceResolve();

            missing = FindMissing();
            if (missing.Length > 0)
            {
                throw new BuildFailedException(
                    $"[AndroidDependencyCheck] {string.Join(", ", missing)} missing from {PluginsFolder}. " +
                    "Run Assets > External Dependency Manager > Android Resolver > Force Resolve, then build again.");
            }
        }

        private static string[] FindMissing()
        {
            var libraries = Directory.Exists(PluginsFolder)
                ? Directory.GetFiles(PluginsFolder).Where(IsLibrary).ToArray()
                : Array.Empty<string>();

            return s_RequiredClasses
                .Where(required => !libraries.Any(library => ContainsClass(library, required.classFile)))
                .Select(required => required.dependency)
                .ToArray();
        }

        private static bool IsLibrary(string path) =>
            path.EndsWith(".aar", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".jar", StringComparison.OrdinalIgnoreCase);

        private static bool ContainsClass(string libraryPath, string classFile)
        {
            using (var archive = ZipFile.OpenRead(libraryPath))
                return ContainsClass(archive, classFile);
        }

        // a .jar holds classes directly; an .aar holds them in classes.jar and libs/*.jar
        private static bool ContainsClass(ZipArchive archive, string classFile)
        {
            if (archive.GetEntry(classFile) != null)
                return true;

            foreach (var entry in archive.Entries.Where(e => IsLibrary(e.FullName)))
            {
                using (var buffer = new MemoryStream())
                {
                    using (var stream = entry.Open())
                        stream.CopyTo(buffer);
                    buffer.Position = 0;
                    using (var inner = new ZipArchive(buffer, ZipArchiveMode.Read))
                    {
                        if (inner.GetEntry(classFile) != null)
                            return true;
                    }
                }
            }
            return false;
        }

        // The resolver is a precompiled DLL that its version handler loads at runtime, so it is reached by reflection.
        private static void ForceResolve()
        {
            var resolver = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("GooglePlayServices.PlayServicesResolver"))
                .FirstOrDefault(type => type != null);
            var resolveSync = resolver?.GetMethod("ResolveSync", BindingFlags.Public | BindingFlags.Static,
                null, new[] { typeof(bool) }, null);

            if (resolveSync == null)
            {
                Debug.LogWarning("[AndroidDependencyCheck] Android Resolver not found; cannot resolve automatically");
                return;
            }

            var succeeded = (bool)resolveSync.Invoke(null, new object[] { true });
            if (!succeeded)
                Debug.LogWarning("[AndroidDependencyCheck] Android Resolver reported a failure");
        }
    }
}
