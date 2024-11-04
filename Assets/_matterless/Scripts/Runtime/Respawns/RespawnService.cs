using System;
using Matterless.Inject;
using Matterless.UTools;
using UnityEngine;

namespace Matterless.Floorcraft
{
	public class RespawnService : ITickable
	{
		[Serializable]
		public class Settings
		{
			public int maxRespawns => m_MaxRespawns;
			public int spawnRechargeDuration => m_SpawnRechargeDuration;

			[SerializeField] private int m_MaxRespawns;
			[SerializeField] private int m_SpawnRechargeDuration = 100;
		}

		private const string RESPAWN_SAVE_DATA_KEY = "RespawnSaveData";
		
		public int respawnQuantity => unlimitedRespawns ? int.MaxValue : m_RespawnQuantity;
		public string timerView => TimeSpan.FromSeconds(m_Timer).ToString(@"mm\:ss");
		public bool unlimitedRespawns { get; private set; }

		private readonly Settings m_Settings;
		private readonly IUnityEventDispatcher m_UnityEventDispatcher;
		private readonly IDomainService m_DomainService;
		private readonly IPlayerPrefsService m_PlayerPrefsService;
		private readonly IStoreService m_StoreService;
		private readonly RespawnSaveData m_RespawnSaveData;
		private readonly bool m_Initialized = false;
		private float m_Timer;
		
		private int m_RespawnQuantity
		{
			get => m_RespawnSaveData.respawnQuantity;
			set
			{
				Debug.Log($"SAVE set m_RespawnService.respawnQuantity: {value}");
				m_RespawnSaveData.respawnQuantity = value;
			}
		}

		public RespawnService(
			RespawnService.Settings settings,
			IUnityEventDispatcher unityEventDispatcher,
			IDomainService domainService,
			IPlayerPrefsService playerPrefsService,
			IStoreService storeService)
		{
			//playerPrefsService.DeleteKey(RESPAWN_SAVE_DATA_KEY);

			m_Timer = 0f;
			m_Settings = settings;
			m_UnityEventDispatcher = unityEventDispatcher;
			m_DomainService = domainService;
			m_PlayerPrefsService = playerPrefsService;
			m_StoreService = storeService;
			m_UnityEventDispatcher.unityOnApplicationPause += OnApplicationPause;
			m_UnityEventDispatcher.unityApplicationQuit += OnApplicationQuit;
			m_UnityEventDispatcher.unityOnApplicationFocus += OnApplicationFocus;
			m_StoreService.onPremiumUnlocked += OnPremiumUnlocked;
			var defaultSaveData = new RespawnSaveData(DateTimeOffset.Now.ToUnixTimeSeconds(),0f, m_Settings.maxRespawns, false);
			m_RespawnSaveData = JsonUtility.FromJson<RespawnSaveData>
			(
				playerPrefsService.GetString
				(
					RESPAWN_SAVE_DATA_KEY,
					JsonUtility.ToJson(defaultSaveData)
				)
			);
			AddRespawnsFromSaveDataTime();
			m_Initialized = true;
			EvaluateUnlimitedRespawns();
		}

		private void OnPremiumUnlocked()
		{
			EvaluateUnlimitedRespawns();
		}

		private bool EvaluateUnlimitedRespawns()
		{
			unlimitedRespawns = m_StoreService.isPremiumUnlocked 
			                      || m_DomainService.sessionIdDomain
			                      || !m_StoreService.premiumEnabled;
			return unlimitedRespawns;
		}

		public void Use()
		{
			if(EvaluateUnlimitedRespawns())
				return;

			if (m_RespawnQuantity >= m_Settings.maxRespawns)
				m_Timer = m_Settings.spawnRechargeDuration;
			
			m_RespawnQuantity--;
		}
		
		public void Tick(float deltaTime, float unscaledDeltaTime)
		{
			// TODO:: we evaluate this every frame. Maybe it's better to create events
			//EvaluateUnlimitedRespawns();
			
			if (m_RespawnQuantity >= m_Settings.maxRespawns || !m_Initialized)
				return;

			m_Timer -= Time.deltaTime;
			
			if (m_Timer < 0)
			{
				m_RespawnQuantity++;
				m_Timer = m_Settings.spawnRechargeDuration;
				EvaluateUnlimitedRespawns();
			}
		}
		private void OnApplicationQuit()
		{
			m_RespawnSaveData.timer = Convert.ToInt64(m_Timer);
			m_RespawnSaveData.appLastClosedTime = DateTimeOffset.Now.ToUnixTimeSeconds();;
			m_RespawnSaveData.dirty = true;
			SaveDataToDisk();
		}
		private void OnApplicationPause(bool pauseStatus)
		{
			if (!pauseStatus)
				return;

			m_RespawnSaveData.timer = Convert.ToInt64(m_Timer);
			m_RespawnSaveData.appLastClosedTime = DateTimeOffset.Now.ToUnixTimeSeconds();;
			m_RespawnSaveData.dirty = true;
			SaveDataToDisk();
		}
		
		private void OnApplicationFocus(bool hasFocus)
		{
			if (!hasFocus)
				return;
			
			AddRespawnsFromSaveDataTime();
		}
		
		private void AddRespawnsFromSaveDataTime()
		{
			//Debug.Log($"Add respawns: {m_RespawnSaveData}");

			if (!m_RespawnSaveData.dirty)
				return;

			var now = DateTimeOffset.Now.ToUnixTimeSeconds();
			var totalSeconds = now - m_RespawnSaveData.appLastClosedTime;
			var newRespawns = Convert.ToInt32( (long)(m_Settings.spawnRechargeDuration - m_RespawnSaveData.timer + totalSeconds) / (long)m_Settings.spawnRechargeDuration);
			m_Timer = m_RespawnSaveData.timer - totalSeconds + (m_Settings.spawnRechargeDuration * newRespawns);
			m_RespawnQuantity = Math.Clamp(m_RespawnQuantity + newRespawns, 0, m_Settings.maxRespawns);
			m_RespawnSaveData.dirty = false;
			//Debug.Log($"{now} -- total:{totalSeconds} -- new:{newRespawns} -- quantity:{m_RespawnQuantity} -- m_Timer:{m_Timer}");
		}

		private void SaveDataToDisk()
		{
			m_PlayerPrefsService.SetString(RESPAWN_SAVE_DATA_KEY, JsonUtility.ToJson(m_RespawnSaveData));
		}
		public void AddOneRespawn()
		{
			if (m_RespawnQuantity >= m_Settings.maxRespawns)
				return;
			
			m_RespawnQuantity++;
		}
	}
}