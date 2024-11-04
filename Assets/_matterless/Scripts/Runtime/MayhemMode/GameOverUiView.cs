using Matterless.Floorcraft.UI;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Matterless.Floorcraft
{
    public class GameOverUiView : UIView<GameOverUiView>
    {
        public event Action onBackButtonClicked;

        #region Inspector
        
        [SerializeField] private AnimatedButton m_BackButton;
        [SerializeField] private TextMeshProUGUI m_EndGameHeaderLabel;
        [SerializeField] private TextMeshProUGUI m_EndGameMessageLabel;
        [SerializeField] private GameObject m_InfoBoxReward;
        [SerializeField] private GameObject m_InfoBoxTryAgain;
        [SerializeField] private TextMeshProUGUI m_CouponCode;
        
        #endregion

        public override GameOverUiView Init()
        {
            AddListeners();
            return this;
        }

        private void AddListeners()
        {
            m_BackButton.onClick.AddListener(OnBackButtonClicked);
        }

        private void OnBackButtonClicked()
        {
            Hide();
            onBackButtonClicked?.Invoke();
        }

        //public void Show(string endGameMessageText, int latestWaveNumber, Action onBackButtonPressed)
        public void Show(int latestWaveNumber, Action onBackButtonPressed)
        {
            int waves = latestWaveNumber - 1;
            if (waves == 1)
            {
                m_EndGameMessageLabel.text = $"You cleared 1 wave";
            }
            else
            {
                m_EndGameMessageLabel.text = $"You cleared {waves} waves";
            }

            bool win = waves >= 5;
            if(win)
            {
                m_EndGameHeaderLabel.text = "Congratulations!";
            }
            else
            {
                m_EndGameHeaderLabel.text = "Try Again!";
            }

#if !MATTERLESS_APPSTORE
            m_InfoBoxReward.SetActive(win);
            if (win)
            {
                // Random code for now, just proof of concept. Format: 1234-5678
                m_CouponCode.text = $"{UnityEngine.Random.Range(1000, 9999)}-{UnityEngine.Random.Range(1000, 9999)}";
            }
            m_InfoBoxTryAgain.SetActive(!win);
#endif
            
            onBackButtonClicked = onBackButtonPressed;
            Show();
        }
    }
}