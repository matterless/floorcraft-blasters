using System;
using UnityEngine;
using Button = Matterless.Module.UI.Button;
using TMPro;
namespace Matterless.Floorcraft
{
    public class MayhemUiView : UIView<MayhemUiView>
    {
        #region Inspector

        [SerializeField] private TMP_Text m_CountdownText;
        [SerializeField] private TMP_Text m_WaveNumberLabel;
        [SerializeField] private TMP_Text m_WaveNumberText;
        [SerializeField] private Button m_StartButton;
        #endregion

        public event Action onStartButtonClicked;

        public override MayhemUiView Init()
        {
            AddListeners();
            return this;
        }

        private void AddListeners()
        {
            m_StartButton.onClick?.RemoveAllListeners();
            m_StartButton.onClick.AddListener(() => Debug.Log("Start button clicked"));
            m_StartButton.onClick.AddListener(() => onStartButtonClicked?.Invoke());;
        }

        // We can use this if we want to show how many seconds left before a new wave starts
        public void UpdateCountdown(int seconds)
        {
            m_CountdownText.text = seconds.ToString();
        }
        
        public void UpdateWaveNumber(int waveNumber)
        {
            m_WaveNumberText.text = waveNumber.ToString();
        }
        
        public void ShowLabels()
        {
            m_WaveNumberLabel.gameObject.SetActive(true);
            m_WaveNumberText.gameObject.SetActive(true);
        }
        
        public void HideLabels()
        {
            m_WaveNumberLabel.gameObject.SetActive(false);
            m_WaveNumberText.gameObject.SetActive(false);
        }

        public void ShowButton()
        {
            m_StartButton.gameObject.SetActive(true);
        }
        
        public void HideButton()
        {
            m_StartButton.gameObject.SetActive(false);
        }
    }
}