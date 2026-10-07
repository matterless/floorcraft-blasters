#if UNITY_EDITOR
	//#define MARKO_DEBUG_BOUNDS
#endif

using System;
using UnityEngine;
using UnityEngine.Rendering;

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

		#region Segments Mesh

		// One mesh holding a copy of m_LaserMesh per segment; uv2.x is the copy's segment index.
		// The shader reads plain vertex attributes because Mali GPUs on OpenGL ES cannot read
		// compute buffers in a vertex shader, which the earlier DrawProcedural version needed.
		private Mesh m_SegmentsMesh;
		private int m_SegmentsMeshCapacity;

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
		private readonly int m_SegmentCountID = Shader.PropertyToID("_SegmentCount");

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

			m_SingleSegmentSize = m_LaserMaxLength / m_Resolution; // Ideal Segment Size as if laser is at max length

            this.gameObject.SetActive(true);
        }

		public float HitDistance
		{
			get => m_LaserHitDistance;
			set => m_LaserHitDistance = value;
		}

		// Segments drawn by the full laser (inversely scaled by world scale, see the hack note above);
		// fewer are drawn while it propagates
		private int MaxDrawnSegments => (int)(m_MaxSegments * 1f/m_WorldScale);

		/// <summary>Makes sure m_SegmentsMesh holds at least <paramref name="segments"/> copies of m_LaserMesh.</summary>
		private void EnsureSegmentsMesh(int segments)
		{
			if (m_SegmentsMesh != null && m_SegmentsMeshCapacity >= segments)
				return;

			int vertexCount = m_MeshVertices.Length;
			int indexCount = m_MeshTriangles.Length;
			var vertices = new Vector3[vertexCount * segments];
			var normals = new Vector3[vertexCount * segments];
			var uvs = new Vector2[vertexCount * segments];
			var segmentIndices = new Vector2[vertexCount * segments];
			var triangles = new int[indexCount * segments];

			for (int segment = 0; segment < segments; segment++)
			{
				int vertexOffset = segment * vertexCount;
				Array.Copy(m_MeshVertices, 0, vertices, vertexOffset, vertexCount);
				Array.Copy(m_MeshNormals, 0, normals, vertexOffset, vertexCount);
				Array.Copy(m_MeshUVs, 0, uvs, vertexOffset, vertexCount);

				for (int i = 0; i < vertexCount; i++)
					segmentIndices[vertexOffset + i] = new Vector2(segment, 0);

				for (int i = 0; i < indexCount; i++)
					triangles[segment * indexCount + i] = m_MeshTriangles[i] + vertexOffset;
			}

			if (m_SegmentsMesh == null)
				m_SegmentsMesh = new Mesh { name = "LaserSegments" };
			else
				m_SegmentsMesh.Clear();

			m_SegmentsMesh.indexFormat = vertices.Length > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16;
			m_SegmentsMesh.vertices = vertices;
			m_SegmentsMesh.normals = normals;
			m_SegmentsMesh.uv = uvs;
			m_SegmentsMesh.uv2 = segmentIndices;
			m_SegmentsMesh.SetTriangles(triangles, 0, false);
			m_SegmentsMeshCapacity = segments;
		}

		/// <summary>Draws the first <paramref name="segments"/> laser segments this frame.</summary>
		private void DrawSegments(int segments)
		{
			m_LaserMaterialInstance.SetFloat(m_SegmentCountID, segments);
			// The shader places the vertices in world space itself, so the mesh is drawn untransformed.
			// No shadows: they would come from the untransformed mesh.
			Graphics.DrawMesh(m_SegmentsMesh, Matrix4x4.identity, m_LaserMaterialInstance, 0, null, 0, null,
				ShadowCastingMode.Off, false);
		}

		private void OnDestroy()
		{
			if (m_SegmentsMesh != null)
				Destroy(m_SegmentsMesh);
		}

		private void Update()
		{
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
					DrawSegments(numberOfSegments);


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
					DrawSegments(MaxDrawnSegments);


					m_DecayTimer += Time.deltaTime;
					return;
				}
				m_CapsuleCollider.gameObject.SetActive(false);

				m_IsDecaying = false;
				m_DecayTimer = 0.0f;
				m_Finished?.Invoke(this);
			}
		}

		public void StartFiring()
		{
			m_IsFiring = true;

			#region Segments for current shot

			/*
			 NOTE (Marko): We adjust the laser segment size here according to hit distance,
			 so this should be done only when we know the hit distance and we want to call this only once when we start firing
			*/
			m_MaxSegments = Mathf.CeilToInt(m_LaserHitDistance / m_SingleSegmentSize); // Keep reference to this so we can use it in Update for DrawSegments
			m_SegmentOffset = m_LaserHitDistance / m_MaxSegments;
			EnsureSegmentsMesh(MaxDrawnSegments);
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
			// drawn with an identity matrix, so the mesh bounds are the world bounds
			m_SegmentsMesh.bounds = m_Bounds;

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