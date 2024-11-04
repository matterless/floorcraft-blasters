using System;
using Matterless.Inject;
using UnityEngine;
namespace Matterless.Floorcraft
{
	public class HonkService : ITickable
	{
		[Serializable]
		public class Settings : IEquipmentSetting
		{
			public float cooldown => 0.1f;
			public float duration => 0.1f;
			public int quantity => 1;
			public bool infinite => true;
			[SerializeField] private float m_Cooldown;
			[SerializeField] private float m_Duration;
			[SerializeField] private int m_Quantity;
			[SerializeField] private bool m_Infinite;
		}
		private readonly SpeederHUDService m_SpeederHUDService;
		private readonly EquipmentService m_EquipmentService;
		private readonly MessageComponentService m_MessageComponentService;
		private readonly SpeederService m_SpeederService;
		private readonly CooldownService m_CooldownService;
		private readonly Settings m_HonkSettings;
		private readonly SpeederStateComponentService m_SpeederStateComponentService;

		public HonkService(
			SpeederHUDService speederHUDService, 
			EquipmentService equipmentService,
			MessageComponentService messageComponentService,
			SpeederService speederService,
			CooldownService cooldownService,
			HonkService.Settings honkSettings,
			SpeederStateComponentService speederStateComponentService
		)
		{
			m_SpeederHUDService = speederHUDService;
			m_EquipmentService = equipmentService;
			m_MessageComponentService = messageComponentService;
			m_SpeederService = speederService;
			m_CooldownService = cooldownService;
			m_HonkSettings = honkSettings;
			m_SpeederStateComponentService = speederStateComponentService;
			m_MessageComponentService.onComponentUpdated += OnMessageComponentUpdated;
		}
		private void OnMessageComponentUpdated(MessageComponentModel messageComponent)
		{
			if (messageComponent.model.message == MessageModel.Message.Honk)
			{
				// Play honk SFX
				if (!m_SpeederService.TryGetSpeederView(messageComponent.entityId, out var speederView))
					return;

				GameObject.Instantiate<HonkSFX>(Resources.Load<HonkSFX>("HonkSFX"),speederView.transform.position,Quaternion.identity);
			}
		}
		public void Tick(float deltaTime, float unscaledDeltaTime)
		{
			if (!m_EquipmentService.TryGetComponentModel(m_SpeederHUDService.serverEntityId, out var equipmentComponent))
            	return;
            
            if (equipmentComponent.model.state != EquipmentState.None) // we are not in honk mode
            	return;

            if (!m_SpeederStateComponentService.TryGetComponentModel(m_SpeederHUDService.serverEntityId, out var speederState)) // can't get speeder state, probably not spawned
            	return;

            if (speederState.model.state == SpeederState.Totaled)
	            return;

            if (!m_CooldownService.inCooldown && m_SpeederHUDService.tapScreenInput)
            {
	            m_CooldownService.ActivateCooldown();
	            m_MessageComponentService.SendMessage(m_SpeederHUDService.serverEntityId, MessageModel.Message.Honk,m_SpeederHUDService.serverEntityId);
            }
		}
		
	}
}