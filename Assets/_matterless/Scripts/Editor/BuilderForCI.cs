using UnityEditor;
using System;
using UnityEditor.Build.Reporting;
using System.IO;
using System.Collections.Generic;

// This script is based on the build function in unity-builder, https://github.com/game-ci/unity-builder/blob/main/dist/default-build-script/Assets/Editor/UnityBuilderAction/Builder.cs
// But we modify it to use our BuildConfig system to keep it as similar as possible with manually building in unity.
namespace Matterless.Floorcraft.Editor
{
    // This static function is called when building from the continuous integration pipeline on github.
    // The function name is specified in the configuration files for unity-builder.
    static class BuilderForCI
    {
        private const string BUILD_CONFIG_PATH = "Assets/_matterless/_BuildConfigs/{0}.asset";
        private const string DEFAULT_BUILD_CONFIG_ASSET = "BC_Floorcraft_Arena";

        public static void Build()
        {
            Dictionary<string, string> customParameters = ParseCustomParameters(); // Parse custom parameters from environment variable
            
            string buildConfigName = "";
            
            customParameters.TryGetValue("configName", out buildConfigName);
            
            if (string.IsNullOrEmpty(buildConfigName))
            {
                buildConfigName = DEFAULT_BUILD_CONFIG_ASSET;
                Console.WriteLine("Unity build for CI didn't specify any build config name. Using default: " + buildConfigName +
                                 ". To specify a config, set the environment variable 'FLOORCRAFT_BUILD_CONFIG_NAME'");
            }


            string configPath = string.Format(BUILD_CONFIG_PATH, buildConfigName);
            BuildConfiguration buildConfig = AssetDatabase.LoadAssetAtPath<BuildConfiguration>(configPath);
            
            if (buildConfig == null)
            {
                Console.WriteLine("Failed to build for CI: couldn't load build config with name " + buildConfigName + ". Make sure name is correct.");
                ExitWithResult(BuildResult.Failed);
                return;
            }

            // tools/play-release.sh passes the next free Play version code. It applies to this build
            // only: store version codes are Auki-internal and must not land in the tracked config.
            int? trackedBuildNumber = null;
            if (customParameters.TryGetValue("buildNumber", out string buildNumberArgument))
            {
                if (!int.TryParse(buildNumberArgument, out int buildNumber) || buildNumber <= 0)
                {
                    Console.WriteLine("Failed to build for CI: -buildNumber must be a positive integer, got '" + buildNumberArgument + "'.");
                    ExitWithResult(BuildResult.Failed);
                    return;
                }

                trackedBuildNumber = buildConfig.buildNumber;
                buildConfig.SetBuildNumber(buildNumber);
                Console.WriteLine("Building " + buildConfigName + " with build number " + buildNumber + ".");
            }

            // We could also support BuildOptions.Development but that's probably
            // not needed by CI since we won't debug and attach breakpoints etc there.
            // Dev builds will still use the define MATTERLESS_DEVELOP etc.
            BuildOptions buildOptions = BuildOptions.None;

            // Perform build
            BuildReport buildReport;
            try
            {
                buildReport = BuildConfigurationEditor.Build(buildConfig, buildOptions, false);
            }
            finally
            {
                // never marked dirty, so the tracked asset keeps its own number
                if (trackedBuildNumber.HasValue)
                    buildConfig.SetBuildNumber(trackedBuildNumber.Value);
            }

            string outputPath = buildReport.summary.outputPath;
            // Read by tools/play-release.sh; Gradle may leave an unchanged bundle's timestamp as it was.
            Console.WriteLine("Build output: " + outputPath);
            bool isFolder = Directory.Exists(outputPath);   // Xcode project
            bool isFile = File.Exists(outputPath);          // Android .aab / .apk
            if (isFolder || isFile)
            {
                // Move build to correct folder as expected by CI. (to change the Build function as little as possible)
                string buildPathArgument = Environment.GetEnvironmentVariable("BUILD_PATH"); // Gets set by CI pipeline.
                string buildFileArgument = Environment.GetEnvironmentVariable("BUILD_FILE");

                if (!string.IsNullOrEmpty(buildPathArgument) && !string.IsNullOrEmpty(buildFileArgument))
                {
                    string outputFolderPath = Path.Combine(buildPathArgument, buildFileArgument);

                    Console.WriteLine("Moving build from '" + outputPath + "' to given BUILD_PATH " + buildPathArgument);
                    if (isFolder)
                        Directory.Move(outputPath, outputFolderPath);
                    else
                        File.Move(outputPath, outputFolderPath);
                }
            }

            // Summary
            BuildSummary summary = buildReport.summary;
            ReportSummary(summary);

            // Result
            BuildResult result = summary.result;
            ExitWithResult(result);
        }


        //------------------------------------------------
        //
        // All code below is copied from StdOutReporter.cs in unity-builder plugin
        // https://github.com/game-ci/unity-builder/blob/main/dist/default-build-script/Assets/Editor/UnityBuilderAction/Reporting/StdOutReporter.cs
        //

        static string EOL = Environment.NewLine;

        public static void ReportSummary(BuildSummary summary)
        {
            Console.WriteLine(
              $"{EOL}" +
              $"###########################{EOL}" +
              $"#      Build results      #{EOL}" +
              $"###########################{EOL}" +
              $"{EOL}" +
              $"Duration: {summary.totalTime.ToString()}{EOL}" +
              $"Warnings: {summary.totalWarnings.ToString()}{EOL}" +
              $"Errors: {summary.totalErrors.ToString()}{EOL}" +
              $"Size: {summary.totalSize.ToString()} bytes{EOL}" +
              $"{EOL}"
            );
        }

        public static void ExitWithResult(BuildResult result)
        {
            if (result == BuildResult.Succeeded)
            {
                Console.WriteLine("Build succeeded!");
                EditorApplication.Exit(0);
            }

            if (result == BuildResult.Failed)
            {
                Console.WriteLine("Build failed!");
                EditorApplication.Exit(101);
            }

            if (result == BuildResult.Cancelled)
            {
                Console.WriteLine("Build cancelled!");
                EditorApplication.Exit(102);
            }

            if (result == BuildResult.Unknown)
            {
                Console.WriteLine("Build result is unknown!");
                EditorApplication.Exit(103);
            }
        }

        public static Dictionary<string, string> ParseCustomParameters()
        {
            Dictionary<string, string> providedArguments = new Dictionary<string, string>();
            string parameters = Environment.GetEnvironmentVariable("CUSTOM_PARAMETERS");
            if (!string.IsNullOrEmpty(parameters)) {
                string[] args = parameters.Split(' ');
                for (int current = 0, next = 1; current < args.Length; current++, next++) {
                    // Parse flag
                    bool isFlag = args[current].StartsWith("-");
                    if (!isFlag) continue;
                    string flag = args[current].TrimStart('-');

                    // Parse optional value
                    bool flagHasValue = next < args.Length && !args[next].StartsWith("-");
                    string value = flagHasValue ? args[next].TrimStart('-') : "";

                    // Assign
                    Console.WriteLine($"Found flag \"{flag}\" with value {value}.");
                    providedArguments.Add(flag, value);
                }
            }
            return providedArguments;
        }
    }
}