using UnityEngine;

namespace Matterless.Floorcraft.TestECS
{
    [System.Serializable, CreateAssetMenu(menuName = "Matterless/Test ECS Configs")]
    public class TestECSAppConfigs : ScriptableObject
    {
        [SerializeField] private AukiSettings m_AukiSettings;
        [SerializeField] private TestECSApp.Settings m_AppSettings;

        public AukiSettings aukiSettings => m_AukiSettings;
        public TestECSApp.Settings appSettings => m_AppSettings;
    }
}