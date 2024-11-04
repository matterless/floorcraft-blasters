using UnityEngine;

namespace Matterless.Floorcraft
{
    public class MarkerView : MonoBehaviour
    {
        #region Inspector
        [SerializeField, Tooltip("The order should much MarkerType enum.")] private GameObject[] m_TypeObject;
        [SerializeField] private GameObject m_Ring;
        #endregion

        #region Factory
        public static MarkerView Create()
            => Instantiate(Resources.Load<MarkerView>("UIPrefabs/UIP_Marker"));
        #endregion

        public void Show() => this.gameObject.SetActive(true);

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void SetTypeObject(int index)
        {
            foreach (var item in m_TypeObject)
                item.SetActive(false);

            m_TypeObject[index].SetActive(true);
        }

        private Transform tipTransform => m_TypeObject[(int)MarkerService.MarkerType.Tip].transform;
        private Transform arrowTransform => m_TypeObject[(int)MarkerService.MarkerType.Arrow].transform;

        public void UpdateView(Vector3 positon, Quaternion rotation, float scale, Vector3 cameraPosition,
            Vector3 targetPosition, Vector3 originPoint)
        {
            // root object
            this.transform.SetPositionAndRotation(positon, rotation);
            this.transform.localScale = Vector3.one * scale;

            // tip 
            if (tipTransform.gameObject.activeSelf)
            {
                tipTransform.localPosition = Vector3.up * (0.00529f + Mathf.PingPong(Time.timeSinceLevelLoad * 0.02f, 0.005f));
                tipTransform.LookAt(2 * transform.position - cameraPosition);
                m_Ring.transform.Rotate(Vector3.up * Time.deltaTime * 100f);
            }

            // arrow
            if (arrowTransform.gameObject.activeSelf)
            {
                Vector3 direction = originPoint - targetPosition;
                if (direction.sqrMagnitude > float.Epsilon)
                {
                    arrowTransform.LookAt(new Vector3(originPoint.x, arrowTransform.position.y, originPoint.z));
                    m_Ring.transform.rotation = arrowTransform.rotation;
                }
            }
        }
    }
}