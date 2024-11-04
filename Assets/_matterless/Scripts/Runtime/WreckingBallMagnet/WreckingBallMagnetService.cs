using System.Collections.Generic;
using Matterless.Inject;
using UnityEngine;
using UnityEngine.Serialization;
namespace Matterless.Floorcraft
{
	public class WreckingBallMagnetService : ITickable
	{
		[System.Serializable]
		public class Settings : IEquipmentSetting
		{
			public IAsset wreckingBallProjectile => m_WreckingBallProjectile;
			public float speed => m_Speed;
			public int quantity => m_Quantity;
			public float cooldown => m_Cooldown;
			public float duration => m_Duration;
			public string magnetViewResourcePath => m_MagnetViewResourcePath;
			public Vector3 wreckingBallOffset => m_WreckingBallOffset;
			public bool infinite => m_Infinite;
			[SerializeField] private Vector3 m_WreckingBallOffset;
			[SerializeField] private Asset m_WreckingBallProjectile;
			[SerializeField] private float m_Speed;
			[FormerlySerializedAs("m_CooldownDuration")][SerializeField] private float m_Cooldown;
			[SerializeField] private string m_MagnetViewResourcePath;
			[SerializeField] private int m_Quantity;
			[SerializeField] private float m_Duration;
			[SerializeField] private bool m_Infinite = false;
		}

		// dependencies
		private readonly SpeederHUDService m_SpeederHUDService;
		private readonly IAukiWrapper m_AukiWrapper;
		private readonly SpeederService m_SpeederService;
		private readonly PropertiesComponentService m_PropertiesEcsService;
		private readonly TransformComponentService m_TransformService;
		private readonly MessageComponentService m_MessageComponentService;
		private readonly Settings m_Settings;
		private readonly PropertiesECSService.Settings m_AssetSettings;
		private readonly IRaycastService m_RaycastService;
		private readonly List<uint> m_LocalEntityIds = new();
		private readonly EquipmentService m_EquipmentService;
		private readonly CooldownService m_CooldownService;

		// local variables
		private List<WreckingBallProjectileServer> m_WreckingBalls = new();
		private MagnetView m_MagnetView;

		public WreckingBallMagnetService(IAukiWrapper aukiWrapper,
			SpeederHUDService speederHUDService,
			PropertiesComponentService propertiesEcsService,
			TransformComponentService transformEcsService,
			MessageComponentService messageComponentService,
			SpeederService speederService,
			Settings settings,
			IRaycastService raycastService,
			EquipmentService equipmentService,
			CooldownService cooldownService,
			PropertiesECSService.Settings assetSettings
		)
		{
			m_AssetSettings = assetSettings;
			m_SpeederService = speederService;
			m_SpeederHUDService = speederHUDService;
			m_RaycastService = raycastService;
			m_AukiWrapper = aukiWrapper;
			m_PropertiesEcsService = propertiesEcsService;
			m_TransformService = transformEcsService;
			m_MessageComponentService = messageComponentService;
			m_Settings = settings;
			m_EquipmentService = equipmentService;
			m_CooldownService = cooldownService;
			m_PropertiesEcsService.onComponentAdded += OnPropertiesComponentAdded;
			m_TransformService.onComponentAdded += OnTransformComponentAdded;
			m_AukiWrapper.onEntityDeleted += OnAukiEntityDeleted;
		}
		private void OnAukiEntityDeleted(uint entityId)
		{
			if (!TryGetWreckingBall(entityId, out WreckingBallProjectileServer wreckingBall))
				return;

			m_LocalEntityIds.Remove(entityId);
			m_WreckingBalls.Remove(wreckingBall);
			wreckingBall.Explode();
			wreckingBall.Dispose();
		}
		private void OnTransformComponentAdded(TransformComponentModel component)
		{
			if (m_SpeederService.TryGetSpeederView(component.entityId, out var speederView)
				&& speederView.entityId != m_SpeederService.serverSpeederEntity) // new speeder which is not ours.
			{
				speederView.wreckingBallView.triggerEvent.AddListener((collider) => OnCollideWithWreckingBall(collider, speederView.entityId)); // when something hits the wrecking ball.
			}
			
			if (!TryGetWreckingBall(component.entityId, out WreckingBallProjectileServer wreckingBall))
				return;

			// add the floor raycast to set the correct position
			if (m_RaycastService.FloorRaycast(component.model.position, out Vector3 position, out _))
			{
				wreckingBall.view.transform.SetPositionAndRotation(position + Vector3.up * 0.002f,
					component.model.rotation);
			}
			else
			{
				wreckingBall.view.transform.SetPositionAndRotation(component.model.position, component.model.rotation);
			}
			wreckingBall.Start();
		}
		private bool TryGetWreckingBall(uint entityId, out WreckingBallProjectileServer returnValue)
		{
			foreach (var wreckingBall in m_WreckingBalls)
			{
				if (wreckingBall.entityId == entityId)
				{
					returnValue = wreckingBall;
					return true;
				}
			}

			returnValue = null;
			return false;
		}
		private void OnPropertiesComponentAdded(PropertiesComponentModel model)
		{
			if (Time.time - m_AukiWrapper.joinTimestamp < 1)
				return; //don't spawn ghost balls
			
			var asset = m_AssetSettings.GetAsset(model.model.id);

			if (asset.assetType == AssetType.Obstacle)
				return;

			if (asset.assetType == AssetType.Projectile)
			{
				var wreckingBall = new WreckingBallProjectileServer();
				m_WreckingBalls.Add(wreckingBall);
				var wreckingBallProjectileView = m_PropertiesEcsService
					.GetGameObject(model.entityId)
					.GetComponent<WreckingBallProjectileView>();
			
				wreckingBall.Init(model.entityId,wreckingBallProjectileView, m_MessageComponentService, m_SpeederService,
					m_LocalEntityIds.Contains(model.entityId), m_AukiWrapper, m_Settings);
			}
		}
		
