namespace Matterless.Floorcraft.TestECS
{
    public class TestECSAnalyticsSerice : IAnalyticsService
    {
        public void ArSpaceEnter(string spaceId)
        {
            //throw new System.NotImplementedException();
        }

        public void ExitSession(float horizontalDuration, float verticalDuration, string spaceId)
        {
            //throw new System.NotImplementedException();
        }

        public void FinishRecording(float duration, string sessionId = "")
        {
            //throw new System.NotImplementedException();
        }

        public void HideQRCode(string sessionId)
        {
            //throw new System.NotImplementedException();
        }

       

        public void PlayerCauseAnotherPlayerExplode(string sessionId)
        {
            //throw new System.NotImplementedException();
        }

        public void PlaceObstacle(AssetType assetType, string assetId, float raycastDistance, string sessionId, int participantCount)
        {
            throw new System.NotImplementedException();
        }

        public void PlayerVehicleExploded(string sessionId, CarExplodeCause cause)
        {
            //throw new System.NotImplementedException();
        }

        public void ArSessionEnter(string sessionId, uint participantCount)
        {
            throw new System.NotImplementedException();
        }

        public void ShowQRCode(string sessionId)
        {
            //throw new System.NotImplementedException();
        }

        public void SpawnVehicle(string sessionId, float raycastDistance, string vehicleType)
        {
            //throw new System.NotImplementedException();
        }

        public void StartRecording(string sessionId = "")
        {
            //throw new System.NotImplementedException();
        }

        public void TakePhoto(string sessionId = "")
        {
            //throw new System.NotImplementedException();
        }

        public void SeenDomain(string domainId)
        {
            throw new System.NotImplementedException();
        }

        public void EnterDomain(string domainId,DomainEnterType domainEnterType)
        {
            throw new System.NotImplementedException();
        }

        public void PowerUpUse(EquipmentState state, int quantity)
        {
            throw new System.NotImplementedException();
        }
    }
}