using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Matterless.Floorcraft
{
    public class EngineVFX : MonoBehaviour
    {
        #region  Inspector
        [SerializeField] Transform m_EnginePlumePivot;
        [SerializeField] private Material m_EnginePlumeMaterial;
        [SerializeField] private ParticleSystem[] m_FireFliesParticles;
        [SerializeField] private TrailRenderer[] m_EngineTrails;
        #endregion
        
        #region ShaderProperties
        private readonly int m_PlumeAlphaTexturePropertyId = Shader.PropertyToID("_PlumeAlpha");
        private readonly int m_PlumeStrengthPropertyId = Shader.PropertyToID("_Strength");
        #endregion
        
        #region Settings
        
        private float m_PlumeMinSize; 
        private float m_PlumeMaxSize;
        private float m_StartPlumeTextureTiling; 
        private float m_EndPlumeTextureTiling; 
        private float m_PlumeTextureOffsetSpeed;
        private float m_FireFliesParticlesMinSpeed; 
        private float m_FireFliesParticlesMaxSpeed; 
        private float m_FireFliesParticleMinEmissionRate;
        private float m_FireFliesParticleMaxEmissionRate;
        private float m_FireFliesParticleConeMinAngle;
        private float m_FireFliesParticleConeMaxAngle;
        
        #endregion
        
        private float m_Offset = 0f;
        private Material m_MaterialInstance;
        private ParticleSystem.MainModule[] m_FireFliesParticleMainModules;
        private ParticleSystem.EmissionModule[] m_FireFliesParticleEmissionModules;
        private ParticleSystem.ShapeModule[] m_FireFliesParticleShapeModules;
        private bool m_ShouldShowParticles = false;
        
        private float[] m_FireFliesParticleStartSize;
        private float[] m_FireFliesParticleStartShapeRadius;

        public bool shouldShowParticles
        {
            set => m_ShouldShowParticles = value;
        }

        public void Init(float size, float worldSize, EngineVFXSettings settings)
        {
            m_PlumeMinSize = settings.PlumeMinSize;
            m_PlumeMaxSize = settings.PlumeMaxSize;
            m_StartPlumeTextureTiling = settings.StartPlumeTextureTiling;
            m_EndPlumeTextureTiling = settings.EndPlumeTextureTiling;
            m_PlumeTextureOffsetSpeed = settings.PlumeTextureOffsetSpeed;
            m_FireFliesParticlesMinSpeed = settings.FireFliesParticlesMinSpeed * worldSize;
            m_FireFliesParticlesMaxSpeed = settings.FireFliesParticlesMaxSpeed * worldSize;
            m_FireFliesParticleMinEmissionRate = settings.FireFliesParticleMinEmissionRate;
            m_FireFliesParticleMaxEmissionRate = settings.FireFliesParticleMaxEmissionRate;
            m_FireFliesParticleConeMinAngle = settings.FireFliesParticleConeMinAngle;
            m_FireFliesParticleConeMaxAngle = settings.FireFliesParticleConeMaxAngle;
                
            m_MaterialInstance = Instantiate(m_EnginePlumeMaterial);
            
            MeshRenderer[] meshRenderers = m_EnginePlumePivot.GetComponentsInChildren<MeshRenderer>();
            foreach (var meshRenderer in meshRenderers)
            {
                meshRenderer.material = m_MaterialInstance;
            }
            
            foreach (TrailRenderer trail in m_EngineTrails)
            {
                trail.startWidth = size * 0.3f * worldSize;
                trail.endWidth = 0.0f;
                trail.minVertexDistance = 0.1f * size * worldSize;
                trail.time = 1;
            }
            
            m_FireFliesParticleMainModules = new ParticleSystem.MainModule[m_FireFliesParticles.Length];
            m_FireFliesParticleEmissionModules = new ParticleSystem.EmissionModule[m_FireFliesParticles.Length];
            m_FireFliesParticleShapeModules = new ParticleSystem.ShapeModule[m_FireFliesParticles.Length];

            m_FireFliesParticleStartSize = new float[m_FireFliesParticles.Length];
            m_FireFliesParticleStartShapeRadius = new float[m_FireFliesParticles.Length];
            for (int i = 0; i < m_FireFliesParticles.Length; i++)
            {
                m_FireFliesParticles[i].transform.localScale = new Vector3(size, size, size);
                m_FireFliesParticleMainModules[i] = m_FireFliesParticles[i].main;
                m_FireFliesParticleEmissionModules[i] = m_FireFliesParticles[i].emission;
                m_FireFliesParticleShapeModules[i] = m_FireFliesParticles[i].shape;
                m_FireFliesParticles[i].Play();
                
                m_FireFliesParticleStartSize[i] = m_FireFliesParticles[i].main.startSizeMultiplier;
                m_FireFliesParticleStartShapeRadius[i] = m_FireFliesParticles[i].shape.radius;
            }

        }

        public void Show()
        {
            m_EnginePlumePivot.gameObject.SetActive(true);
        }
        
        public void Hide()
        {
            m_EnginePlumePivot.gameObject.SetActive(false);
            m_ShouldShowParticles = false;
        }

        public void UpdateVFX(float speed, float scale)
        {
            float tiling = Mathf.Lerp(m_StartPlumeTextureTiling, m_EndPlumeTextureTiling, speed);
            m_MaterialInstance.SetTextureScale(m_PlumeAlphaTexturePropertyId, new Vector2(tiling, 1));
            
            m_Offset += Time.deltaTime * m_PlumeTextureOffsetSpeed;
            m_MaterialInstance.SetTextureOffset(m_PlumeAlphaTexturePropertyId, new Vector2(m_Offset, 0));
            
            m_MaterialInstance.SetFloat(m_PlumeStrengthPropertyId, Mathf.Lerp(0.75f, 1.0f, speed));
            m_EnginePlumePivot.localScale = new Vector3(1, 1, Mathf.Lerp(m_PlumeMinSize, m_PlumeMaxSize, speed));

            bool shouldHideTrails = Mathf.Approximately(speed, 0.0f);
            
            foreach (TrailRenderer trail in m_EngineTrails)
            {
                trail.emitting = !shouldHideTrails;
            }
            
            if (!m_ShouldShowParticles)
            {
                for (int i = 0; i < m_FireFliesParticles.Length; i++)
                {
                    m_FireFliesParticleEmissionModules[i].rateOverTime = 0;
                }
                return;
            }
            
            for (int i = 0; i < m_FireFliesParticles.Length; i++)
            {
                m_FireFliesParticleMainModules[i].startSpeed = Mathf.Lerp(m_FireFliesParticlesMinSpeed, m_FireFliesParticlesMaxSpeed, speed);
                m_FireFliesParticleEmissionModules[i].rateOverTime = Mathf.Lerp(m_FireFliesParticleMinEmissionRate, m_FireFliesParticleMaxEmissionRate, speed);
                m_FireFliesParticleShapeModules[i].angle = Mathf.Lerp(m_FireFliesParticleConeMaxAngle, m_FireFliesParticleConeMinAngle, speed);
                
                ParticleSystem.MainModule main = m_FireFliesParticles[i].main;
                main.startSizeMultiplier = m_FireFliesParticleStartSize[i] * scale;
                
                ParticleSystem.ShapeModule shape = m_FireFliesParticles[i].shape;
                shape.radius = m_FireFliesParticleStartShapeRadius[i] * scale;
            }
            
        }
    }
}
