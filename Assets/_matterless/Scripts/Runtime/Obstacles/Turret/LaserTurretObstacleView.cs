using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Matterless.Audio;
using UnityEngine;
using UnityEngine.Events;

namespace Matterless.Floorcraft
{
    public class LaserTurretObstacleView : ObstacleView
    {
        [SerializeField] private WedgeMeshGenerator m_Wedge;
        [SerializeField] private Transform m_LaserBase;
        [SerializeField] private Transform m_LaserPivot;
        [SerializeField] private Transform m_LaserSightPivot;
        [SerializeField] private LaserView m_LaserView;
        [SerializeField] private LaserSightView m_LaserSightCollider;
        [SerializeField] private LaserCoreView m_LaserCore;
        [SerializeField] private ParticleSystem m_Explosion;
        
        [SerializeField] private SpeederSFX m_SpeederSFX;
        [SerializeField] private RadarView m_RadarView;
        
        public float cooldown { get; private set; }
        private float deadTime;
        private float shootingTime;
        public float aggressionTime { get; private set; }
        private float aggressionCooldown;
        private int rotateDirection = 1;
        private float laserConeDegrees = 5f;
        public float rotationSpeed = 30f;
        
        public UnityEvent<ObstacleView, Collider> onSeen;
        public UnityEvent<ObstacleView, Collider> onShot;
        
        private IEnumerator m_LaserCoroutine = null;
        private LaserService.Settings m_Settings;
        
        public void InitLaser(LaserService.Settings laserSettings, LaserBeamService laserService, AudioService audioService)
        {
            m_LaserSightCollider.onTriggerEnter.AddListener(other => onSeen.Invoke(this, other));
            m_RadarView.onTriggerEnter.AddListener(OnRadarEnter);
            m_RadarView.onTriggerExit.AddListener(OnRadarExit);
            m_Wedge.MeshSetup(m_LaserSightCollider.transform.localPosition.z - m_LaserSightCollider.transform.localScale.z / 2, m_LaserSightCollider.transform.localPosition.z + m_LaserSightCollider.transform.localScale.z / 2, laserConeDegrees);
            m_Settings = laserSettings;
            m_LaserView.Init(entityId,laserSettings, m_SpeederSFX, this.transform.localScale.x, laserService, audioService);
            
            m_LaserCore.onTriggerEnter.AddListener(other => onShot.Invoke(this, other));
            
            //Physics.IgnoreCollision(m_LaserView.pivot.laser.capsuleCollider, m_LaserSightCollider.GetComponent<Collider>());
            //Physics.IgnoreCollision(m_LaserView.pivot.laser.capsuleCollider, m_LaserCore.GetComponent<Collider>());

            InitCrates();
        }

        private void OnEnable()
        {
            if(m_LaserCoroutine != null)
                StopCoroutine(m_LaserCoroutine);

            m_LaserCoroutine = LaserLoop();
            StartCoroutine(m_LaserCoroutine);
        }
        
            private IEnumerator LaserLoop()
        {
            while (true)
            {
                float deltaTime = Time.deltaTime;
                
                CheckCrateSpawns(deltaTime);
                
                if (deadTime > 0)
                {
                    deadTime -= Time.deltaTime;
                    if (deadTime < 0)
                    {
                        deadTime = 0;

                        m_LaserPivot.gameObject.SetActive(true);
                        m_LaserSightCollider.gameObject.SetActive(true);
                    }

                    yield return null;
                    continue;
                }
                
                if (cooldown > 0)
                {
                    cooldown -= deltaTime;
                    if (cooldown < 0)
                        cooldown = 0;
                }

                if (aggressionTime > 0)
                {
                    if (SelectedTarget == null)
                    {
                        aggressionTime = 0;
                        aggressionCooldown = Random.Range(1, 5);
                    }
                    else
                    {
                        aggressionTime -= deltaTime;
                        if (aggressionTime < 0)
                        {
                            aggressionCooldown = Random.Range(1, 5);
                            aggressionTime = 0;
                            SelectedTarget = null;
                        }    
                    }
                }

                if (aggressionCooldown > 0)
                {
                    aggressionCooldown -= deltaTime;
                    if (aggressionCooldown < 0)
                        aggressionCooldown = 0;
                }
                
                
                if (shootingTime > 0)
                {
                    shootingTime -= deltaTime;
                    if (shootingTime < 0)
                    {
                        shootingTime = 0;
                        aggressionCooldown = 3f;
                        SelectedTarget = null;
                        m_LaserSightCollider.gameObject.SetActive(true);
                        m_Wedge.gameObject.SetActive(true);

                        m_LaserSightPivot.transform.localRotation = Quaternion.Euler(0, Random.Range( 0, laserConeDegrees) - laserConeDegrees / 2, 0);
                    }
                }
                else
                {
                    if (SelectedTarget == null && cooldown <= 0 && aggressionCooldown <= 0)
                    {
                        GameObjectView target = TrackedTargets.FirstOrDefault(x => x.entityId == PriorityTarget);
                        if (target == null && TrackedTargets.Count > 0)
                        {
                            target = TrackedTargets[Random.Range(0, TrackedTargets.Count - 1)];
                        }

                        SelectedTarget = target;
                        aggressionTime = 3f;
                    }
                    
                    if (SelectedTarget != null)
                    {
                        Vector3 targetDirection = SelectedTarget.transform.position - transform.position;
                        float targetAngle = Vector3.SignedAngle(m_LaserSightPivot.forward, targetDirection, Vector3.up);
                        float rotationStep = 10f * rotationSpeed * Time.deltaTime;

                        float rotationDirection = Mathf.Sign(targetAngle);
                        Vector3 rotationAxis = Vector3.up;
                        Quaternion targetRotation;
                        if (targetAngle > rotationStep || targetAngle < -rotationStep)
                        {
                            targetRotation = Quaternion.AngleAxis(rotationStep * rotationDirection, rotationAxis);
                        }
                        else
                        {
                            
                            targetRotation = Quaternion.AngleAxis(targetAngle * rotationDirection, rotationAxis);
                            FireLaser();
                        }
                        m_LaserBase.rotation *= targetRotation;
                    }
                    else
                    {
                        m_LaserBase.Rotate(new Vector3(0,rotateDirection * rotationSpeed * Time.deltaTime,0));    
                    }
                }

                yield return null;
            }
        }

