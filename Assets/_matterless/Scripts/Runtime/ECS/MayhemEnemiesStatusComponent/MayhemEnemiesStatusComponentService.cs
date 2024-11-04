namespace Matterless.Floorcraft
{
    public class MayhemEnemiesStatusComponentService : GenericComponentService<MayhemEnemiesStatusComponentModel, MayhemEnemiesStatusModel>
    {
        public MayhemEnemiesStatusComponentService(IECSController ecsController, IComponentModelFactory componentModelFactory) 
            : base(ecsController, componentModelFactory)
        {
        }
        
        protected override void UpdateComponentMethod(MayhemEnemiesStatusComponentModel model, MayhemEnemiesStatusModel data)
        {
            UnityEngine.Debug.Log($"UpdateComponentMethod  {data}");
            model.model = data;
        }
    }
}