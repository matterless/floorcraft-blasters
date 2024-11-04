using System;
using Matterless.Inject;
using UnityEngine;
using UnityEngine.Serialization;
namespace Matterless.Floorcraft
{
	public class FlameThrowerService : ITickable
	{
		
		[Serializable]
		public class Settings : IEquipmentSetting
		{
			public float duration => m_Duration;
			public float cooldown => m_Cooldown;
			public string flameThrowerViewResourcePath => m_FlameThrowerViewResourcePath;
			public Pose flameOffset => m_FlameOffset;
			public int quantity => m_Quantity;
			public bool infinite => m_Infinite;
			[FormerlySerializedAs("m_FlameThrowerDuration")][SerializeField] private float m_Duration = 3f;
			[SerializeField] private string m_FlameThrowerViewResourcePath;
			[SerializeField] private Pose m_FlameOffset;
			[FormerlySerializedAs("m_FlameThrowerCooldownDuration")][SerializeField] private float m_Cooldown;
			[SerializeField] private int m_Quantity;
			[SerializeField] private bool m_Infinite = false;
		}
		
		private readonly SpeederHUDService m_SpeederHUDService;
		private readonly EquipmentService m_EquipmentService;
		private readonly CooldownService m_CooldownService;
		private readonly SpeederStateComponentService m_SpeederStateComponentService;

		public FlameThrowerService(SpeederHUDService speederHUDService, 
			EquipmentService equipmentService,
			CooldownService cooldownService,
			SpeederStateComponentService speederStateComponentService
			)
		{
			m_SpeederHUDService = speederHUDService;
			m_EquipmentService = equipmentService;
			m_CooldownService = cooldownService;
			m_SpeederStateComponentService = speederStateComponentService;
		}
		public void Tick(float deltaTime, float unscaledDeltaTime)
		{
			if (!m_EquipmentService.TryGetComponentModel(m_SpeederHUDService.serverEntityId, out var equipmentComponent))
				return;

			if (!m_SpeederStateComponentService.TryGetComponentModel(m_SpeederHUDService.serverEntityId, out var speederComponent))
				return;
			
			if (equipmentComponent.model.state == EquipmentState.Flamethrower && !m_CooldownService.inCooldown && m_SpeederHUDService.tapScreenInput) 
			{
				m_SpeederStateComponentService.SetState(m_SpeederHUDService.serverEntityId, SpeederState.FlameThrower);
				m_EquipmentService.Use(m_SpeederHUDService.serverEntityId);
				m_CooldownService.ActivateCooldown();
				return;
			}
			
			if (speederComponent.model.state.HasFlag(SpeederState.FlameThrower))
			{
				if (m_CooldownService.abilityTimer <= 0)
				{
					m_SpeederStateComponentService.UnsetState(m_SpeederHUDService.serverEntityId, SpeederState.FlameThrower);
				}
			}

		}
	}
}