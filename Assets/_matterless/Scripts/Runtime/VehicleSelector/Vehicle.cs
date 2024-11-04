using UnityEngine;
using UnityEngine.Serialization;

namespace Matterless.Floorcraft
{
    [System.Serializable]
    [CreateAssetMenu(menuName = "Matterless/Vehicle")]
    public class Vehicle : Asset
    {
        [SerializeField] private bool m_Premium = false;
        [SerializeField] private string m_NameTag;
        [SerializeField, FormerlySerializedAs("m_Prefab")] private GameObject m_SelectorPrefab;
        [SerializeField] private int m_Style;
        [SerializeField] private Color m_StyleColor;
        [SerializeField] private ushort m_MaxSpeed = 500;
        [SerializeField] private ushort m_MaxTurningRadius = 360;
        [SerializeField] private ushort m_BrakeDistance = 7;
        [SerializeField] private ushort m_Acceleration = 225;
        [SerializeField] private ushort m_BrakePower = 1000;
        [SerializeField] private ushort m_GroundClearance = 3;
        [SerializeField] private ushort m_BoostPower = 10;
        [SerializeField] private EngineVFXSettings m_EngineVFXSettings;

        public bool premium => m_Premium;
        public string nameTag => m_NameTag;
        
        public GameObject selectorPrefab => m_SelectorPrefab;
        public int style => m_Style;
        public Color styleColor => m_StyleColor;

        public float maxSpeed => (float)m_MaxSpeed / 1000f;
        public float maxTurningRadius => m_MaxTurningRadius;
        public float brakeDistance => (float)m_BrakeDistance/ 1000f;
        public float acceleration => (float)m_Acceleration/ 1000f;
        public float brakePower => (float)m_BrakePower/ 1000f;
        public float groundClearance => (float)m_GroundClearance / 1000f;

        public float boostPower => (float)m_BoostPower;
        
        public EngineVFXSettings engineVFXSettings => m_EngineVFXSettings;

    }
}