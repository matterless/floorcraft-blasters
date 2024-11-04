using Matterless.Audio;
using UnityEngine;
namespace Matterless.Floorcraft
{
	public class LaserView : MonoBehaviour
	{
		[SerializeField] private GameObject m_ChargeFX;
		[SerializeField] private AnimationCurve m_ChargeVFXScaleCurve;
		[SerializeField] private AnimationCurve m_ReleaseVFXScaleCurve;
		[ColorUsage(true, true)] [SerializeField] private Color m_ChargeStartColor;
		[ColorUsage(true, true)] [SerializeField] private Color m_ChargeEndColor;
		
		
		private LaserBeamService m_LaserService;
		private uint m_Owner;
		private float m_OriginScale;
		
		private float m_ChargeTimer = 0.0f;
		private float m_ReleaseTimer = 0.0f;
		private bool m_IsCharging;
		private bool m_IsReleasing;
		
		private float m_ChargeMinSize;
		private float m_ChargeMaxSize; 
		private float m_ChargeTime;
		private float m_ReleaseTime;
		
		private bool m_ShouldFireAutomatically;
		private bool m_ManualFireAfterCharge;
		private AudioService m_AudioService;
		private ulong m_LaserChargingSoundId;

		public uint entityId => m_Owner;

		public void Init(uint id, LaserService.Settings settings, SpeederSFX speederSfx, float scale, LaserBeamService laserBeamService, AudioService audioService)
		{
			m_Owner = id;
			m_OriginScale = scale;
			m_LaserService = laserBeamService;
			m_AudioService = audioService;
			
			m_ChargeMinSize = 0.0f;
			m_ChargeMaxSize = 1.0f;
			m_ChargeTime = settings.laserChargeDuration;
			m_ReleaseTime = 0.3f;
		}
		
		public void StartCharging(bool shouldFireAutomatically)
		{
			m_ShouldFireAutomatically = shouldFireAutomatically;
			
			m_IsCharging = true;
			m_ChargeTimer = 0f;
			
			m_IsReleasing = false;
			m_ReleaseTimer = 0f;

			m_AudioService.Play("laser_activate");
			m_LaserChargingSoundId = m_AudioService.Play("laser_charging");
		}

		public void Update()
		{
			if (m_IsCharging)
			{
				m_ChargeFX.SetActive(true);
				float t = m_ChargeVFXScaleCurve.Evaluate(m_ChargeTimer / m_ChargeTime);
				m_ChargeFX.transform.localScale = Vector3.one * (t * m_ChargeMaxSize);
				m_ChargeTimer += Time.deltaTime;
		        
				if (m_ChargeTimer < m_ChargeTime)
				{
					return;
				}

				if (m_ShouldFireAutomatically || m_ManualFireAfterCharge)
				{
					Fire();
				}
			}

			if (m_IsReleasing)
			{
				m_ChargeFX.SetActive(true);
				float t = m_ReleaseVFXScaleCurve.Evaluate(m_ReleaseTimer / m_ReleaseTime);
				m_ChargeFX.transform.localScale = Vector3.one * (t * m_ChargeMaxSize);
				m_ReleaseTimer += Time.deltaTime;

				if (m_ReleaseTimer < m_ReleaseTime)
				{
					return;
				}
				
				m_LaserService.CreateLaserBeam(m_Owner, m_OriginScale, m_ChargeFX.transform);
				m_AudioService.Stop(m_LaserChargingSoundId);
				m_AudioService.Play("laser_fire");
				m_ChargeFX.SetActive(false);
				m_IsReleasing = false;
				m_ReleaseTimer = 0.0f;
			}
		}
		
		private void Fire()
		{
			//m_ChargeFX.SetActive(false);
			
			m_IsCharging = false;
			m_ChargeTimer = 0.0f;
			
			m_ManualFireAfterCharge = false;
			m_ShouldFireAutomatically = false;

			m_IsReleasing = true;
		}
		
		public void ManualFire()
		{
			if(!m_IsCharging || m_ShouldFireAutomatically)
			{
				return;
			}
			
			if (m_ChargeTimer < m_ChargeTime)
			{
				m_ManualFireAfterCharge = true;
			}
			else
			{
				Fire();
			}
		}

		public void ResetLaser()
		{
			m_AudioService.Stop(m_LaserChargingSoundId);
			m_ChargeFX.SetActive(false);
			
			m_IsCharging = false;
			m_IsReleasing = false;
		}
	}
}