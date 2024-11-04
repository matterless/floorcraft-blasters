using System;
using UnityEngine;

namespace Matterless.Floorcraft
{
    public class GameObjectView : MonoBehaviour
    {
        public uint entityId { get; protected set; }

        public event Action<GameObjectView> onDisable;
        public event Action<GameObjectView> onDestroy;

        protected virtual void OnDestroy()
        {
            onDestroy?.Invoke(this);
        }
        
        protected virtual void OnDisable()
        {
            onDisable?.Invoke(this);
        }
    }
}
