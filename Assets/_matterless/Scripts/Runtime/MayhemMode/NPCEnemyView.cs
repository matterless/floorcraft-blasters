using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace Matterless.Floorcraft
{

    public class NPCEnemyView : MonoBehaviour
    {
        [SerializeField] private TMP_Text m_NameText;
        [SerializeField] private Collider m_Collider;
        [SerializeField] private Transform m_CanvasTransform;
        [SerializeField] private GameObject m_Shield;
        [SerializeField] private EnemyVirusRND1 m_EnemyVirus;
        [SerializeField] private LineRenderer m_LineRenderer;
        [SerializeField] private GameObject m_DeathEffect;
        [SerializeField] private ParticleSystem m_ExplosionParticles;
        [SerializeField] private AudioSource m_ExplosionSound;
        
        private int m_Health = 1;
        float m_Speed = 0.05f;
        private List<Material> m_Materials;
        private bool m_IsShieldOn;
        
        uint mTtargetId;

        public Transform target
        {
            get => m_Target;
            set => m_Target = value;
        }

        public bool isShieldOn => m_IsShieldOn;

        Transform m_Target;
        private SpeederView m_latestInteractedSpeeder;
        private bool m_IsDead = false;
        private string m_Name = "";
        private bool m_IsBoss;
        private bool m_IsHost;

        public Action<uint> damageTaken;
        public Action obstacleHit;
        public Action<uint> destroyed;
        private uint m_Uid;
        private static readonly int Scale = Shader.PropertyToID("_Scale");
        private Vector3 m_GroundPosition;
        private Quaternion m_Orientation;
        private Vector3 m_Velocity;
        private Vector3 m_InitialPosition;
        private Vector3 m_NetworkPosition;
        private float m_InterpolationStartTime;
        private float m_InterpolationJourneyLength;

        private bool m_IsInterpolating;

        public Vector3 targetPosition
        {
            get => m_Target.position;
            set => m_Target.position = value;
        }

        public float speed => m_Speed;

        public void UpdateView(float dt)
        {
#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.Y))
            {
                TakeDamage();
            }
#endif

            if (m_IsHost)
            {
                m_GroundPosition += m_Velocity * dt;
            }
            else
            {
                // We can't assume the game will run at the same frame rate on all devices, thus dt will change and this will make position differences between client and host
                // Interpolation should happen here
                if (m_IsInterpolating)
                {
                    float distanceCovered = (Time.time - m_InterpolationStartTime) * 0.5f;
                    float fractionOfJourney = distanceCovered / m_InterpolationJourneyLength;

                    m_GroundPosition = Vector3.Lerp(m_InitialPosition, m_NetworkPosition, fractionOfJourney);

                    if (fractionOfJourney >= 1.0f)
                    {
                        Debug.Log("Interpolation finished");
                        m_IsInterpolating = false;
                    }
                }
                else
                {
                    m_GroundPosition = Vector3.MoveTowards(m_GroundPosition, m_Target.position, dt * m_Speed);   
                }
            }

            transform.position = m_GroundPosition;
            
            m_CanvasTransform.LookAt(Camera.main.transform.position);
            m_CanvasTransform.Rotate(0, 180f, 0);

            if (m_Target == null)
            {
                m_LineRenderer.enabled = false;
                return;
            }
            
#if UNITY_EDITOR
            Debug.DrawLine(transform.position, m_Target.position, Color.red);
#endif
            
            if (m_LineRenderer)
            {
                m_LineRenderer.enabled = true;
                m_LineRenderer.SetPosition(0, transform.position);
                m_LineRenderer.SetPosition(1, m_Target.position);
            }
        }

        public void UpdateNetworkPosition(Vector3 networkPosition)
        {
            m_InitialPosition = transform.position;
            m_NetworkPosition = networkPosition;
            m_InterpolationJourneyLength = Vector3.Distance(m_InitialPosition, m_NetworkPosition);

            // Make this adjustable from settings
            if (m_InterpolationJourneyLength >= 0.01f)
            {
                Debug.Log("Starting interpolation");
                m_InterpolationStartTime = Time.time;
                m_IsInterpolating = true;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!m_IsHost)
            {
                return;
            }
            
            if (m_IsDead)
            {
                return;
            }
            
            var speederView = collision.gameObject.GetComponent<SpeederView>();
            if (speederView != null)
            {
                if (speederView != null)
                {
                    if(!m_IsShieldOn)
                    {
                        Destroy();
                    }
                }
                return;
            }

            GameObjectView otherView = collision.gameObject.GetComponent<GameObjectView>();
            if (otherView != null && otherView.CompareTag(UnityGameObjectTag.Laser))
            {
                TakeDamage();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!m_IsHost)
            {
                return;
            }
            
            GameObjectView otherView = other.gameObject.GetComponent<GameObjectView>();

            if (otherView != null)
            {
                if (otherView.CompareTag(UnityGameObjectTag.Laser))
                {
                    TakeDamage();
                }
                else if (otherView.CompareTag(UnityGameObjectTag.Obstacle))
                {
                    // we need to disable this collider because obstacle has multiple colliders
                    // and sometimes we get multiple on enter messages
                    m_Collider.enabled = false;
                    obstacleHit?.Invoke();
                    Destroy();
                }
                else if (otherView.CompareTag(UnityGameObjectTag.Speeder))
                {
                    var speederView = other.gameObject.GetComponent<SpeederView>();

                    if (speederView != null)
                    {
                        m_latestInteractedSpeeder = speederView;
                        if (!m_IsShieldOn)
                        {
                            Destroy(); 
                        }
                    }
                }
            }
        }

        public void TakeDamage()
        {
            damageTaken?.Invoke(m_Uid);
        }

        public void UpdateHealth(int health)
        {
            m_Health = health;
            
            if (m_IsShieldOn && m_Health == 1)
            {
                m_IsShieldOn = false;
                m_EnemyVirus.OnShieldDestroyed();
            }
            
            if (m_Health > 0)
            {
                return;
            }
            
            Destroy();
            m_IsDead = true;
        }

        private void Destroy()
        {
            if (m_DeathEffect != null)
            {
                m_DeathEffect.SetActive(true);
                m_DeathEffect.transform.parent = null; // Stay after entity destruction
            }

            if (m_ExplosionParticles != null)
            {
                m_ExplosionParticles.gameObject.SetActive(true);
                m_ExplosionParticles.gameObject.transform.parent = null;
                m_ExplosionParticles.Play();
                m_ExplosionSound.Play();
            }
            
            destroyed?.Invoke(m_Uid);
            m_Collider.enabled = true;
        }

        public void PlayDestroyAnimations()
        {
            if (m_DeathEffect != null)
            {
                m_DeathEffect.SetActive(true);
                m_DeathEffect.transform.parent = null; // Stay after entity destruction
            }

            if (m_ExplosionParticles != null)
            {
                m_ExplosionParticles.gameObject.SetActive(true);
                m_ExplosionParticles.gameObject.transform.parent = null;
                m_ExplosionParticles.Play();
                m_ExplosionSound.Play();
            }
        }

        protected virtual Transform GetTransform()
        {
            return null;
        }
        
        public void Init(uint uid, int health, float speed, Transform target, 
            EnemyViewModel enemyViewModel, bool isHost, Action<uint> onDestroyed)
        {
            m_Uid = uid;
            destroyed = onDestroyed;
            m_Name = "Unnamed goon";
            m_NameText.text = m_Name;
            m_Speed = speed;
            m_Health = health;
            m_IsHost = isHost;

            m_IsShieldOn = m_Health > 1;
            m_EnemyVirus.Init((int)uid, m_Health > 1);
            
            m_GroundPosition = enemyViewModel.groundPosition;
            m_Orientation = enemyViewModel.orientation;
            m_Target = target;
            m_Speed = enemyViewModel.speed;

            var up = Vector3.up;
            Vector3 targetPos = m_Target == null ? Vector3.zero : m_Target.position;
            var vector = Vector3.ProjectOnPlane(targetPos - m_GroundPosition, up);
            var dir = vector.normalized;
            
            m_Orientation = Quaternion.LookRotation(dir, up);
            m_Velocity = m_Orientation * Vector3.forward * m_Speed;
        }
    }
}

