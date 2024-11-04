using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Matterless.Floorcraft
{
    public class LeaderboardLabel : MonoBehaviour
    {
        public TextMeshProUGUI text;
        public RectTransform rect;
        public CanvasGroup bg;
        public int index = -1;

        public void MoveToPosition(Vector2 targetPosition,float duration)
        {
            CancelInvoke();
            Vector2 p2 = new Vector2(rect.anchoredPosition.x + 50f,
                rect.anchoredPosition.y+((targetPosition.y - rect.anchoredPosition.y) / 2f));
            List<Vector3> bezierPoints = GetBezierCurve(rect.anchoredPosition, p2, targetPosition, 10);
            StartCoroutine(MoveObjectToPositions(rect, bezierPoints, duration));
        }
        private IEnumerator MoveObjectToPositions(RectTransform obj, List<Vector3> positions, float duration)
        {
            int index = 0;
            Vector3 startPosition = obj.anchoredPosition;
            Vector3 targetPosition = positions[index];
            float startTime = Time.time;

            while (index < positions.Count)
            {
                float t = (Time.time - startTime) / duration;
                obj.anchoredPosition = Vector3.Lerp(startPosition, targetPosition, t);

                if (t >= 1.0f)
                {
                    index++;
                    if (index < positions.Count)
                    {
                        startPosition = targetPosition;
                        targetPosition = positions[index];
                        startTime = Time.time;
                    }
                }

                yield return null;
            }
            obj.anchoredPosition = positions[positions.Count - 1];
        }
      
        private List<Vector3> GetBezierCurve(Vector3 p0, Vector3 p1, Vector3 p2, int step)
        {
            List<Vector3> curve = new List<Vector3>();
            for (float t = 0; t <= 1; t += 1f/ (step-1))
            {
                var u = 1 - t;
                var tt = t * t;
                var uu = u * u;
                Vector3 p = uu * p0 + 2 * u * t * p1 + tt * p2;
                curve.Add(p);
            }
            curve.Add(p2);

            return curve;
        }
    }
}