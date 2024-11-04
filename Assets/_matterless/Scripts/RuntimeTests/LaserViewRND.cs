using UnityEngine;

namespace Matterless.Floorcraft
{
	public class LaserViewRND : MonoBehaviour
	{
		
		#region Test
		[SerializeField] private int m_Resolution = 64;
		[SerializeField] private float m_LaserHitDistance = 3.0f;
		[SerializeField] private AnimationCurve m_LaserDecayCurve;
		[SerializeField] private AnimationCurve m_LaserPropagationCurve;
		[SerializeField] private AnimationCurve m_ChargeVFXScaleCurve;
		#endregion
		
		#region Inspector
		[SerializeField] private Mesh m_LaserMesh;
		[SerializeField] private Material m_LaserMaterial;
		[SerializeField] private GameObject m_ChargeFX;
		[SerializeField] private GameObject m_SplashFX;
		[ColorUsage(true, true)] [SerializeField] private Color m_LaserStartColor;
		[ColorUsage(true, true)] [SerializeField] private Color m_LaserEndColor;
		[ColorUsage(true, true)] [SerializeField] private Color m_LaserDecayColor;
		[ColorUsage(true, true)] [SerializeField] private Color m_ChargeStartColor;
		[ColorUsage(true, true)] [SerializeField] private Color m_ChargeEndColor;
		#endregion
		
		#region DrawProcedural
		private ComputeBuffer m_TrisIndexBuffer;
		private ComputeBuffer m_VertexBuffer;
		private ComputeBuffer m_NormalBuffer;
		private ComputeBuffer m_UVBuffer;
		
		private int[] m_MeshTriangles;
		private Vector3[] m_MeshVertices;
		private Vector3[] m_MeshNormals;
		private Vector2[] m_MeshUVs;
		private Bounds m_Bounds;
		#endregion
		
		#region Settings
		private float m_laserMaxLength;
		private float m_ChargeMinSize;
		private float m_ChargeMaxSize; 
		private float m_ChargeTime; 
		private float m_LaserPropagationDuration;
		private float m_LaserDecayDuration;
		private float m_LaserDuration;
		#endregion
		
		#region States
		private bool m_isCharging;
		private bool m_isFiring;
		private bool m_isDecaying;
		#endregion
		
		#region Timers
		private float m_ChargeTimer = 0.0f;
		private float m_PropagationTimer = 0.0f;
		private float m_DecayTimer = 0.0f;
		#endregion
		
		private Transform m_LaserOrigin => m_ChargeFX.transform;
		private Color m_LaserColor;
		private float m_LaserLength;
		private float m_VehicleScale = 0.05f;
		private float m_LaserAppliedScale = 0.05f; // This should be in relation to the vehilce size
		private float m_SingleSegmentSize;
		private int m_MaxSegments;
		private float m_SegmentOffset;
		private int _TextureOffsetCounter = 0;

		public void Init()
		{
			#region Init VFX Settinfs
			// Fetch these from appconfig settings
			m_laserMaxLength = m_LaserHitDistance; //settings.laserMaxLength;
			m_ChargeMinSize = 0.0f * m_VehicleScale;
			m_ChargeMaxSize = 1.0f * m_VehicleScale;
			m_ChargeTime = 1.0f;//settings.laserChargeDuration;
			m_LaserPropagationDuration = 0.3f; //settings.laserMaxLength;
			m_LaserDecayDuration = 1.0f; //settings.laserDecayDuration;
			//m_LaserDuration = m_LaserPropagationDuration + m_LaserDecayDuration;
			#endregion
			
			#region Init Mesh
			m_MeshTriangles = m_LaserMesh.triangles;
			m_MeshVertices = m_LaserMesh.vertices;
			m_MeshNormals = m_LaserMesh.normals;
			m_MeshUVs = m_LaserMesh.uv;
			m_SplashFX.GetComponent<ParticleSystem>().Stop(true);
			#endregion
		}
		
