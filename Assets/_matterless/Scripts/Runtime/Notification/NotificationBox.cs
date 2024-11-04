using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Matterless.Floorcraft
{
    public class NotificationBox : MonoBehaviour
    {
        [SerializeField] private TMP_Text m_Text;
        [SerializeField] private Image m_NotificationIcon;
        [SerializeField] private Image m_NotificationBackground;

        private float m_FadeSpeed = 0.5f;
        private float m_WaitTime = 1.2f;
        private Coroutine m_Coroutine;
        
        public event Action<NotificationBox> onAnimationFinished;

        public void Show(string text, Sprite icon)
        {
            m_Text.text = text;
            m_NotificationIcon.sprite = icon;
            
            if (m_Coroutine != null)
            {
                StopCoroutine(m_Coroutine);
            }
            
            m_Coroutine = StartCoroutine(FadeNotification());
        }
        
        [ContextMenu("stop")]
        public void Pause()
        {
            if (m_Coroutine != null)
            {
                StopCoroutine(m_Coroutine);
            }
            SetColor(0);
        }
        
        private void OnAnimationFinished()
        {
            onAnimationFinished?.Invoke(this);
        }

        private IEnumerator FadeNotification()
        {
            
            float time = 0;
            
            //Fade in
            while (time < m_FadeSpeed)
            {
                float t = time / m_FadeSpeed;
                SetColor(t);
                time += Time.deltaTime;
                yield return null;
            }
            
            //Wait
            time = 0;
            while (time < m_WaitTime)
            {
                time += Time.deltaTime;
                yield return null;
            }
            
            //Fade out
            time = 0;
            while (time < m_FadeSpeed)
            {
                float t = 1 - time / m_FadeSpeed;
                SetColor(t);
                time += Time.deltaTime;
                yield return null;
            }
            
            SetColor(0);
            OnAnimationFinished();
            m_Coroutine = null;
        }

        private void SetColor(float alpha)
        {
            Color backroundColor = m_NotificationBackground.color;
            backroundColor.a = alpha;
            m_NotificationBackground.color = backroundColor;
            m_Text.color = new Color(1, 1, 1, alpha);
            m_NotificationIcon.color = new Color(1, 1, 1, alpha);
        }
    }
}