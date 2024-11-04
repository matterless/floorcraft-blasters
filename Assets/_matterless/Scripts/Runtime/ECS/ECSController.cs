using System;
using System.Collections.Generic;
using UnityEngine;
using Auki.ConjureKit.Hagall.Messages;
using Auki.ConjureKit;
using static UnityEngine.CullingGroup;

namespace Matterless.Floorcraft
{
    public class ECSController : IECSController
    {
        public event Action<ECSState> onStateChanged;
        public event Action<IEntityComponentModel> onOtherAdded;
        public event Action<IEntityComponentModel> onOtherUpdate;
        public event Action<string,uint,bool> onDeleted;

        // key : componentTypeId, value : function
        private readonly Dictionary<string, Func<uint, uint, bool, IEntityComponentModel>> m_ComponentModelFactoryFunctions = new();
        //// key : entityId, value : {key : componentTypeId, Value : IEntityComponent}
        private readonly Dictionary<uint, Dictionary<uint, IEntityComponentModel>> m_EntityDictionary = new();
        // component Type Id :uint <-> component Type :string
        private readonly BidirectionalDictionary<uint, string> m_ComponentTypesMapping = new();
        // auki wrapper
        private readonly IAukiWrapper m_AukiWrapper;

        public ECSController(IAukiWrapper aukiWrapper)
        {
            m_AukiWrapper = aukiWrapper;
            // sessions
            // we handle this from Connection Service
            //m_AukiWrapper.onJoined += OnJoinedSession;
            //m_AukiWrapper.onLeft += OnLeftSession;
            // entities
            // This event is triggered only for entities created by other participants in the session.
            // m_AukiWrapper.onEntityAdded += RegisterComponentInDictionary;
            // This event is triggered only for entities deleted by other participants in the session.
            m_AukiWrapper.onEntityDeleted += OnEntityDeleted;
            // components
            m_AukiWrapper.onComponentAdd += OnOtherComponentAdded;
            m_AukiWrapper.onComponentUpdate += OnOtherComponentUpdated;
            m_AukiWrapper.onComponentDelete += OnOtherComponentDeleted;
        }

        #region InitComponent
        private bool m_ComponentInitInProgress;
        private bool m_HasError;
        private int m_ComponentAdded;
        private Action m_OnSuccess;
        private Action m_OnError;

        private void IncreaseAndCheckECSInitProgress()
        {
            m_ComponentAdded++;

            if(m_ComponentAdded == m_ComponentModelFactoryFunctions.Count)
            {

                Debug.Log("~~~~~~ ECS CONTROLLER INIT COMPLETED ~~~~~~");
                m_ComponentInitInProgress = false;
                onStateChanged?.Invoke(ECSState.Succeed);
                m_OnSuccess.Invoke();
            }
        }

        private Session m_CurrentSession;

        public void Initialise(Session session, Action onSuccess, Action onError)
        {
            if (m_ComponentInitInProgress)
                throw new Exception("Component Init In Progress");

            m_OnSuccess = onSuccess;
            m_OnError = onError;

            onStateChanged?.Invoke(ECSState.Initialising);

            ClearDataStructures(); // clear lists & dictionaries also here, as we are unable to clear them on session left every time
            m_ComponentAdded = 0;
            m_CurrentSession = session;
            m_HasError = false;

            Debug.Log($"joined space {m_CurrentSession.Id} with {m_CurrentSession.GetEntities().Count} entities");
            Debug.Log("***** START ECS CONTROLLER INIT *******");
            var entityTypes = m_ComponentModelFactoryFunctions.Keys;

            Debug.Log($"Component types to added: {entityTypes.Count}");

            // IMPORTANT
            // all these methods are asynch
            // which means that we need to handle what happens when we leave the session before all these are finalised


            foreach (var typeName in entityTypes)
            {
                if (m_HasError)
                    break;

                AddComponentToConjureKit(session, typeName);
            }
        }

        public void Clear()
        {
            if (m_ComponentInitInProgress)
                throw new Exception("ECS init is int progress...");

            onStateChanged?.Invoke(ECSState.None);

            ClearDataStructures();
        }

        private void ClearDataStructures()
        {
            m_ComponentTypesMapping.Clear();
            m_EntityDictionary.Clear();
            m_RegisterComponents.Clear();
        }

        private List<uint> m_RegisterComponents = new List<uint>();

