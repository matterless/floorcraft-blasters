using UnityEngine;

namespace Matterless.Floorcraft
{
    public static class SpeederHelper
    {
        public static (Vector3, Vector3) GetFloorNormalAndPosition(ISpeederSimulation simulation, IRaycastService raycastService, Vector3? targetPosition)
        {
            var floorRaycastOrigin = simulation.groundPosition;
            floorRaycastOrigin += simulation.rotation * Vector3.forward * Mathf.Clamp01(simulation.boosting);
            
            if (targetPosition.HasValue)
            {
                // This lets you drive either under a table or up onto it based on where you aim.
                floorRaycastOrigin.y = targetPosition.Value.y;
            }

            bool hasFloorHit = raycastService.FloorRaycast(floorRaycastOrigin, out Vector3 floorPosition, out Vector3 floorNormal);
            if(!hasFloorHit)
            {
                floorPosition = simulation.groundPosition;
                floorNormal = Vector3.up;
            }

            return (floorNormal, floorPosition);
        }
        public static Vector3? GetTarget(IRaycastService raycastService) => (raycastService.hasHit) ? raycastService.hitPose.position : null;
    }
}