		private void TrySpawnLaser()
		{
			if (m_PropagationTimer > 0.0f || m_DecayTimer > 0.0f)
			{
				return;
			}
			
			#region Procedural Draw Init
			/* NOTE (Marko): Init procedural mesh
			 * We need to do this only once, so we do it here but we can do this on Init -> Depends on how we end up implementing this
			 * Make sure to call this only once
			 * Make sure it is disposed if we end up having this on monobehaviour
			 */
			
			m_TrisIndexBuffer = new ComputeBuffer(m_MeshTriangles.Length, sizeof(int));
			m_TrisIndexBuffer.SetData(m_MeshTriangles);
			
			m_VertexBuffer = new ComputeBuffer(m_MeshVertices.Length, sizeof(float) * 3);
			m_VertexBuffer.SetData(m_MeshVertices);
			
			m_NormalBuffer = new ComputeBuffer(m_MeshNormals.Length, sizeof(float) * 3);
			m_NormalBuffer.SetData(m_MeshNormals);
			
			m_UVBuffer = new ComputeBuffer(m_MeshVertices.Length, sizeof(float) * 2);
			m_UVBuffer.SetData(m_MeshUVs);
			
			m_LaserMaterial.SetBuffer("Indicies", m_TrisIndexBuffer);
			m_LaserMaterial.SetBuffer("Vertices",	m_VertexBuffer);
			m_LaserMaterial.SetBuffer("Normals", m_NormalBuffer);
			m_LaserMaterial.SetBuffer("UVs", m_UVBuffer);
			
			m_SingleSegmentSize = m_laserMaxLength / m_Resolution; // Ideal Segment Size as if laser is at max length
			m_Bounds = new Bounds(Vector3.zero, Vector3.one * 50); // Bounds for frustum culling
			#endregion
			
			#region Procedural Draw for current shot
			/*
			 NOTE (Marko): We adjust the laser segment size here according to hit distance,
			 so this should be done only when we know the hit distance and we want to call this only once when we start firing
			*/
			m_MaxSegments = Mathf.CeilToInt(m_LaserHitDistance / m_SingleSegmentSize); // Keep reference to this so we can use it in Update for DrawProcedural
			m_SegmentOffset = m_LaserHitDistance / m_MaxSegments;
			m_LaserMaterial.SetFloat("_Offset", m_SegmentOffset);
			
			_TextureOffsetCounter++;
			m_LaserMaterial.SetFloat("_TextureOffset", (float)_TextureOffsetCounter * 1f/16f + 1f/16f * 0.5f);
			m_LaserMaterial.SetFloat("_Decay", 0);
			
			// Set the laser transforms -> Should be something that doesn't follow the vehicle
			m_LaserMaterial.SetMatrix("LocalToWorld", transform.localToWorldMatrix * Matrix4x4.Scale(new Vector3(m_LaserAppliedScale, m_LaserAppliedScale, 1)));
			
			#endregion
		}
		
		private void DisposeBuffers()
		{
			/*
			 * NOTE (Marko): Dispose buffers when we are done with them
			 * As explained above, we can keep buffers in memory but object holding these buffers shouldn't be destroyed before desposing them
			 * Doing this here as an example, not as recommended way, someone with more knowledge should we do this
			 */
			m_TrisIndexBuffer.Dispose();
			m_VertexBuffer.Dispose();
			m_NormalBuffer.Dispose();
		}

