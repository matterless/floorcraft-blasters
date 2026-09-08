using System.Collections.Generic;
using UnityEngine;

namespace Matterless.Floorcraft
{
    public class ScoreComponentService : GenericComponentService<ScoreComponentModel,ScoreModel>
    {
        private readonly Settings m_Settings;

        public Settings settings => m_Settings;
        // the key is speeder entityId
        private readonly Dictionary<uint, ScoreComponentModel> m_Models = new();
        public Dictionary<uint, ScoreComponentModel> scoreComponentModels => m_Models;

        public ScoreComponentService(IECSController ecsController, IComponentModelFactory componentModelFactory,Settings settings) : base(ecsController, componentModelFactory)
        {
            m_Settings = settings;
        }

        public ScoreComponentModel GetScoreComponentModel(uint entityId)
        {
            return m_Models.ContainsKey(entityId) ? m_Models[entityId] : null;
        }

        protected override void OnComponentAdded(ScoreComponentModel model)
        {
            m_Models.Add(model.entityId,model);
        }

        protected override void UpdateComponentMethod(ScoreComponentModel model, ScoreModel data)
        {
            model.model = data;
        }

        protected override void OnComponentDeleted(uint entityId, bool isMine)
        {
            m_Models.Remove(entityId);
        }

        [System.Serializable]
        public class Settings
        {
            [SerializeField] private int m_ObstacleKill = 5;
            [SerializeField] private int m_LaserKill = 5;
            [SerializeField] private int m_SpeederKill = 10;
            [SerializeField] private float m_KilledDeductRate = 0.75f;

            public int obstacleKill => m_ObstacleKill;
            public int laserKill => m_LaserKill;
            public int speederKill => m_SpeederKill;
            public float killedDeductRate => m_KilledDeductRate;
        }
    }
}


