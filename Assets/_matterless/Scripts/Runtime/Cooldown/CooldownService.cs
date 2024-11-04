using Matterless.Inject;
using UnityEngine;
namespace Matterless.Floorcraft
{
	public class CooldownService : ITickable
	{
		private float m_AbilityTimer;
		private float m_CooldownTimer;
		private IEquipmentSetting m_EquipmentSetting;
		
		public bool inCooldown
		{
			get {
				
				return m_AbilityTimer + m_CooldownTimer > 0;
				
			}
		}
		
		public float duration => m_EquipmentSetting.duration + m_EquipmentSetting.cooldown;
		public float cooldownTimer => m_CooldownTimer + m_AbilityTimer;
		public float abilityTimer => m_AbilityTimer;
		

		public void Tick(float deltaTime, float unscaledDeltaTime)
		{
			if (m_AbilityTimer - Time.deltaTime <= 0 && m_AbilityTimer > 0)
			{
				Debug.Log("Ability Over");
			}
			if (m_AbilityTimer > 0)
			{
				m_AbilityTimer -= Time.deltaTime;
			}
			if (m_CooldownTimer - Time.deltaTime <= 0 && m_CooldownTimer > 0)
			{
				Debug.Log("Cooldown Over");
			}
			if (m_AbilityTimer <= 0 && m_CooldownTimer > 0)
			{
			
				m_CooldownTimer -= Time.deltaTime;
			}
		}
		public void SetCooldownSettings(IEquipmentSetting equipmentSetting)
		{
			Debug.Log($"Set cooldown {equipmentSetting.cooldown} set duration {equipmentSetting.duration}");
			m_EquipmentSetting = equipmentSetting;
		}
		public void ActivateCooldown()
		{
			Debug.Log("Activate Cooldown");
			m_AbilityTimer = m_EquipmentSetting.duration;
			m_CooldownTimer = m_EquipmentSetting.cooldown;
		}

		public void ReduceCooldown(float byTime)
		{
			if (m_CooldownTimer > 0)
			{
				m_CooldownTimer -= byTime;

				//dont reduce to below zero
				if (m_CooldownTimer <= 0)
				{
					m_CooldownTimer = 0.01f;
				}
			}
		}
	}
}