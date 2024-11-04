using Matterless.Inject;
using UnityEngine;

namespace Matterless.Floorcraft
{
    public class MarkerService : ITickable, IMarkerService
    {
        public enum MarkerType { Tip = 0, Arrow };

        private readonly Transform m_CameraTransform;
        private readonly IRaycastService m_RaycastService;
        private readonly MarkerTypeService m_MarkerTypeService;
        private readonly SpeederService m_SpeederService;
        private readonly MarkerView m_View;
        private readonly WorldScaleService m_WorldScaleService;

        private MarkerType m_MarkerType = MarkerType.Tip;
        private Vector3 m_Position;
        private Vector3 m_RotationPivotPoint;
        private float m_Scale;
        private Quaternion m_Rotation;
        private Transform m_Target;

        public bool isHidden { get; private set; } = true;

        public MarkerService(IAukiWrapper aukiWrapper, IRaycastService raycastService,
            MarkerTypeService markerTypeService, SpeederService speederService, WorldScaleService worldScaleService)
        {
            m_CameraTransform = aukiWrapper.arCamera.transform;
            m_RaycastService = raycastService;
            m_MarkerTypeService = markerTypeService;
            m_SpeederService = speederService;
            m_WorldScaleService = worldScaleService;
            m_View = MarkerView.Create();
            m_View.Hide();
        }

        public void Hide()
        {
            isHidden = true;
            m_View?.Hide();
        }

        public void Show()
        {
            isHidden = false;
            m_View.Show();
        }

        public void SetTarget(Transform target)
        {
            m_Target = target;
            m_MarkerTypeService.SetType(m_Target == null ? MarkerType.Tip : MarkerType.Arrow);
        } 
        
        public void Tick(float deltaTime, float unscaledDeltaTime)
        {
            m_RotationPivotPoint = m_SpeederService.TryGetSpeederView(m_SpeederService.serverSpeederEntity, out var speeder)
                ? speeder.transform.position : m_CameraTransform.position;
           
            m_View.SetTypeObject((int)m_MarkerTypeService.GetType());
            if (!m_RaycastService.hasHit)
                return;

            Place(m_RaycastService.hitPose.position);

            // update view
            m_View.UpdateView(
                m_Position, m_Rotation, m_Scale * m_WorldScaleService.worldScale,
                m_CameraTransform.position,
                m_Target == null ? m_Position : m_Target.position,
                m_RotationPivotPoint
                );
        }

        private void Place(Vector3 position)
        {
            if (isHidden)
            {
                m_Scale = 0;
                return;
            }

            m_Position = position;

            if (m_RaycastService != null && m_CameraTransform != null)
            {
                var distance = Vector3.Distance(position, m_CameraTransform.position);
                m_Scale = Mathf.Lerp(0.4f, 10, distance / 10f) * 2;
            }
        }
    }
}