using System.Collections;
using System.Collections.Generic;
using Matterless.Floorcraft;
using UnityEngine;

namespace Matterless.Floorcraft
{
    public class CloneComponentService : GenericComponentService<CloneComponentModel,CloneModel>
    {
        public static CloneComponentModel Create(uint typeId, uint entityId, bool isMine)
            => new CloneComponentModel(typeId, entityId, isMine);
        public CloneComponentService(IECSController ecsController, IComponentModelFactory componentModelFactory) : base(ecsController, componentModelFactory)
        {
        }

        protected override void UpdateComponentMethod(CloneComponentModel model, CloneModel data)
        {
            model.model = data;
        }
    }
}