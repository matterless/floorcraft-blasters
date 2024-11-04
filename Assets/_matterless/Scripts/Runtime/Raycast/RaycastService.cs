using Matterless.Inject;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Vector3 = UnityEngine.Vector3;

namespace Matterless.Floorcraft
{
    public class RaycastService : IRaycastService, ITickable
    {
        private const float MAX_UPWARDS_FLOOR_SNAP = 0.2f;
        private const float MAX_DOWNWARDS_FLOOR_SNAP = 1.0f; // Keep high for now since we have no other good way of getting down (e.g table to floor)
        
        private readonly List<ARRaycastHit> m_Hits = new List<ARRaycastHit>();
        private readonly IAukiWrapper m_AukiWrapper;
        private readonly Settings m_Settings;

#if UNITY_EDITOR
        private readonly GameObject m_Point;
#endif

        public RaycastService(IAukiWrapper aukiWrapper, Settings settings)
        {
            m_AukiWrapper = aukiWrapper;
            m_Settings = settings;

#if UNITY_EDITOR
            m_Point = GameObject.Find("_TARGET_");
#endif
        }
        
        private bool FloorRaycastHelper(Vector3 position, out Vector3 floorPosition, out Vector3 floorNormal, float maxUpwardsSnap, float maxDownwardsSnap)
        {
            var hitResults = new List<ARRaycastHit>();

            var gettingHits = m_AukiWrapper.arRaycastManager.Raycast(
                new Ray(position + Vector3.up * maxUpwardsSnap, Vector3.down),
                hitResults,
                m_Settings.trackableType);
            
            if (gettingHits && hitResults.Count > 0 &&
                hitResults[0].pose.up.y > 0.6 && // Ignore vertical planes
                hitResults[0].distance < maxDownwardsSnap + maxUpwardsSnap) // Only snap within a range
            {
                floorPosition = hitResults[0].pose.position;
                floorNormal = hitResults[0].pose.up;
                return true;
            }
            
            floorPosition = Vector3.zero;
            floorNormal = Vector3.zero;
            return false;
        }

        public bool FloorRaycast(Vector3 position, out Vector3 floorPosition, out Vector3 floorNormal)
        {
            bool hit = FloorRaycastHelper(position, out floorPosition, out floorNormal, MAX_UPWARDS_FLOOR_SNAP, MAX_DOWNWARDS_FLOOR_SNAP);
            if (hit)
                return true;
            
            // Try again with unlimited snap (just pass high value).
            // This is to re-snap even after a big AR drift.
            // E.g. after brief backgrounding of app or fast phone movement, re-calibrating again after a bad lighthouse scan etc.
            hit = FloorRaycastHelper(position, out floorPosition, out floorNormal, 100, 100);
            return hit;
        }
        
        // public bool CeliRaycast(Vector3 position, out Vector3 hitPoint, out Vector3 hitNormal)
        // {
        //     var hitResults = new List<ARRaycastHit>();
        //     if (m_AukiWrapper.arRaycastManager.Raycast(
        //         new Ray(position, Vector3.up),
        //         hitResults,
        //         TrackableType.PlaneWithinBounds))
        //     {
        //         hitPoint = hitResults[0].pose.position;
        //         hitNormal = hitResults[0].pose.up;
        //         return true;
        //     }
        //     hitPoint = Vector3.zero;
        //     hitNormal = Vector3.zero;
        //     return false;
        // }

        
        
        public void Tick(float deltaTime, float unscaledDeltaTime)
        {
            if (m_AukiWrapper.arCamera == null || m_AukiWrapper.arRaycastManager == null)
                return;

            var gettingHits = m_AukiWrapper.arRaycastManager.Raycast(
                m_AukiWrapper.arCamera.ViewportToScreenPoint(new Vector3(0.5f, 0.5f, 0)),
                m_Hits,
                m_Settings.trackableType);

            hasHit = gettingHits && m_Hits.Count > 0 && m_Hits[0].pose.up.y > 0.6f;
            
#if UNITY_EDITOR
            hasHit = true;
#endif
        }

        public bool hasHit { get; private set; }

        public Camera camera=>m_AukiWrapper.arCamera;

        public Pose hitPose
        {
            get
            {
#if UNITY_EDITOR
                return new Pose(m_Point.transform.position,m_Point.transform.rotation);
#else
                return m_Hits[0].pose;
#endif
            }
        }

        public float distance
        {
            get
            {
                
#if UNITY_EDITOR
                return Vector3.Distance(m_AukiWrapper.arCamera.transform.position, m_Point.transform.position);
#else
                return Vector3.Distance(m_AukiWrapper.arCamera.transform.position, m_Hits[0].pose.position);
#endif
            }
        }
        

        #region Settings
        [System.Serializable]
        public class Settings
        {
            [SerializeField] private TrackableType m_TrackableType = TrackableType.PlaneWithinPolygon;

            public TrackableType trackableType => m_TrackableType;
        }
        #endregion
    }
}