		void OnCollideWithWreckingBall(Collider collider, uint entityId)
		{
			if (collider.gameObject.GetComponent<GameObjectView>() is GameObjectView view)
			{
				if (!m_SpeederService.TryGetSpeederView(view.entityId, out var speederView))
				{
					//
					//
					// We did not collide with a speeder
					return;
				}
				m_MessageComponentService.SendMessage(m_SpeederService.serverSpeederEntity, MessageModel.Message.Kill, view.entityId);
			}
		}

		public void Tick(float deltaTime, float unscaledDeltaTime)
		{
			/*foreach (var wreckingBall in m_WreckingBalls)
			{
				wreckingBall.Update();
			}

			var serverSpeederEntity = m_SpeederService.serverSpeederEntity;

			if (!m_EquipmentService.TryGetComponentModel(serverSpeederEntity, out EquipmentStateComponentModel equipmentState))
				return; // we are not in game or not inited
			
			foreach (var kvpA in m_SpeederService.speederViews)
			{
				foreach (var kvpB in m_SpeederService.speederViews)
				{
					if (kvpA.Key == serverSpeederEntity || kvpB.Key == serverSpeederEntity)
					{
						//
						//
						// We don't want to ignore collisions between server speeder and a held wrecking ball
						continue;
					}
					// ignore all other collisions
					Physics.IgnoreCollision(kvpA.Value.wreckingBallView.sphereCollider, kvpB.Value.wreckingBallView.sphereCollider);	
				}
			}
			
			if (equipmentState.model.state == EquipmentState.Magnet)
			{
				
				if (!m_CooldownService.inCooldown && m_EquipmentService.GetUses(serverSpeederEntity) > 0)
				{
					m_EquipmentService.SetState(serverSpeederEntity, EquipmentState.MagnetAndWreckingBall);
				}
			}
			else if (equipmentState.model.state == EquipmentState.MagnetAndWreckingBall && !m_CooldownService.inCooldown)
			{
				if (m_SpeederHUDService.tapScreenInput) FireWreckingBall();	
			}*/
		}
		private void FireWreckingBall()
		{
			m_EquipmentService.Use(m_SpeederService.serverSpeederEntity);
			m_CooldownService.ActivateCooldown();
			var serverSpeederEntity = m_SpeederService.serverSpeederEntity;
			m_EquipmentService.SetState(serverSpeederEntity, EquipmentState.Magnet);
			var position = m_SpeederService.speederViews[m_SpeederService.serverSpeederEntity].wreckingBallView.transform.position;
			var rotation = m_SpeederService.serverSpeeder.rotation;
			rotation *= Quaternion.Euler(0,180,0);
			m_AukiWrapper.AddEntity(new Pose(position, rotation), false, entity =>
			{
				m_LocalEntityIds.Add(entity.Id);
				m_PropertiesEcsService.AddComponent(entity.Id, new PropertiesModel(m_Settings.wreckingBallProjectile.id));
				m_TransformService.AddComponent(entity.Id, new TransformModel(position, rotation, 0));
				m_MessageComponentService.AddComponent(entity.Id,
					new MessageModel(MessageModel.Message.None, entity.Id));
			}, _ => { Debug.LogError("Error creating wrecking ball entity"); });
		}
	}
}