        public void FireLaser()
        {
            shootingTime = m_Settings.duration;
            cooldown = shootingTime + Random.Range(3, 10);
            rotateDirection = Random.Range(0, 2) * 2 - 1;
            
            m_LaserSightCollider.gameObject.SetActive(false);
            m_Wedge.gameObject.SetActive(false);
            
            m_LaserView.StartCharging(true);
        }

        public void DestroyLaser()
        {
            m_Explosion.gameObject.SetActive(true);
            m_Explosion.Play();

            m_LaserPivot.gameObject.SetActive(false);
            m_LaserSightCollider.gameObject.SetActive(false);
            m_Wedge.gameObject.SetActive(false);

            deadTime = 10f;
        }
        
        

        private const int MaxCrates = 0;
        private const float MinCrateSpawnTime = 10f;
        private const float MaxCrateSpawnTime = 20f;
        
        private readonly Dictionary<int, CrateView> m_CrateViews = new();
        private readonly Dictionary<int, float> m_CrateRespawnTimes = new();

        private void InitCrates()
        {
            Object cratePrefab = Resources.Load("NewEntities/CrateView");
            for (int i = 0; i < MaxCrates; i++)
            {
                Vector3 location = CirclePointGenerator.GetPoint(0.5f, i * (Mathf.PI * 2) / MaxCrates, transform.position); 

                GameObject crate = (GameObject)Instantiate(cratePrefab);
                crate.transform.position = location;
                crate.SetActive(false);
                m_CrateViews.Add(i, crate.GetComponent<CrateView>());
                m_CrateRespawnTimes.Add(i, i);
            }

        }
        
        private void CheckCrateSpawns(float deltaTime)
        {
            foreach (var crateIndex in m_CrateViews.Keys)
            {
                if (m_CrateViews[crateIndex].gameObject.activeSelf == false)
                {
                    if (m_CrateRespawnTimes[crateIndex] <= 0)
                    {
                        m_CrateViews[crateIndex].gameObject.SetActive(true);
                        m_CrateRespawnTimes[crateIndex] = Random.Range(MinCrateSpawnTime, MaxCrateSpawnTime);
                    }
                    else
                    {
                        m_CrateRespawnTimes[crateIndex] -= deltaTime;
                    }
                }
            }
        }

        public int PriorityTargetScore { get; private set; }
        public uint PriorityTarget { get; private set; }
        public List<GameObjectView> TrackedTargets { get; } = new();
        
        public GameObjectView SelectedTarget { get; private set; }

        private void OnRadarExit(Collider other)
        {
            if (other.gameObject.CompareTag(UnityGameObjectTag.Speeder))
            {
                GameObjectView otherView = other.gameObject.GetComponent<GameObjectView>();
                if (otherView != null)
                {
                    RemoveTarget(otherView);
                }
            }
        }

        private void RemoveTarget(GameObjectView gameObjectView)
        {
            if (SelectedTarget == gameObjectView)
            {
                SelectedTarget = null;
            }
            
            if (TrackedTargets.Contains(gameObjectView))
                TrackedTargets.Remove(gameObjectView);
            
            gameObjectView.onDestroy -= RemoveTarget;
            gameObjectView.onDisable -= RemoveTarget;
            Debug.Log($"Remove target {gameObjectView.entityId}");
        }

        private void OnRadarEnter(Collider other)
        {
            if (other.gameObject.CompareTag(UnityGameObjectTag.Speeder))
            {
                GameObjectView otherView = other.gameObject.GetComponent<GameObjectView>();
                if (otherView != null)
                {
                    if(!TrackedTargets.Contains(otherView))
                        TrackedTargets.Add(otherView);
                    
                    otherView.onDestroy += RemoveTarget;
                    otherView.onDisable += RemoveTarget;
                    
                    Debug.Log($"Add target {otherView.entityId}");
                }
            }
        }

        public void SetPriorityTarget(uint entity, int score)
        {
            PriorityTarget = entity;
            PriorityTargetScore = score;
        }
    }
}