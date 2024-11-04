using System.Collections.Generic;

namespace Matterless.Floorcraft
{
    public class SettingsModel
    {
        public void Clone(SettingsModel data)
        {
            language = data.language;
            masterVolume = data.masterVolume;
            musicVolume = data.musicVolume;
            sfxVolume = data.sfxVolume;
        }
        
        public Language language;
        public float masterVolume;
        public float musicVolume;
        public float sfxVolume;
    }
}