        private void AddComponentToConjureKit(Session session, string typeName)
        {
            Debug.Log($"Add Component To ConjureKit typeId: {typeName} - session: {session.Id}");

            // add component type to ConjureKit
            session.AddComponentType(typeName, 
                 // on success
                typeId =>
                {
                    Debug.Log($"Component added {typeName} typeId: {typeId} - session: {session.Id}");

                    m_ComponentTypesMapping.Add(typeId, typeName);

                    // subscribe to added component
                    session.SubscribeToComponentType(typeId, () =>
                    {
                        if (!m_HasError)
                        {
                            Debug.Log($"Subscribe {typeName} typeId: {typeId} - session:{session.Id}");
                            m_RegisterComponents.Add(typeId);
                            FetchEntityComponets(session, typeId);
                        }
                    }, OnInitError);
                },
                //  on error
                OnInitError);
        }

        private void OnInitError(string errorMessage)
        {
            Debug.LogWarning($"ECS Init error: {errorMessage}");
            m_HasError = true;
            m_OnError.Invoke();
        }

        // On SubscribeToComponentType completed
        private void FetchEntityComponets(Session session, uint typeId)
        {
            if (m_HasError)
                return;

            if (m_CurrentSession == null)
                throw new Exception("Session is null");

            // for each entity
            foreach (var entity in session.GetEntities())
            {
                // get list of entities components for this component type
                session.GetComponents(typeId, componentList =>
                {
                    if (!m_HasError)
                    {
                        //Debug.Log($"Fetch {GetComponentNameById(typeId)}, count:{componentList.Count}");
                        for (int i = 0; i < componentList.Count; i++)
                        {
                            // check if entity component belongs to the entity
                            if (componentList[i].EntityId == entity.Id)
                            {
                                //Debug.Log($"{entity.Id} entity component {GetComponentNameById(componentList[i].ComponentTypeId)} datacount {componentList[i].Data.Length}");
                                AddEntityComponentModel(
                                    componentList[i].EntityId,
                                    componentList[i].ComponentTypeId,
                                    componentList[i].Data);
                            }
                        }

                        IncreaseAndCheckECSInitProgress();
                    }
                }, OnInitError);
            }
        }

        private void OnEntityDeleted(uint entityId)
        {
            if(!m_EntityDictionary.ContainsKey(entityId))
                return;

            foreach(var model in m_EntityDictionary[entityId].Values)
            {
                var typeName = GetComponentNameById(model.typeId);
                var isMine = m_AukiWrapper.IsMine(entityId);
                onDeleted?.Invoke(typeName, entityId, isMine);
            }

            m_EntityDictionary.Remove(entityId);
        }
        #endregion

        #region public Method

        public void RegisterOnCreateEntityComponentModelFunction(
            string typeName, 
            Func<uint,uint,bool,IEntityComponentModel> createEntityComponentEvent)
        {
            m_ComponentModelFactoryFunctions.Add(typeName, createEntityComponentEvent);
        }

        public uint GetComponentIdByName(string name) => m_ComponentTypesMapping.GetItem(name);
        private bool TryGetComponentIdByName(string name, out uint item)
        {
            if (m_ComponentTypesMapping.HasItem(name))
            {
                item = m_ComponentTypesMapping.GetItem(name);
                return true;
            }
            item = default;
            return false;
        }


        public void AddComponent(IEntityComponentModel model)
        {
            //Debug.Log(model.data[0]);

            // register localy
            RegisterComponentInEntityDictionary(model);
            // serialise
            model.Serialize();
            // add component to auki
            m_CurrentSession.AddComponent(model.typeId, model.entityId, model.data,
                () => Debug.Log($"Add component {GetComponentNameById(model.typeId)} to {model.entityId} with data[0]: {model.data[0]}"));
        }

        public void BroadcastComponent(IEntityComponentModel model)
        {
            //Debug.Log($"BroadcastComponent{GetComponentNameById(model.typeId)} to {model.entityId} with data[0]: {model.data[0]}");
            model.Serialize();
            m_CurrentSession.UpdateComponent(model.typeId, model.entityId, model.data);
        }

        public void DeleteComponentFromEntity(string componentType, uint entityId)
        {
            m_CurrentSession.DeleteComponent(GetComponentIdByName(componentType), entityId, null);
        }

