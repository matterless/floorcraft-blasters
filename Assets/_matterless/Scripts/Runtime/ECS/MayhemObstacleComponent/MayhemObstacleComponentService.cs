namespace Matterless.Floorcraft
{
    public class MayhemObstacleComponentService : GenericComponentService<MayhemObstacleComponentModel, MayhemObstacleModel>
    {
        public MayhemObstacleComponentService(IECSController ecsController, IComponentModelFactory componentModelFactory) 
            : base(ecsController, componentModelFactory)
        {
        }
        
        protected override void UpdateComponentMethod(MayhemObstacleComponentModel model, MayhemObstacleModel data)
        {
            UnityEngine.Debug.Log($"UpdateComponentMethod  {data.state}");
            model.model = data;
        }
    }
}