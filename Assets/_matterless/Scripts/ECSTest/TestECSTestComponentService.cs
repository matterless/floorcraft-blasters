using System;
using UnityEngine;

namespace Matterless.Floorcraft.TestECS
{
    public class TestECSTestComponentService : GenericComponentService<TestECSTestComponentModel,Color>
    {
        public TestECSTestComponentService(IECSController ecsController, IComponentModelFactory componentModelFactory) 
            : base(ecsController, componentModelFactory)
        {
            
        }

        protected override void OnComponentAdded(TestECSTestComponentModel model)
        {
            Debug.Log($"On Component Added {model.entityId} -> {model.color} ({(model.isMine ? "mine" : "other")})");
        }

        protected override void OnComponentUpdated(TestECSTestComponentModel model)
        {
            Debug.Log($"On Component Updated {model.entityId} -> {model.color} ({(model.isMine ? "mine" : "other")})");
        }

        protected override void OnComponentDeleted(uint entityId, bool isMine)
        {
            Debug.Log($"On Component Deleted {entityId} ({(isMine ? "mine" : "other")})");
        }

        // define the update method
        protected override void UpdateComponentMethod(TestECSTestComponentModel model, Color data)
        {
            model.color = data;
        }

    }
}