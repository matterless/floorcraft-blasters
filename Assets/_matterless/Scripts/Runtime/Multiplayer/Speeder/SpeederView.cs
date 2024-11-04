using System;
using Matterless.Audio;
using UnityEngine;

namespace Matterless.Floorcraft
{
    public class SpeederView : GameObjectView
    {
        #region Inspector
        [SerializeField] private Transform m_Shadow;
        [SerializeField] private GameObject m_UnderGlowHighlight;
        [SerializeField] private ParticleSystem m_ExplosionParticles;
        [SerializeField] private ParticleSystem m_WindTrails;
        [SerializeField] private ParticleSystem m_SkidParticles;
        [SerializeField] private TrailRenderer m_TrailRenderer;
        [SerializeField] private AudioSource m_ExplosionSound;
        [SerializeField] private SpeederSFX m_SpeederSFX;
        [SerializeField] private Collider m_Collider;
        [SerializeField] private Transform m_MeshPivot;
        [SerializeField] private Transform m_PowerUpPivot;
        [SerializeField] private EngineVFX m_EngineVFX;
        #endregion


        private Renderer m_CurrentRenderer;
        public Transform meshPivot => m_MeshPivot;
        private OffScreenIndicator m_OffScreenIndicator;
        private SpeederViewModel m_Last;
        private Vehicle m_Vehicle;
        private GameObject m_Crown;
        private FlameThrowerView m_Flame;
        private GameObject m_MagnetView;
        private LaserView m_LaserView;
        private WreckingBallProjectileView m_WreckingBallProjectileView;
        private string m_BaseName;
        private float m_BankLimit;
        private float m_LastBanking;
        private float m_BankFactor;
        private float m_BankRate;
        private float m_MaxSpeed;
        private GameObject m_MeshObject;
        
        public Collider boxCollider => GetComponent<Collider>();
        public WreckingBallProjectileView wreckingBallView => m_WreckingBallProjectileView;
       

        private int m_ToonOutlineLayer;
        private int m_DefaultLayer;

