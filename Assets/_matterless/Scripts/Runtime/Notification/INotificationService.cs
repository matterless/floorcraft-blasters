using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace  Matterless.Floorcraft
{
    public interface INotificationService
    {
        void ShowMessage(NotificationType messageType);
    }
}
