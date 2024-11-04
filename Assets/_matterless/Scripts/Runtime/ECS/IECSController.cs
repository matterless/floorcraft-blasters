using Auki.ConjureKit;
using System;

namespace Matterless.Floorcraft
{
    public enum ECSState
    {
        None, Initialising, Succeed
    }

    public interface IECSController
    {
        event Action<ECSState> onStateChanged;
        event Action<IEntityComponentModel> onOtherAdded;
        event Action<IEntityComponentModel> onOtherUpdate;
        event Action<string,uint,bool> onDeleted;

        void Initialise(Session session, Action onSuccess, Action onError);
        void Clear();
        void RegisterOnCreateEntityComponentModelFunction (
            string typeName, 
            Func<uint, uint, bool, IEntityComponentModel> function);
        uint GetComponentIdByName(string name);
        void AddComponent(IEntityComponentModel model);
        void BroadcastComponent(IEntityComponentModel model);
        void DeleteComponentFromEntity(string componentType, uint entityId);
        IEntityComponentModel GetEntityComponentModel(string typeName, uint entityId);
        bool TryGetEntityComponentModel<M>(string typeName, uint id, out M entityComponentModel);




        //TODO:: REMOVE THESE
        void UpdateComponentOnEntity(string componentType, uint entityId, byte[] data);
        void AddComponentToEntity(string componentType, uint entityId, byte[] data);

    }
}