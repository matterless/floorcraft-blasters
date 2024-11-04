using Matterless.Localisation;
using System;
using Matterless.Floorcraft.UI;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Matterless.Floorcraft
{
    public class SpeederHUD : UIView<SpeederHUD>
    {
        public event Action onBoostButtonClicked;
        public event Action onBoostButtonUp;
        public event Action onBoostButtonDown;
        public event Action onBrakeButtonClicked;
        public event Action onRespawnButtonClicked;

        [SerializeField] private PointerButton m_BoostButton;
        [SerializeField] private Button m_BrakeButton;
        [SerializeField] private Button m_RespawnButton;
        [SerializeField] private Text m_leaderboard;
        [SerializeField] private ChargingUi m_ChargingUi;

        private bool m_Destroyed = false;
        
        public override SpeederHUD Init(ILocalisationService localisationService)
        {
            m_ChargingUi.Init(localisationService, "LASER_CHARGED_LABEL", "LASER_CHARGING_LABEL");
            
            m_BoostButton.onClick.AddListener(OnBoostButtonClicked);
            m_BoostButton.PointerUp = ()=> onBoostButtonUp?.Invoke();
            m_BoostButton.PointerExit = ()=> onBoostButtonUp?.Invoke();
            m_BoostButton.PointerDown = ()=> onBoostButtonDown?.Invoke();
            m_BrakeButton.onClick.AddListener(() => onBrakeButtonClicked?.Invoke());
            m_RespawnButton.onClick.AddListener(OnRespawnButtonClicked);
            return this;
        }

        private void OnRespawnButtonClicked() => onRespawnButtonClicked?.Invoke();

        private void OnBoostButtonClicked()
        {
            onBoostButtonClicked?.Invoke();
        }

        private void OnDestroy()
        {
            m_Destroyed = true;
        }

        public void Show()
        {
            this.gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (m_Destroyed)
                return;

            gameObject.SetActive(false);
        }

        public void Totaled()
        {
            m_BoostButton.interactable = false;
            m_BrakeButton.interactable = false;
            // NOTE : (Marko) This was causing the charging bar not to be visible when coming back from spawn screen
            //m_CanvasGroup.alpha = 0;
            //m_RespawnButton.transform.localScale = Vector3.one; //todo: maybe comment this out
            //m_ChargingUi.Hide();
        }

        public void Respawn()
        {
            this.gameObject.SetActive(true);
            m_BoostButton.interactable = true;
            m_BrakeButton.interactable = true;
            // NOTE : (Marko) This was causing the charging bar not to be visible when coming back from spawn screen
            //m_CanvasGroup.alpha = 1;
            m_RespawnButton.transform.localScale = Vector3.zero;
            // NOTE : (Marko) Too much dead code. This doesn't need to be called as we disable/enable the parent game object. Also we don't ever call Show when we get from Vertical spawn screen
            //m_ChargingUi.Show();
        }

        public void SetLeaderboardText(string text)
        {
            // TODO : leaderboard?
            // m_leaderboard.text = text;
        }

        public void UpdateCharging(float value)
        {
            m_ChargingUi.UpdateUi(value);
        }
        
        public void SetLocalisationTags(string charged, string charging)
        {
            m_ChargingUi.SetLocalisationTags(charged, charging);
        }

        // NOTE (Marko) : Horizontal view is a separate object we don't need to turn off the mask ever
        // public void SetMaskActivation(bool maskActivation)
        // {
        //     m_ChargingUi.SetMaskActive(maskActivation);
        // }
    }

}

