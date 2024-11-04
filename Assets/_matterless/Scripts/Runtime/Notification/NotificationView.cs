using Matterless.Localisation;
using System.Collections.Generic;
using UnityEngine;

namespace Matterless.Floorcraft
{
    public class NotificationView : UIView<NotificationView>
    {
        [SerializeField] private NotificationBox m_NotificationBoxPrototype;

        private Stack<NotificationBox> m_NotificationBoxes = new Stack<NotificationBox>();
        private Stack<NotificationBox> m_CurrentAnimationBoxes = new Stack<NotificationBox>();
        public void ShowMessage(NotificationData data)
        {
            StopMessage();
            NotificationBox box = GetAvailableNotificationBox();
            box.Show(data.text, data.icon);
            m_CurrentAnimationBoxes.Push(box);
        }
        
        
        
        public void StopMessage()
        {
            foreach (var b in m_CurrentAnimationBoxes)
            {
                b.Pause();
            }
            m_CurrentAnimationBoxes.Clear();
        }

        public override NotificationView Init()
        {
            return this;
        }

        private NotificationBox GetAvailableNotificationBox()
        {
            NotificationBox box = null;

            if (m_NotificationBoxes.Count == 0)
            {
                box = Instantiate(m_NotificationBoxPrototype, transform);
                box.onAnimationFinished += OnNotificationBoxAnimationFinished;
            }
            else
            {
                box = m_NotificationBoxes.Pop();
            }

            return box;
        }

        private void OnNotificationBoxAnimationFinished(NotificationBox box)
        {
            m_NotificationBoxes.Push(box);
            if (m_CurrentAnimationBoxes.Count == 0)
                return;
            m_CurrentAnimationBoxes.Pop();

        }
    }
}