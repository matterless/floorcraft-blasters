using Matterless.Inject;
using UnityEngine;
using Auki.ConjureKit;
using Matterless.UTools;
using System.Collections;
using Newtonsoft.Json;
using System.IO;

namespace Matterless.Floorcraft.TestECS
{
    [System.Serializable]
    public class AutoConnectionConfigs
    {
        const string CONFIG_NAME = "autoconnectioncofig.json";

        public bool isHost { get; set; }
        public string hostPathConfig { get; set; }
        public string currentSessionId { get; set; }

        public static AutoConnectionConfigs GetMyConfig()
            => JsonConvert.DeserializeObject<AutoConnectionConfigs>(File.ReadAllText(GetMyConfigPath()));
        public static AutoConnectionConfigs GetConfig(string path)
        {
            return JsonConvert.DeserializeObject<AutoConnectionConfigs>(File.ReadAllText(path));
        }

        public void Save(string sessionId)
        {
            currentSessionId = sessionId;
            File.WriteAllText(GetMyConfigPath(),JsonConvert.SerializeObject(this));
        }

        private static string GetMyConfigPath()
        {
            var path = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(path, CONFIG_NAME);
        }
    }

    public class TestECSApp : ITickable
    {
        [System.Serializable]
        public class Settings
        {
            [SerializeField] private bool m_CreateEntity = true;

            public bool createEntity => m_CreateEntity;
        }

        private readonly IAukiWrapper m_AukiWrapper;
        private readonly ICoroutineRunner m_CoroutineRunner;
        private readonly TestECSTestComponentService m_TestECSTestComponentService;
        private readonly TestECSAppView m_AppView;
        private readonly Settings m_Settings;
        private Entity m_Entity;
        private TestECSTestComponentModel m_Model;
        private AutoConnectionConfigs m_MyAutoConfig;

        public TestECSApp (
            IAukiWrapper aukiWrapper, 
            ICoroutineRunner coroutineRunner,
            TestECSTestComponentService testECSTestComponentService,
            TestECSAppView appView,
            Settings settings)
        {
            aukiWrapper.onJoined += AukiWrapper_onJoined;
            aukiWrapper.onLeft += AukiWrapper_onLeft;
            m_AukiWrapper = aukiWrapper;
            m_CoroutineRunner = coroutineRunner;
            m_TestECSTestComponentService = testECSTestComponentService;
            m_AppView = appView;
            m_Settings = settings;
            m_TestECSTestComponentService.onComponentAdded += M_TestECSTestComponentService_onComponentAdded;
            m_TestECSTestComponentService.onComponentUpdated += M_TestECSTestComponentService_onComponentUpdated;
            m_TestECSTestComponentService.onComponentDeleted += M_TestECSTestComponentService_onComponentDeleted;
            
            m_AppView.UpdateHeaderText("no session");

            m_MyAutoConfig = AutoConnectionConfigs.GetMyConfig();

            if(m_MyAutoConfig.isHost)
            {
                m_AukiWrapper.Join();
            }
            else
            {
                var hostConfig = AutoConnectionConfigs.GetConfig(m_MyAutoConfig.hostPathConfig);
                m_AukiWrapper.Join(hostConfig.currentSessionId,null);
            }
        }
        private void AukiWrapper_onJoined(Session session)
        {
            Debug.Log($"Join {session.Id}");

            m_AppView.UpdateHeaderText(session.Id);

            if(m_MyAutoConfig.isHost)
            {
                m_MyAutoConfig.Save(session.Id);
            }

            if (!m_Settings.createEntity)
                return;

            // TODO:: ????
            // if we do not wait
            // then ECSController::FetchEntityComponets is running 2 times ???????????????
            m_CoroutineRunner.StartUnityCoroutine(CreateEntity());
        }

        private void AukiWrapper_onLeft()
        {
            Debug.LogWarning("Session left!");
            m_AppView.UpdateHeaderText("no session");
        }

        private void M_TestECSTestComponentService_onComponentAdded(TestECSTestComponentModel model)
        {
            //Debug.Log($"On component added {model.entityId} -> {model.value}");
            m_AppView.CreateEntry(model);
        }

        private void M_TestECSTestComponentService_onComponentDeleted(uint entityId, bool isMine)
        {
            //Debug.Log($"On component deleted {entityId}");
            m_AppView.DeleteEntry(entityId);
        }

        private void M_TestECSTestComponentService_onComponentUpdated(TestECSTestComponentModel model)
        {
            //Debug.Log($"On component updated {model.entityId} -> {model.value}");
            m_AppView.UpdateEntry(model);
        }

        private IEnumerator CreateEntity()
        {
            yield return new WaitForSeconds(0.5f);

            //create entity
            m_AukiWrapper.AddEntity(new Pose(), false,
                // on success
                (entity) =>
                {
                    m_Entity = entity;
                    Debug.Log($"Entity created: {entity.Id}");
                    AddComponent(entity);
                },
                // on error
                Debug.Log);
        }

        private void AddComponent(Entity entity)
        {
            var color = Random.ColorHSV();
            Debug.Log($"Try to add component with value {color}");
            m_Model = m_TestECSTestComponentService.AddComponent(entity.Id, color);
            Debug.Log($"Created model value: {m_Model.color}");
        }

        private void UpdateComponent()
        {
            var color = Random.ColorHSV();
            Debug.Log($"Try to update component with value {color}");
            m_TestECSTestComponentService.UpdateComponent(m_Model, color);
        }

        private float m_Timer;

        public void Tick(float deltaTime, float unscaledDeltaTime)
        {
            if (m_Model == null)
                return;

            m_Timer += deltaTime;

            if (m_Timer > 2)
            {
                m_Timer = 0;
                UpdateComponent();
            }
        }
    }
}