using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace Matterless.Floorcraft
{
    public class LeaderboardService
    {   
        [System.Serializable]
        public class Settings
        {
            [SerializeField] private float m_FontSize;
            [SerializeField] private float m_SpaceBetweenLabel;
            [SerializeField] private int m_ShowTop;
            [SerializeField] private float m_SwipeDuration;
            public float fontSize => m_FontSize;
            public float spaceBetweenLabel => m_SpaceBetweenLabel;
            public int ShowTop => m_ShowTop;
            public float SwipeDuration => m_SwipeDuration;
        }
        public Settings settings { get; set; }
        
        private IAukiWrapper m_AukiWrapper;
        private ScoreComponentService m_ScoreComponentService;
        private CrownService m_CrownService;
        private NameComponentService m_NameComponentService;
        private Settings m_Settings;
        private NameComponentService.Settings m_NameSettings;
        
        private Dictionary<uint, ScoreComponentModel> cache = new Dictionary<uint, ScoreComponentModel>();
        private Dictionary<uint, string> cacheNameList = new Dictionary<uint, string>();
        private List<uint> cacheList = new List<uint>();
        
        private LeaderboardView m_View;

        public LeaderboardService(IAukiWrapper aukiWrapper, ScoreComponentService scoreComponentService,CrownService crownService,NameComponentService nameComponentService , Settings settings, NameComponentService.Settings nameSettings)
        {
            m_AukiWrapper = aukiWrapper;
            m_CrownService = crownService;
            m_Settings = settings;
            m_NameSettings = nameSettings;
            m_NameComponentService = nameComponentService;
            m_AukiWrapper.onLeft += ResetLeaderboard;

            m_ScoreComponentService = scoreComponentService;
            m_ScoreComponentService.onComponentAdded += OnScoreComponentAdded;
            m_ScoreComponentService.onComponentUpdated += OnScoreComponentUpdate;
            m_NameComponentService.onComponentAdded += OnNameComponentAdded;
            m_NameComponentService.onComponentDeleted += DeleteLabel;

            m_View = LeaderboardView.Create(m_Settings.spaceBetweenLabel , m_Settings.fontSize , m_Settings.ShowTop,m_Settings.SwipeDuration, "UIPrefabs/UIP_Leaderboard");
        }

        private void OnScoreComponentAdded(ScoreComponentModel model)
        {
            if (m_NameComponentService.nameComponentModels.ContainsKey(model.entityId) && !cache.ContainsKey(model.entityId))
            {
                AddLabel(model.entityId);
            }
        }

        private void OnNameComponentAdded(NameComponentModel model)
        {
            Debug.Log("label name add " + model.entityId);
            if (m_ScoreComponentService.scoreComponentModels.ContainsKey(model.entityId))
            {
                AddLabel(model.entityId);
            }
        }
        private void OnScoreComponentUpdate(ScoreComponentModel model)
        {
            Debug.Log("OnScoreComponentUpdate");
            UpdateLabel();
        }

        private void AddLabel(uint entityId)
        {
            Debug.Log("label add " + entityId);
            m_View.AddLabel(entityId);
            UpdateLabel();
        }

        private void UpdateLabel()
        {
            RefreshOrder();
            if (m_View.IsVisible())
            {
                m_View.UpdateLabel(cacheList,m_ScoreComponentService.scoreComponentModels,cacheNameList,m_CrownService.crownKeeper);
            }
        }

        private void DeleteLabel(uint entityId, bool isMine)
        {
            m_View.DeleteLabel(entityId);
            RefreshOrder();
            m_View.UpdateLabel(cacheList,m_ScoreComponentService.scoreComponentModels,cacheNameList,m_CrownService.crownKeeper);
        }

        private void RefreshOrder()
        {
            cache.Clear();
            cache = m_ScoreComponentService.scoreComponentModels;
            cache = m_ScoreComponentService.scoreComponentModels.OrderBy(x => x.Value.model.score)
                .ToDictionary(x => x.Key, x => x.Value);
            cacheList = new List<uint>(cache.Keys);
            cacheNameList = m_NameComponentService.nameComponentModels.ToDictionary(x => x.Key, x =>
                m_NameSettings.nameTags[x.Value.model.name]);
            foreach (var entity in cacheList)
            {
            
                if (entity == m_CrownService.crownKeeper)
                {
                    cacheList.Remove(entity);
                    break;
                }
            }

            if (m_CrownService.crownKeeper != 0)
            {
                cacheList.Add(m_CrownService.crownKeeper);
            }
        }

        private void ResetLeaderboard()
        {
            cache.Clear();
            cacheList.Clear();
            m_View.ResetLeaderboard();
        }

        public void ShowLeaderBoard()
        {
            m_View.Show();
            UpdateLabel();
        }
        public void HideLeaderBoard()
        {
            m_View.Hide();
        }

        public void SetOrientation(ScreenOrientation orientation)
        {
            m_View.SetLeaderboardOrientation(orientation);
        }
    }
}