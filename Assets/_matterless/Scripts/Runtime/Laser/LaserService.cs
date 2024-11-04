using System;
using Matterless.Inject;
using UnityEngine;
using UnityEngine.Serialization;

namespace Matterless.Floorcraft
{
	public class LaserService : ITickable
	{
		[Serializable]
		public class Settings : IEquipmentSetting
		{
			public float cooldown => m_Cooldown;
			public LayerMask laserLayerMask => m_LaserLayerMask;
			public string laserViewResourcePath => m_LaserViewResourcePath;
			public float duration => m_LaserAttackDuration + m_LaserPropagationDuration + m_ActivationDuration;
			public int quantity => m_Quantity;
			public float laserPropagationDuration => m_LaserPropagationDuration;
			public float laserChargeDuration => m_LaserChargeDuration;
			public float laserAttackDuration => m_LaserAttackDuration;
			public float laserDecayDuration => m_LaserDecayDuration;
			public float laserMaxLength => m_LaserMaxLength;
			public int laserResolution => m_LaserResolution;
			public bool infinite => m_Infinite;
			public Color laserStartColor => m_LaserStartColor;
			public Color laserEndColor => m_LaserEndColor;
			public Color laserDecayColor => m_LaserDecayColor;
			

			[FormerlySerializedAs("m_CooldownDuration")][SerializeField] private float m_Cooldown;
			[SerializeField] private string m_LaserViewResourcePath;
			
			[SerializeField] private int m_Quantity;
			[SerializeField] private LayerMask m_LaserLayerMask;
			[SerializeField] private float m_LaserMaxLength;
			[SerializeField] private float m_ActivationDuration;
			[SerializeField] private float m_LaserPropagationDuration;
			[SerializeField] private float m_LaserChargeDuration;
			[SerializeField] private float m_LaserAttackDuration;
			[SerializeField] private float m_LaserDecayDuration;
			[SerializeField] private int m_LaserResolution;
			[SerializeField] private bool m_Infinite = false;
			[ColorUsage(true, true)] [SerializeField] private Color m_LaserStartColor;
			[ColorUsage(true, true)] [SerializeField] private Color m_LaserEndColor;
			[ColorUsage(true, true)] [SerializeField] private Color m_LaserDecayColor;
		}
		private readonly EquipmentService m_EquipmentService;
		private readonly SpeederHUDService m_SpeederHUDService;
		private readonly SpeederStateComponentService m_SpeederStateComponentService;
		private readonly LaserService.Settings m_Settings;
		private readonly SpeederService m_SpeederService;
		
		private float m_LaserCooldownTimer;
		private float m_LaserDurationTimer;
		private readonly CooldownService m_CooldownService;
		
		public LaserService(
			SpeederHUDService speederHUDService, 
			EquipmentService equipmentService,
			SpeederStateComponentService speederStateComponentService,
			SpeederService speederService,
			CooldownService cooldownService,
			LaserService.Settings settings)
		{
			m_EquipmentService = equipmentService;
			m_SpeederHUDService = speederHUDService;
			m_SpeederStateComponentService = speederStateComponentService;
			m_CooldownService = cooldownService;
			m_SpeederService = speederService;
			m_Settings = settings;
			Debug.Log("Laser service created");
		}
		
		public void Tick(float deltaTime, float unscaledDeltaTime)
		{
			// Below here is server stuff only (changing state) 
			if (!m_EquipmentService.TryGetComponentModel(m_SpeederHUDService.serverEntityId, out var equipmentComponent))
				return;

			if (!m_SpeederStateComponentService.TryGetComponentModel(m_SpeederHUDService.serverEntityId, out var speederState)) // can't get speeder state, probably not spawned
				return;
			
			if (speederState.model.state.HasFlag(SpeederState.LaserFire) && m_CooldownService.abilityTimer <= 0)
			{
				m_SpeederStateComponentService.UnsetState(m_SpeederHUDService.serverEntityId, SpeederState.LaserFire);
			}
			
			if (equipmentComponent.model.state == EquipmentState.Laser && !m_CooldownService.inCooldown)
			{
				if (!speederState.model.state.HasFlag(SpeederState.LaserCharge)
				    && !speederState.model.state.HasFlag(SpeederState.LaserFire)
				    && m_SpeederHUDService.holdScreenInput)
				{
					Debug.Log("Charge laser");
					
					m_SpeederStateComponentService.SetState(m_SpeederHUDService.serverEntityId, SpeederState.LaserCharge);	
				}
				else if(speederState.model.state.HasFlag(SpeederState.LaserCharge)
				        && !speederState.model.state.HasFlag(SpeederState.LaserFire)
				        && !m_SpeederHUDService.holdScreenInput)
				{
					Debug.Log("Fire laser");
					m_SpeederStateComponentService.SetState(m_SpeederHUDService.serverEntityId, SpeederState.LaserFire);
					m_SpeederStateComponentService.UnsetState(m_SpeederHUDService.serverEntityId, SpeederState.LaserCharge);
					m_CooldownService.ActivateCooldown();
					m_EquipmentService.Use(m_SpeederHUDService.serverEntityId);
				}
			}
		}
	}
}