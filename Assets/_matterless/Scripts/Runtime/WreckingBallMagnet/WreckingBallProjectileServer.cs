using UnityEngine;
namespace Matterless.Floorcraft
{
	public class WreckingBallProjectileServer
	{
		public uint entityId => m_EntityId;
		public WreckingBallProjectileView view => m_View;

		private SpeederService m_SpeederService;
		private MessageComponentService m_MessageComponentService;
		private WreckingBallMagnetService m_WreckingBallMagnetService;
		private WreckingBallMagnetService.Settings m_Settings;
		
		private uint m_EntityId;
		private WreckingBallProjectileView m_View;
		private float m_ElapsedTime;
		private float m_Threshold = 3f;
		
		private bool m_IsServer;
		private IAukiWrapper m_AukiWrapper;
		public void Init(uint entityId, WreckingBallProjectileView view,
			MessageComponentService messageComponentService,
			SpeederService speederService,
			bool isServer,
			IAukiWrapper aukiWrapper,
			WreckingBallMagnetService.Settings settings)
		{
			m_View = view;
			m_EntityId = entityId;
			m_Settings = settings;
			m_SpeederService = speederService;
			m_View.triggerEvent.AddListener(OnTriggerEnter);
			m_MessageComponentService = messageComponentService;
			m_View.gameObject.SetActive(true);
			m_IsServer = isServer;
			m_AukiWrapper = aukiWrapper;
		}

		public void Start()
		{
			m_ElapsedTime = 0;
			m_View.rigidBody.velocity = m_View.rigidBody.transform.forward * m_Settings.speed;
			UpdateCollisionIgnores();
		}

		void OnTriggerEnter(Collider collider)
		{
			GameObjectView view = collider.gameObject.GetComponent<GameObjectView>();
			if (view is PowerUpSpawnPointView || view is ObstacleView) //ignore power up spawn points and obstacles.
				return;
			
			m_MessageComponentService.SendMessage(view.entityId, MessageModel.Message.Kill, m_EntityId);
		}

		public void Update()
		{
			if (m_ElapsedTime < m_Threshold && m_ElapsedTime + Time.deltaTime > m_Threshold && m_IsServer)
			{
				if (m_IsServer)
				{
					m_AukiWrapper.DeleteEntity(m_EntityId, OnDeleteCallback);
					Explode();
					Dispose();	
				}
			}
			m_ElapsedTime += Time.deltaTime;
			UpdateCollisionIgnores();
		}

		void OnDeleteCallback()
		{
			
		}

		// If we fired the wrecking ball (m_IsServer)
		// We don't want to collide since we cannot shoot ourselves.
		//
		// or if is not a server speeder then we are not server and don't want to collide since
		// we will get kill message from the real server (remote speeder)
		// 
		// So, we only have the collision left that is your current speeder and enemy projectiles.
		void UpdateCollisionIgnores()
		{
			foreach (var kvp in m_SpeederService.speederViews)
			{
				if (m_IsServer || kvp.Value.entityId != m_SpeederService.serverSpeederEntity)
				{
					Physics.IgnoreCollision(m_View.sphereCollider, kvp.Value.boxCollider);
				}
			}
			
			if (!m_SpeederService.TryGetSpeederView(m_SpeederService.serverSpeederEntity, out var speederView))
				return;
			
			var serverWreckingBallViewCollider = speederView.wreckingBallView.sphereCollider;
			Physics.IgnoreCollision(m_View.sphereCollider, serverWreckingBallViewCollider);
		}
		public void Dispose()
		{
			m_View.triggerEvent.RemoveListener(OnTriggerEnter);
			Object.Destroy(m_View);
		}
		public void Explode()
		{
			if (m_View != null) m_View.Explode();
			
			if (!m_SpeederService.TryGetSpeederView(m_SpeederService.serverSpeederEntity, out var speederView))
				return;

			if (Vector3.Distance(m_View.transform.position, speederView.transform.position) < 0.150f)
			{
				m_MessageComponentService.SendMessage(speederView.entityId, MessageModel.Message.Kill, m_EntityId);
			}
		}
	}
}