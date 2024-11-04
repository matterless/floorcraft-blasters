using UnityEngine;
using UnityEngine.UI;

namespace Matterless.Floorcraft.TestECS
{
    public class TestECSColorEntry : MonoBehaviour
    {
        [SerializeField] Image m_Image;
        [SerializeField] GameObject m_IsMineOutline;
        [SerializeField] Text m_Text;

        public uint id { get; private set; }

        public void Hide() => this.gameObject.SetActive(false);

        public TestECSColorEntry Init(uint id, bool isMine, Color color)
        {
            this.id = id;
            m_Image.color = color;
            m_IsMineOutline.SetActive(isMine);
            m_Text.text = id.ToString();
            this.gameObject.SetActive(true);
            return this;
        }

        public void UpdateColor(Color color)
        {
            m_Image.color = color;
        }
    }
}