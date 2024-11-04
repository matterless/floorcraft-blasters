using System.Collections.Generic;
using UnityEngine;

namespace Matterless.Floorcraft
{
    public class PropertiesComponentService :
        GenericComponentService<PropertiesComponentModel, PropertiesModel>
    {
        private readonly Dictionary<uint, GameObject> m_GameObjects = new();
        private readonly PropertiesECSService.Settings m_Settings;
        private readonly Dictionary<uint, object> m_Viewes = new Dictionary<uint, object>();

        public PropertiesComponentService(IECSController eCSController, ComponentModelFactory factory,
            PropertiesECSService.Settings settings)
            : base(eCSController, factory)
        {
            m_Settings = settings;
        }


        public bool hasAsset(uint id) => m_Settings.GetAsset(id) != null;
        public GameObject GetGameObject(uint entityId) => m_GameObjects[entityId];
        public T GetView<T>(uint entityId) where T : class
        {
            if (!m_Viewes.ContainsKey(entityId))
                m_Viewes.Add(entityId, m_GameObjects[entityId].GetComponent<T>());

            return m_Viewes[entityId] as T;
        }

        protected override void OnComponentAdded(PropertiesComponentModel model)
        {
            GameObject go = GameObject.Instantiate(Resources.Load<GameObject>(m_Settings.GetAsset(model.model.id).resourcesPath));
            m_GameObjects.Add(model.entityId, go);
        }

        protected override void UpdateComponentMethod(PropertiesComponentModel model, PropertiesModel data)
        {
            model.model = data;
        }

        protected override void OnComponentDeleted(uint entityId, bool isMine)
        {
            // properties component should handle the game object lifecycle
            Object.Destroy(m_GameObjects[entityId].gameObject);
            m_GameObjects.Remove(entityId);
            m_Viewes.Remove(entityId);
        }
    }
}