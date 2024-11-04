using UnityEngine;
using UnityEngine.Serialization;

namespace Matterless.Floorcraft
{
    [System.Serializable]
    public class DashSettings : IEquipmentSetting
    {
        [SerializeField] private float m_Duration = 0.12f;
        [SerializeField] private float m_Rate = 5f;
        [FormerlySerializedAs("m_CoolDown")][SerializeField] private float m_Cooldown = 7f;
        [SerializeField] private int m_Quantity;
        [SerializeField] private bool m_Infinite = false;

        public float duration => m_Duration;
        public int quantity => m_Quantity;
        public float rate => m_Rate;
        public float cooldown => m_Cooldown;
        public bool infinite => m_Infinite;
    }
}