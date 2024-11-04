using UnityEngine;

namespace Matterless.Floorcraft
{
    public enum AssetId
    {
        Pillar = 100,
        Plane = 101,
        PlaneMatterless = 102,
        ProximityMine = 103,
        WreckingBallProjectile = 104,
        FlameThrowerPowerUpSpawnPoint = 105, 
        WreckingBallMagnetSpawnPoint = 106,
        DashAttackSpawnPoint = 107,
        LaserSpawnPoint = 108,
        ProximityMineSpawnPoint = 109,
        ShadowCloneSpawnPoint = 110,
        Car0 = 200,
        Car1 = 201,
        Muscle0 = 210,
        Muscle1 = 211,
        Muscle2 = 212,
        Speedster0 = 213,
        Speedster1 = 214,
        Speedster2 = 215,
        Speedster3 = 216,
        Speedster4 = 217,
        Speedster5 = 218,
        Speedster6 = 219,
        Test = 220,
        MayhemPillar = 290,
        EnemyRed = 300,
        EnemyBlue = 301,
        EnemyGreen = 302
    }

    public enum AssetType
    {
        Vehicle = 0,
        Obstacle = 1,
        ProximityMine = 2,
        Projectile = 3,
        PowerUpSpawnPoint = 4,
        Enemy = 5
    }

    public interface IAsset
    {
        uint id { get; }
        string resourcesPath { get; }
        AssetType assetType { get; }
        public Vector3 scale { get; }
        public AssetId assetId { get; }
        bool persistent { get; }
    }

    [System.Serializable]
    [CreateAssetMenu(menuName = "Matterless/Asset")]
    public class Asset : ScriptableObject, IAsset
    {
        [SerializeField] private AssetId m_Id;
        [SerializeField] private string m_ResourcesPath;
        [SerializeField] private AssetType m_AssetType;
        [SerializeField] private float m_Size;
        [SerializeField] private bool m_PersistentInSession;

        public uint id => (uint)m_Id;
        public string resourcesPath => m_ResourcesPath;
        public AssetType assetType => m_AssetType;
        public Vector3 scale => Vector3.one * m_Size;
        public AssetId assetId => m_Id;
        public float size => m_Size;

        public bool persistent => m_PersistentInSession;
    }
}