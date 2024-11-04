using System;

namespace Matterless.Floorcraft
{
    public class TransformComponentService :
        GenericComponentService<TransformComponentModel, TransformModel>
    {

        public TransformComponentService(
            IECSController ecsController, 
            IComponentModelFactory componentModelFactory) 
            : base(ecsController, componentModelFactory)
        {
            
        }

        protected override void UpdateComponentMethod(
            TransformComponentModel model, 
            TransformModel data)
        {
            model.model = data;
        }


        public void SetFrequencyToEveryFrame()
        {
            throw new NotImplementedException();
        }

        public void ResetFrequency()
        {
            throw new NotImplementedException();
        }
    }
}