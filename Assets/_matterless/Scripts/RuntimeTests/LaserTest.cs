using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Matterless.Floorcraft
{
    public class LaserTest : MonoBehaviour
    {
        
        [SerializeField] private float m_ChargeDuration = 2f;
        [SerializeField] private float m_LaserDuration = 5f;
        [SerializeField] private float m_LaserPropagationTime = 10.0f;
        [SerializeField] private float m_MaxLength = 500f;
        [SerializeField] private ParticleSystem m_FirePointFX;
        [SerializeField] private ParticleSystem m_ChargeFX;
        [SerializeField] private ParticleSystem m_HitFX;
        [SerializeField] private ParticleSystem m_HeadFX;
        [SerializeField] private LineRenderer m_BeamFX;


        private Transform parent;
        private readonly float defaultLineRendererWidth = 5.0f;
        private readonly float defaultVFXScale = 1.0f;
        
        private bool isCharging = false;
        private bool isFiring = false;
        private float chargeTimer;
        private float laserBeamTimer;
        private float propagationTimer;
        
        void Start()
        {
            var scene = SceneManager.GetActiveScene();
            
            parent = scene.name == "vfx_look_dev2" ? gameObject.transform : transform.parent.transform.parent.transform.parent.transform;
            Debug.Log(parent.name);
            
            float scalePercentage = parent.localScale.x / defaultVFXScale; //assume uniform scale
            m_BeamFX.startWidth = defaultLineRendererWidth * scalePercentage;
            chargeTimer = m_ChargeDuration;
            laserBeamTimer = m_LaserDuration;
            propagationTimer = 0;
            m_BeamFX.SetPosition(0, transform.position);
            m_BeamFX.SetPosition(1, transform.position);
            ParticleSystem.MainModule mian = m_ChargeFX.main;
            mian.startLifetime = m_ChargeDuration;
        }

        void Update()
        {
            if (isFiring)
            {
                float scalePercentage = parent.localScale.x/defaultVFXScale; //assume uniform scale
                m_BeamFX.startWidth = defaultLineRendererWidth * scalePercentage;
                
                if (laserBeamTimer > 0)
                {
                    Debug.Log("Firing");
                    m_BeamFX.enabled = true;
                    if(!m_FirePointFX.isPlaying)
                        m_FirePointFX.Play(true);
                    Vector3 laserEndPosition = Vector3.Lerp(transform.position, transform.position + Vector3.forward * m_MaxLength, propagationTimer/m_LaserPropagationTime);
                    m_BeamFX.SetPosition(1, laserEndPosition);
                    RaycastHit hit;
                    if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit,
                            m_MaxLength * propagationTimer / m_LaserPropagationTime))
                    {
                        m_BeamFX.SetPosition(1, hit.point);
                        m_HitFX.transform.position = hit.point;
                        if (!m_HitFX.isPlaying)
                            m_HitFX.Play();
                        m_HeadFX.Stop();
                    }
                    else
                    {
                        m_HeadFX.transform.position = laserEndPosition;
                        if(!m_HeadFX.isPlaying)
                            m_HeadFX.Play();
                        m_HitFX.Stop();
                    }
                    
                    propagationTimer += Time.deltaTime;
                    laserBeamTimer -= Time.deltaTime;
                    return;
                }
                else
                {
                    Debug.Log("Firing Ended");
                    m_FirePointFX.Stop(true);
                    m_BeamFX.enabled = false;
                    propagationTimer = 0;
                    laserBeamTimer = m_LaserDuration;
                    isFiring = false;
                    m_HitFX.Stop();
                    m_HeadFX.Stop();
                }
            }

            if (isCharging)
            {
                if (chargeTimer > 0)
                {
                    Debug.Log("Charging");
                    if(!m_ChargeFX.isPlaying)
                        m_ChargeFX.Play(true);
                    chargeTimer -= Time.deltaTime;
                    return;
                }
                else
                {
                    Debug.Log("Charging Ended");
                    m_ChargeFX.Stop(true);
                    chargeTimer = m_ChargeDuration;
                    isCharging = false;
                    isFiring = true;
                }
            }
            
            if (Input.GetMouseButtonUp(0))
            {
                isCharging = true;
            }
            
        }
    }    
}

