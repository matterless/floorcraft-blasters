#if UNITY_EDITOR
	//#define MARKO_DEBUG_BOUNDS
#endif

using System;
using UnityEngine;

//NOTE (Marko) : LaserBeamView is hacked to fix the issue with the WorldScale and avoid changing shader to account for the world scale
//HACK: Treat laser as if its actually 3 meters long (Default laser length)
// Scale the laser down by the world scale at the stage we set TRS Matrix
// Inversely scale everything we send to the shader by the world scale as well as the maximum amount of laser segments

namespace Matterless.Floorcraft
{
	public class LaserBeamView : MonoBehaviour, IPoolable
	{

		#region Test

		[SerializeField] private AnimationCurve m_LaserDecayCurve;
		[SerializeField] private AnimationCurve m_LaserPropagationCurve;
		[SerializeField] private LaserCollider m_CapsuleCollider;

		#endregion

		#region Inspector

		[SerializeField] private Mesh m_LaserMesh;
		[SerializeField] private Material m_LaserMaterial;
		[SerializeField] private ParticleSystem m_SplashFX;
		
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

		private float m_LaserMaxLength;
		private float m_LaserPropagationDuration;
		private float m_LaserDecayDuration;
		private float m_LaserDuration;
		private float m_HoldDuration;
		private int m_Resolution;
		
		private Color m_LaserStartColor;
		private Color m_LaserEndColor;
		private Color m_LaserDecayColor;
		#endregion

		#region States

		private bool m_IsFiring;
		private bool m_IsDecaying;

		#endregion

		#region Timers

		private float m_PropagationTimer = 0.0f;
		private float m_DecayTimer = 0.0f;

		#endregion

		public Vector3 OriginPosition { get; private set; }
		public Quaternion OriginRotation { get; private set; }
		
		private float m_LaserHitDistance;
		private Color m_LaserColor;
		private float m_LaserLength;
		private float m_OriginScale; //0.05f;
		private float m_WorldScale;
		private readonly float m_DefaultCapsuleColliderRadius = 2;
		private float m_SingleSegmentSize;
		private int m_MaxSegments;
		private float m_SegmentOffset;
		private static int s_TextureOffsetCounter = 0;
		private Action<LaserBeamView> m_Finished;
		private Material m_LaserMaterialInstance;
		
		private readonly int m_LocalToWorldMatrixID = Shader.PropertyToID("_LocalToWorld");
		private readonly int m_LaserColorID = Shader.PropertyToID("_Color");
		private readonly int m_LaserDistanceID = Shader.PropertyToID("_Distance");
		private readonly int m_LaserTextureOffsetID = Shader.PropertyToID("_TextureOffset");
		private readonly int m_LaserDecayID = Shader.PropertyToID("_Decay");
		private readonly int m_SegmentOffsetID = Shader.PropertyToID("_Offset");
		private readonly int m_TrailColorID = Shader.PropertyToID("_TrailColor");
		
		#region Debug
	#if MARKO_DEBUG_BOUNDS
		void DrawBounds(Bounds b, float delay=0)
		{
			// bottom
			var p1 = new Vector3(b.min.x, b.min.y, b.min.z);
			var p2 = new Vector3(b.max.x, b.min.y, b.min.z);
			var p3 = new Vector3(b.max.x, b.min.y, b.max.z);
			var p4 = new Vector3(b.min.x, b.min.y, b.max.z);

			Debug.DrawLine(p1, p2, Color.blue, delay);
			Debug.DrawLine(p2, p3, Color.red, delay);
			Debug.DrawLine(p3, p4, Color.yellow, delay);
			Debug.DrawLine(p4, p1, Color.magenta, delay);

			// top
			var p5 = new Vector3(b.min.x, b.max.y, b.min.z);
			var p6 = new Vector3(b.max.x, b.max.y, b.min.z);
			var p7 = new Vector3(b.max.x, b.max.y, b.max.z);
			var p8 = new Vector3(b.min.x, b.max.y, b.max.z);

			Debug.DrawLine(p5, p6, Color.blue, delay);
			Debug.DrawLine(p6, p7, Color.red, delay);
			Debug.DrawLine(p7, p8, Color.yellow, delay);
			Debug.DrawLine(p8, p5, Color.magenta, delay);

			// sides
			Debug.DrawLine(p1, p5, Color.white, delay);
			Debug.DrawLine(p2, p6, Color.gray, delay);
			Debug.DrawLine(p3, p7, Color.green, delay);
			Debug.DrawLine(p4, p8, Color.cyan, delay);
		}
	#endif
		#endregion
		
