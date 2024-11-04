namespace Matterless.Floorcraft
{
    public class SpeederStateComponentService : GenericComponentService<SpeederStateComponentModel, SpeederStateModel>
    {
        public SpeederStateComponentService(IECSController ecsController, IComponentModelFactory componentModelFactory) 
            : base(ecsController, componentModelFactory)
        {
        }

        protected override void UpdateComponentMethod(SpeederStateComponentModel model, SpeederStateModel data)
        {
            UnityEngine.Debug.Log($"UpdateComponentMethod  {data.state}");
            model.model = data;
        }

        public void SetState(uint entityId, SpeederState state)
        {
            var model = GetComponentModel(entityId);
            UpdateComponent(entityId, new SpeederStateModel(model.model.state | state));
        }

        public void UnsetState(uint entityId, SpeederState state)
        {
            var model = GetComponentModel(entityId);
            UpdateComponent(entityId, new SpeederStateModel(model.model.state & ~state));
        }
    }
}