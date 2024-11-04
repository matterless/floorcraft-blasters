using System.Collections.Generic;
using UnityEngine;

namespace Matterless.Floorcraft
{
    public class LeaderboardView : UIView<LeaderboardView>
    {
        #region Inspector
        [SerializeField] private LeaderboardLabel m_Label;
        [SerializeField] private LeaderboardLabel m_TestLabel;
        [SerializeField] private Transform m_Container;
        [SerializeField] private RectTransform m_ContainerRect;
        #endregion

        private float m_CanvasScaleFactor;
        private Vector2 m_StartPoint;
        private static float m_SpaceBetweenLabels;
        private static float m_fontSize;
        private static float m_ShowTop;
        private static float m_SwipeDuration;
        private Dictionary<uint, LeaderboardLabel> m_LabelDictionary = new Dictionary<uint, LeaderboardLabel>();

        private void Awake()
        {
            m_StartPoint = m_Label.rect.anchoredPosition;
            m_Label.text.fontSize = m_fontSize;
            m_CanvasScaleFactor = 1f;
        }

        #region Factory
        public static LeaderboardView Create(float spaceBetweenLabels, float fontSize,int showTop,float swipeDuration, string path)
        {
            m_SpaceBetweenLabels = spaceBetweenLabels;
            m_fontSize = fontSize;
            m_ShowTop = showTop;
            m_SwipeDuration = swipeDuration;
            return Create(path);
        }

        #endregion

        public void AddLabel(uint entity)
        {
            LeaderboardLabel label = Instantiate(m_Label,m_Container);
            m_LabelDictionary.Add(entity,label);
            label.gameObject.SetActive(true);
        }

        public void UpdateLabel(List<uint> order, Dictionary<uint,ScoreComponentModel> data,Dictionary<uint,string> nameData, uint crown)
        {
            CancelInvoke();
            // play animation
            for (int i = 0; i < order.Count; i++)
            {
                int labelIndex = i;
                if (labelIndex < order.Count && m_LabelDictionary.ContainsKey(order[labelIndex]))
                {
                    UpdateLabelTransparency(m_LabelDictionary[order[labelIndex]],
                        m_ShowTop <= order.Count - 1 - i ? 0.5f : 1f);

                    // TODO : replace the name 
                    if (order[i] == crown)
                    {
                        m_LabelDictionary[order[i]].text.text =
                            $"{order.Count - labelIndex}. {nameData[order[i]]}- <sprite name=\"crown\"> {data[order[labelIndex]].model.score}";
                    }
                    else
                    {
                        m_LabelDictionary[order[i]].text.text =
                            $"{order.Count - labelIndex}. {nameData[order[i]]}- {data[order[labelIndex]].model.score}";
                    }

                    if (m_LabelDictionary[order[i]].index != order.Count - i)
                    {
                        UpdateLabelPosition(m_LabelDictionary[order[labelIndex]],
                            m_StartPoint - new Vector2(0, m_SpaceBetweenLabels * (order.Count - 1 - labelIndex)));
                    }

                    m_LabelDictionary[order[i]].index = order.Count - i;

                    float textSize = m_LabelDictionary[order[labelIndex]].text.GetPreferredValues().x *
                                     m_CanvasScaleFactor +
                                     m_LabelDictionary[order[labelIndex]].text.rectTransform.anchoredPosition.x * 2;
                    m_LabelDictionary[order[labelIndex]].rect.sizeDelta = new Vector2(textSize,
                        m_LabelDictionary[order[labelIndex]].rect.sizeDelta.y);
                }
            }
        }

        private void UpdateLabelTransparency(LeaderboardLabel label, float alpha)
        {
            label.bg.alpha = alpha;
        }

        public void DeleteLabel(uint entity)
        {
            if (!m_LabelDictionary.ContainsKey(entity))
                return;
            
            Destroy(m_LabelDictionary[entity].gameObject);
            m_LabelDictionary.Remove(entity);
        }

        public void ResetLeaderboard()
        {
            foreach (var label in m_LabelDictionary)
            {
                DeleteLabel(label.Key);
            }
            m_LabelDictionary.Clear();
        }

        public void SetLeaderboardOrientation(ScreenOrientation orientation)
        {
            if (orientation == ScreenOrientation.LandscapeLeft)
            {
                m_ContainerRect.pivot = new Vector2(0, 1);
                m_ContainerRect.anchorMax = new Vector2(1, 1);
                m_ContainerRect.anchorMin = new Vector2(1, 0);
                m_ContainerRect.anchoredPosition = new Vector2(-1100, -650);
                m_ContainerRect.offsetMin = new Vector2(m_ContainerRect.offsetMin.x, 1100);
                m_ContainerRect.localScale = new Vector3(2, 2, 2);
            }
            else if (orientation == ScreenOrientation.LandscapeRight)
            {
                m_ContainerRect.pivot = new Vector2(0, 1);
                m_ContainerRect.anchorMax = new Vector2(0, 1);
                m_ContainerRect.anchorMin = new Vector2(0, 0);
                m_ContainerRect.anchoredPosition = new Vector2(350, -650);
                m_ContainerRect.offsetMin = new Vector2(m_ContainerRect.offsetMin.x, 1100);
                m_ContainerRect.localScale = new Vector3(2, 2, 2);
            }
            else if (orientation == ScreenOrientation.Portrait || orientation == ScreenOrientation.PortraitUpsideDown)
            {
                m_ContainerRect.pivot = new Vector2(0, 1);
                m_ContainerRect.anchorMax = new Vector2(0, 1);
                m_ContainerRect.anchorMin = new Vector2(0, 0);
                m_ContainerRect.anchoredPosition = new Vector2(0, -300);
                m_ContainerRect.offsetMin = new Vector2(m_ContainerRect.offsetMin.x, 500);
                m_ContainerRect.localScale = new Vector3(1, 1, 1);
            }
        }

        private void UpdateLabelPosition(LeaderboardLabel label , Vector2 targetPosition)
        {
            label.MoveToPosition(targetPosition,m_SwipeDuration/10f);
        }
    }

}
