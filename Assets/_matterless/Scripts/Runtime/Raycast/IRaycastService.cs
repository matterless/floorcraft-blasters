using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace Matterless.Floorcraft
{
    public interface IRaycastService
    {
        public Camera camera { get; }
        public bool hasHit { get; }
        // public ARRaycastHit hit { get; }
        public Pose hitPose { get; }
        public float distance { get; }

        //bool CeliRaycast(Vector3 position, out Vector3 hitPoint, out Vector3 hitNormal);
        bool FloorRaycast(Vector3 position, out Vector3 floorPosition, out Vector3 floorNormal);
        void Tick(float deltaTime, float unscaledDeltaTime);
    }
}