		public void Init(uint id, LaserService.Settings settings, float scale, float worldScale, Transform origin,
			Action<LaserBeamView> finished)
		{
			m_LaserMaterialInstance = Instantiate(m_LaserMaterial);
			OriginPosition = origin.position;
			OriginRotation = origin.rotation;

			m_OriginScale = scale;
			m_WorldScale = worldScale;
			
		    m_LaserStartColor = settings.laserStartColor;
		    m_LaserEndColor = settings.laserEndColor;
		    m_LaserDecayColor = settings.laserDecayColor;

			#region Init VFX Settings

			// Fetch these from appconfig settings
			m_Resolution = settings.laserResolution;
			// NOTE(Marko): Actually hack starts here
			// m_LaserMaxLength was scaled by the world scale, instead we scale initial laser hit distance
			m_LaserMaxLength = settings.laserMaxLength;
			m_LaserHitDistance = m_LaserMaxLength * m_WorldScale;
			m_LaserPropagationDuration = settings.laserPropagationDuration;
			m_HoldDuration = settings.laserAttackDuration;
			m_LaserDecayDuration = settings.laserDecayDuration;

			#endregion

			#region Init Mesh

			m_MeshTriangles = m_LaserMesh.triangles;
			m_MeshVertices = m_LaserMesh.vertices;
			m_MeshNormals = m_LaserMesh.normals;
			m_MeshUVs = m_LaserMesh.uv;

			#endregion

			m_SplashFX.Stop(true);
			m_CapsuleCollider.Init(id);
			m_Finished = finished;

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

			m_LaserMaterialInstance.SetBuffer("Indicies", m_TrisIndexBuffer);
			m_LaserMaterialInstance.SetBuffer("Vertices", m_VertexBuffer);
			m_LaserMaterialInstance.SetBuffer("Normals", m_NormalBuffer);
			m_LaserMaterialInstance.SetBuffer("UVs", m_UVBuffer);

			m_SingleSegmentSize = m_LaserMaxLength / m_Resolution; // Ideal Segment Size as if laser is at max length


            #endregion

            this.gameObject.SetActive(true);
        }

		public float HitDistance
		{
			get => m_LaserHitDistance;
			set => m_LaserHitDistance = value;
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
			m_UVBuffer.Dispose();
		}

		private void Update()
		{
			/* Note(Marko): Disregard this. It's used to set shader buffers every frame so I can work with shader while game is running
			m_LaserMaterial.SetBuffer("Indicies", m_TrisIndexBuffer);
			m_LaserMaterial.SetBuffer("Vertices",	m_VertexBuffer);
			m_LaserMaterial.SetBuffer("Normals", m_NormalBuffer);
			m_LaserMaterial.SetFloat("_Offset", m_SegmentOffset);
			*/

			if (m_IsFiring)
			{
				if (m_PropagationTimer < m_LaserPropagationDuration + m_HoldDuration)
				{
					// NOTE (Marko): Laser Propagation adjusted by animation curve -> probably not needed
					float t = m_LaserPropagationCurve.Evaluate(m_PropagationTimer / m_LaserPropagationDuration);
					// NOTE: (Marko) Part of the hack, we scale the max laser length by world scale here instead on init
					float propagationDistance = Mathf.Lerp(0.0f, m_LaserMaxLength * m_WorldScale, Mathf.Clamp01(t));

					m_LaserLength = Mathf.Min(propagationDistance, m_LaserHitDistance);

					if (propagationDistance >= m_LaserHitDistance)
					{
						if(!m_SplashFX.isPlaying);
							m_SplashFX.Play(true);
							
						m_SplashFX.transform.position =
							OriginPosition + OriginRotation * Vector3.forward * m_LaserHitDistance;
						m_SplashFX.transform.localScale = Vector3.one * (m_OriginScale * 0.5f * m_WorldScale);
					}
					else
					{
						m_SplashFX.Stop(true);
					}

					m_LaserColor = Color.Lerp(m_LaserStartColor, m_LaserEndColor, t);

					// Always set material properties before drawing
					m_LaserMaterialInstance.SetColor(m_LaserColorID, m_LaserColor);
					m_LaserMaterialInstance.SetFloat(m_LaserDistanceID, m_LaserLength * 1f/m_WorldScale);
					m_LaserMaterialInstance.SetFloat(m_LaserDecayID, 0);

					// Draw the laser
					// NOTE(Marko): Part of the hack, inversly scale the number of segments by world scale
					int numberOfSegments = (int)Mathf.Lerp(0, m_MaxSegments * 1f/m_WorldScale,
						Mathf.Clamp01(propagationDistance / m_LaserHitDistance));
					Graphics.DrawProcedural(m_LaserMaterialInstance, m_Bounds, MeshTopology.Triangles, m_TrisIndexBuffer.count,
						numberOfSegments);


					m_CapsuleCollider.capsuleCollider.center = new Vector3(0, 0, m_LaserLength / (m_OriginScale * 2f));
					m_CapsuleCollider.capsuleCollider.height = m_LaserLength / m_OriginScale;

					m_PropagationTimer += Time.deltaTime;
					return;
				}

				m_SplashFX.Stop();
				m_IsFiring = false;
				m_IsDecaying = true;
				m_PropagationTimer = 0.0f;
			}

			if (m_IsDecaying)
			{
				if (m_DecayTimer < m_LaserDecayDuration)
				{
					// Always set material properties before drawing
					float t = m_LaserDecayCurve.Evaluate(m_DecayTimer / m_LaserDecayDuration);
					m_LaserMaterialInstance.SetFloat(m_LaserDecayID, t);

					m_CapsuleCollider.capsuleCollider.center = new Vector3(0, 0, m_LaserLength * t + m_LaserLength) / (m_OriginScale * 2f);
					m_CapsuleCollider.capsuleCollider.height = (1-t) * m_LaserLength / m_OriginScale;

					// Draw the laser
					Graphics.DrawProcedural(
						m_LaserMaterialInstance, 
						m_Bounds, 
						MeshTopology.Triangles, 
						m_TrisIndexBuffer.count, 
						(int) (m_MaxSegments * 1f/m_WorldScale));


					m_DecayTimer += Time.deltaTime;
					return;
				}
				m_CapsuleCollider.gameObject.SetActive(false);

				m_IsDecaying = false;
				m_DecayTimer = 0.0f;
				DisposeBuffers();
				m_Finished?.Invoke(this);
			}
		}

