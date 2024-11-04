using System;
using UnityEngine;

namespace Matterless.Floorcraft
{
    public class WorldScaleService
    {
        private float m_WorldScale;
        
        public float worldScale => m_WorldScale;

        private event Action<float> m_OnScaleChanged; 
        
        public void RegisterOnScaleChanged(Action<float> action)
        {
            m_OnScaleChanged += action;
        }

        public WorldScaleService(Settings settings)
        {
            SetScaleInner(settings.worldScale);
        }

        public void SetScale(float scale)
        {
            SetScaleInner(scale);
            m_OnScaleChanged?.Invoke(m_WorldScale);
        }

        private void SetScaleInner(float scale)
        {
            if (scale < 0.3f || scale > 2)
            {
                Debug.LogWarning("Clamping world scale because it is out of bounds! [0.3, 2]");
            }
            
            m_WorldScale = Mathf.Clamp(scale, 0.3f, 2);
            Debug.Log($"World scale set to {scale}");
            // NOTE: (Marko) This is a hack. After working on the VFX Scale issue I have found out that VFX actually never looked good -> VFX were made as the reference scale is Vector3.one
            // So along scaling the gravity by the world scale, we also should scale gravity (VFX) by the vehicle scale
            // 0.0255f is the VEHICLE SCALE
            Physics.gravity = new Vector3(0, -9.81f * m_WorldScale * 0.0255f, 0);
        }

        [Serializable]
        public class Settings
        {
            [Range(0.3f, 2.0f)]
            [SerializeField] private float m_WorldScale;

            public float worldScale => m_WorldScale;
        }
    }
}