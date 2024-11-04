using System;
using System.Collections;
using Matterless.UTools;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Button = Matterless.Module.UI.Button;

namespace Matterless.Floorcraft
{
    public class SidebarUiView : UIView<SidebarUiView>
    {
        public event Action onSettingsButtonClicked;
        public event Action onScreenshotButtonClicked;
        public event Action onRecordButtonClicked;
        public event Action onMenuButtonClicked;

        [SerializeField] private Button m_SettingsButton;
        [SerializeField] private Button m_ScreenshotButton;
        [SerializeField] private Button m_RecordButton;
        [SerializeField] private Image m_StopRecordButtonImage;
        [SerializeField] private GameObject m_StartRecordButtonImage;
        [SerializeField] private Button m_MenuButton;
        [SerializeField] private Image m_MenuButtonIcon;
        [SerializeField] private RectTransform m_ViewTransform;
        [SerializeField] private Sprite m_OpenMenuButtonSprite;
        [SerializeField] private Sprite m_CloseMenuButtonSprite;
        [SerializeField] private Canvas m_ViewCanvas;
        [SerializeField] private RectTransform m_SafeAreaTransform;

        [SerializeField] private GameObject m_RecordingButtonRecordingCircle;

        private bool m_IsMenuOpen;
        private bool m_IsOpenedUp;
        private Button[] m_Buttons;
        
        private Material   m_RecordButtonRecordingCircleFillMaterial;
        private Coroutine m_Coroutine;
        private static readonly int FillAmount = Shader.PropertyToID("_FillAmount");

        public Vector2 Position => m_ViewTransform.anchoredPosition;

        public override SidebarUiView Init()
        {
            AddListeners();
            m_Buttons = new Button[] {m_SettingsButton, m_ScreenshotButton, m_RecordButton};
            m_RecordButtonRecordingCircleFillMaterial = Instantiate(m_RecordingButtonRecordingCircle.GetComponent<Image>().material);
            m_RecordingButtonRecordingCircle.GetComponent<Image>().material = m_RecordButtonRecordingCircleFillMaterial;
            return this;
        }

        private void OnDestroy()
        {
            RemoveListeners();
        }

        private void AddListeners()
        {
            m_SettingsButton.onClick.AddListener(() => onSettingsButtonClicked?.Invoke());
            m_ScreenshotButton.onClick.AddListener(() => onScreenshotButtonClicked?.Invoke());
            m_RecordButton.onClick.AddListener(() => onRecordButtonClicked?.Invoke());
            
            m_MenuButton.onDrag.AddListener(OnButtonDragged);
            m_MenuButton.onClick.AddListener(() => onMenuButtonClicked?.Invoke());
        }

        private void RemoveListeners()
        {
            m_SettingsButton.onClick.RemoveAllListeners();
            m_ScreenshotButton.onClick.RemoveAllListeners();
            m_RecordButton.onClick.RemoveAllListeners();
        }

        private void OnButtonDragged(Vector2 delta)
        {
            m_ViewTransform.anchoredPosition = clampToCanvas(m_ViewTransform.anchoredPosition + delta / m_ViewCanvas.scaleFactor);
        }
        
        private Vector2 clampToCanvas(Vector2 p)
        {
            // 62.5f is half the size of the button
            float buttonMargin = 62.5f;

            
            float width = m_SafeAreaTransform.rect.width;
            float height = m_SafeAreaTransform.rect.height;
            
            Vector2 topLeft = new Vector2(buttonMargin, height - buttonMargin);
            Vector2 bottomRight = new Vector2(width - buttonMargin, buttonMargin);
            
            if (m_IsMenuOpen)
            {
                if (m_IsOpenedUp)
                    topLeft.y = m_SafeAreaTransform.rect.height - m_ViewTransform.rect.height + buttonMargin;
                else
                    bottomRight.y = m_ViewTransform.rect.height - buttonMargin;
            }
            
            float x = p.x, y = p.y;

            if (x > bottomRight.x)
                x = bottomRight.x;
            else if (x < topLeft.x)
                x = topLeft.x;

            if (y < bottomRight.y)
                y = bottomRight.y;
            else if (y > topLeft.y)
                y = topLeft.y;

            return new Vector2(x, y);
        }
        
        public void OpenCloseMenu()
        {
            m_IsMenuOpen = !m_IsMenuOpen;
            m_MenuButtonIcon.sprite = m_IsMenuOpen
                ? m_CloseMenuButtonSprite
                : m_OpenMenuButtonSprite;
            
            
            if (m_Coroutine != null)
                StopCoroutine(m_Coroutine);
            
            m_IsOpenedUp = m_ViewTransform.anchoredPosition.y < m_SafeAreaTransform.rect.height * 0.5f;
            m_Coroutine = StartCoroutine(AnimateButtons(m_IsMenuOpen, m_IsOpenedUp));
            
        }
        
        private static Vector3 ExponentialLerp (Vector3 a, Vector3 b, float t, float k = 3.0f)
        {
            return Vector3.Lerp(a, b, 1 - Mathf.Pow(k, -t));
        }
        
        private IEnumerator AnimateButtons(bool isOpening, bool isOpenedUp)
        {
            
            Vector3 direction = isOpenedUp ? Vector3.up : Vector3.down;
            Vector3 endPosition;
            
            float time = 0;
            while (time < 0.8f)
            {
                time += Time.deltaTime;
                for (int i = 0; i < m_Buttons.Length; i++)
                {
                    endPosition = isOpening ? m_ViewTransform.position + direction * ((i + 1) * 165.0f * m_ViewCanvas.scaleFactor) : m_ViewTransform.position;    
                    m_Buttons[i].transform.position = ExponentialLerp(m_Buttons[i].transform.position,endPosition, 4 * Time.deltaTime, 6.0f);
                }
                yield return null;
            }
            
            for (int i = 0; i < m_Buttons.Length; i++)
            {
                endPosition = isOpening ? m_ViewTransform.position + direction * ((i + 1) * 165.0f * m_ViewCanvas.scaleFactor) : m_ViewTransform.position;    
                m_Buttons[i].transform.position = endPosition;
            }
            
            m_Coroutine = null;
        }

        public void ToggleRecordButton(bool isRecording)
        {
            m_StartRecordButtonImage.SetActive(!isRecording);
            m_StopRecordButtonImage.gameObject.SetActive(isRecording);
            
            m_RecordingButtonRecordingCircle.SetActive(isRecording);
            m_RecordButtonRecordingCircleFillMaterial.SetFloat(FillAmount, 0);
        }
        
        public void ResetRecordButton()
        {
            m_StartRecordButtonImage.SetActive(true);
            m_StopRecordButtonImage.gameObject.SetActive(false);
        }

        public void SetFloatingButtonPosition(Vector2 position)
        {
            m_ViewTransform.anchoredPosition = clampToCanvas(position);
        }

        public void SetRecordingProgress(float timePassed, float maxTime, float f)
        {
            m_RecordButtonRecordingCircleFillMaterial.SetFloat(FillAmount, f);
            Color color = m_StopRecordButtonImage.color;
            color.a = Mathf.Sin(timePassed * 5.0f) * 0.5f + 0.5f;
            m_StopRecordButtonImage.color = color;
        }
    }
}