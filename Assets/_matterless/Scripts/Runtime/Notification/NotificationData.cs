using Matterless.Localisation;
using UnityEngine;

namespace Matterless.Floorcraft
{
    [System.Serializable]
    public class NotificationData
    {
        [SerializeField] private NotificationType m_Type;
        [SerializeField] private string m_Text;
        [SerializeField] private Sprite m_Icon;

        public string text { get; private set; }
        public Sprite icon => m_Icon;
        public NotificationType type => m_Type;

        public void Localise(ILocalisationService localisationService)
        {
            localisationService.onLanguageChanged += () =>
            {
                text = localisationService.Translate(m_Text);
            };

            text = localisationService.Translate(m_Text);
        }
    }
}