        public SpeederView Init
        (
            Camera arCamera,
            Vehicle vehicle,
            SpeederSimulation.Settings simulationSettings,
            CrownService.Settings crownSettings,
            WreckingBallMagnetService.Settings wreckingBallMagnetSettings,
            FlameThrowerService.Settings flamethrowerSettings,
            LaserBeamService laserService,
            LaserService.Settings laserSettings,
            SpeederViewModel speederViewModel,
            AudioService audioService,
            SpeederStateComponentService speederStateComponentService,
            WorldScaleService worldScaleService
        )
        {
            m_WorldScaleService = worldScaleService;
            m_SpeederStateComponentService = speederStateComponentService;
            m_ToonOutlineLayer = LayerMask.NameToLayer("Toon Outline");
            m_DefaultLayer = LayerMask.NameToLayer("Default");
            SetVehicle(vehicle);
            
            base.entityId = speederViewModel.entityId;
            m_BaseName = $"__SpeederView {entityId}";
            m_Shadow.gameObject.name = $"__SpeederView.Shadow {entityId}";
            m_ExplosionParticles.gameObject.name = $"__SpeederView.Explosion {entityId}";
            m_BankLimit = simulationSettings.bankLimit;
            m_BankFactor = simulationSettings.bankFactor;
            m_BankRate = simulationSettings.bankRate;
            m_MaxSpeed = vehicle.maxSpeed;
            m_Shadow.localScale = Vector3.one;
            m_Last = speederViewModel;
            transform.localScale = vehicle.scale;
            m_TrailRenderer.emitting = false;
            m_Shadow.transform.SetParent(null);
            m_ExplosionParticles.transform.SetParent(null);
            ExplosionView explosionView = m_ExplosionParticles.GetComponent<ExplosionView>();
            explosionView.SetScaleDependentValues(m_WorldScaleService.worldScale * vehicle.size);
            m_OffScreenIndicator = OffScreenIndicator.Create(arCamera, this.transform);
            m_OffScreenIndicator.ChangeIconColor(vehicle.styleColor);
            m_OffScreenIndicator.name = $"__SpeederView.OffScreenIndicator {entityId}";
            m_OffScreenIndicator.isHidde = false;
            transform.SetPositionAndRotation(speederViewModel.groundPosition, speederViewModel.orientation);
            Hide(); // NOTE: (Marko) Hide until we get the first update and the summon effect since we deleted InitRenderer function which deactivated the renderers
            m_Crown = Instantiate(Resources.Load<GameObject>(crownSettings.crownResourcePath));
            m_Crown.SetActive(false);
            m_Crown.transform.SetParent(transform);
            m_Crown.transform.localPosition = Vector3.zero + crownSettings.crownOffset.position;
            m_Crown.transform.localRotation = Quaternion.identity * crownSettings.crownOffset.rotation;
            m_Flame = Instantiate(Resources.Load<FlameThrowerView>(flamethrowerSettings.flameThrowerViewResourcePath));
            m_Flame.Init(entityId);
            var flameCollider = m_Flame.GetComponent<Collider>();
            Physics.IgnoreCollision(flameCollider, m_Collider);
            m_Flame.transform.SetParent(transform);
            m_Flame.transform.localScale = Vector3.one;
            m_Flame.transform.localPosition = Vector3.zero + flamethrowerSettings.flameOffset.position;
            m_Flame.transform.localRotation = Quaternion.identity * flamethrowerSettings.flameOffset.rotation;
            m_Flame.gameObject.SetActive(false);
            m_WreckingBallProjectileView = Instantiate(Resources.Load<WreckingBallProjectileView>(wreckingBallMagnetSettings.wreckingBallProjectile.resourcesPath));
            Destroy(m_WreckingBallProjectileView.rigidBody);
            m_WreckingBallProjectileView.transform.SetParent(transform);
            m_WreckingBallProjectileView.transform.localPosition = Vector3.zero + wreckingBallMagnetSettings.wreckingBallOffset;
            m_WreckingBallProjectileView.transform.localRotation = Quaternion.identity;
            m_WreckingBallProjectileView.gameObject.SetActive(false);
            m_MagnetView = Instantiate(Resources.Load<GameObject>(wreckingBallMagnetSettings.magnetViewResourcePath));
            m_MagnetView.gameObject.SetActive(false);
            m_MagnetView.transform.SetParent(transform);
            m_MagnetView.transform.localPosition = Vector3.zero;
            m_MagnetView.transform.localRotation = Quaternion.identity;
            m_LaserView = Instantiate(Resources.Load<LaserView>(laserSettings.laserViewResourcePath), m_PowerUpPivot, false);
            m_LaserView.gameObject.SetActive(false);
            m_LaserView.Init(entityId, laserSettings, m_SpeederSFX, m_Vehicle.size, laserService, audioService);

            m_EngineVFX.Init(vehicle.size, m_WorldScaleService.worldScale, vehicle.engineVFXSettings);
            m_EngineVFX.gameObject.name = $"__SpeederView.EngineVFX {entityId}";
            m_EngineVFX.transform.SetParent(null);
            return this;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (m_Shadow != null && m_Shadow.gameObject != null)
                Destroy(m_Shadow.gameObject);
            if (m_OffScreenIndicator != null && m_OffScreenIndicator.gameObject != null)
                Destroy(m_OffScreenIndicator.gameObject);
            if (m_EngineVFX != null && m_EngineVFX.gameObject != null)
                Destroy(m_EngineVFX.gameObject);
        }

        void SetScale(float scale)
        {
            this.transform.localScale = Vector3.one * scale;
            m_Shadow.transform.localScale = Vector3.one * scale;
        }

        #region Collisions
        public Action<SpeederView, Collision> onCollisionEntered;
        public Action<SpeederView, Collider> onTriggerEntered;
        private SpeederStateComponentService m_SpeederStateComponentService;
        private WorldScaleService m_WorldScaleService;


        private void OnTriggerEnter(Collider other)
        {
            onTriggerEntered?.Invoke(this, other);
        }

        private void OnCollisionEnter(Collision collision)
        {
            onCollisionEntered?.Invoke(this, collision);
        }
        #endregion

        #region Exposed VFX

        void UpdateShadowPose(Vector3 position, Quaternion rotation)
        {
            m_Shadow.SetPositionAndRotation(position, rotation);
        }

