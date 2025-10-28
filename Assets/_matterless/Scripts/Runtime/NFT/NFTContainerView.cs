using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Matterless.Floorcraft
{
    /// <summary>
    /// View component for displaying an NFT image in the UI
    /// </summary>
    public class NFTContainerView : MonoBehaviour
    {
        private Image m_backgroundImage;
        [SerializeField] private Image m_Image;
        [SerializeField] private TMP_Text m_loadingText;
        
        private void Awake()
        {
            m_backgroundImage = GetComponent<Image>();
            if (m_Image != null)
            {
                m_Image.gameObject.SetActive(false);
            }
        }
        
        public void SetImage(Sprite sprite)
        {
            if (m_Image != null && sprite != null)
            {
                m_Image.sprite = sprite;
                m_Image.gameObject.SetActive(true);
                m_backgroundImage.enabled = false;
                m_loadingText.text = "";
            }
        }
    }
}

