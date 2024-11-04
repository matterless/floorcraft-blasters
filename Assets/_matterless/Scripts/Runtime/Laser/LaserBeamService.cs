using System.Collections.Generic;
using Matterless.Inject;
using UnityEngine;
using UObject = UnityEngine.Object;

namespace Matterless.Floorcraft
{

    public class LaserBeamService : ITickable
    {
        private readonly LaserService.Settings m_Settings;
        private readonly WorldScaleService m_WorldScaleService;
        private readonly IPoolingService m_PoolingService;
        private List<LaserBeamView> m_LaserViews = new();
        private readonly RaycastHit[] m_ResultsCache = new RaycastHit[20];

        public LaserBeamService(
            LaserService.Settings settings, 
            WorldScaleService worldScaleService,
            IPoolingService poolingService)
        {
            m_Settings = settings;
            m_WorldScaleService = worldScaleService;
            m_PoolingService = poolingService;
            m_PoolingService.SetLimit<LaserBeamView>(100);
        }

        static string resourcePath = "PFB_LaserBeam"; //m_Settings.laserViewResourcePath

        private LaserBeamView InstnatiateLaserBeamView()
        {
            return UObject.Instantiate(Resources.Load<LaserBeamView>(resourcePath));
        }

        public void CreateLaserBeam(uint owner, float originScale, Transform origin)
        {
            // get from pool
            LaserBeamView laserView =
                m_PoolingService.Pop<LaserBeamView>(InstnatiateLaserBeamView);


            laserView.transform.SetPositionAndRotation(origin.position, origin.rotation);
            laserView.Init(owner, m_Settings, originScale, m_WorldScaleService.worldScale, origin, OnLaserFinished);
            UpdateLaserLength(laserView);
            laserView.StartFiring();
            m_LaserViews.Add(laserView);
        }

        private void OnLaserFinished(LaserBeamView laser)
        {
            m_LaserViews.Remove(laser);
            m_PoolingService.Push<LaserBeamView>(laser, ()=>UObject.Destroy(laser.gameObject));
        }

        private void UpdateLaserLength(LaserBeamView laserBeamView)
        {
            var ray = new Ray
            {
                // aligned with the laser turret.
                origin = laserBeamView.OriginPosition,
                direction = laserBeamView.OriginRotation * Vector3.forward
            };

            var laserLength = m_Settings.laserMaxLength * m_WorldScaleService.worldScale;
            if (
                Physics.RaycastNonAlloc(
                    ray, 
                    m_ResultsCache, 
                    m_Settings.laserMaxLength, 
                    m_Settings.laserLayerMask,
                    QueryTriggerInteraction.Ignore)
                > 0
            )
            {
                foreach (RaycastHit raycastHit in m_ResultsCache)
                {
                    var distance = Vector3.Distance(ray.origin, raycastHit.point);
                    if (distance < laserLength)
                    {
                        laserLength = distance;
                    }
                }

                //laserLength += 0.025f;
                laserBeamView.HitDistance = laserLength;
            }
            
        }

        public void Tick(float deltaTime, float unscaledDeltaTime)
        {
            foreach (LaserBeamView laserBeamView in m_LaserViews)
            {
                UpdateLaserLength(laserBeamView);
            }
        }
    }
}