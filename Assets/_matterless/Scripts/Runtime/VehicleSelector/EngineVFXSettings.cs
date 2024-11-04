using UnityEngine;
using UnityEngine.Serialization;

namespace Matterless.Floorcraft
{
    [CreateAssetMenu(menuName = "Matterless/EngineVFX")]
    public class EngineVFXSettings: ScriptableObject
    {
        [SerializeField] float m_PlumeMinSize = 1.5f;
        [SerializeField] float m_PlumeMaxSize = 2.25f;
        [SerializeField] float m_StartPlumeTextureTiling = 1.0f;
        [SerializeField] float m_EndPlumeTextureTiling = 0.5f;
        [SerializeField] float m_PlumeTextureOffsetSpeed = 2f;
        [SerializeField] float m_FireFliesParticlesMinSpeed = 0.5f;
        [SerializeField] float m_FireFliesParticlesMaxSpeed = 1.5f;
        [SerializeField] float m_FireFliesParticleMinEmissionRate = 10f;
        [SerializeField] float m_FireFliesParticleMaxEmissionRate = 25f;
        [SerializeField] float m_FireFliesParticleConeMinAngle = 5f;
        [SerializeField] float m_FireFliesParticleConeMaxAngle = 27f;
        
        public float PlumeMinSize => m_PlumeMinSize;
        public float PlumeMaxSize => m_PlumeMaxSize;
        public float StartPlumeTextureTiling => m_StartPlumeTextureTiling;
        public float EndPlumeTextureTiling => m_EndPlumeTextureTiling;
        public float PlumeTextureOffsetSpeed => m_PlumeTextureOffsetSpeed;
        public float FireFliesParticlesMinSpeed => m_FireFliesParticlesMinSpeed;
        public float FireFliesParticlesMaxSpeed => m_FireFliesParticlesMaxSpeed;
        public float FireFliesParticleMinEmissionRate => m_FireFliesParticleMinEmissionRate;
        public float FireFliesParticleMaxEmissionRate => m_FireFliesParticleMaxEmissionRate;
        public float FireFliesParticleConeMinAngle => m_FireFliesParticleConeMinAngle;
        public float FireFliesParticleConeMaxAngle => m_FireFliesParticleConeMaxAngle;
        
    }
}