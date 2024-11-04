using System;
using UnityEngine;

namespace Matterless.Floorcraft
{
    public interface IBaseComponentService
    {
        event Action<uint> onComponentDeleted;

        void AddComponent(uint entityId, string id);
    }

    public interface IPropertiesComponentService : IBaseComponentService
    {
        event Action<uint, PropertiesComponentModel> onComponentAdded;
        event Action<uint, PropertiesComponentModel> onComponentUpdated;

        PropertiesComponentModel GetModel(uint entityId);
        GameObject GetGameObject(uint entityId);
    }
}