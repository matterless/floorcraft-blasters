using UnityEngine;
using UnityEngine.UI;

namespace Matterless.Floorcraft
{
    public class OffScreenIndicator : MonoBehaviour
    {
        [SerializeField] private RectTransform m_Parent;
        [SerializeField] private RectTransform m_RectTransform;
        [SerializeField] private RectTransform m_Dot;
        [SerializeField] private RectTransform m_Arrow;
        [SerializeField] private RectTransform m_Icon;
        [SerializeField] private RawImage m_IconImage;
        [SerializeField] private Vector2 m_Margin;
        [SerializeField] private float m_Scale;
        [SerializeField] private Transform m_Target;

        public bool isHidde { get; set; }


        private Camera m_ArCamera;
        private Vector2 m_Anchor;
        private float m_IconVisibility;
        private float m_WarmupTimer;
        private const float WARMUP_TIME = 0.5f;

        public static OffScreenIndicator Create(Camera arCamera, Transform target)
            => Instantiate(Resources.Load<GameObject>("UIPrefabs/UIP_OffScreenIndicator"))
                .GetComponentInChildren<OffScreenIndicator>().Init(arCamera, target);
            

        private OffScreenIndicator Init(Camera arCamera, Transform target)
        {
            m_ArCamera = arCamera;
            m_Target = target;
            isHidde = false;
            m_Icon.localScale = m_Arrow.localScale = m_Dot.localScale = Vector3.zero;
            m_WarmupTimer = 0;
            return this;
        }

        public void ChangeIconColor(Color color)
        {
            m_IconImage.color = color;
        }
        void Update()
        {
            m_RectTransform.localScale = m_Target == null || isHidde ? Vector3.zero : Vector3.one * m_Scale;

            //Debug.Log($"isHidden: {isHidde} | scale: {m_RectTransform.localScale}");
            //Debug.Log($"m_ArCamera: {m_ArCamera == null} | m_Target: {m_Target == null}");

            if (m_ArCamera == null)
                m_ArCamera = Camera.main;

            if (m_Target == null || m_ArCamera == null) 
                return;

            var pos = m_ArCamera.WorldToViewportPoint(m_Target.position);
            var canvasPos = new Vector2(pos.x - 0.5f, pos.y - 0.5f);
            var clampedCanvasPos = new Vector2(Mathf.Clamp(canvasPos.x, -0.5f, 0.5f),
                                 Mathf.Clamp(canvasPos.y, -0.5f, 0.5f));
            var extension = new Vector2(Mathf.Abs(clampedCanvasPos.x) * 2 - (1 - m_Margin.x),
                                        Mathf.Abs(clampedCanvasPos.y) * 2 - (1 - m_Margin.y));
            var exp = Mathf.Max(Mathf.Clamp01(extension.x) / m_Margin.x, Mathf.Clamp01(extension.y) / m_Margin.y);

            canvasPos = new Vector2(canvasPos.x * m_Parent.sizeDelta.x, canvasPos.y * m_Parent.sizeDelta.y);
            clampedCanvasPos = new Vector2(clampedCanvasPos.x * m_Parent.sizeDelta.x, clampedCanvasPos.y * m_Parent.sizeDelta.y);
            m_Anchor = clampedCanvasPos;

            m_WarmupTimer = Mathf.MoveTowards(m_WarmupTimer, exp >= 0.99f ? WARMUP_TIME : 0, Time.deltaTime);
            var iconVisibility = (m_WarmupTimer + Mathf.Epsilon) >= WARMUP_TIME;
            m_IconVisibility = Mathf.MoveTowards(m_IconVisibility, iconVisibility ? 1 : 0, Time.deltaTime * 10);
            m_Icon.localScale = Vector3.one * m_IconVisibility;
            m_Arrow.localScale = Vector3.one * m_IconVisibility;
            m_Dot.localScale = iconVisibility ? Vector3.one * (1 - m_IconVisibility) : Vector3.one * (1 - Mathf.Pow(2, -5 * exp));

            m_Arrow.localEulerAngles = Vector3.forward * Mathf.Rad2Deg * Mathf.Atan2(canvasPos.y - m_Anchor.y, canvasPos.x - m_Anchor.x);
            m_Icon.localPosition = -(canvasPos - m_Anchor).normalized * 50 * m_IconVisibility;
            m_RectTransform.anchoredPosition = Vector2.MoveTowards(m_RectTransform.anchoredPosition,
                                            m_Anchor,
                                            (m_RectTransform.anchoredPosition - m_Anchor).sqrMagnitude * Time.deltaTime);
        }

        private void OnDestroy()
        {
            Destroy(transform.parent.gameObject);
        }
    }
}
