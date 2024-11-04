using Matterless.Localisation;
using UnityEngine;

namespace Matterless.Floorcraft
{
    [System.Serializable]
    [CreateAssetMenu(menuName = "Matterless/Notification Settings")]
    public class NotificationSettings : ScriptableObject
    {
        [SerializeField] private NotificationData[] m_NotificationDatas;

        public void Localise(ILocalisationService localisationService)
        {
            foreach (var nd in m_NotificationDatas)
                    nd.Localise(localisationService);
        }

        public NotificationData GetNotificationData(NotificationType type)
        {
            foreach (var nd in m_NotificationDatas)
            {
                if (nd.type == type)
                {
                    return nd;
                }
            }

            return m_NotificationDatas[0];
        }
    }
}