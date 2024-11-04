using System;

namespace Matterless.Floorcraft
{
    public interface IAnalyticsService
    {
        void ArSessionEnter(string sessionId, uint participantCount);
        void ShowQRCode(string sessionId);
        void HideQRCode(string sessionId);
        void ExitSession(float horizontalDuration, float verticalDuration, string spaceId);
        void SpawnVehicle(string sessionId, float raycastDistance, string vehicleType);
        void PlayerVehicleExploded(string sessionId, CarExplodeCause cause);
        void PlayerCauseAnotherPlayerExplode(string sessionId);

        void PlaceObstacle(AssetType assetType, string assetId, float raycastDistance, string sessionId,
            int participantCount);
        void StartRecording(string sessionId = "");
        void FinishRecording(float duration, string sessionId = "");
        void TakePhoto(string sessionId = "");
        void SeenDomain(string domainId);
        void EnterDomain(string domainId,DomainEnterType domainEnterType);
        void PowerUpUse(EquipmentState state, int quantity);
    }
}