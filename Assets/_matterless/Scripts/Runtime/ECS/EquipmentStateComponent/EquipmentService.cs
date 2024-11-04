using System;
using System.Collections.Generic;
using Matterless.Inject;
using UnityEngine;
namespace Matterless.Floorcraft
{
	public class EquipmentService : GenericComponentService<EquipmentStateComponentModel, EquipmentStateModel>, ITickable
	{
		[Serializable]
		public class Settings
		{
			public List<EquipmentState> debugMenuAvailableEqupimentStates => m_DebugMenuAvailableEqupimentStates;
			[SerializeField] private List<EquipmentState> m_DebugMenuAvailableEqupimentStates;
		}
		
		private readonly WreckingBallMagnetService.Settings m_WreckingBallSettings;
		private readonly DashSettings m_DashSettings;
		private readonly FlameThrowerService.Settings m_FlameThrowerSettings;
		private readonly CooldownService m_CooldownService;
		private readonly LaserService.Settings m_LaserSettings;
		private readonly ProximityMineService.Settings m_ProximityMinesSettings;
		private readonly ShadowCloneService.Settings m_ShadowCloneSettings;
		private readonly HonkService.Settings m_HonkSettings;
		private readonly Dictionary<EquipmentState, IEquipmentSetting> m_EquipmentStateToSettingDictionary;
		
		private int m_Quantity;
		private bool m_Infinite;
		private Dictionary<uint, EquipmentState> m_EquipAfterAbilityCommands = new Dictionary<uint, EquipmentState>();
		private List<uint> keysToRemove = new List<uint>();

		public EquipmentService(IECSController ecsController, IComponentModelFactory componentModelFactory,
			WreckingBallMagnetService.Settings wreckingBallSettings, 
			DashSettings dashSettings,
			FlameThrowerService.Settings flameThrowerSettings,
			CooldownService cooldownService,
			ProximityMineService.Settings proximityMinesSettings,
			ShadowCloneService.Settings shadowCloneSettings,
			HonkService.Settings honkSettings,
			IAukiWrapper aukiWrapper,
			LaserService.Settings laserSettings
			) : base(ecsController, componentModelFactory)
		{
			m_WreckingBallSettings = wreckingBallSettings;
			m_DashSettings = dashSettings;
			m_FlameThrowerSettings = flameThrowerSettings;
			m_CooldownService = cooldownService;
			m_CooldownService.SetCooldownSettings(honkSettings);
			m_LaserSettings = laserSettings;
			m_ProximityMinesSettings = proximityMinesSettings;
			m_ShadowCloneSettings = shadowCloneSettings;
			m_HonkSettings = honkSettings;
			m_EquipmentStateToSettingDictionary	= new (){
				{EquipmentState.None, m_HonkSettings},
				{EquipmentState.MagnetAndWreckingBall, m_WreckingBallSettings},
				{EquipmentState.Magnet, m_WreckingBallSettings},
				{EquipmentState.Dash, m_DashSettings},
				{EquipmentState.Flamethrower, m_FlameThrowerSettings},
				{EquipmentState.Laser, m_LaserSettings},
				{EquipmentState.ProximityMines, m_ProximityMinesSettings},
				{EquipmentState.Clone, m_ShadowCloneSettings},
			};
			aukiWrapper.onLeft += OnLeft;
		}
		private void OnLeft()
		{
			m_EquipAfterAbilityCommands.Clear();
		}
		protected override void UpdateComponentMethod(EquipmentStateComponentModel model, EquipmentStateModel data)
		{
			UnityEngine.Debug.Log($"UpdateComponentMethod  {data.state}");
			model.model = data;
		}
		public void SetState(uint entityId, EquipmentState state)
		{
			m_CooldownService.SetCooldownSettings(m_EquipmentStateToSettingDictionary[state]);
			UpdateComponent(entityId, new EquipmentStateModel(state));
		}
		public void Use(uint entityId)
		{
			if (m_Infinite)
				return;
			
			Debug.Log($"use with quantity {m_Quantity}");
			m_Quantity--;
			if (m_Quantity <= 0)
			{
				SetEquipAfterAbilityCommand(entityId, EquipmentState.None);
			}
		}
		public void SetEquipAfterAbilityCommand(uint entityId, EquipmentState equipmentState)
		{
			m_EquipAfterAbilityCommands[entityId] = equipmentState;
		}
		public void Tick(float deltaTime, float unscaledDeltaTime)
		{
			HandleEquipAfterAbilityCommands();
		}

		private void HandleEquipAfterAbilityCommands()
		{
			keysToRemove.Clear();
			foreach (var command in m_EquipAfterAbilityCommands)
			{
				if (m_CooldownService.abilityTimer <= 0)
				{
					var key = command.Key;
					var component = GetComponentModel(key);
					if (command.Value != component.model.state)
					{
						SetState(command.Key, command.Value);
						m_CooldownService.ActivateCooldown();
					}
					SetFullQuantity(command.Value);
					keysToRemove.Add(key);
				}
			}
			foreach (var key in keysToRemove)
			{
				m_EquipAfterAbilityCommands.Remove(key);
			}
		}
		public void SetFullQuantity(EquipmentState equipmentState)
		{
			m_Infinite = m_EquipmentStateToSettingDictionary[equipmentState].infinite;
			m_Quantity = m_EquipmentStateToSettingDictionary[equipmentState].quantity;
		}
		public int GetUses(uint serverSpeederEntity) => m_Infinite ? int.MaxValue : m_Quantity;
	}
	
}
