//#define CASH_PERLIN_NOISE
using System.Runtime.CompilerServices;
using UnityEngine;

public class EnemyVirusRND : MonoBehaviour
{
    [SerializeField] private GameObject m_VirusBody;
    [SerializeField] private GameObject m_ElectricCrackle;
    [SerializeField] private Material m_SharedElectricCrackleMaterial;
    private Material m_ElectricCrackleMaterial;
    
    //Rotation speed in each axis
    [SerializeField] private float m_XSpeed = 0.1f;
    [SerializeField] private float m_YSpeed = 0.1f;
    [SerializeField] private float m_ZSpeed = 0.1f;
    
    //Rotation max angle in each axis
    [SerializeField] private float m_XMaxAngle = 10f;
    [SerializeField] private float m_YMaxAngle = 10f;
    [SerializeField] private float m_ZMaxAngle = 10f;
    
    private float m_PerInstanceOffset;
    private static int m_Seed = 0;
    private SkinnedMeshRenderer[] m_LungMeshes;
    private float[] m_Offsets;
    private float[] m_BreatheFrequency;
    
    private static readonly int Offset = Shader.PropertyToID("_Offset");
    private static readonly int FirstFlipBook = Shader.PropertyToID("_FlipBookOne");
    private static readonly int SecondFlipBook = Shader.PropertyToID("_FlipBookTwo");
    private static readonly int ThirdFlipBook = Shader.PropertyToID("_FlipBookThree");
    private static readonly int FourthFlipBook = Shader.PropertyToID("_FlipBookFour");

#if CASH_PERLIN_NOISE
    [SerializeField] private bool m_UseCashedPerlinNoise = true;
    private static float[] m_CashedPerlinNoise;
    private int m_NoiseSize = 256;
#endif
    
