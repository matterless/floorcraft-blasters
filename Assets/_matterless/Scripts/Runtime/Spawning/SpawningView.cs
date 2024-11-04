using System;
using TMPro;
using UnityEngine;
using Button = Matterless.Module.UI.Button;

namespace Matterless.Floorcraft
{
    public class SpawningView : UIView<SpawningView>
    {
        #region Inspector
        [SerializeField] private Button m_SpawnButton;
        [SerializeField] private TextMeshProUGUI m_RechargeLabel;
        [SerializeField] private TextMeshProUGUI m_StatusLabel;
        [SerializeField] private TextMeshProUGUI m_StatusMessage;
        [SerializeField] private TextMeshProUGUI m_SpawnCountLabel;
        [SerializeField] private TextMeshProUGUI m_ObstacleCreateButtonLabel;
        [SerializeField] private TextMeshProUGUI m_ResetObstaclesButtonLabel;
        
        [SerializeField] private Button m_ObstacleCreateButton;
        [SerializeField] private Button m_ResetObstaclesButton;
        #endregion

        public event Action onObstacleCreateButtonClicked;
        public event Action onResetObstaclesButtonClicked;
        public event Action onSpawnButtonClicked;

        private Action m_SpawnAction;
        private Action m_ChangePlaceableAction;
        private Action m_StoreClickedAction;
        private bool m_HasRespawnPoints = true;
        private bool m_IsSpawnObstacleButtonInteractable;

        public override SpawningView Init()
        {
            m_SpawnButton.onClick.AddListener(OnSpawnButtonClicked);
            
            m_ObstacleCreateButton.onClick.AddListener(() => onObstacleCreateButtonClicked?.Invoke());
            m_ResetObstaclesButton.onClick.AddListener(() => onResetObstaclesButtonClicked?.Invoke());
            
            m_ObstacleCreateButton.interactable = true;
            m_ResetObstaclesButton.interactable = false;
            
            return this;
        }

        public void Show(Action onSpawn, Action onStoreClicked)
        {
            m_SpawnAction = onSpawn;
            m_StoreClickedAction = onStoreClicked;
            base.Show();
        }

        public void UpdateView(SpawningViewModel viewModel)
        {
            //Debug.Log(rechargeText);
            m_HasRespawnPoints = viewModel.respawnCount > 0;
            m_SpawnCountLabel.text = viewModel.unlimitedRespawns ? "" : $"({viewModel.respawnCount.ToString()}/{viewModel.maxCount.ToString()})";
            m_RechargeLabel.gameObject.SetActive(viewModel.respawnCount != viewModel.maxCount && !viewModel.unlimitedRespawns);
            m_RechargeLabel.text = viewModel.rechargeText;
        }

        public void UpdateScanningStatus(bool canSpawn, bool hasGroundHit, bool isConnected, string scanningLabel, string lookAroundLabel, string disconnectedMessage)
        {
            m_StatusLabel.gameObject.SetActive(!hasGroundHit);
            m_StatusMessage.gameObject.SetActive(!hasGroundHit);
            m_SpawnButton.interactable = canSpawn;
            m_ObstacleCreateButton.interactable = m_IsSpawnObstacleButtonInteractable && hasGroundHit && isConnected;
            m_StatusLabel.text = isConnected ? scanningLabel : disconnectedMessage;
            m_StatusMessage.text = isConnected ? (canSpawn ? string.Empty : lookAroundLabel) : disconnectedMessage;
        }

        public void SetObstacleButtonLabels(string placeButtonLabel, bool isSpawnObstacleButtonInteractable, bool isResetSpawnObstacleButtonInteractable, string removeButtonLabel)
        {
            m_ObstacleCreateButtonLabel.text = placeButtonLabel;
            m_ResetObstaclesButtonLabel.text = removeButtonLabel;
            m_ObstacleCreateButton.interactable = isSpawnObstacleButtonInteractable;
            m_ResetObstaclesButton.interactable = isResetSpawnObstacleButtonInteractable;
            m_IsSpawnObstacleButtonInteractable = isSpawnObstacleButtonInteractable;
        }

        private void OnSpawnButtonClicked()
        {
            if (m_HasRespawnPoints)
            {
                m_SpawnAction.Invoke();
                onSpawnButtonClicked?.Invoke();
                Hide();
            }
            else
            {
                m_StoreClickedAction.Invoke();
            }
        }
    }
}