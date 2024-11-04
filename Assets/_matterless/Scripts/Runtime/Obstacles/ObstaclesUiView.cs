using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Matterless.Floorcraft
{
    public class ObstaclesUiView : MonoBehaviour
    {
        public event Action onPlaneCreateButtonClicked;
        public event Action onPlaneMatterlessCreateButtonClicked;
        
        [SerializeField] private Button m_PlaneCreateButton;
        [SerializeField] private Button m_PlaneMatterlessCreateButton;

        [FormerlySerializedAs("m_Label")][SerializeField] private Text m_PlaceLabel;
        [SerializeField] private Text m_RemoveLabel;
        
        #region Factory
        public static ObstaclesUiView Create()
            => Instantiate(Resources.Load<ObstaclesUiView>("UIPrefabs/UIP_ObstaclesView")).Init();
        #endregion
        
        private ObstaclesUiView Init()
        {
            m_PlaneCreateButton.onClick.AddListener(()=>onPlaneCreateButtonClicked?.Invoke());

            m_PlaneMatterlessCreateButton.onClick.AddListener(()=>onPlaneMatterlessCreateButtonClicked.Invoke());
            
            return this;
        }
        
        public void UpdateUi(string placeButtonLabel, bool isSpawnObstacleButtonInteractable, bool isResetSpawnObstacleButtonInteractable, string removeButtonLabel)
        {
            m_PlaceLabel.text = placeButtonLabel;
            m_RemoveLabel.text = removeButtonLabel;
        }
        
        public void HideCreateButton()
        {
            m_PlaneCreateButton.gameObject.SetActive(false);
            m_PlaneMatterlessCreateButton.gameObject.SetActive(false);
        }
    }
}
    

