using System;
using System.Collections;
using System.Collections.Generic;
using Auki.ConjureKit;
using Matterless.UTools;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Matterless.Floorcraft
{
    public partial class MayhemModeService
    {
        public class MayhemModeInstance
        {
            private NPCEnemyService m_NpcEnemyService;
            private MayhemObstacleComponentService m_MayhemObstacleComponentService;
            private MayhemUiService m_MayhemUiService;
            private Settings m_Settings;
            private List<EnemyWaveModel> m_EnemyWaveModels;
            private Transform m_TargetTransform;
            private MayhemModeObstacleView m_MayhemModeObstacleView;
            private SpawnLocationsService m_SpawnLocationsService;
            private ICoroutineRunner m_CoroutineRunner;
            private IAukiWrapper m_AukiWrapper;
            private event Action<uint, int> m_OnObstacleShouldBeRemoved;
            private List<WaveSettings> m_WaveSettings;
            private EnemyWave m_CurrentWave;
            private List<Vector3> m_SpawnPoints = new();

            public Transform targetTransform => m_TargetTransform;
            
            private uint m_Id;
            private int m_CurrentHealth;
            private bool m_IsWaveDone;
            private bool m_IsWaveEnd;
            private bool m_IsSpawningDone;
            private float m_WaveTimer;
            private float m_WaveEndTimer;
            private float m_TimeBetweenWaves;
            private float m_TimeBeforeFinishWave = 1;
            private float m_TimeBeforeFirstWave = 1;
            private int m_WaveNumber;
            private bool m_IsActive;

            public bool isStarted => m_IsActive;

            public void Init(
                uint id,
                NPCEnemyService npcEnemyService,
                MayhemObstacleComponentService mayhemObstacleComponentService,
                MayhemUiService mayhemUiService,
                Settings settings,
                Transform targetTransform,
                MayhemModeObstacleView mayhemModeObstacleView,
                SpawnLocationsService spawnLocationsService,
                List<WaveSettings> waveSettings,
                ICoroutineRunner coroutineRunner,
                IAukiWrapper aukiWrapper)
            {
                m_WaveSettings = waveSettings;
                m_MayhemModeObstacleView = mayhemModeObstacleView;
                m_MayhemObstacleComponentService = mayhemObstacleComponentService;
                m_MayhemUiService = mayhemUiService;
                m_Id = id;
                m_TargetTransform = targetTransform;
                m_Settings = settings;
                m_NpcEnemyService = npcEnemyService;
                m_SpawnLocationsService = spawnLocationsService;
                m_CoroutineRunner = coroutineRunner;
                m_AukiWrapper = aukiWrapper;
                m_NpcEnemyService.onEnemyKilled += OnEnemyKilled;
                Debug.Log("MayhemModeInstance Init");
                m_CurrentHealth = m_Settings.targetMaxHealth;
                m_TimeBetweenWaves = m_Settings.timeBetweenWaves;
                m_IsWaveDone = true;
                m_SpawnPoints.Clear();
                m_IsActive = false;
                
                m_SpawnLocationsService.Init(m_Id, true);
                m_MayhemUiService.SetOnStartButtonClicked(Start);
            }

            public void Start()
            {
                Debug.Log("MayhemModeInstance Start");
                m_EnemyWaveModels = new List<EnemyWaveModel>();
                m_CurrentWave = new EnemyWave();
                PopulateWaves();
                m_WaveNumber = 0;
                m_IsWaveDone = true;
                m_IsWaveEnd = true;
                m_IsSpawningDone = true;
                m_MayhemUiService.HideButton();
                m_IsActive = true;
            }

            public void ClearOnObstacleDestroy()
            {
                m_NpcEnemyService.ClearEnemies();
                ClearWaves();
                m_IsWaveDone = false;
                m_IsWaveEnd = false;
                m_IsActive = false;
            }

            private void OnWaveCompleted()
            {
                m_MayhemModeObstacleView.DeactivateSpawnPoints();
                m_SpawnPoints.Clear();
                m_IsWaveDone = true;
                m_IsWaveEnd = true;
            }

            private void OnWaveStart()
            {
                TrySetNewWave();
                m_MayhemUiService.HideLabels();
                SetSpawnPoints();
                //m_MessageComponentService.SendMessage(m_Id, MessageModel.Message.WaveStart, m_Id);
                SendMayhemEventMessage(MessageModel.Message.WaveStart);
            }

            private void UpdateAndBroadcastWaveUI(int waveNumber)
            {
                m_MayhemUiService.ShowLabels();
                // We will show the next wave number since we are going to load next one
                m_MayhemUiService.UpdateWaveNumber(waveNumber + 1);
                // Updating mayhem obstacle with next wave number to update the client with correct data
                m_MayhemObstacleComponentService.UpdateComponent(m_Id, new MayhemObstacleModel(MayhemObstacleState.None, m_CurrentHealth, waveNumber + 1));
                //m_MessageComponentService.SendMessage(m_Id, MessageModel.Message.WaveComplete, m_Id);
                
                //SendMayhemUpdateMessage(-1, waveNumber + 1);

                if (m_CurrentHealth > 0)
                {
                    SendMayhemEventMessage(MessageModel.Message.WaveComplete);
                }
            }

            private void OnEnemyKilled()
            {
                m_CurrentWave.OnEnemyKilled();
            }

            private void PopulateWaves()
            {
                for (int i = 0; i < m_WaveSettings.Count; i++)
                {
                    m_EnemyWaveModels.Add(new EnemyWaveModel()
                    {
                        spawnFrequencyMax = m_WaveSettings[i].spawnFrequencyMax,
                        spawnFrequencyMin = m_WaveSettings[i].spawnFrequencyMin,
                        maxNumberOfEnemies = m_WaveSettings[i].maxNrOfEnemies,
                        spawnPointCount = m_WaveSettings[i].spawnPointCount,
                        onlyTheseEnemyTypes = m_WaveSettings[i].onlyTheseEnemyTypes,
                        onWaveCompleted = OnWaveCompleted,
                        spawnEnemy = CreateEnemy
                    });
                }
            }

            private void TrySetNewWave()
            {
                if (m_EnemyWaveModels.Count > m_WaveNumber)
                {
                    m_CurrentWave.UpdateModel(m_EnemyWaveModels[m_WaveNumber]);
                }
                else
                {
                    m_CurrentWave.UpdateModel(m_EnemyWaveModels[m_EnemyWaveModels.Count - 1]);
                }
                
                m_WaveNumber++;
            }

            private void SetSpawnPoints()
            {
                NPCEnemySpawnPoint[] spawnPoints = m_MayhemModeObstacleView.GetSpawnPoints(m_SpawnLocationsService.GenerateSpawnPoints(m_Id, m_CurrentWave.model.spawnPointCount));
                foreach (var sp in spawnPoints)
                {
                    m_SpawnPoints.Add(sp.position);
                }
            }

            private void CreateEnemy()
            {
                int rand = Random.Range(0, m_SpawnPoints.Count);
                Vector3 newPos = m_SpawnPoints[rand];
                m_NpcEnemyService.CreateEnemy(newPos, m_TargetTransform, OnEnemySpawned, m_CurrentWave.model.onlyTheseEnemyTypes);
                m_SpawnPoints.RemoveAt(rand);
            }

            private void OnEnemySpawned(uint uid)
            {
                m_CurrentWave.OnEnemySpawned();
            }

            private void TakeDamage(int damage)
            {
                m_CurrentHealth -= damage;
                m_MayhemModeObstacleView.SetHealth(m_CurrentHealth);

                if (!m_MayhemObstacleComponentService.TryGetComponentModel(m_Id, out MayhemObstacleComponentModel mayhemObstacleComponentModel))
                {
                    return;
                }
                
                m_MayhemObstacleComponentService.UpdateComponent(m_Id, new MayhemObstacleModel(mayhemObstacleComponentModel.model.state, m_CurrentHealth, m_WaveNumber));
                //SendMayhemUpdateMessage(m_CurrentHealth, m_WaveNumber);
                if (m_CurrentHealth <= 0)
                {
                    //m_MessageComponentService.SendMessage(m_Id, MessageModel.Message.ObstacleTotaled, m_Id);
                    SendMayhemEventMessage(MessageModel.Message.ObstacleTotaled);
                    m_CoroutineRunner.StartUnityCoroutine(DelayedDeath());
                }
            }

            private IEnumerator DelayedDeath()
            {
                // A short delay to let all clients trigger the tower death effect (and un-child it) before entity gets destroyed
                yield return new WaitForSeconds(0.3f);
                ClearOnObstacleDestroy();
                m_OnObstacleShouldBeRemoved?.Invoke(m_Id, m_WaveNumber);
            }
            
            private void SendMayhemEventMessage(MessageModel.Message message)
            {
                uint[] participants = m_AukiWrapper.GetSession().GetParticipantsIds().ToArray();
                MayhemEventMessage eventMessage = new MayhemEventMessage(CustomMessageId.MayhemEvent, message);
                m_AukiWrapper.SendCustomMessage(participants, eventMessage.GetBytes());
            }
            
            private void SendMayhemUpdateMessage(int towerHealth, int waveNumber)
            {
                uint[] participants = m_AukiWrapper.GetSession().GetParticipantsIds().ToArray();
                MayhemUpdateMessage updateMessage = new MayhemUpdateMessage(CustomMessageId.MayhemUpdate, towerHealth, waveNumber);
                m_AukiWrapper.SendCustomMessage(participants, updateMessage.GetBytes());
            }
            
            private void SendMayhemUpdateMessageToParticipant(uint participant, int towerHealth, int waveNumber)
            {
                uint[] participants = { participant };
                MayhemUpdateMessage updateMessage = new MayhemUpdateMessage(CustomMessageId.MayhemUpdate, towerHealth, waveNumber);
                m_AukiWrapper.SendCustomMessage(participants, updateMessage.GetBytes());
            }

            public void Tick(float deltaTime)
            {
                if(!m_IsActive)
                {
                    return;
                }
                
                if (m_IsWaveDone)
                {
                    m_WaveTimer += deltaTime;
                    m_WaveEndTimer += deltaTime;

                    if (m_WaveEndTimer >= m_TimeBeforeFinishWave && m_IsWaveEnd)
                    {
                        UpdateAndBroadcastWaveUI(m_WaveNumber);
                        m_WaveEndTimer = 0;
                        m_IsWaveEnd = false;
                    }

                    if (m_WaveTimer >= m_TimeBetweenWaves + m_TimeBeforeFinishWave)
                    {
                        m_IsWaveDone = false;
                        m_WaveTimer = 0;
                        OnWaveStart();
                    }

                    return;
                }
                
                if (m_EnemyWaveModels == null)
                {
                    return;
                }
                
                if (m_EnemyWaveModels.Count == 0)
                {
                    return;
                }

                if (m_CurrentWave == null)
                {
                    Debug.LogError("No active wave, missing something?");
                    return;
                }

                m_CurrentWave.Update(deltaTime);
            }

            public void DamageObjective()
            {
                TakeDamage(1);
            }

            public void SetOnObstacleShouldBeRemoved(Action<uint, int> evt)
            {
                m_OnObstacleShouldBeRemoved += evt;
            }

            private void ClearWaves()
            {
                m_EnemyWaveModels?.Clear();
            }
        }
    }
}