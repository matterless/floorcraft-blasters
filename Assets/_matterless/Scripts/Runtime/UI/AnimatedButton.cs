using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

namespace Matterless.Floorcraft
{
    public class AnimatedButton : Matterless.Module.UI.Button
    {
        private enum OnClickTransitionSpeed
        {
            Instant,
            Delayed
        }
        
        [SerializeField] private OnClickTransitionSpeed m_OnClickTransitionSpeed = OnClickTransitionSpeed.Delayed;
        [SerializeField] private bool m_ScaleDownOnDisable = true;

        private RectTransform m_RectTransform;

        #region Animation Settings

        private readonly float m_AnimationTime = 0.1f;
        private readonly float m_ScaleFactorInPixels = 30f;
        private readonly float m_MinScaleFactor = 0.80f;
        private readonly float m_FloatMinAlpha = 0.5f;

        #endregion

        private List<Graphic> m_GraphicsInsideButton = new List<Graphic>();
        private Coroutine m_Coroutine;

        protected override void Awake()
        {
            base.Awake();

            m_RectTransform = GetComponent<RectTransform>();
            
            List<Graphic> graphics = GetComponentsInChildren<Graphic>().ToList();
            AnimatedButtonElement[] animatedButtonElements = GetComponentsInChildren<AnimatedButtonElement>();
            
            if (animatedButtonElements.Length > 0)
            {
                foreach (AnimatedButtonElement animatedButtonElement in animatedButtonElements)
                {
                    if (animatedButtonElement.excludeFromAlphaAnimation)
                    {
                        graphics.Remove(animatedButtonElement.GetComponent<Graphic>());
                    }
                }
            }
            
            m_GraphicsInsideButton = graphics;

        }

        protected override void OnButtonDisabled(bool instant)
        {
            if (m_TargetGraphic == null)
                return;

            if (m_Coroutine != null)
                StopCoroutine(m_Coroutine);


            if (instant || m_OnClickTransitionSpeed == OnClickTransitionSpeed.Instant)
            {
                TransitionInstant(true, false, m_ScaleDownOnDisable);
                return;
            }

            m_Coroutine = StartCoroutine(Transition(true, false, m_ScaleDownOnDisable));
        }

        protected override void OnButtonPressed()
        {
            if (m_TargetGraphic == null)
                return;

            if (m_Coroutine != null)
                StopCoroutine(m_Coroutine);

            m_Coroutine = StartCoroutine(Transition(true));
        }

        protected override void OnButtonClicked()
        {
            if (m_TargetGraphic == null)
                return;

            if (m_Coroutine != null)
                StopCoroutine(m_Coroutine);

            if (m_OnClickTransitionSpeed == OnClickTransitionSpeed.Instant)
            {
                TransitionInstant(false, true);
                return;
            }

            m_Coroutine = StartCoroutine(Transition(false, true));
        }

        protected override void OnButtonReleased(bool instant)
        {
            if (m_TargetGraphic == null)
                return;

            if (m_Coroutine != null)
                StopCoroutine(m_Coroutine);
            
            if (m_OnClickTransitionSpeed == OnClickTransitionSpeed.Instant)
            {
                TransitionInstant(false, false);
                return;
            }

            m_Coroutine = StartCoroutine(Transition(false, false));
        }

        protected override void OnButtonDragged()
        {
            if (m_TargetGraphic == null)
                return;

            if (m_Coroutine != null)
                StopCoroutine(m_Coroutine);

            m_Coroutine = StartCoroutine(Transition(true, false, invertScale: true));
        }

        private IEnumerator Transition(bool isGoingToPressedState, bool isFinishingWithClick = false, bool isScaleDownEnabled = true, bool invertScale = false)
        {
            // Calculate scale factor for each button according to its size so that each button scales down the same amount of pixels
            float scaleFactor = (m_RectTransform.rect.width - m_ScaleFactorInPixels) / m_RectTransform.rect.width;
            scaleFactor = Mathf.Max(m_MinScaleFactor, isScaleDownEnabled ? scaleFactor : 1.0f);
            scaleFactor = Mathf.Min(1.35f, invertScale ? 1.0f / scaleFactor : scaleFactor);

            float time = 0;
            float duration = isGoingToPressedState ? m_AnimationTime : m_AnimationTime * 0.5f;
            

            while (time < duration)
            {
                time += Time.deltaTime;
                float t = isGoingToPressedState ? time / m_AnimationTime : 1 - time / duration;
                transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * scaleFactor, t);
                SetGraphicsAlpha(m_GraphicsInsideButton, Mathf.Lerp(1.0f, m_FloatMinAlpha, t));
                
                yield return null;
            }

            if (isGoingToPressedState)
            {
                transform.localScale = Vector3.one * scaleFactor;
                SetGraphicsAlpha(m_GraphicsInsideButton, m_FloatMinAlpha);
                yield break;
            }

            transform.localScale = Vector3.one;
            SetGraphicsAlpha(m_GraphicsInsideButton, 1.0f);

            if (isFinishingWithClick)
                m_OnClick?.Invoke();
        }

        protected void TransitionInstant(bool isGoingToPressedState, bool isFinishingWithClick = false, bool isScaleDownEnabled = true)
        {
            Vector3 scale = Vector3.one;
            float alpha = 1.0f;

            if (isGoingToPressedState)
            {
                if (isScaleDownEnabled)
                {
                    Rect rect;
                    if (m_RectTransform == null)
                        rect = GetComponent<RectTransform>().rect;
                    else
                        rect = m_RectTransform.rect;

                    float scaleFactor = (rect.width - m_ScaleFactorInPixels) / rect.width;
                    scaleFactor = Mathf.Max(m_MinScaleFactor, scaleFactor);


                    scale *= scaleFactor;    
                }
                
                alpha = m_FloatMinAlpha;
            }

            m_TargetGraphic.transform.localScale = scale;
            SetGraphicsAlpha(m_GraphicsInsideButton, alpha);
            
            if (isFinishingWithClick)
                m_OnClick?.Invoke();
        }

        protected override void InstantClearState()
        {
            base.InstantClearState();

            if (m_TargetGraphic == null)
                return;
            
            transform.localScale = Vector3.one;
            SetGraphicsAlpha(m_GraphicsInsideButton, 1.0f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetGraphicsAlpha(List<Graphic> graphics, float alpha)
        {
            for (int i = 0; i < graphics.Count; i++)
            {
                Color color = graphics[i].color;
                color.a = alpha;
                graphics[i].color = color;
            }
        }
    }
}