		public void StartFiring()
		{
			m_IsFiring = true;

			#region Procedural Draw for current shot

			/*
			 NOTE (Marko): We adjust the laser segment size here according to hit distance,
			 so this should be done only when we know the hit distance and we want to call this only once when we start firing
			*/
			m_MaxSegments = Mathf.CeilToInt(m_LaserHitDistance / m_SingleSegmentSize); // Keep reference to this so we can use it in Update for DrawProcedural
			m_SegmentOffset = m_LaserHitDistance / m_MaxSegments;
			m_LaserMaterialInstance.SetFloat(m_SegmentOffsetID, m_SegmentOffset);

			s_TextureOffsetCounter++;
			m_LaserMaterialInstance.SetFloat(m_LaserTextureOffsetID, (float)s_TextureOffsetCounter * 1f/16f + 1f/16f * 0.5f);
			
			m_LaserMaterialInstance.SetFloat(m_LaserDecayID, 0);
			m_LaserMaterialInstance.SetColor(m_TrailColorID, m_LaserDecayColor);

			Matrix4x4 laserTransform = new();
			laserTransform.SetTRS(OriginPosition, OriginRotation, Vector3.one * m_WorldScale);
			laserTransform *= Matrix4x4.Scale(new Vector3(m_OriginScale * 1.5f, m_OriginScale * 1.5f, 1));
			
			// Bounds for frustum culling
			m_Bounds = new Bounds(OriginPosition + OriginRotation * Vector3.forward * (m_LaserHitDistance * 0.5f), new Vector3(0.25f, 0.25f,0.25f)); 
			m_Bounds.Encapsulate(OriginPosition);
			m_Bounds.Encapsulate(OriginPosition + OriginRotation * Vector3.forward * m_LaserHitDistance);
			
		#if MARKO_DEBUG_BOUNDS
			DrawBounds(m_Bounds, 5.0f);
		#endif

			// Set the laser transforms -> Should be something that doesn't follow the vehicle
			m_LaserMaterialInstance.SetMatrix(m_LocalToWorldMatrixID, laserTransform);

			#endregion

			m_CapsuleCollider.transform.localScale *= m_OriginScale;
			m_CapsuleCollider.capsuleCollider.radius = m_DefaultCapsuleColliderRadius * m_WorldScale;
			m_CapsuleCollider.gameObject.SetActive(true);
		}

		public void OnPop()
		{
		}

		public void OnPush()
		{
			this.gameObject.SetActive(false);
			
			// Taking collider to original scale since we are using the same laser with pooling and the scale will be overriden next time we use this instance
			m_CapsuleCollider.transform.localScale = new Vector3(1, 1, 1);
		}
	}
}