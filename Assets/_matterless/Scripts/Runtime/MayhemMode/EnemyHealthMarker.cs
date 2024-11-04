using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Matterless.Floorcraft
{
    public class EnemyHealthMarker : MonoBehaviour
    {
        [FormerlySerializedAs("fullHealthBar")] [SerializeField] Image m_FullHealthBar;
        [FormerlySerializedAs("emptyHealthBar")] [SerializeField] Image m_EmptyHealthBar;

        public void Damage()
        {
            m_FullHealthBar.gameObject.SetActive(false);
            m_EmptyHealthBar.gameObject.SetActive(true);
        }

        public void Heal()
        {
            m_FullHealthBar.gameObject.SetActive(true);
            m_EmptyHealthBar.gameObject.SetActive(false);
        }
    }
}