        public IEntityComponentModel GetEntityComponentModel(string typeName, uint entityId)
        {
            return m_EntityDictionary[entityId][GetComponentIdByName(typeName)];
        }
        public bool TryGetEntityComponentModel<M>(string typeName, uint entityId, out M entityComponentModel)
        {
            if (m_EntityDictionary.ContainsKey(entityId) && TryGetComponentIdByName(typeName, out var item))
            {
                if (m_EntityDictionary[entityId].ContainsKey(item))
                {
                    entityComponentModel = (M)m_EntityDictionary[entityId][item];
                    return true;
                }
            }
            entityComponentModel = default(M);
            return false;
        }

        #endregion

        // This will called at the start of a session
        // and is registering existing components for existing entities
        private void AddEntityComponentModel(uint entityId, uint componentTypeId, byte[] data)
        {
            Debug.Log($"Add existing entity {GetComponentNameById(componentTypeId)} on {entityId} entity with data[0]: {data[0]}");
            // create model using factory function
            var model = m_ComponentModelFactoryFunctions[GetComponentNameById(componentTypeId)]
                .Invoke(componentTypeId, entityId, false);
            // init model data
            model.Deserialize(data);
            // cache model
            RegisterComponentInEntityDictionary(model);
            // invoke event
            onOtherAdded?.Invoke(model);
        }

        // Called by m_AukiWrapper.onComponentAdd
        private void OnOtherComponentAdded(ComponentAddBroadcast broadcast)
        {
            UnityEngine.Debug.Log($"OnOtherComponentAdded: typeId: {broadcast.EntityComponent.ComponentTypeId} - entityId: {broadcast.EntityComponent.EntityId}");

            // This is a semi-hack
            if (!m_RegisterComponents.Contains (broadcast.EntityComponent.ComponentTypeId))
                return;

            var entityComponent = broadcast.EntityComponent;
            AddEntityComponentModel(
                entityComponent.EntityId, entityComponent.ComponentTypeId, entityComponent.Data);
        }

        private void RegisterComponentInEntityDictionary(IEntityComponentModel model)
        {
            // if there is no entry create a new one (lazy)
            if (!m_EntityDictionary.ContainsKey(model.entityId))
                m_EntityDictionary[model.entityId] = new();

            if (m_EntityDictionary[model.entityId].ContainsKey(model.typeId))
                throw new Exception($"Same key entity:{model.entityId} - type:{model.typeId}");

            // add component
            m_EntityDictionary[model.entityId].Add(model.typeId, model);
        }

        private void OnOtherComponentUpdated(ComponentUpdateBroadcast broadcast)
        {
            // This is a semi-hack
            if (!m_RegisterComponents.Contains(broadcast.EntityComponent.ComponentTypeId))
                return;

            // get model
            var entityId = broadcast.EntityComponent.EntityId;
            var typeId = broadcast.EntityComponent.ComponentTypeId;

            // we need this, otherwise we may have key not exists erros
            // when we enter a session that has already components
            if (!m_EntityDictionary.ContainsKey(entityId))
            {
                Debug.LogWarning($"Entity {entityId} does not exists yet");
                return;
            }

            // we need this, otherwise we may have key not exists erros
            // when we enter a session that has already components
            if (!m_EntityDictionary[entityId].ContainsKey(typeId))
            {
                Debug.LogWarning($"Component {GetComponentNameById(typeId)} does not exists on entity {entityId} yet");
                return;
            }

            var model = m_EntityDictionary[entityId][typeId];
            // update model
            model.Deserialize(broadcast.EntityComponent.Data);
            // invoke event
            onOtherUpdate?.Invoke(model);
        }

        private void OnOtherComponentDeleted(ComponentDeleteBroadcast broadcast)
        {
            if (!m_RegisterComponents.Contains(broadcast.EntityComponent.ComponentTypeId))
                return;

            // delete model
            var entityId = broadcast.EntityComponent.EntityId;
            var typeId = broadcast.EntityComponent.ComponentTypeId;
            m_EntityDictionary[entityId].Remove(typeId);
            // invoke event
            var typeName = GetComponentNameById(typeId);
            onDeleted?.Invoke(typeName, entityId, false);
        }

        private string GetComponentNameById(uint id) => m_ComponentTypesMapping.GetItem(id);

        #region TO BE REMOVED
        public void AddComponentToEntity(string componentType, uint entityId, byte[] data)
        {
            throw new Exception("Remove this method");
        }
        public void UpdateComponentOnEntity(string componentType, uint entityId, byte[] data)
        {
            throw new Exception("Remove this method");
        }
        #endregion
    }
}