#if UNITY_IOS
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor;
using System.IO;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace Matterless.Editor
{
    // Will execute after XCode project is built
    public class ExemptFromEncryption : IPostprocessBuildWithReport 
    {
        private const string ITS_APP_USES_NON_EXEMPT_ENCRYPTION_KEY = "ITSAppUsesNonExemptEncryption";
        private const string INFO_PLIST_FILE = "Info.plist";

        public int callbackOrder { get { return 0; } }

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform == BuildTarget.iOS) // Check if the build is for iOS 
            {
                string plistPath = Path.Combine(report.summary.outputPath, INFO_PLIST_FILE);

                // Read Info.plist file into memory
                PlistDocument plist = new PlistDocument(); 
                plist.ReadFromString(File.ReadAllText(plistPath));

                // Set the key
                PlistElementDict rootDict = plist.root;
                Debug.Log($"Set {ITS_APP_USES_NON_EXEMPT_ENCRYPTION_KEY} = false");
                rootDict.SetBoolean(ITS_APP_USES_NON_EXEMPT_ENCRYPTION_KEY, false);

                // Override Info.plist
                File.WriteAllText(plistPath, plist.WriteToString()); 
            }
        }
    }
}
#endif