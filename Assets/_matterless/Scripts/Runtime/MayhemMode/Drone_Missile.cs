using UnityEngine;
using UnityEngine.Serialization;

namespace Matterless.Floorcraft
{

    public class Drone_Missile : MonoBehaviour
    {
        [FormerlySerializedAs("explosion")] [SerializeField] ParticleSystem m_Explosion;

        private float m_Speed;
        private float m_Damage;
        private Transform m_Target;
        bool m_IsDead = false;
        private Renderer m_Renderer;
        
        void Start()
        {
            m_Renderer = GetComponent<Renderer>();
        }

        void Update()
        {
            if(m_IsDead) return;
            if(m_Target == null) Kill();
            transform.LookAt(m_Target);
            transform.position += transform.forward * (m_Speed * Time.deltaTime);
        }

        public void Init(Vector3 transformPosition, Vector3 transformForward, Transform enemyTransform,
            float configDroneMissileLauncherSpeed, float configDroneMissileLauncherDamage)
        {
            transform.position = transformPosition;
            transform.forward = transformForward;
            m_Target = enemyTransform;
            m_Speed = configDroneMissileLauncherSpeed;
            m_Damage = configDroneMissileLauncherDamage;
        }

        private void OnTriggerEnter(Collider collision)
        {
            if (!collision.transform.CompareTag("Enemy"))
            {
                Debug.Log("Missile hit something that wasn't an enemy!");
                return;
            }
            Debug.Log("Missile hit enemy!");
            
            if(m_IsDead)
            {
                Debug.Log("Missile already dead!");
                return;
            }

            var enemy = collision.gameObject.GetComponent<NPCEnemyView>();
            enemy.TakeDamage();
            
            Kill();
        }
        
        public AudioSource engineSound;
        public AudioSource explosionSound;

        private void Kill()
        {
            m_IsDead = true;
            m_Renderer.enabled = false;
            m_Explosion.Play();
            engineSound.Stop();
            explosionSound.Play();
            Destroy(gameObject, 1f);
        }
    }
}