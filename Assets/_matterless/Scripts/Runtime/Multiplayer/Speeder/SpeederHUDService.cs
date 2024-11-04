using System;
using Matterless.Inject;
using Matterless.Localisation;
using UnityEngine;
using UnityEngine.iOS;

namespace Matterless.Floorcraft
{
    public class SpeederHUDService : ITickable
    {
        public event Action onRespawnButtonClicked;

        private SpeederHUD m_View;
        private readonly SpeederHUD m_VerticalView;
        private readonly SpeederHUD m_HorizontalView;
        private readonly EquipmentService m_EquipmentService;
        private readonly IAnalyticsService m_AnalyticsService;

        private readonly CooldownService m_CooldownService;

        private Action<float> m_OnUpdate;
        private bool m_IsVisible = false;
        private bool m_IsLandscape = false;

        public uint serverEntityId
        {
            get => m_ServerEntityId;
            set => m_ServerEntityId = value;
        }
        public bool brakeInput
        {
            get
            {
                try
                {
                    return m_BrakeInput;
                }
                finally
                {
                    m_BrakeInput = false;
                }
            }
        }

        public bool holdScreenInput => m_HoldScreenInput;

        public bool tapScreenInput
        {
            get
            {
                try
                {
                    return m_TapScreenInput;
                }
                finally
                {
                    m_TapScreenInput = false;
                }
            }
        }
        private bool m_BrakeInput;
        private bool m_TapScreenInput;
        private bool m_HoldScreenInput;
        private uint m_ServerEntityId;
        private EquipmentState m_LastEquipmentComponent;

        public SpeederHUDService(
            ILocalisationService localisationService,
            EquipmentService equipmentService,
            IAnalyticsService analyticsService,
            CooldownService cooldownService
        )
        {
            m_EquipmentService = equipmentService;
            m_AnalyticsService = analyticsService;
            m_CooldownService = cooldownService;
            
            m_VerticalView = SpeederHUD.Create("UIPrefabs/UIP_SpeederHUD").Init(localisationService);
            localisationService.RegisterUnityUIComponents(m_VerticalView.gameObject);
            m_VerticalView.gameObject.SetActive(false);
            m_VerticalView.onBoostButtonUp += OnBoostButtonUp;
            m_VerticalView.onBoostButtonDown += OnBoostButtonDown;
            m_VerticalView.onBrakeButtonClicked += OnBrakeButtonClicked;
            m_VerticalView.onRespawnButtonClicked += OnRespawnButtonClicked;
            
            m_HorizontalView = SpeederHUD.Create("UIPrefabs/UIP_SpeederHUD_Horizontal").Init(localisationService);
            localisationService.RegisterUnityUIComponents(m_HorizontalView.gameObject);
            m_HorizontalView.gameObject.SetActive(false);
            m_HorizontalView.onBoostButtonUp += OnBoostButtonUp;
            m_HorizontalView.onBoostButtonDown += OnBoostButtonDown;
            m_HorizontalView.onBrakeButtonClicked += OnBrakeButtonClicked;
            m_HorizontalView.onRespawnButtonClicked += OnRespawnButtonClicked;
            
            m_View = m_VerticalView;
        }

        private void OnRespawnButtonClicked()
        {
            onRespawnButtonClicked?.Invoke();
        }

        private void OnBoostButtonClicked()
        {
            Debug.Log($"Pressed screen cooldown is {m_CooldownService.inCooldown}");
            if (m_CooldownService.inCooldown)
                return;


            m_TapScreenInput = true;
        }

        private void OnBoostButtonDown()
        {
            if (m_CooldownService.inCooldown)
                return;
            
            m_HoldScreenInput = true;
        }
        
        private void OnBoostButtonUp()
        {
            m_HoldScreenInput = false;
        }

        private void OnBrakeButtonClicked() => m_BrakeInput = true;

        public void SetLeaderBoard(string text)
        {
            m_View.SetLeaderboardText(text);
        }

        public void Show()
        {
            ShowCurrentViewInternal();
        }

        public void Hide() => m_View.Hide();

        public void Totaled()
        {
            m_View.Totaled();
            m_HoldScreenInput = false;
            m_TapScreenInput = false;
        }

        // public void Scored() => m_View.Scored();

        public void Respawn() => m_View.Respawn();

        public void Tick(float deltaTime, float unscaledDeltaTime)
        {
            if (m_View.IsVisible())
            {
                m_View.UpdateCharging(Mathf.InverseLerp(m_CooldownService.duration, 0,
                    m_CooldownService.cooldownTimer));
                if (!m_EquipmentService.TryGetComponentModel(serverEntityId, out var equipmentComponent))
                    return;

                if (m_LastEquipmentComponent != equipmentComponent.model.state)
                {
                    if (equipmentComponent.model.state == EquipmentState.None)
                    {
                        m_View.SetLocalisationTags("HONK_CHARGED_LABEL", "HONK_CHARGING_LABEL");
                    }
                    else if (equipmentComponent.model.state == EquipmentState.Clone)
                    {
                        m_View.SetLocalisationTags("CLONE_CHARGED_LABEL", "CLONE_CHARGING_LABEL");
                    }
                    else if (equipmentComponent.model.state == EquipmentState.Dash)
                    {
                        m_View.SetLocalisationTags("BOOST_CHARGED_LABEL", "BOOST_CHARGING_LABEL");
                    }
                    else if (equipmentComponent.model.state == EquipmentState.Flamethrower)
                    {
                        m_View.SetLocalisationTags("FLAME_CHARGED_LABEL", "FLAME_CHARGING_LABEL");
                    }
                    else if (equipmentComponent.model.state == EquipmentState.Laser)
                    {
                        m_View.SetLocalisationTags("LASER_CHARGED_LABEL", "LASER_CHARGING_LABEL");
                    }
                    else if (equipmentComponent.model.state == EquipmentState.Magnet ||
                             equipmentComponent.model.state == EquipmentState.MagnetAndWreckingBall)
                    {
                        m_View.SetLocalisationTags("MAGNET_CHARGED_LABEL", "MAGNET_CHARGING_LABEL");
                    }
                    else if (equipmentComponent.model.state == EquipmentState.ProximityMines)
                    {
                        m_View.SetLocalisationTags("MINE_CHARGED_LABEL", "MINE_CHARGING_LABEL");
                    }
                }

                m_LastEquipmentComponent = equipmentComponent.model.state;


                //m_View.UpdateCharging(ChargingUi.CHARGED_STATE);
                //m_View.UpdateCharging(ChargingUi.CHARGING_STATE);
            }
        }

        public void SetOrientation(ScreenOrientation orientation)
        {
            m_IsLandscape = orientation == ScreenOrientation.LandscapeLeft || orientation == ScreenOrientation.LandscapeRight;
            
            // cache if current view is visible
            bool isVisible = m_View.IsVisible();
            // hide current view
            if (isVisible)
            {
                m_View.Hide();
            }

            // change view based on screen orientation
            if(m_IsLandscape)
            {
                m_View = m_HorizontalView;
            }
            else
            {
                m_View = m_VerticalView;
            }

            // if was visible make the current one visible
            if (isVisible)
                ShowCurrentViewInternal();

        }

        private void ShowCurrentViewInternal()
        {
            m_View.Show();
            // apply orrientation to current view
            // NOTE (Marko) : Horizontal view is a separate object we don't need to turn off the mask ever
            //m_View.SetMaskActivation(m_IsLandscape);
        }

        private void ResetView()
        {
            m_View.Hide();
            m_View = m_VerticalView;
        }
    }
}