		private void Update()
		{
			/* Note(Marko): Disregard this. It's used to set shader buffers every frame so I can work with shader while game is running
			*m_LaserMaterial.SetBuffer("Indicies", m_TrisIndexBuffer);
			m_LaserMaterial.SetBuffer("Vertices",	m_VertexBuffer);
			m_LaserMaterial.SetBuffer("Normals", m_NormalBuffer);
			m_LaserMaterial.SetFloat("_Offset", m_SegmentOffset);/*
			*/
			
			if (Input.GetMouseButtonDown(0))
			{
				m_isCharging = true;
			}

	        if (m_isCharging)
	        {
	            if (m_ChargeTimer < m_ChargeTime)
	            {
		            m_ChargeFX.SetActive(true);
		            float t = m_ChargeVFXScaleCurve.Evaluate(m_ChargeTimer / m_ChargeTime);
		            //m_ChargeFX.GetComponent<Renderer>().material.SetColor("_Color", Color.Lerp(m_ChargeStartColor, m_ChargeEndColor, Mathf.Clamp01(t * 2 - 1)));
		            m_ChargeFX.transform.localScale = Vector3.one * (t * m_ChargeMaxSize);//Vector3.Lerp(Vector3.one * m_ChargeMinSize, Vector3.one * m_ChargeMaxSize, m_ChargeTimer / m_ChargeTime);
	                m_ChargeTimer += Time.deltaTime;
	                return;
	            }
	            
	            m_ChargeFX.SetActive(false);
	            m_isFiring = true;
	            m_isCharging = false;
	            m_ChargeTimer = 0.0f;
	        }
	        
	        if (m_isFiring)
	        {
		        TrySpawnLaser(); // I would rather have stat machine instead of calling this every frame -> We would call this only on EnterState
	            if (m_PropagationTimer < m_LaserPropagationDuration + 0.25f)
	            {
	                // NOTE (Marko): Laser Propagation adjusted by animation curve -> probably not needed
	                float t = m_LaserPropagationCurve.Evaluate(m_PropagationTimer / m_LaserPropagationDuration);
	                float propagationDistance = Mathf.Lerp(0.0f, m_laserMaxLength, Mathf.Clamp01(t));
	                
	                m_LaserLength = Mathf.Min(propagationDistance, m_LaserHitDistance);

	                if (propagationDistance >= m_LaserHitDistance)
	                {
		                if(!m_SplashFX.GetComponent<ParticleSystem>().isPlaying);
							m_SplashFX.GetComponent<ParticleSystem>().Play(true);
		                m_SplashFX.transform.position = m_LaserOrigin.position + m_LaserOrigin.forward * m_LaserHitDistance;
		                m_SplashFX.transform.localScale = Vector3.one * 0.025f;
		                
	                }
	                else
	                {
		                m_SplashFX.GetComponent<ParticleSystem>().Stop(true);
	                }
	                
	                m_LaserColor = Color.Lerp(m_LaserStartColor, m_LaserEndColor, t);
	                
	                // Always set material properties before drawing
	                m_LaserMaterial.SetColor("_Color", m_LaserColor);
	                m_LaserMaterial.SetFloat("_Distance", m_LaserLength);
	                m_LaserMaterial.SetFloat("_Decay", 0);
	                
	                
	                // Draw the laser
	                int numberOfSegments = (int) Mathf.Lerp(0, m_MaxSegments, Mathf.Clamp01(propagationDistance / m_LaserHitDistance));
	                Graphics.DrawProcedural(m_LaserMaterial, m_Bounds, MeshTopology.Triangles, m_TrisIndexBuffer.count, numberOfSegments);
	                
	                m_PropagationTimer += Time.deltaTime;
	                return;
	            }
	            
	            m_SplashFX.GetComponent<ParticleSystem>().Stop();
	            m_isFiring = false;
	            m_isDecaying = true;
	            m_PropagationTimer = 0.0f;
	        }
	        
	        if (m_isDecaying)
	        {
	            if (m_DecayTimer < m_LaserDecayDuration)
	            {
	                // Always set material properties before drawing
	                float t = m_LaserDecayCurve.Evaluate(m_DecayTimer/m_LaserDecayDuration);
	                m_LaserMaterial.SetFloat("_Decay", t);
		            
	                // Draw the laser
	                Graphics.DrawProcedural(m_LaserMaterial, m_Bounds, MeshTopology.Triangles, m_TrisIndexBuffer.count,
		                m_MaxSegments);

	                
	                m_DecayTimer += Time.deltaTime;
	                return;
	            }
	            
	            m_isDecaying = false;
	            m_DecayTimer = 0.0f;
	            DisposeBuffers();
	        }
		}
	}
}