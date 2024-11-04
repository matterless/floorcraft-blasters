using Newtonsoft.Json;
using System.IO;
using UnityEngine;

namespace Matterless.Floorcraft
{
    [System.Serializable]
    public class AutoConnectionConfigs
    {
        const string CONFIG_NAME = "autoconnectioncofig.json";

        public bool isHost { get; set; }
        public string hostPathConfig { get; set; }
        public string currentSessionId { get; set; }

        public static bool hasConfig
            => File.Exists(GetMyConfigPath());

        public static AutoConnectionConfigs GetMyConfig()
            => JsonConvert.DeserializeObject<AutoConnectionConfigs>(File.ReadAllText(GetMyConfigPath()));
        public static AutoConnectionConfigs GetConfig(string path)
        {
            return JsonConvert.DeserializeObject<AutoConnectionConfigs>(File.ReadAllText(path));
        }

        public void Save(string sessionId)
        {
            currentSessionId = sessionId;
            File.WriteAllText(GetMyConfigPath(), JsonConvert.SerializeObject(this));
        }

        private static string GetMyConfigPath()
        {
            var path = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(path, CONFIG_NAME);
        }
    }
}