//#define VFX_DEBUG

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace Matterless.Floorcraft
{
    public class ObstacleView : GameObjectView
    {
        private uint m_ObstacleType;
        public uint obstacleType => m_ObstacleType;
        public event Action<float> onScaleChanged;
        
        [SerializeField] private AudioSource m_SpawnSfx;
        [SerializeField] private GameObject m_Repulsor;
        [SerializeField] private GameObject m_Billboard;
        [SerializeField] private GameObject m_Ship;
        [SerializeField] private GameObject m_Beam;
        [SerializeField] private GameObject m_Colliders;
        [SerializeField] private GameObject[] m_ElectricCrackles;
        [FormerlySerializedAs("m_ElectricCrackleMaterial")] [SerializeField] private Material m_ElectricCrackleMaterial1;
        [SerializeField] private Material m_ElectricCrackleMaterial2;
#if VFX_DEBUG
        [SerializeField] [Range(0, 1)] 
#endif        
        private float m_Health = 1.0f;
        
        [Space(10)]
        [Header("LCD Screen")]
        [SerializeField] private Texture2D[] m_ScreenDamageTextures;

        private readonly int m_SummonPropertyId = Shader.PropertyToID("_Summon");
        private readonly float m_SpawnAnimationDuration = 4.0f;
        private int m_ToonOutlineLayer;
        private IEnumerator m_SpawnAnimationCoroutine = null;
        private IEnumerator m_RotateAndBobCoroutine = null;
        private IEnumerator m_HitVisualsCoroutine = null;
        
        private float m_HitVisualsDuration = 0.5f;
        
        // NOTE(Marko): Having instances of materials is not needed for the Mayhem mode, this is to prevent
        // the material from being shared between all the towers
        private Material m_MaterialInstance;
        private Material m_BeamMaterialInstance;

        private Vector3 m_InitialScale;
        private static readonly int Damage = Shader.PropertyToID("_Damage");
        private static readonly int FlipBookTexture = Shader.PropertyToID("_FlipBookTexture");
        private static readonly int IsHit = Shader.PropertyToID("_IsHit");
        
        private static int s_DamageIndex = 0;

        private static readonly int SyncedTime = Shader.PropertyToID("_SyncedTime");

        public ObstacleView Init(uint entityId, uint obstacleId, Vector3 scale, float worldScale)
        {
            m_ToonOutlineLayer = LayerMask.NameToLayer("Toon Outline");
            base.entityId = entityId;
            m_ObstacleType = obstacleId;
            m_InitialScale = scale;
            SetScale(worldScale);
            //this.transform.SetPositionAndRotation(pose.position, pose.rotation);
            m_Colliders.SetActive(false);
            this.gameObject.SetActive(true);
            
            foreach (GameObject crackle in m_ElectricCrackles)
            {
                crackle.SetActive(false);
            }
                
            return this;
        }

        private void OnEnable()
        {
            m_SpawnSfx.Play();
            
            if (m_RotateAndBobCoroutine != null)
                StopCoroutine(m_RotateAndBobCoroutine);
            
            m_RotateAndBobCoroutine = RotateAndBob();
            StartCoroutine(m_RotateAndBobCoroutine);
            
            
            if (m_SpawnAnimationCoroutine != null)
                StopCoroutine(m_SpawnAnimationCoroutine);

            m_SpawnAnimationCoroutine = SpawnAnimation();
            StartCoroutine(m_SpawnAnimationCoroutine);
        }

#if VFX_DEBUG
        private void Update()
        {
            if (Input.GetMouseButtonDown(1))
            {
                m_Health -= 0.1f;
                SetDamageVisuals(m_Health);
            }
            
        }
#endif
        public void SetDamageVisuals(float health)
        {
            m_Health = health;
            if (m_HitVisualsCoroutine != null)
                StopCoroutine(m_HitVisualsCoroutine);
            
            m_HitVisualsCoroutine = HitVisuals(m_Health);
            StartCoroutine(m_HitVisualsCoroutine);
        }

        private IEnumerator HitVisuals(float health)
        {
            float damage = Mathf.Clamp01(1 - health);
            s_DamageIndex = (int) Mathf.Floor(damage/0.2f);
            
            m_MaterialInstance.SetFloat(Damage, damage);
            m_BeamMaterialInstance.SetFloat(Damage, damage);
            
            m_MaterialInstance.SetFloat(IsHit, 1.0f);
            m_BeamMaterialInstance.SetFloat(IsHit, 1.0f);
            m_MaterialInstance.SetTexture(FlipBookTexture, m_ScreenDamageTextures[5]);
            
            yield return new WaitForSeconds(m_HitVisualsDuration);
            
            m_MaterialInstance.SetTexture(FlipBookTexture, m_ScreenDamageTextures[s_DamageIndex]);
            m_MaterialInstance.SetFloat(IsHit, 0.0f);
            m_BeamMaterialInstance.SetFloat(IsHit, 0.0f);
        }

        private IEnumerator SpawnAnimation()
        {
            float t = 0;
            
            // NOTE(Marko): This will not be needed when we get to Mayhem mode cause we only have one tower per session
            m_MaterialInstance = m_Repulsor.GetComponent<MeshRenderer>().material;
            m_Billboard.GetComponent<MeshRenderer>().material = m_MaterialInstance;
            m_Ship.GetComponent<MeshRenderer>().material = m_MaterialInstance;
            
            m_BeamMaterialInstance = m_Beam.GetComponent<MeshRenderer>().material;
            
            while (t < m_SpawnAnimationDuration)
            {
                m_MaterialInstance.SetFloat(m_SummonPropertyId, Mathf.Lerp(0, 1,  t / m_SpawnAnimationDuration));
                m_BeamMaterialInstance.SetFloat(m_SummonPropertyId, Mathf.Lerp(0, 1,  t / m_SpawnAnimationDuration));
                t += Time.deltaTime;
                
                yield return null;
            }
            m_MaterialInstance.SetFloat(m_SummonPropertyId, 1.0f);
            m_BeamMaterialInstance.SetFloat(m_SummonPropertyId, 1.0f);
            
            m_Colliders.SetActive(true);
            
            m_Repulsor.gameObject.layer = m_ToonOutlineLayer;
            m_Billboard.gameObject.layer = m_ToonOutlineLayer;
            m_Ship.gameObject.layer = m_ToonOutlineLayer;
            
            foreach (GameObject crackle in m_ElectricCrackles)
            {
                crackle.SetActive(true);
            }
        }
        
        private IEnumerator RotateAndBob()
        {
            while (true)
            {
                long currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                
                //NOTE (Marko): Removed the rotation of the repulsor cause it was too distracting
                // https://matterless.atlassian.net/browse/BLASTERS-130?atlOrigin=eyJpIjoiMTBhODUxMDBjMzkyNDhmNDhhNjA5ZDhjY2ZlYmJlODYiLCJwIjoiaiJ9
                
                //float t = (currentTime & 0x7FFF) / (float) 0x7FFF;
                //m_Repulsor.transform.rotation = Quaternion.Euler(0, t * 360.0f, 0);
                
                float t = (currentTime & 0xFFFF) / (float) 0xFFFF;
                m_Billboard.transform.rotation = Quaternion.Euler(0, t * -360.0f, 0);
                
                t = (currentTime & 0x3FFFF) / (float) 0x3FFFF;
                m_Ship.transform.rotation = Quaternion.Euler(0, t * 360.0f, 0);
                
                // 16 x 8 -> 128 tiles of animation / 12 fps = 10.6666 -> 10666 so wrap around every 10666 ms
                float electricalCrackleT = (currentTime % 0x29AA) * 0.001f;
                m_ElectricCrackleMaterial1.SetFloat(SyncedTime, electricalCrackleT);
                m_ElectricCrackleMaterial2.SetFloat(SyncedTime, electricalCrackleT);
                
                yield return null;
            }
        }
        
        public void SetScale(float scale)
        {
            this.transform.localScale = m_InitialScale * scale;
            onScaleChanged?.Invoke(transform.localScale.x);
        }
    }
}   