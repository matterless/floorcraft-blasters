using System;
using System.Collections.Generic;
using Matterless.Inject;
using UnityEngine;

namespace Matterless.Floorcraft
{
	public class PowerUpSpawnPointService : GenericComponentService<SpawnPointStateComponentModel, SpawnPointCooldownStateModel>, ITickable
	{
		[Serializable]
		public class Settings
		{
			public float cooldownDuration => m_CooldownDuration;
			[SerializeField] private float m_CooldownDuration = 10f;
		}
		private readonly PropertiesECSService.Settings m_AssetSettings;
		private readonly IAukiWrapper m_AukiWrapper;
		private readonly IRaycastService m_RaycastService;
		private readonly PropertiesComponentService m_PropertiesEcsService;
		private readonly TransformComponentService m_TransformService;
		private readonly SpeederService m_SpeederService;
		private readonly SpeederHUDService m_SpeederHUDService;
		private readonly EquipmentService m_EquipmentStateComponentService;
		private readonly CooldownService m_CooldownService;
		private readonly Settings m_Settings;
		private readonly MessageComponentService m_MessageComponentService;

		private readonly Dictionary<AssetId, EquipmentState> assetToPowerUpDictionary = new Dictionary<AssetId, EquipmentState> {
			{AssetId.LaserSpawnPoint, EquipmentState.Laser}, 
			{AssetId.FlameThrowerPowerUpSpawnPoint, EquipmentState.Flamethrower}, 
			{AssetId.WreckingBallMagnetSpawnPoint, EquipmentState.MagnetAndWreckingBall}, 
			{AssetId.DashAttackSpawnPoint, EquipmentState.Dash},
		};
		
		private List<PowerUpSpawnPointView> m_Views = new();
		private EquipmentState? m_PowerUpToEquip;
		private List<SpawnPointCooldown> m_Cooldowns = new();
		private List<uint> keysToClear = new();
		public PowerUpSpawnPointService(IECSController ecsController, IComponentModelFactory componentModelFactory,
			IAukiWrapper aukiWrapper,
			IRaycastService raycastService,
			PropertiesComponentService propertiesComponentService,
			MessageComponentService messageComponentService,
			TransformComponentService transformService,
			SpeederService speederService,
			SpeederHUDService speederHUDService,
			EquipmentService equipmentStateComponentService,
			CooldownService cooldownService,
			PowerUpSpawnPointService.Settings settings,
			PropertiesECSService.Settings assetSettings) : base(ecsController, componentModelFactory)
		{
			m_AssetSettings = assetSettings;
			m_AukiWrapper = aukiWrapper;
			m_RaycastService = raycastService;
			m_PropertiesEcsService = propertiesComponentService;
			m_TransformService = transformService;
			m_SpeederService = speederService;
			m_SpeederHUDService = speederHUDService;

			m_EquipmentStateComponentService = equipmentStateComponentService;
			m_CooldownService = cooldownService;
			m_Settings = settings;
			m_MessageComponentService = messageComponentService;
			m_PropertiesEcsService.onComponentAdded += OnPropertiesComponentAdded;
			m_TransformService.onComponentAdded += OnTransformComponentAdded;
			m_AukiWrapper.onEntityDeleted += OnAukiEntityDeleted;
			m_AukiWrapper.onLeft += OnAukiLeft;
		}
		private void OnAukiLeft()
		{
			m_LocalIds.Clear();
		}
		private void OnAukiEntityDeleted(uint entityId)
		{
			for (int i = m_Views.Count - 1; i >= 0; i--)
			{
				if (m_Views[i] == null)
				{
					m_Views.RemoveAt(i);
					continue;
				}
				if (m_Views[i].entityId == entityId)
				{
					var view = m_Views[i];
					m_Views.RemoveAt(i);
					view.Dispose();
					continue;
				}
			}
		}
		private void OnTransformComponentAdded(TransformComponentModel component)
		{
			if (!TryGetView(component.entityId, out var view))
				return;
            
			// add the floor raycast to set the correct position
			Vector3 position;
			Vector3 normal;
			if (m_RaycastService.FloorRaycast(component.model.position, out position, out normal))
			{
				view.transform.parent.SetPositionAndRotation(position, component.model.rotation);
			}
			else
			{
				view.transform.parent.SetPositionAndRotation(component.model.position, component.model.rotation);
			}
		}
		private void OnPropertiesComponentAdded(PropertiesComponentModel component)
		{
			var asset = m_AssetSettings.GetAsset(component.model.id);

			if (asset.assetType != AssetType.PowerUpSpawnPoint)
				return;

			var view = m_PropertiesEcsService
				.GetGameObject(component.entityId)
				.GetComponentInChildren<PowerUpSpawnPointView>();
			
			view.Init(component.entityId, assetToPowerUpDictionary[asset.assetId], m_Settings.cooldownDuration);
			view.onCollisionEntered.AddListener(OnCollisionEnter);
			m_Views.Add(view);
		}
		