        public void Kill()
        {
            m_LaserView.ResetLaser();

            if(m_SpeederStateComponentService.TryGetComponentModel(entityId, out var speederState))
            {
                m_SpeederStateComponentService.UnsetState(entityId, SpeederState.LaserFire | SpeederState.LaserCharge);
            }

            m_ExplosionParticles.transform.position = this.transform.position;
            m_ExplosionParticles.Play();
            m_ExplosionSound.Play();
            Hide();
        }

        void Hide()
        {
            m_OffScreenIndicator.isHidde = true;
            m_Shadow.gameObject.SetActive(false);
            this.gameObject.SetActive(false);
            m_EngineVFX.Hide();
        }

        void Show()
        {
            m_Shadow.gameObject.SetActive(true);
            m_OffScreenIndicator.isHidde = false;
            this.gameObject.SetActive(true);
            m_EngineVFX.Show();
        }

        void FadeSpeeder(float delta)
        {
            foreach (Material material in m_CurrentRenderer.materials) 
                    material.SetFloat("_Summon", Mathf.Min(delta * 0.5f ,1));
            
            if (delta > 1.0f)
                m_CurrentRenderer.gameObject.layer = m_ToonOutlineLayer;
            else
                m_CurrentRenderer.gameObject.layer = m_DefaultLayer;
            
            // So Particles are not visible while speeder is spawning
            if ((delta * 0.5f) < 0.5f)
                return;;
            
            m_EngineVFX.shouldShowParticles = true;
        }

        public ParticleSystem.EmissionModule GetWindTrailsEmission()
        {
            return m_WindTrails.emission;
        }

        void UpdateWindTrails(bool enabled)
        {
            var emission = m_WindTrails.emission;
            emission.enabled = enabled && !m_TrailRenderer.emitting;
        }

        void UpdateTrailRenderer(bool emit)
        {
            m_TrailRenderer.emitting = emit;
        }
        
        public void SetUnderGlowHighlight(bool enabled)
        {
            m_UnderGlowHighlight.SetActive(enabled);
        }

        #endregion

        #region Exposed SFX
        void SFXSet(float value, bool mute = false) => m_SpeederSFX.Set(value, mute);
        void SFXSetSkid(bool b) => m_SpeederSFX.SetSkid(b);
        public void SFXUpdateBoostingValue(float boosting) => m_SpeederSFX.UpdateBoostingValue(boosting);
        #endregion
        
