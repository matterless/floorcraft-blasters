using UnityEngine;

namespace Matterless.Floorcraft
{
    public class PlayerPrefsService : IPlayerPrefsService
    {
        public PlayerPrefsService()
        {
        }

        public bool HasKey(string key) => PlayerPrefs.HasKey(key);

        public void DeleteKey(string key) => PlayerPrefs.DeleteKey(key);
        public void DeleteAll() => PlayerPrefs.DeleteAll();
        
        public bool GetBool(string key, bool defaultValue)
        {
            if (!PlayerPrefs.HasKey(key))
                return defaultValue;

            return PlayerPrefs.GetInt(key) == 1;
        }
        
        public int GetInt(string key, int defaultValue)
            => PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key) : defaultValue;
        
        public float GetFloat(string key, float defaultValue)
            => PlayerPrefs.HasKey(key) ? PlayerPrefs.GetFloat(key) : defaultValue;
        
        public string GetString(string key, string defaultValue)
            => PlayerPrefs.HasKey(key) ? PlayerPrefs.GetString(key) : defaultValue;

        public void SetBool(string key, bool value) => PlayerPrefs.SetInt(key, value ? 1 : 0);
        public void SetInt(string key, int value) => PlayerPrefs.SetInt(key, value);
        public void SetFloat(string key, float value) => PlayerPrefs.SetFloat(key, value);
        public void SetString(string key, string value) => PlayerPrefs.SetString(key, value);
    }
}