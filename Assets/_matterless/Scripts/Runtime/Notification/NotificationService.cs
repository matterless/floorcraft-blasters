using Auki.ConjureKit;
using Matterless.Inject;
using Matterless.Localisation;
using UnityEngine;

namespace Matterless.Floorcraft
{
    public class NotificationService : INotificationService
#if UNITY_EDITOR
        , ITickable
#endif
    {
        private NotificationView m_View;
        private readonly IAukiWrapper m_AukiWrapper;
        private readonly IDomainService m_DomainService;
        private readonly NotificationSettings m_NotificationSettings;
        private uint m_CacheParticipantId = 0;

        public NotificationService(
            IAukiWrapper aukiWrapper,
            IDomainService domainService,
            ILocalisationService localisationService,
            NotificationSettings notificationSettings)
        {
            m_AukiWrapper = aukiWrapper;
            m_DomainService = domainService;
            m_NotificationSettings = notificationSettings;
            m_NotificationSettings.Localise(localisationService);
            m_View = NotificationView.Create("UIPrefabs/UIP_NotificationView").Init();

            m_AukiWrapper.onJoined += OnJoinedRoom;
            m_AukiWrapper.onParticipantJoined += OnParticipantJoined;
            m_DomainService.onLightHouseAssign += ShowLightHouseAssign;
            m_DomainService.onLightHouseScanFail += ShowLightHouseScanFail;
        }

        private void ShowLightHouseAssign()
        {
            ShowMessage(NotificationType.StaticLighthouseAssign);
        }
        private void ShowLightHouseScanFail()
        {
            ShowMessage(NotificationType.StaticLighthouseFail);
        }

        private void OnJoinedRoom(Session session)
        {

            Debug.Log("new session join " + session.Id);
            m_AukiWrapper.onParticipantLeft += OnParticipantLeft;
            m_CacheParticipantId = m_AukiWrapper.GetSession().ParticipantId;
            if (m_AukiWrapper.GetSession().GetParticipants().Count != 1)
            {
                ShowMessage(NotificationType.OnJoinedRoom);
            }
        }

        private void OnParticipantJoined(Participant participant)
        {
            ShowMessage(NotificationType.OnParticipantJoined);
        }

        private void OnParticipantLeft(uint participantId)
        {
            if (participantId != m_CacheParticipantId)
            {
                ShowMessage(NotificationType.OnParticipantLeft);
            }
            else
            {
                StopMessage();
                m_AukiWrapper.onParticipantLeft -= OnParticipantLeft;
            }
        }

        public void Destroy()
        {
            ShowMessage(NotificationType.Destroy);
        }

        public void Destroyed()
        {
            ShowMessage(NotificationType.Destroyed);
        }

        public void ShowMessage(NotificationType messageType)
        {
            Debug.Log("show message " + messageType);
            NotificationData data = m_NotificationSettings.GetNotificationData(messageType);
            m_View.ShowMessage(m_NotificationSettings.GetNotificationData(messageType));
        }
        private void StopMessage()
        {
            m_View.StopMessage();
        }

#if UNITY_EDITOR
        public void Tick(float deltaTime, float unscaledDeltaTime)
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                ShowMessage(NotificationType.OnJoinedRoom);
            }

            if (Input.GetKeyDown(KeyCode.B))
            {
                ShowMessage(NotificationType.Destroy);
            }

            if (Input.GetKeyDown(KeyCode.C))
            {
                ShowMessage(NotificationType.StaticLighthouseAssign);
            }
        }
#endif
    }
}