using UnityEngine;
using UnityEngine.Serialization;
using System;
using System.Collections;
using Random = UnityEngine.Random;

public class EnemyVirusRND1 : MonoBehaviour
{
    [SerializeField] private GameObject m_ElectricCrackle;
    [SerializeField] private GameObject m_ShadowLight;
    [SerializeField] private GameObject m_Shield;
    [SerializeField] private GameObject m_VirusBody;
    [FormerlySerializedAs("m_VirusBodyMaterial")] [SerializeField] private Material m_SharedVirusBodyMaterial;
    [SerializeField] private Material m_SharedElectricCrackleMaterial;
    [SerializeField] private Material m_SharedShadowLightMaterial;
    [FormerlySerializedAs("m_ShieldMaterial")] [SerializeField] private Material m_SharedShieldMaterial;
    
    //[SerializeField] private float m_WorldScale = 1.0f;
    
    private Material m_ElectricCrackleMaterial;
    private Material m_VirusBodyMaterial;
    private Material m_ShieldMaterial;
    private Material m_ShadowLightMaterial;
    
    private static int m_Seed = 0;
    private Vector3 m_VirusBodyNoiseSampleOffset;
    
    private static readonly int Offset = Shader.PropertyToID("_Offset");
    private static readonly int FirstFlipBook = Shader.PropertyToID("_FlipBookOne");
    private static readonly int SecondFlipBook = Shader.PropertyToID("_FlipBookTwo");
    private static readonly int ThirdFlipBook = Shader.PropertyToID("_FlipBookThree");
    private static readonly int FourthFlipBook = Shader.PropertyToID("_FlipBookFour");
    private static readonly int RotationAxis = Shader.PropertyToID("_RotationAxis");
    private static readonly int AnimationOffset = Shader.PropertyToID("_AnimationOffset");
    private static readonly int SyncedTime = Shader.PropertyToID("_SyncedTime");
    private static readonly int Summon = Shader.PropertyToID("_Summon");
    
    private float m_DespawnDuration = 0.4f;
    private Coroutine m_ShieldDespawnCoroutine;

    public void Init(int seed, bool isToughenedEnemy = false)
    {
        //Update Seed
        //For each enemy instance we want different values
        m_Seed = seed;
        Random.InitState(m_Seed);
        

        
        //We want different crackle for each instance so we create a new material and assign different flip book values
        //Material is SRP Batcher compatible so we don't have to worry about draw calls
        m_ElectricCrackleMaterial = Instantiate(m_SharedElectricCrackleMaterial);
        m_ElectricCrackle.GetComponent<Renderer>().material = m_ElectricCrackleMaterial;
        
        m_ShadowLightMaterial = Instantiate(m_SharedShadowLightMaterial);
        m_ShadowLight.GetComponent<Renderer>().material = m_ShadowLightMaterial;
        
        // Electric crackle material has texture with 4 flip book options, one or more can be enabled at the same time.
        // We offset the animation start position in case two or more materials have the same flip book option combination
        
        // We have 16 x 8 -> 128 tiles of animation.
        // float offset = fmod(floor(_Offset), 128) -> Shader code.    
        float ElectricCrackleOffset = Random.Range(0.0f, 128.0f);
        m_ElectricCrackleMaterial.SetFloat(Offset, ElectricCrackleOffset);
        m_ShadowLightMaterial.SetFloat(Offset, ElectricCrackleOffset);
        
        // 2 ** 4 = 16
        byte option = (byte) Random.Range(1, 16);
        
        // m_SharedElectricCrackleMaterial has no flip book options enabled
        if ((0x1 & option) != 0)
        {
            m_ElectricCrackleMaterial.SetFloat(FirstFlipBook, 1f);
            m_ShadowLightMaterial.SetFloat(FirstFlipBook, 1f);
        }

        if ((0x1 << 1 & option) != 0)
        {
            m_ElectricCrackleMaterial.SetFloat(SecondFlipBook, 1f);
            m_ShadowLightMaterial.SetFloat(SecondFlipBook, 1f);
        }

        if ((0x1 << 2 & option) != 0)
        {
            m_ElectricCrackleMaterial.SetFloat(ThirdFlipBook, 1f);
            m_ShadowLightMaterial.SetFloat(ThirdFlipBook, 1f);
        }

        if ((0x1 << 3 & option) != 0)
        {
            m_ElectricCrackleMaterial.SetFloat(FourthFlipBook, 1f);
            m_ShadowLightMaterial.SetFloat(FourthFlipBook, 1f);
        }
        
        if (isToughenedEnemy)
        {
            m_ShieldMaterial = Instantiate(m_SharedShieldMaterial);
            m_Shield.GetComponent<Renderer>().material = m_ShieldMaterial;
            m_ShieldMaterial.SetVector(RotationAxis, Random.onUnitSphere);
            m_Shield.SetActive(true);
        }
        else
        {
            m_Shield.SetActive(false);
        }
        
        m_VirusBodyMaterial = Instantiate(m_SharedVirusBodyMaterial);
        m_VirusBody.GetComponent<Renderer>().material = m_VirusBodyMaterial;
        
        m_VirusBodyNoiseSampleOffset = Random.onUnitSphere;
        
        long currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        float tBase = (currentTime & 0xFFFFFFF) * 0.08f;
        Vector3 noiseTimeSampleOffset = Vector3.one * tBase + m_VirusBodyNoiseSampleOffset * 0xFF;
        
        m_VirusBodyMaterial.SetVector(AnimationOffset, noiseTimeSampleOffset);
    }
    
