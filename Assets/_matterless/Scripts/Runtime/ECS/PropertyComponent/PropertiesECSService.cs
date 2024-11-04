using System.Collections.Generic;

namespace Matterless.Floorcraft
{
    public class PropertiesECSService
    {
        public class Settings
        {
            private Dictionary<uint, IAsset> m_Assets;

            public Settings
            (
                VehicleSelectorService.Settings vehicleSettings,
                ObstacleService.Settings obstacleSettings,
                WreckingBallMagnetService.Settings wreckingBallMagnetSettings,
                ProximityMineService.Settings proximityMineSettings,
                PlaceableSelectorService.Settings placeableSettings,
                NPCEnemyService.Settings enemySettings
            )
            {
                m_Assets = new();

                foreach (var item in vehicleSettings.vehicles)
                    m_Assets.Add(item.id, item);

                foreach (var item in obstacleSettings.obstacles)
                    m_Assets.Add(item.id, item);

                m_Assets.Add(proximityMineSettings.proximityMine.id, proximityMineSettings.proximityMine);
                m_Assets.Add(wreckingBallMagnetSettings.wreckingBallProjectile.id, wreckingBallMagnetSettings.wreckingBallProjectile);
                foreach (var placeable in placeableSettings.placeables)
                {
                    if (m_Assets.ContainsKey(placeable.id))
                        continue;
                    
                    m_Assets.Add(placeable.id,placeable);
                }
                
                foreach (var item in enemySettings.Enemies)
                    m_Assets.Add(item.id, item);
            }

            public IAsset GetAsset(uint id) => m_Assets.ContainsKey(id) ? m_Assets[id] : null; // O(1)

        }
    }
}