		protected override void UpdateComponentMethod(SpawnPointStateComponentModel model, SpawnPointCooldownStateModel data)
		{
			UnityEngine.Debug.Log($"UpdateComponentMethod  {data.inCooldown}");
			model.model = data;
		}
		
		// Should only happen between server speeder and power up point due to ignores in tick
		private void OnCollisionEnter(Collision collision, EquipmentState equipmentState, uint powerUpSpawnPointId)
		{
			var gameObjectView = collision.gameObject.GetComponent<GameObjectView>();
			if (gameObjectView.entityId != m_SpeederService.serverSpeederEntity)
				return;

			if (!m_EquipmentStateComponentService.TryGetComponentModel(m_SpeederService.serverSpeederEntity, out var component))
				return;
			
			// New power up. After ability is done, equip new state.
			m_EquipmentStateComponentService.SetFullQuantity(equipmentState);
			UpdateComponent(powerUpSpawnPointId, new SpawnPointCooldownStateModel(true));
			m_Cooldowns.Add(new SpawnPointCooldown {
				cooldown = m_Settings.cooldownDuration,
				entityId = powerUpSpawnPointId,
			});
			Debug.Log("Power up spawn point cooldown activated");
			m_EquipmentStateComponentService.SetEquipAfterAbilityCommand(m_SpeederService.serverSpeederEntity, equipmentState);
		}

		private bool TryGetView(uint entityId, out PowerUpSpawnPointView powerUpSpawnPointView)
		{
			foreach (var view in m_Views)
			{
				if (view.entityId == entityId)
				{
					powerUpSpawnPointView = view;
					return true;
				}
			}
			powerUpSpawnPointView = null;
			return false;
		}
		public void Tick(float deltaTime, float unscaledDeltaTime)
		{
			// Handle Spawn Point Cooldowns
			keysToClear.Clear();
			for (int i = m_Cooldowns.Count - 1; i >= 0; i--)
			{
				var cooldown = m_Cooldowns[i].cooldown;
				var entityId = m_Cooldowns[i].entityId;
				if (m_Cooldowns[i].cooldown - Time.deltaTime <= 0 && m_Cooldowns[i].cooldown > 0)
				{
					Debug.Log("Power up spawn point cooldown cleared");
					UpdateComponent(entityId, new SpawnPointCooldownStateModel(false));
					m_Cooldowns.RemoveAt(i);
					continue;
				}
				m_Cooldowns[i] = new SpawnPointCooldown {
					cooldown = cooldown - Time.deltaTime,
					entityId = entityId,
				};
			}
			
			foreach (var view in m_Views)
			{
				if (!TryGetComponentModel(view.entityId, out var cooldownComponent))
					continue;

				if (view.cooldown == false && cooldownComponent.model.inCooldown)
				{
					view.timer = m_Settings.cooldownDuration;
				}
				view.cooldown = cooldownComponent.model.inCooldown;
			}
			
			// ignore all collisions between remote objects
			foreach (var speederView in m_SpeederService.speederViews)
			{
				if (speederView.Value.entityId == m_SpeederService.serverSpeederEntity)
					continue; //don't ignore collisions between server and power up point
				
				foreach (var view in m_Views)
				{
					Physics.IgnoreCollision(speederView.Value.boxCollider, view.capsuleCollider);
				}
			}
		}

		private List<PowerUpSpawnPointView> m_powerUpSpawnPointViewsCache = new();
		private List<uint> m_LocalIds = new();
		private List<PowerUpSpawnPointView> GetViewsOfType(EquipmentState equipmentState)
		{
			m_powerUpSpawnPointViewsCache.Clear();
			foreach (var view in m_Views)
			{
				if (view.equipmentType == equipmentState)
				{
					m_powerUpSpawnPointViewsCache.Add(view);
					
				}
			}
			
			return m_powerUpSpawnPointViewsCache;
		}
		
		public void RemoveAllLocalOfType(Placeable selectedPlaceable)
		{
			var views = GetViewsOfType(assetToPowerUpDictionary[selectedPlaceable.assetId]);
			for (int i = m_Views.Count - 1; i >= 0; i--)
			{
				if (m_LocalIds.Contains(m_Views[i].entityId) && views.Contains(m_Views[i]))
				{
					var view = m_Views[i];
					var entityId = view.entityId;
					m_AukiWrapper.DeleteEntity(entityId, () => Debug.Log($"Delete powerUpSpawnPoint entity {entityId}"));
					m_Views.RemoveAt(i);
					view.Dispose();
				}
			}
		}
		public void AddLocalId(uint entityId)
		{
			m_LocalIds.Add(entityId);
		}
	}
	public struct SpawnPointCooldown
	{
		public uint entityId;
		public float cooldown;
	}
}