    public void OnShieldDestroyed()
    {
        if (!m_Shield.activeSelf)
            return;
        
        if (m_ShieldDespawnCoroutine != null)
        {
            StopCoroutine(m_ShieldDespawnCoroutine);
        }
        
        m_ShieldDespawnCoroutine = StartCoroutine(ShieldDespawnCoroutine());
    }


    private IEnumerator ShieldDespawnCoroutine()
    {
        float time = 0;
        Vector3 startScale = m_Shield.transform.localScale;
        while (time < m_DespawnDuration)
        {
            m_Shield.transform.localScale = startScale * (1 + 1.5f * time / m_DespawnDuration);
            m_ShieldMaterial.SetFloat(Summon, 1 - time / m_DespawnDuration);
            time += Time.deltaTime;
            yield return null;
        }
        
        m_Shield.SetActive(false);
        m_ShieldDespawnCoroutine = null;
    }


    void Update()
    {
        long currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        float tBase = (currentTime & 0xFFFFFFF) * 0.08f;

        Vector3 noiseTimeSampleOffset = Vector3.one * tBase + m_VirusBodyNoiseSampleOffset * 0xFF;
        
        if(m_VirusBodyMaterial != null)
            m_VirusBodyMaterial.SetVector(AnimationOffset, noiseTimeSampleOffset);

        if (m_Shield.activeSelf)
        {
            float shieldTime = ((currentTime & 0xFFF) / (float)0xFFF) * 360.0f * Mathf.Deg2Rad;
            Vector2 syncedTimeVector2 = new Vector2((shieldTime), Mathf.Sin(shieldTime) * 0.8f);
            
            if(m_ShieldMaterial != null)
                m_ShieldMaterial.SetVector(SyncedTime, syncedTimeVector2);
        }

        // 16 x 8 -> 128 tiles of animation / 12 fps = 10.6666 -> 10666 so wrap around every 10666 ms
        float electricalCrackleT = (currentTime % 0x29AA) * 0.001f;
        
        if(m_ElectricCrackleMaterial != null)
            m_ElectricCrackleMaterial.SetFloat(SyncedTime, electricalCrackleT);
        
        
        if(m_ShadowLightMaterial != null)
            m_ShadowLightMaterial.SetFloat(SyncedTime, electricalCrackleT);
    }
}
