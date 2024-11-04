namespace Matterless.Floorcraft
{
    public interface IPlayerPrefsService
    {
        bool HasKey(string key);
        void DeleteKey(string key);
        void DeleteAll();
        bool GetBool(string key, bool defaultValue);
        int GetInt(string key, int defaultValue);
        float GetFloat(string key, float defaultValue);
        string GetString(string key, string defaultValue);
        void SetBool(string key, bool value);
        void SetInt(string key, int value);
        void SetFloat(string key, float value);
        void SetString(string key, string value);
    }
}