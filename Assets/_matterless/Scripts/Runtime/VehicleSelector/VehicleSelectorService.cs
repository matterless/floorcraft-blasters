using System;
using System.Collections.Generic;
using System.Numerics;
using Matterless.Inject;
using Matterless.Localisation;
using UnityEngine;
using UnityEngine.EventSystems;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace Matterless.Floorcraft
{
    public class VehicleSelectorService : IVehicleSelectorService, ITickable
    {
        private enum ScrollState
        {
            Static,
            Dragging,
            LockToAnchor
        } 
        private readonly IVehicleSelectorView m_View;
        private readonly AudioUiService m_AudioUiService;
        private readonly ILocalisationService m_LocalisationService;
        private readonly IRendererService m_RendererService;
        private readonly IStoreService m_StoreService;
        private readonly Settings m_Settings;
        private readonly List<Transform> m_Vehicles;
        private readonly float m_Give = 0.25f;

        private Action<Vehicle> m_OnVehicleSelected;
        private float m_Scroll;
        private float m_Anchor;
        private float m_PreviewAnchor;
       
        private int m_CurrentPage = 0;
        private int m_CurrentPageTarget = 0;
        private Action m_OnSpectatorMode;
        private float m_PreviousScroll;
        private ScrollState m_State;
        private float m_PressPositionX;
        private float m_PositionX;

        private int totalPages => m_Vehicles == null ? 0 : m_Vehicles.Count;
        
        public VehicleSelectorService(
            AudioUiService audioUiService,
            ILocalisationService localisationService,
            IRendererService rendererService,
            IStoreService storeService,
            Settings settings)
        {
            m_View = VehicleSelectorView.Create("UIPrefabs/UIP_VehicleSelector").Init();
            m_View.onDrag += OnDrag;
            m_View.onEndDrag += OnEndDrag;
            m_View.onStoreButtonClicked += OnStoreButtonClicked;
            m_View.onNextButtonClicked += OnNextButtonClicked;
            m_View.onPreviousButtonClicked += OnPreviousButtonClicked;
            m_View.onSelectButtonClicked += OnSelectButtonClicked;
            
            localisationService.RegisterUnityUIComponents(m_View.gameObject);

            m_View.onSpectatorModeClicked += OnSpectatorModeClicked;
            m_AudioUiService = audioUiService;
            m_LocalisationService = localisationService;
            m_RendererService = rendererService;
            m_StoreService = storeService;
            m_StoreService.onPremiumUnlocked += OnPremiumUnlocked;
            m_Settings = settings;

            // instantiate vehicles
            m_Vehicles = new List<Transform>();

            foreach (var item in settings.vehicles)
                m_Vehicles.Add(GameObject.Instantiate(item.selectorPrefab, m_View.parentTransform).transform);

            m_Scroll = 0;
            m_PreviousScroll = 0;
            m_Anchor = 0;
            SetInitialVehicleRotation();
            UpdateView(m_CurrentPage, true);
            m_State = ScrollState.Static;
        }
        private void OnPreviousButtonClicked()
        {
            var newPage = Mathf.Clamp(m_Scroll - 1, -m_Give, totalPages - 1);
            m_PreviewAnchor = m_Anchor = Mathf.RoundToInt(newPage);
            m_State = ScrollState.LockToAnchor;
            m_AudioUiService.PlaySelectSound();
            m_AudioUiService.PlayScrollSound();
        }
        private void OnNextButtonClicked()
        {
            var newPage = Mathf.Clamp(m_Scroll + 1, 0, totalPages - 1 + m_Give);
            m_PreviewAnchor = m_Anchor = Mathf.RoundToInt(newPage);
            m_State = ScrollState.LockToAnchor;
            m_AudioUiService.PlaySelectSound();
            m_AudioUiService.PlayScrollSound();
        }

        private void OnStoreButtonClicked()
        {
            m_StoreService.Show();
            m_AudioUiService.PlaySelectSound();
        }

        private void OnPremiumUnlocked()
        {
            UpdateView(m_CurrentPage, true);
        }
        
        private void OnSpectatorModeClicked()
        {
            m_OnSpectatorMode.Invoke();
            m_RendererService.DisableDimm();
            m_AudioUiService.PlaySelectSound();
        }

        private void OnSelectButtonClicked()
        {
            m_AudioUiService.PlaySelectSound();
            m_OnVehicleSelected(m_Settings.vehicles[m_CurrentPage]);
            m_RendererService.DisableDimm();
            Hide();
        }

        private void OnEndDrag(PointerEventData data)
        {
            m_State = ScrollState.LockToAnchor;
            var newPage = Mathf.Clamp(m_Scroll, 0, totalPages - 1);
            m_PreviewAnchor = m_Anchor = Mathf.RoundToInt(newPage);
        }

        private void UpdateView(int page, bool forceUpdate = false)
        {
            if(page == m_CurrentPageTarget && !forceUpdate)
                return;
            
            m_CurrentPageTarget = page;
            var vehicle = m_Settings.vehicles[page];
            m_View.UpdateView(
                m_LocalisationService.Translate(vehicle.nameTag),
                m_StoreService.premiumEnabled && (vehicle.premium && !m_StoreService.isPremiumUnlocked));
        }
        
        private void OnDrag(PointerEventData data)
        {
            m_State = ScrollState.Dragging;
            m_PressPositionX = data.pressPosition.x;
            m_PositionX = data.position.x;

            var newPage = Mathf.Clamp(m_Scroll, 0, totalPages - 1);
            m_PreviewAnchor = Mathf.RoundToInt(newPage);
        }

        public void Show(Action<Vehicle> onVehicleSelected, Action onSpectatorMode)
        {
            m_State = ScrollState.Static;
            m_OnSpectatorMode = onSpectatorMode;
            m_OnVehicleSelected = onVehicleSelected;
            m_View.Show();
            m_RendererService.EnableDimm();
        }

        public void Show()
        {
            m_View.Show();
            m_RendererService.EnableDimm();
        }

        public void Hide()
        {
            m_View.Hide();
        }

        // NOTE: (Marko Tatalovic) Added for Blasters Only
        private void SetInitialVehicleRotation()
        {
            for (int i = 0; i < m_Vehicles.Count; i++)
            {
                m_Vehicles[i].localRotation = Quaternion.Euler(0, 200, 0);
            }
        }

        public void Tick(float deltaTime, float unscaledDeltaTime)
        {
            if (m_Settings == null || m_Vehicles == null)
                return;

            if (m_State == ScrollState.Static)
            {
                //Do nothing on purpose
            }
            else if (m_State == ScrollState.LockToAnchor)
            {
                m_Scroll = Mathf.MoveTowards(m_Scroll, m_Anchor, deltaTime * m_Settings.fadeSpeed);
            }
            else if (m_State == ScrollState.Dragging)
            {
                float delta = m_Settings.scrollMultiplier * (m_PressPositionX - m_PositionX) / m_Settings.pageWidth;
                m_Scroll = Mathf.Clamp(m_Anchor + delta, -m_Give, totalPages - 1 + m_Give);
                if (Mathf.RoundToInt(m_PreviousScroll) != Mathf.RoundToInt(m_Scroll)) m_AudioUiService.PlayScrollSound();
            }
            
            m_View.previousButtonEnabled = m_Scroll > 0;
            m_View.nextButtonEnabled = m_Scroll < totalPages - 1;
            m_CurrentPage = (int)m_PreviewAnchor;
            UpdateView(m_CurrentPage);
            m_PreviousScroll = m_Scroll;
            
            for (int i = 0; i < m_Vehicles.Count; i++)
            {
                m_Vehicles[i].localPosition = Vector3.right * (i - m_Scroll) * 1 * m_Settings.gridWidth;
                // NOTE: (Marko Tatalovic) for Blasters we added Bob movement;
                m_Vehicles[i].localPosition += Vector3.up * Mathf.Sin(Time.timeSinceLevelLoad) * 0.001f;
                /* NOTE: (Marko Tatalovic) for Blasters we removed rotation;
                m_Vehicles[i].localScale =
                    m_Vehicles[i].localEulerAngles = Vector3.up * (Time.timeSinceLevelLoad * 180 + 360 * Mathf.Sin(Mathf.PI * i / 5f));
                */
                float scale = Mathf.Clamp(1 - Mathf.Abs(i - m_Scroll), m_Settings.minScale, m_Settings.maxScale) * 0.01f;
                m_Vehicles[i].localScale = Vector3.one * scale;
                // visibility
                m_Vehicles[i].gameObject.SetActive(scale > m_Settings.minScale + m_Settings.visibilityThreshold);
            }
        }

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
            [SerializeField] private List<Vehicle> m_Vehicles;
            [SerializeField] private float m_SpeedMultiplier;
            [SerializeField] private float m_AutoScrollDecelerationMultiplier;
            [SerializeField] private float m_FadeSpeed;
            public float minScale => m_MinScale;
            public float speedMultiplier => m_SpeedMultiplier;
            public float maxScale => m_MaxScale;
            public float visibilityThreshold => m_VisibilityThreshold;
            public float gridWidth => m_GridWidth;
            public float scrollMultiplier => m_ScrollMultiplier;
            public float fadeTime => m_FadeTime;
            public List<Vehicle> vehicles => m_Vehicles;

            public float pageWidth => m_PageWidth;
            public float autoScrollDecelerationMultiplier => m_AutoScrollDecelerationMultiplier;
            public float fadeSpeed
            {
                get {
                    return m_FadeSpeed;
                }
                set {
                    m_FadeSpeed = value;
                }
            }
            public Vehicle GetAsset(uint id) => m_Vehicles.Find(x => x.id == id);
        }
    }
}