using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace Matterless.Floorcraft
{
    public class ARPlaneOverlayService
    {
        private readonly ARPlaneManager m_ARPlaneManager;
        private readonly Settings m_Settings;
        private readonly List<ARPlane> m_ARPlanes = new List<ARPlane>();
        
        public ARPlaneOverlayService(ARPlaneManager arPlaneManager, Settings settings)
        {
            m_ARPlaneManager = arPlaneManager;
            m_Settings = settings;
        }
        
        public void SetPlaneOverlay(bool on)
        {
            Debug.Log($"SetPLaneOverlay {on}");
            
            if (on)
            {
                foreach (ARPlane arPlane in m_ARPlaneManager.trackables)
                {
                    AddPlane(arPlane);
                }
                m_ARPlaneManager.planesChanged += UpdateARPlanes;
            }
            else
            {
                m_ARPlaneManager.planePrefab.GetComponent<MeshRenderer>().material 
                    = m_Settings.hidePlaneMaterial;
                
                foreach (var plane in m_ARPlanes)
                {
                    plane.GetComponent<MeshRenderer>().material = m_Settings.hidePlaneMaterial;
                }
                
                m_ARPlaneManager.planesChanged -= UpdateARPlanes;
                m_ARPlanes.Clear();
            }
        }

        private void UpdateARPlanes(ARPlanesChangedEventArgs planesChanged)
        {
            Debug.Log($"UpdateARPlanes {planesChanged.added.Count} / {planesChanged.removed.Count} / {planesChanged.updated}");
            
            planesChanged.added.ForEach(AddPlane);
            planesChanged.removed.ForEach(plane => m_ARPlanes.Remove(plane));
            planesChanged.updated.ForEach(plane => m_ARPlanes[m_ARPlanes.FindIndex(arPlane => arPlane.trackableId == plane.trackableId)] = plane);
        }
        
        private void AddPlane(ARPlane arPlane)
        {
            Debug.Log($"AddPlane {arPlane.alignment}");
            
            Material[] showPlaneMaterials;
            
            if (arPlane.alignment == UnityEngine.XR.ARSubsystems.PlaneAlignment.HorizontalUp)
                showPlaneMaterials = m_Settings.showPlaneMaterialsHorizontal;
            else
                showPlaneMaterials = m_Settings.showPlaneMaterialsVertical;

            arPlane.GetComponent<MeshRenderer>().materials = new Material[]
            {
                showPlaneMaterials[Random.Range(0, showPlaneMaterials.Length - 1)]
            };
            
            m_ARPlanes.Add(arPlane);
        }
        
        [System.Serializable]
        public class Settings
        {
            [SerializeField] private Material[] m_ShowPlaneMaterialsHorizontal;
            [SerializeField] private Material[] m_ShowPlaneMaterialsVertical;
            [SerializeField] private Material m_HidePlaneMaterial;

            public Material[] showPlaneMaterialsHorizontal => m_ShowPlaneMaterialsHorizontal;
            public Material[] showPlaneMaterialsVertical => m_ShowPlaneMaterialsVertical;
            public Material hidePlaneMaterial => m_HidePlaneMaterial;
        }
    }
}