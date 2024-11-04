#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace Matterless.Editor
{
    public class DisablingBitcodeiOS
    {
        private const string ENABLE_BITCODE_KEY = "ENABLE_BITCODE";
        
        [PostProcessBuild(1000)]
        public static void PostProcessBuildAttribute(BuildTarget target, string pathToBuildProject)
        {
            if (target == BuildTarget.iOS)
            {
                string projectPath = PBXProject.GetPBXProjectPath(pathToBuildProject);

                PBXProject pbxProject = new PBXProject();
                pbxProject.ReadFromFile(projectPath);

                var targetGuid = pbxProject.GetUnityMainTargetGuid();

                pbxProject.SetBuildProperty(targetGuid, ENABLE_BITCODE_KEY, "NO");
                pbxProject.WriteToFile(projectPath);

                var projectInString = File.ReadAllText(projectPath);

                projectInString = projectInString.Replace($"{ENABLE_BITCODE_KEY} = YES;",
                    $"{ENABLE_BITCODE_KEY} = NO;");
                File.WriteAllText(projectPath, projectInString);
            }
        }
    }
}
#endif