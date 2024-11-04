using Matterless.Floorcraft.UI;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Matterless.Floorcraft
{
    public class QrCodeUiView : UIView<QrCodeUiView>
    {
        public event Action onBackButtonClicked;
        public event Action<bool> onScanningButtonPressed;
        
        #region Inspector
        
        [SerializeField] private Button m_BackButton;
        [SerializeField] private PointerButton m_ScanningButton;
        
        #endregion

        public override QrCodeUiView Init()
        {
            AddListeners();
            return this;
        }

        private void AddListeners()
        {
            m_BackButton.onClick.AddListener(() => onBackButtonClicked?.Invoke());
            m_ScanningButton.PointerDown += ScanningButtonPressed;
            m_ScanningButton.PointerUp += ScanningButtonRelease;
            m_ScanningButton.PointerExit += ScanningButtonRelease;
        }

        private void ScanningButtonPressed()
        {
            onScanningButtonPressed?.Invoke(true);
            m_ScanningButton.transform.localScale = Vector3.one * 2f;
        }

        private void ScanningButtonRelease()
        {
            onScanningButtonPressed?.Invoke(false);
            m_ScanningButton.transform.localScale = Vector3.one;
        }
    }
}