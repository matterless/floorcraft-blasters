namespace Matterless.Floorcraft
{
    public class EnemyStateComponentService : GenericComponentService<EnemyStateComponentModel, EnemyStateModel>
    {
        public EnemyStateComponentService(IECSController ecsController, IComponentModelFactory componentModelFactory) 
            : base(ecsController, componentModelFactory)
        {
        }
        
        protected override void UpdateComponentMethod(EnemyStateComponentModel model, EnemyStateModel data)
        {
            UnityEngine.Debug.Log($"UpdateComponentMethod  {data.state}");
            model.model = data;
        }
        
        public void SetState(uint entityId, EnemyState state, int health)
        {
            var model = GetComponentModel(entityId);
            UpdateComponent(entityId, new EnemyStateModel(model.model.state | state, health));
        }
        
        public void UnsetState(uint entityId, EnemyState state, int health)
        {
            var model = GetComponentModel(entityId);
            UpdateComponent(entityId, new EnemyStateModel(model.model.state & ~state, health));
        }
    }
}