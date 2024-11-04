using UnityEngine;

namespace Matterless.Floorcraft
{
    public interface IMarkerService
    {
        bool isHidden { get; }

        void Hide();
        void SetTarget(Transform target);
        void Show();
    }
}