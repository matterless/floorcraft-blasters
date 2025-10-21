using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Matterless.Floorcraft
{
    public class WalletUiView : UIView<WalletUiView>
    {
        public event Action onConnectWalletButtonClicked;
        public event Action onOpenWalletButtonClicked;
        public event Action onHideWalletButtonClicked;
        public event Action onDisconnectWalletButtonClicked;

        [SerializeField] private Button m_ConnectWalletButton;
        [SerializeField] private Button m_DisconnectWalletButton;
        [SerializeField] private Button m_OpenWalletButton;
        [SerializeField] private Button m_HideWalletButton;
        [SerializeField] private TMP_Text m_OnConnectedAddressText;
        [SerializeField] private Transform m_WalletInfoContainer;
        [SerializeField] private GameObject m_WalletInfoBackground;
        [SerializeField] private Canvas m_WalletCanvas;
        [SerializeField] private TMP_Text m_WalletAddressText;
        [SerializeField] private TMP_Text m_EthBalanceText;
        [SerializeField] private TMP_Text m_AukiBalanceText;

        public override WalletUiView Init()
        {
            AddListeners();
            return this;
        }

        private void AddListeners()
        {
            m_ConnectWalletButton.onClick.AddListener(() => onConnectWalletButtonClicked?.Invoke());
            m_OpenWalletButton.onClick.AddListener(() => onOpenWalletButtonClicked?.Invoke());
            m_HideWalletButton.onClick.AddListener(() => onHideWalletButtonClicked?.Invoke());
            m_DisconnectWalletButton.onClick.AddListener(() => onDisconnectWalletButtonClicked?.Invoke());
        }

        public void SetConnectButtonVisibility(bool isVisible)
        {
            m_ConnectWalletButton.gameObject.SetActive(isVisible);
        }

        public void SetOpenWalletButtonVisibility(bool isVisible)
        {
            m_OpenWalletButton.gameObject.SetActive(isVisible);
        }

        public void SetConnectButtonInteractability(bool isInteractable)
        {
            m_ConnectWalletButton.interactable = isInteractable;
        }

        public void SetOpenWalletButtonInteractability(bool isInteractable)
        {
            m_OpenWalletButton.interactable = isInteractable;
        }

        public string GetWalletAddressText(string walletAddress)
        {
            return FormatWalletAddress(walletAddress);
        }

        private string FormatWalletAddress(string address)
        {
            if (string.IsNullOrEmpty(address) || address.Length < 10)
                return address;

            // Format as 0x1234...5678 (4 characters after 0x, then last 4 characters)
            return $"{address.Substring(0, 6)}...{address.Substring(address.Length - 4)}";
        }

        public void ShowWalletInfo()
        {
            m_WalletCanvas.sortingOrder = 99;
            m_WalletInfoContainer.gameObject.SetActive(true);
            m_WalletInfoBackground.SetActive(true);
            m_OpenWalletButton.gameObject.SetActive(false);
        }

        public void HideWalletInfo()
        {
            m_WalletInfoContainer.gameObject.SetActive(false);
            m_WalletInfoBackground.SetActive(false);
            // Set canvas sort order back to 0 when wallet info is hidden
            m_WalletCanvas.sortingOrder = 0;
            // Re-enable Open Wallet button when info is hidden
            m_OpenWalletButton.gameObject.SetActive(true);
        }

        public void ResetCanvasSortingOrder()
        {
            m_WalletCanvas.sortingOrder = 0;
        }

        public void SetConnectedAddressText(string text)
        {
            m_OnConnectedAddressText.text = text;
        }

        public void SetWalletAddress(string text)
        {
            m_WalletAddressText.text = text;
        }

        public void SetEthBalanceText(string text)
        {
            m_EthBalanceText.text = text;
        }

        public void SetAukiBalanceText(string text)
        {
            m_AukiBalanceText.text = text;
        }
    }
}