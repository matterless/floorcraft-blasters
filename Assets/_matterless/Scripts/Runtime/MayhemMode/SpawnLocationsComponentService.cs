namespace Matterless.Floorcraft
{
    public class SpawnLocationsComponentService : GenericComponentService<SpawnLocationsComponentModel, SpawnLocationsModel>
    {
        public SpawnLocationsComponentService(IECSController ecsController, IComponentModelFactory componentModelFactory) 
            : base(ecsController, componentModelFactory)
        {
        }

        protected override void UpdateComponentMethod(SpawnLocationsComponentModel model, SpawnLocationsModel data)
        {
            UnityEngine.Debug.Log($"UpdateComponentMethod  {data.data[0]}");
            model.model = data;
        }
    }
}