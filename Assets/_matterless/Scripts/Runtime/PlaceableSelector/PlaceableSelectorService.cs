using System;
using System.Collections.Generic;
using Matterless.Inject;
using Matterless.Localisation;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
namespace Matterless.Floorcraft
{
	public class PlaceableSelectorService : ITickable
	{
		[System.Serializable]
		public class Settings
		{
			[SerializeField] private float m_PageWidth = 500f;
			[SerializeField] private float m_MinScale = 0.25f;
			[SerializeField] private float m_MaxScale = 1;
			[SerializeField] private float m_VisibilityThreshold = 0.0001f;
			[SerializeField] private float m_GridWidth = 0.02f;
			[SerializeField] private float m_ScrollMultiplier = 1f;
			[SerializeField] private float m_FadeTime = 0.25f;
			[SerializeField] private List<Placeable> m_Placeables;
			[SerializeField] private Placeable m_MayhemObstacle;
			[SerializeField] private Placeable m_FFAObstacle;

			public float minScale => m_MinScale;
			public float maxScale => m_MaxScale;
			public float visibilityThreshold => m_VisibilityThreshold;
			public float gridWidth => m_GridWidth;
			public float scrollMultiplier => m_ScrollMultiplier;
			public float fadeTime => m_FadeTime;
			public List<Placeable> placeables => m_Placeables;
			public Placeable MayhemObstacle => m_MayhemObstacle;
			public Placeable FFAObstacle => m_FFAObstacle;

			public float pageWidth => m_PageWidth;

			public Placeable GetAsset(uint id) => m_Placeables.Find(x => x.id == id);
		}
		
		private readonly PlaceableSelectorView m_View;
		private readonly AudioUiService m_AudioUiService;
		private readonly ILocalisationService m_LocalisationService;
		private readonly ObstaclesUiService m_ObstaclesUiService;
		private readonly Settings m_Settings;
		private readonly ObstacleService m_ObstacleService;
		private readonly List<Transform> m_Placeables;
		
		private Action<Placeable> m_OnVehicleSelected;
		private UnityEvent m_OnBackButtonClicked = new UnityEvent();
		private float m_Scroll;
		private float m_Anchor;
		private bool m_Dragging;
		private int m_CurrentPage = 0;
		
		private int totalPages => m_Placeables == null ? 0 : m_Placeables.Count;
		public UnityEvent OnBackButtonClicked => m_OnBackButtonClicked;
		

		public PlaceableSelectorService(
			AudioUiService audioUiService,
			ILocalisationService localisationService,
			ObstaclesUiService obstaclesUiService,
			Settings settings,
			ObstacleService obstacleService
			)
		{
			m_View = PlaceableSelectorView.Create("UIPrefabs/UIP_PlaceableSelectorView").Init();
			m_View.onDrag += OnDrag;
			m_View.onEndDrag += OnEndDrag;

			localisationService.RegisterUnityUIComponents(m_View.gameObject);

			m_View.onBackButtonClicked += OnCanceled;
			m_View.RegisterOnSelect(SelectPlaceable);
			m_AudioUiService = audioUiService;
			m_LocalisationService = localisationService;
			m_ObstaclesUiService = obstaclesUiService;
			m_Settings = settings;
			m_ObstacleService = obstacleService;

			// instantiate vehicles
			m_Placeables = new List<Transform>();

			foreach (var item in settings.placeables)
				m_Placeables.Add(GameObject.Instantiate(item.selectorPrefab, m_View.parentTransform).transform);

			// Setting the default obstacle
			m_ObstacleService.SetPlaceable(settings.FFAObstacle);
			m_Dragging = false;
			m_Scroll = m_Anchor = 0;
			m_View.Hide();
			var text = m_LocalisationService.Translate(m_Settings.placeables[m_CurrentPage].selectLocalisationTag);
			m_View.UpdateSelectButtonText(text);
		}
		private void SelectPlaceable()
		{
			m_ObstacleService.SetPlaceable(m_Settings.placeables[m_CurrentPage]);
			OnCanceled();
		}

		public void Tick(float deltaTime, float unscaledDeltaTime)
		{
			if (m_Settings == null || m_Placeables == null)
				return;

			if (!m_Dragging)
				m_Scroll = Mathf.Lerp(m_Scroll, m_Anchor, deltaTime * Mathf.Abs(m_Scroll - m_Anchor) * m_Settings.fadeTime);

			for (int i = 0; i < m_Placeables.Count; i++)
			{
				m_Placeables[i].localPosition = Vector3.right * (i - m_Scroll) * 1 * m_Settings.gridWidth;
				m_Placeables[i].localScale =
					m_Placeables[i].localEulerAngles = Vector3.up * (Time.timeSinceLevelLoad * 180 + 360 * Mathf.Sin(Mathf.PI * i / 5f));

				float scale = Mathf.Clamp(1 - Mathf.Abs(i - m_Scroll), m_Settings.minScale, m_Settings.maxScale) * 0.01f;
				m_Placeables[i].localScale = Vector3.one * scale;
				// visibility
				m_Placeables[i].gameObject.SetActive(scale > m_Settings.minScale + m_Settings.visibilityThreshold);
			}
		}
		
		private void OnEndDrag(PointerEventData data)
		{
			m_Dragging = false;
			var newPage = Mathf.Clamp(m_Scroll, 0, totalPages - 1);
			m_Anchor = Mathf.RoundToInt(newPage);
			m_CurrentPage = (int)m_Anchor;
			var text = m_LocalisationService.Translate(m_Settings.placeables[m_CurrentPage].selectLocalisationTag);
			m_View.UpdateSelectButtonText(text);
		}

		private void OnDrag(PointerEventData data)
		{
			m_Dragging = true;
			var give = 0.25f;
			float delta = m_Settings.scrollMultiplier * (data.pressPosition.x - data.position.x) / m_Settings.pageWidth;
			var lastScroll = Mathf.RoundToInt(m_Scroll);
			m_Scroll = Mathf.Clamp(m_Anchor + delta, -give, totalPages - 1 + give);

			if (Mathf.RoundToInt(m_Scroll) != lastScroll)
				m_AudioUiService.PlayScrollSound();
		}
		
		private void OnCanceled()
		{
			m_View.Hide();
			m_OnBackButtonClicked?.Invoke();
		}


		public void ShowView()
		{
			m_View.Show();
		}
	}
}