        public void UpdateView(SpeederViewModel viewState)
        {
            var bobCycle = 1 + Mathf.Sin(Time.time * 2) * 0.5f;
            var bob = bobCycle * m_Vehicle.groundClearance;
            var position = viewState.groundPosition + viewState.floorNormal * bob;
            var shadowPosition = viewState.groundPosition + (viewState.floorNormal * (0.002f));
            var speedProportion = viewState.speed / m_MaxSpeed; 
            var visible = viewState.speederState.HasFlag(SpeederState.Totaled) ? 0f : 1f;
            var scale = Mathf.Clamp01(6 * (viewState.age - 0.5f)) * m_Vehicle.size * m_WorldScaleService.worldScale * visible;
            var alwaysVisibleScale = Mathf.Clamp01(6 * (viewState.age - 0.5f)) * m_Vehicle.size * m_WorldScaleService.worldScale;
            var bankValue = Vector3.Dot
            (
             Vector3.Cross(m_Last.orientation * Vector3.forward, viewState.orientation * Vector3.forward),
             Vector3.up
            );
            bankValue /= Time.deltaTime;
            var maxBank = viewState.speed < Mathf.Epsilon ? 0 : 
                Mathf.Clamp(m_BankFactor * bankValue, -m_BankLimit, m_BankLimit);
            var banking = Mathf.MoveTowards(m_LastBanking, maxBank, Time.deltaTime * m_BankRate);
            var skidding = Mathf.Abs(m_BankLimit - Mathf.Abs(banking)) < Mathf.Epsilon;
            var bankVec = banking * (viewState.speed / m_MaxSpeed);
            var rotation = Quaternion.LookRotation
            (
                viewState.orientation * Vector3.forward,
                Quaternion.AngleAxis(bankVec, viewState.orientation * Vector3.forward) * Vector3.up
            );
            m_Flame.gameObject.SetActive(viewState.speederState.HasFlag(SpeederState.FlameThrower));
            
            if (viewState.speederState.HasFlag(SpeederState.LaserFire) && !m_Last.speederState.HasFlag(SpeederState.LaserFire))
            {
                m_LaserView.ManualFire();
            }
            else if (viewState.speederState.HasFlag(SpeederState.LaserCharge) && !m_Last.speederState.HasFlag(SpeederState.LaserCharge))
            {
                m_LaserView.StartCharging(false);
            }
            
            m_LaserView.gameObject.SetActive(viewState.equipmentState == EquipmentState.Laser);
            //m_LaserView.pivot.gameObject.SetActive(viewState.speederState.HasFlag(SpeederState.LaserCharge) || viewState.speederState.HasFlag(SpeederState.LaserFire));
            m_MagnetView.SetActive(viewState.equipmentState == EquipmentState.MagnetAndWreckingBall || viewState.equipmentState == EquipmentState.Magnet);
            m_WreckingBallProjectileView.gameObject.SetActive(viewState.equipmentState == EquipmentState.MagnetAndWreckingBall);
            m_Crown.SetActive(viewState.crownKeeper);
            SFXSet(speedProportion,
                viewState.speederState.HasFlag(SpeederState.Loading) || viewState.speederState.HasFlag(SpeederState.Totaled));
            UpdateWindTrails((speedProportion) > 0.9f);
            SFXSetSkid(skidding);
            UpdateShadowPose(shadowPosition, viewState.orientation);
            m_UnderGlowHighlight.GetComponent<Renderer>().material.SetFloat("_Scale", 6 / m_WorldScaleService.worldScale);
            FadeSpeeder(viewState.age);
            SetScale(scale * visible);
            UpdateTrailRenderer(viewState.boosting > 0 && viewState.braking < 0);
            if (Application.isEditor)
            {
                name = $"{m_BaseName} :{viewState.speederState}";
            }
            if (viewState.speederState.HasFlag(SpeederState.Totaled) ||
                viewState.speederState.HasFlag(SpeederState.Loading))
            {
                Hide();    
            }
            else
            {
                Show();
            }
            transform.SetPositionAndRotation(position, rotation);
            m_EngineVFX.UpdateVFX(viewState.speed * 1.0f/m_WorldScaleService.worldScale  / m_MaxSpeed , m_WorldScaleService.worldScale);
            m_EngineVFX.transform.SetPositionAndRotation(meshPivot.transform.position, rotation);
            m_EngineVFX.transform.localScale = Vector3.one * scale;
            m_ExplosionParticles.transform.localScale = Vector3.one * alwaysVisibleScale;
            if (viewState.speederState.HasFlag(SpeederState.Boosting) && !m_Last.speederState.HasFlag(SpeederState.Boosting))
            {
                m_SpeederSFX.Nitro();
            }
            if (viewState.speederState.HasFlag(SpeederState.Braking) && !m_Last.speederState.HasFlag(SpeederState.Braking))
            {
                m_SpeederSFX.Brake();
            }
            if (viewState.speederState.HasFlag(SpeederState.OverHeat))
            {
                m_SpeederSFX.Brake();
            }
            m_LastBanking = banking;
            m_Last = viewState;
        }
        public void SetVehicle(Vehicle vehicle)
        {
            if(m_MeshObject != null)
                Destroy(m_MeshObject);

            m_MeshObject = Instantiate(vehicle.selectorPrefab, m_MeshPivot);
            m_MeshObject.transform.localScale = Vector3.one;
            m_MeshObject.transform.localPosition = Vector3.zero;
            m_MeshObject.layer = 0;

            foreach (var gameObject in m_MeshObject.GetComponentsInChildren<Transform>())
                gameObject.gameObject.layer = 0;

            m_CurrentRenderer = m_MeshObject.GetComponent<Renderer>();

            m_Vehicle = vehicle;

            if (m_OffScreenIndicator != null)
            {
                m_OffScreenIndicator.ChangeIconColor(vehicle.styleColor);
            }
        }
    }
}