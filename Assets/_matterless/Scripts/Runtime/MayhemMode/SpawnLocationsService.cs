using System;

namespace Matterless.Floorcraft
{
    public class SpawnLocationsService
    {
        private SpawnLocations m_SpawnLocations;
        private readonly SpawnLocationsComponentService m_SpawnLocationsComponentService;

        private bool m_IsInitialized;

        public event Action onSpawnLocationsUpdated;

        public SpawnLocationsService(
            SpawnLocationsComponentService spawnLocationsComponentService
            )
        {
            m_SpawnLocationsComponentService = spawnLocationsComponentService;
            m_SpawnLocationsComponentService.onComponentUpdated += OnUpdated;
        }

        private int[] m_CurrentLocationIndexes = new [] {0};
        public int[] currentLocationIndexes => m_CurrentLocationIndexes;

        public void Init(uint id, bool isHost)
        {
            if (!m_IsInitialized)
            {
                m_SpawnLocations = new SpawnLocations();
            }

            if (isHost)
            {
                m_SpawnLocationsComponentService.AddComponent(id, new SpawnLocationsModel(new byte[2]));
            }

            m_IsInitialized = true;
        }
        
        public void Init()
        {
            if (!m_IsInitialized)
            {
                m_SpawnLocations = new SpawnLocations();
            }

            m_IsInitialized = true;
        }

        private void OnUpdated(SpawnLocationsComponentModel model)
        {
            if (model.isMine)
            {
                return;
            }

            m_CurrentLocationIndexes = m_SpawnLocations.Decode(model.model.data);
            onSpawnLocationsUpdated?.Invoke();
        }

        public int[] GenerateSpawnPoints(uint id, int count)
        {
            int[] points = m_SpawnLocations.GenerateSpawnPoints(count);
            m_SpawnLocationsComponentService.UpdateComponent(id, new SpawnLocationsModel(m_SpawnLocations.encodedPoints));
            return points;
        }
    }
}
