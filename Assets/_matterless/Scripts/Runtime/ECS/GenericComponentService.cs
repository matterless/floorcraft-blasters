using System;
using UnityEngine;

namespace Matterless.Floorcraft
{
    /// <summary>
    /// Generic Component Service
    /// </summary>
    /// <typeparam name="M">Entity Component Model type</typeparam>
    /// <typeparam name="D">Data Model type</typeparam>
    public class GenericComponentService<M,D> where M : IEntityComponentModel
    {
        private readonly IECSController m_EcsController;
        private readonly string m_TypeName;
        private readonly Func<uint, uint, bool, IEntityComponentModel> m_ModelFactoryMethod;

        #region Public Events
        public event Action<M> onComponentAdded;
        public event Action<M> onComponentUpdated;
        public event Action<uint,bool> onComponentDeleted;
        #endregion

        public GenericComponentService(IECSController ecsController, IComponentModelFactory componentModelFactory)
        {
            m_EcsController = ecsController;
            m_TypeName = componentModelFactory.GetTypeName(this.GetType());
            m_ModelFactoryMethod = componentModelFactory.GetFactoryMethod(this.GetType());

            ecsController.onOtherAdded += OnOtherAdded;
            ecsController.onOtherUpdate += OnOtherUpdated;
            ecsController.onDeleted += OnDeleted;
            ecsController.RegisterOnCreateEntityComponentModelFunction(m_TypeName, CreateEntityComponentModel);
        }

        #region Public Methods
        public M AddComponent(uint entityId, D data)
        {
            // create new model
            var typeId = m_EcsController.GetComponentIdByName(m_TypeName);
            var model = (M)CreateEntityComponentModel(typeId, entityId, true);
            // update component
            UpdateComponentMethod(model, data);
            // add component to Auki ECS
            m_EcsController.AddComponent(model);
            // call virtual method
            OnComponentAdded(model);
            // event
            onComponentAdded?.Invoke(model);
            return (M)model;
        }

        public void UpdateComponent(uint entityId, D data)
        {
            var model = (M)m_EcsController.GetEntityComponentModel(m_TypeName, entityId);
            UpdateComponent(model, data);
        }

        public void UpdateComponent(M model, D data)
        {
            // update method
            UpdateComponentMethod(model, data);
            // call virtual method
            OnComponentUpdated(model);
            // event
            onComponentUpdated?.Invoke(model);

            //// check if we should broadcast
            //if (!BroadcastCheck(model))
            //    return;

            // broadcast
            m_EcsController.BroadcastComponent(model);
        }

        public void DeleteComponent(uint entityId)
        {
            m_EcsController.DeleteComponentFromEntity(m_TypeName, entityId);
            // call virtual method
            OnComponentDeleted(entityId, true);
            onComponentDeleted?.Invoke(entityId, true);
        }
        
        public M GetComponentModel(uint id) => (M)m_EcsController.GetEntityComponentModel(m_TypeName, id);
        public bool TryGetComponentModel(uint id, out M entityComponentModel) => m_EcsController.TryGetEntityComponentModel(m_TypeName, id, out entityComponentModel);
        
        #endregion

        private void OnOtherAdded(IEntityComponentModel model)
        {
            if (model is not M componentModel)
                return;

            // call virtual method
            OnComponentAdded(componentModel);
            // event
            onComponentAdded?.Invoke(componentModel);
        }
        private void OnOtherUpdated(IEntityComponentModel model)
        {
            if (model is not M componentModel)
                return;

            // call virtual method
            OnComponentUpdated(componentModel);
            // event
            onComponentUpdated?.Invoke(componentModel);
        }
        private void OnDeleted(string typeName, uint entityId, bool isMine)
        {
            if (typeName != m_TypeName)
                return;

            // call virtual method
            OnComponentDeleted(entityId, isMine);
            // event
            onComponentDeleted?.Invoke(entityId, isMine);
        }

        private IEntityComponentModel CreateEntityComponentModel(uint typeId, uint entityId, bool isMine)
        {
            var model = m_ModelFactoryMethod.Invoke(typeId, entityId, isMine);
            return model;
        }

        #region Virtual Methods
        protected virtual void UpdateComponentMethod(M model, D data)
        {
            throw new NotImplementedException("UpdateComponentMethod has not been implemented!");
        }

        /// <summary>
        /// Add here the custom logic for broadcast acceptance test. Default is true.
        /// e.g. on every update OR every x time OR a generic comparison method
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        //protected virtual bool BroadcastCheck(M model) => true;

        protected virtual void OnComponentAdded(M model) { }

        protected virtual void OnComponentUpdated(M model) { }

        protected virtual void OnComponentDeleted(uint entityId, bool isMine) { }
        #endregion
    }
}