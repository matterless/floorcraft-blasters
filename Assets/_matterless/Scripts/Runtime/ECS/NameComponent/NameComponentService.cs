using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Matterless.Floorcraft
{
    public class NameComponentService : GenericComponentService<NameComponentModel,NameModel>
    {
        private readonly Settings m_Settings;
      
        private readonly Dictionary<uint, NameComponentModel> m_Models = new();
        public Dictionary<uint, NameComponentModel> nameComponentModels => m_Models;
        private List<int> m_NameTagIndexList = new List<int>();
        public NameComponentService(IECSController ecsController, IComponentModelFactory componentModelFactory ,Settings settings ) : base(ecsController, componentModelFactory)
        {
            m_Settings = settings;
            for (int i = 0; i < m_Settings.nameTags.Count; i++)
            {
                m_NameTagIndexList.Add(i);
            }
        }
        protected override void OnComponentAdded(NameComponentModel model)
        {
            m_Models.Add(model.entityId,model);
        }

        protected override void UpdateComponentMethod(NameComponentModel model, NameModel data)
        {
            model.model = data;
        }

        protected override void OnComponentDeleted(uint entityId, bool isMine)
        {
            m_Models.Remove(entityId);
        }
        public int GetRandomNameTagIndex()
        {
            return m_NameTagIndexList.Except(m_Models.Select(x => x.Value.model.name).ToList()).ToList()[Random.Range(0, m_Settings.nameTags.Count)];
        }
        [System.Serializable]
        public class Settings
        {
            [SerializeField] private List<string> m_NameTags = new List<string>()
            {
                "DRAGONFLY",
                "MAVERICK",
                "THUNDER",
                "PANTHER",
                "COBRA",
                "WOLFPACK",
                "CYCLONE",
                "PHOENIX",
                "GHOST",
                "VIPER",
                "HURRICANE",
                "AVALANCHE",
                "RAPTOR",
                "TITAN",
                "CRUSADER",
                "SPITFIRE",
                "HAVOC",
                "SABRE",
                "LIGHTNING",
                "HAMMER",
                "RAZOR",
                "STORM",
                "FALCON",
                "BULLET",
                "SCORPION",
                "GOLIATH",
                "CHIMERA",
                "RIKER",
                "GRIFFIN",
                "DELTA",
                "ECHO",
                "TANGO",
                "FOXTROT",
                "CHARLIE",
                "ALPHA",
                "BRAVO",
                "LIMA",
                "GOLF",
                "VICTOR",
                "SIERRA",
                "ROMEO",
                "HOTEL",
                "INDIA",
                "JULIET",
                "KILO",
                "NOVEMBER",
                "OSCAR",
                "PAPA"
            };

            [SerializeField] private string m_nameTagResourcePath;
            [SerializeField] private Vector3 m_NameTagOffset;
            [SerializeField] private float m_fontSize;
            public List<string> nameTags => m_NameTags;
            public string nameTagResourcePath => m_nameTagResourcePath;
            public Vector3 nameTagOffset => m_NameTagOffset;
            public float fontSize => m_fontSize;
        }
    }
}
