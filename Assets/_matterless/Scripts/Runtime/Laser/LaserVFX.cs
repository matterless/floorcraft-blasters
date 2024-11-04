using UnityEngine;
namespace Matterless.Floorcraft
{
	public class LaserVFX : MonoBehaviour
	{
		[SerializeField] private float m_ChargeDuration = 2f;
		[SerializeField] private float m_LaserDuration = 5f;
		[SerializeField] private float m_LaserPropagationTime = 10.0f;
		[SerializeField] private float m_MaxLength = 3f;
		[SerializeField] private ParticleSystem m_FirePointFX;
		[SerializeField] private ParticleSystem m_ChargeFX;
		[SerializeField] private ParticleSystem m_HitFX;
		[SerializeField] private ParticleSystem m_HeadFX;
		[SerializeField] private LineRenderer m_BeamFX;
		public LaserCollider laser;
		
		private bool isCharging = false;
		private bool isFiring = false;
		private float chargeTimer;
		private float laserBeamTimer;
		private float propagationTimer;
		private LaserService.Settings m_Settings;
		private float m_LaserLength;
		private float m_AppliedVehicleScale;
		private Vector3 m_Target;
		private float m_Speed;
		private Vector3 m_LaserEndPosition;
		private float m_ColliderSpeed;
		private float m_ScaledLength;
		private SpeederSFX m_SpeederSFX;
		
		public bool ShouldFireManually { get; set; }
		private bool m_ManualFireAfterCharge;

		public void Init(uint id, LaserService.Settings settings, SpeederSFX speederSfx)
		{
			m_Settings = settings;
			m_SpeederSFX = speederSfx;
			m_ChargeDuration = m_Settings.laserChargeDuration;
			m_LaserDuration = m_Settings.laserAttackDuration;
			m_LaserPropagationTime = m_Settings.laserPropagationDuration;
			m_MaxLength = m_Settings.laserMaxLength;
			m_LaserLength = m_Settings.laserMaxLength;
			laser.Init(id);
			laser.capsuleCollider.height = 0;
			laser.capsuleCollider.center = Vector3.zero;
			chargeTimer = m_ChargeDuration;
			laserBeamTimer = m_Settings.duration;
			ParticleSystem.MainModule mian = m_ChargeFX.main;
			mian.startLifetime = m_ChargeDuration;
			m_AppliedVehicleScale = transform.root.localScale.x;
			m_Speed = (((transform.position + transform.forward * m_MaxLength) - transform.position) / m_Settings.laserPropagationDuration).magnitude;
			m_ColliderSpeed = m_MaxLength / m_AppliedVehicleScale / m_Settings.laserPropagationDuration;
		}
		
		private void OnEnable()
		{
			isCharging = true;
			isFiring = false;
			laser.capsuleCollider.height = 0;
			laser.capsuleCollider.center = Vector3.zero;
			propagationTimer = 0;
			m_LaserEndPosition = transform.position;
			m_BeamFX.SetPosition(0, transform.position);
			m_BeamFX.SetPosition(1, transform.position);
			chargeTimer = m_ChargeDuration;
			laserBeamTimer = m_Settings.duration;
		}

		public void SetLength(float length)
		{
			m_LaserLength = length;
			m_ScaledLength = m_LaserLength / m_AppliedVehicleScale;
			m_Target = transform.position + transform.forward * m_LaserLength;
		}
		
		void Update()
        {
	        m_BeamFX.SetPosition(0, transform.position);
            if (isFiring)
            {
                if (laserBeamTimer > 0)
                {
	                m_LaserEndPosition = Vector3.MoveTowards(m_LaserEndPosition, m_Target, m_Speed*Time.deltaTime);
	                laser.capsuleCollider.height = Mathf.MoveTowards(laser.capsuleCollider.height,m_ScaledLength,m_ColliderSpeed * Time.deltaTime);
                    laser.capsuleCollider.center = Vector3.up * Mathf.MoveTowards(laser.capsuleCollider.center.y,m_ScaledLength/2f,m_ColliderSpeed / 2 * Time.deltaTime);
                    m_BeamFX.SetPosition(1, m_LaserEndPosition);
                    m_BeamFX.enabled = true;
                    m_FirePointFX.Play(true);
                    if (m_LaserLength < m_MaxLength)
                    {
                        m_BeamFX.SetPosition(1, m_LaserEndPosition);
                        m_HitFX.transform.position = m_LaserEndPosition;
                        m_HitFX.Play();
                        m_HeadFX.Stop();
                    }	
                    else
                    {
                        m_HeadFX.transform.position = m_LaserEndPosition;
                        m_HeadFX.Play();
                        m_HitFX.Stop();
                    }
                    propagationTimer += Time.deltaTime;
                    laserBeamTimer -= Time.deltaTime;
                }
                else
                {
	                m_FirePointFX.Stop(true);
                    m_BeamFX.enabled = false;
                    propagationTimer = 0;
                    laserBeamTimer = m_LaserDuration;
                    isFiring = false;
                    m_HitFX.Stop();
                    m_HeadFX.Stop();
                }
            }

            if (isCharging)
            {
	            if (!m_ChargeFX.isPlaying)
	            {
		            m_SpeederSFX.ChargeLaser();
		            m_ChargeFX.Play(true);
	            }
                if (chargeTimer > 0)
                {
	                chargeTimer -= Time.deltaTime;
                }
                else if(!ShouldFireManually)
                {
	                Fire();
                }
                else if (m_ManualFireAfterCharge)
                {
	                m_ManualFireAfterCharge = false;
	                Fire();
                }
            }
        }
		
		public void ManualFire()
		{
			if (chargeTimer > 0)
			{
				m_ManualFireAfterCharge = true;
			}
			else
			{
				Fire();
			}
		}

		private void Fire()
		{
			m_SpeederSFX.BlastLaser();
			m_ChargeFX.Stop(true);
			chargeTimer = m_ChargeDuration;
			isCharging = false;
			isFiring = true;
		}

		public float GetLength()
		{
			return m_LaserLength;
		}
	}
}