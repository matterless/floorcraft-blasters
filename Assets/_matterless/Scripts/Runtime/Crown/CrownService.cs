using System;
using UnityEngine;

namespace Matterless.Floorcraft
{
    public class CrownService
    {
        private readonly ScoreComponentService m_ScoreComponentService;
        private uint m_CrownKeeper;
        
        public uint crownKeeper => m_CrownKeeper;

        public event Action<uint, int> onCrownKeeperChanged;
        
        public CrownService(ScoreComponentService scoreComponentService)
        {
            m_ScoreComponentService = scoreComponentService;
            m_ScoreComponentService.onComponentAdded += OnScoreUpdate;
            m_ScoreComponentService.onComponentUpdated += OnScoreUpdate;
            m_ScoreComponentService.onComponentDeleted += OnScoreDelete;
        }

        public void OnCrownKeeperDestroy(uint entityId)
        {
            Debug.Log($"{entityId} speeder destroy ,crown keeper {m_CrownKeeper}");
            UpdateCrown();
        }

        public void OnCrownKeeperRespawn(uint entityId)
        {
            Debug.Log($"{entityId} speeder Respawn ,crown keeper {m_CrownKeeper}");
            UpdateCrown();
        }

        private void OnScoreUpdate(ScoreComponentModel model)
        {
            UpdateCrown();
        }

        private void OnScoreDelete(uint entityId, bool isMine)
        {
            OnCrownKeeperDestroy(entityId);
        }
      
        private void UpdateCrown()
        {
            int highestScore = -1;
            uint highestView = 0;

            //if (m_ScoreComponentService.scoreComponentModels.Count == 1)
            //{
            //    m_CrownKeeper = 0;
            //    return;
            //}

            foreach (var view in m_ScoreComponentService.scoreComponentModels)
            {
                int score = view.Value.model.score;
                
                if (highestScore <= score || highestView == 0)
                {
                    if (!(view.Key > highestView && highestScore == score) && score != 0)
                    {
                        highestScore = score;
                        highestView = view.Key;
                    }
                }
            }
            
            m_CrownKeeper = highestView;
            onCrownKeeperChanged?.Invoke(m_CrownKeeper, highestScore);
            
        }

        [System.Serializable]
        public class Settings
        {
            [SerializeField] private string m_CrownResourcePath = "";
            [SerializeField] private Pose m_CrownOffset;
            public string crownResourcePath => m_CrownResourcePath;
            public Pose crownOffset => m_CrownOffset;
        }
    }
}