    private void Start()
    {
        //Update Seed
        //For each enemy instance we want different values
        m_Seed++;
        Random.InitState(m_Seed);
        
        //We want different crackle for each instance so we create a new material and assign different flip book values
        //Material is SRP Batcher compatible so we don't have to worry about draw calls
        m_ElectricCrackleMaterial = Instantiate(m_SharedElectricCrackleMaterial);
        m_ElectricCrackle.GetComponent<Renderer>().material = m_ElectricCrackleMaterial;
        
        // Electric crackle material has texture with 4 flip book options, one or more can be enabled at the same time.
        // We offset the animation start position in case two or more materials have the same flip book option combination
        
        // We have 16 x 8 -> 128 tiles of animation.
        // float offset = fmod(floor(_Offset), 128) -> Shader code.      
        m_ElectricCrackleMaterial.SetFloat(Offset, Random.Range(0.0f, 128.0f));
        
        // 2 ** 4 = 16
        byte option = (byte) Random.Range(1, 16);
        
        // m_SharedElectricCrackleMaterial has no flip book options enabled
        if ((0x1 & option) != 0)
            m_ElectricCrackleMaterial.SetFloat(FirstFlipBook, 1f);
        if ((0x1 << 1 & option) != 0)
            m_ElectricCrackleMaterial.SetFloat(SecondFlipBook, 1f);
        if ((0x1 << 2 & option) != 0)
            m_ElectricCrackleMaterial.SetFloat(ThirdFlipBook, 1f);
        if ((0x1 << 3 & option) != 0)
            m_ElectricCrackleMaterial.SetFloat(FourthFlipBook, 1f);
        
        m_LungMeshes = GetComponentsInChildren<SkinnedMeshRenderer>();
        
        //Make different breathe frequency and offset for each lung mesh
        m_Offsets = new float[m_LungMeshes.Length]; // Offset sine wave
        m_BreatheFrequency = new float[m_LungMeshes.Length];
        
        for (int i = 0; i < m_LungMeshes.Length; i++)
        {
            m_Offsets[i] = Random.Range(0f, Mathf.PI * 2f);
            m_BreatheFrequency[i] = Random.Range(1.8f, 3.2f);
        }
        
        // Per instance offset for rotation 
        m_PerInstanceOffset = Random.Range(0f, Mathf.PI * 2f);
        
        // Random scale for each instance
        //transform.localScale = Vector3.one * Random.Range(0.7f, 1.3f);
        
#if CASH_PERLIN_NOISE
        if (m_CashedPerlinNoise == null)
        {
            Debug.Log("Cashed Perlin Noise");
            m_CashedPerlinNoise = new float[m_NoiseSize * m_NoiseSize];
            
            for (int y = 0; y < m_NoiseSize; y++)
            {
                for (int x = 0; x < m_NoiseSize; x++)
                {
                    float xCoord = x / (float)m_NoiseSize;
                    float yCoord = y / (float)m_NoiseSize;
                    m_CashedPerlinNoise[y * m_NoiseSize + x] = Mathf.PerlinNoise(xCoord, yCoord);
                    
                }
            }
            
        }
#endif
    }
    
#if CASH_PERLIN_NOISE
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CoordToIndexHorizontal(float offset, int noiseSize, float speed)
    {
        int xCoord = (int)((Time.time + offset) * speed * noiseSize ) % noiseSize;
        int yCoord = Mathf.FloorToInt((Time.time + offset) * speed * noiseSize) % noiseSize;
        
        return yCoord * noiseSize + xCoord;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CoordToIndexVertical(float offset, int noiseSize, float speed)
    {
        int xCoord = (int)((Time.time + offset) * speed * noiseSize ) % noiseSize;
        int yCoord = Mathf.FloorToInt((Time.time + offset) * speed * noiseSize) % noiseSize;
        
        return xCoord * noiseSize + yCoord;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CoordToIndexDiagonal(float offset, int noiseSize, float speed)
    {
        int yCoord = Mathf.FloorToInt((Time.time + offset) * speed * noiseSize) % noiseSize;
        
        return yCoord * noiseSize + yCoord;
    }
#endif
    
    private void Update()
    {
        for (int i = 0; i < m_LungMeshes.Length; i++)
        {
            m_LungMeshes[i].SetBlendShapeWeight(0, (Mathf.Sin(Time.time * m_BreatheFrequency[i] + m_Offsets[i]) * 0.5f + 0.5f) * 100f);
        }
        
#if CASH_PERLIN_NOISE
        float x, y, z;
        
        
        if (m_UseCashedPerlinNoise)
        {
            x = m_XMaxAngle * m_CashedPerlinNoise[CoordToIndexHorizontal(m_PerInstanceOffset, m_NoiseSize, m_XSpeed)];
            y = m_YMaxAngle * m_CashedPerlinNoise[CoordToIndexVertical(m_PerInstanceOffset, m_NoiseSize, m_YSpeed)];
            z = m_ZMaxAngle * m_CashedPerlinNoise[CoordToIndexDiagonal(m_PerInstanceOffset, m_NoiseSize, m_ZSpeed)];    
        }
        else
        {
            x = m_XMaxAngle * Mathf.PerlinNoise((Time.time + m_PerInstanceOffset) * m_XSpeed, 0.0f);
            y = m_YMaxAngle * Mathf.PerlinNoise(0.0f, (Time.time + m_PerInstanceOffset) * m_YSpeed);
            z = m_ZMaxAngle * Mathf.PerlinNoise((Time.time + m_PerInstanceOffset) * m_ZSpeed, (Time.time + m_PerInstanceOffset) * m_ZSpeed);
        }
        
#else
        float x = m_XMaxAngle * Mathf.PerlinNoise((Time.time + m_PerInstanceOffset) * m_XSpeed, 0.0f);
        float y = m_YMaxAngle * Mathf.PerlinNoise(0.0f, (Time.time + m_PerInstanceOffset) * m_YSpeed);
        float z = m_ZMaxAngle * Mathf.PerlinNoise((Time.time + m_PerInstanceOffset) * m_ZSpeed, (Time.time + m_PerInstanceOffset) * m_ZSpeed);
#endif
        
        m_VirusBody.transform.localRotation = Quaternion.Euler(x, y, z); 
        
        // BobMovement
        //transform.position = new Vector3(transform.position.x, Mathf.Sin((Time.time + m_PerInstanceOffset) * 2f) * 0.26f, transform.position.z);
    }
}
