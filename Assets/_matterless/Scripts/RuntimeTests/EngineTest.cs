using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;


public class EngineTest : MonoBehaviour
{
    #region  Inspector
    [SerializeField] Transform m_EnginePlumePivot;
    [SerializeField] private Material m_EnginePlumeMaterial;
    [SerializeField] private ParticleSystem[] m_FireFliesParticles;
    [SerializeField] private TrailRenderer[] m_EngineTrails;
    #endregion
    
    #region Settings
    private const float c_PlumeMinSize = 1.5f;
    private const float c_PlumeMaxSize = 2.25f;
    private const float c_StartPlumeTextureTiling = 1.0f;
    private const float c_EndPlumeTextureTiling = 0.5f;
    private const float c_PlumeTextureOffsetSpeed = 2f;
    private const float c_FireFliesParticlesMinSpeed = 0.5f;
    private const float c_FireFliesParticlesMaxSpeed = 1.5f;
    private const float c_FireFliesParticleMinEmisionRate = 10f;
    private const float c_FireFliesParticleMaxEmisionRate = 25f;
    private const float c_FireFliesParcticleConeMinAngle = 5f;
    private const float c_FireFliesParcticleConeMaxAngle = 27f;
    
    
    #endregion
    
    
    #region ShaderProperties
    private readonly int m_PlumeAlphaTexturePropertyId = Shader.PropertyToID("_PlumeAlpha");
    private readonly int m_PlumeStrengthPropertyId = Shader.PropertyToID("_Strength");
    #endregion
    
    [Range(0, 1)]
    public float speed = 0f;
    
    
    private float m_Offset = 0f;

    void Start()
    {
        foreach (TrailRenderer trail in m_EngineTrails)
        {
            trail.startWidth = 0.375f;
            trail.endWidth = 0.0f;
        }
            
    }
    void Update()
    {
        float tiling = Mathf.Lerp(c_StartPlumeTextureTiling, c_EndPlumeTextureTiling, speed);
        m_EnginePlumeMaterial.SetTextureScale(m_PlumeAlphaTexturePropertyId, new Vector2(tiling, 1));
        
        m_Offset += Time.deltaTime * c_PlumeTextureOffsetSpeed;
        m_EnginePlumeMaterial.SetTextureOffset(m_PlumeAlphaTexturePropertyId, new Vector2(m_Offset, 0));
        
        m_EnginePlumeMaterial.SetFloat(m_PlumeStrengthPropertyId, Mathf.Lerp(0.75f, 1.0f, speed));
        m_EnginePlumePivot.localScale = new Vector3(1, 1, Mathf.Lerp(c_PlumeMinSize, c_PlumeMaxSize, speed));

        foreach (var particle in m_FireFliesParticles)
        {
            var main = particle.main;
            main.startSpeed = Mathf.Lerp(c_FireFliesParticlesMinSpeed, c_FireFliesParticlesMaxSpeed, speed);
            var emission = particle.emission;
            emission.rateOverTime = Mathf.Lerp(c_FireFliesParticleMinEmisionRate, c_FireFliesParticleMaxEmisionRate, speed);
            var shape = particle.shape;
            shape.angle = Mathf.Lerp(c_FireFliesParcticleConeMaxAngle, c_FireFliesParcticleConeMinAngle, speed);
        }
       
    }
}
