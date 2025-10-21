using System;
using System.Linq;
using System.Threading.Tasks;
using Reown.AppKit.Unity;
using UnityEngine;
using Matterless.Inject;
using Matterless.UTools;
using Nethereum.Web3;
using System.Numerics;

namespace Matterless.Floorcraft
{
    public class WalletService
    {
        public event Action onWalletConnected;
        public event Action onWalletDisconnected;
        public event Action<bool> onModalStateChanged;

        private readonly WalletSettings m_WalletSettings;
        private readonly ChainSettings m_ChainSettings;
        private GameObject m_AppKitPrefab;
        private bool m_IsInitialized = false;


        public WalletService(WalletSettings walletSettings, ChainSettings chainSettings)
        {
            m_WalletSettings = walletSettings;
            m_ChainSettings = chainSettings;

            InstantiateAppKitPrefab();
            InitializeWallet();
        }

        public async void Disconnect()
        {
            await AppKit.DisconnectAsync();
        }

        public string GetConnectedAddress()
        {
            if (AppKit.IsAccountConnected)
            {
                return AppKit.Account.Address;
            }
            return string.Empty;
        }

        private async Task InitializeWallet()
        {
            var config = new AppKitConfig(
                projectId: m_WalletSettings.projectId,
                new Metadata(
                    name: m_WalletSettings.projectName,
                    description: m_WalletSettings.projectDescription,
                    url: m_WalletSettings.projectUrl,
                    iconUrl: m_WalletSettings.projectIconUrl,
                    new RedirectData
                    {
                        Native = "unity-floorcraft-app://"
                    }
                )
            )
            {
                supportedChains = new[]
                {
                    ChainConstants.Chains.Base       
                }
            };

            await AppKit.InitializeAsync(config);
            await OnAppKitInitialized();
        }


        private async Task OnAppKitInitialized()
        {
            if (AppKit.IsInitialized)
            {
                AppKit.AccountConnected += OnAccountConnected;
                AppKit.AccountDisconnected += OnAccountDisconnected;
                AppKit.ModalController.OpenStateChanged += OnModalStateChanged;
            }
        }

        public void Connect()
        {
            if (!AppKit.IsInitialized)
            {
                Debug.LogError("AppKit not initialized");
                return;
            }

            AppKit.OpenModal(ViewType.Connect);
        }

        private void OnAccountConnected(object sender, Connector.AccountConnectedEventArgs e)
        {
            string address = e.Account.Address;

            onWalletConnected?.Invoke();
        }

        private void InstantiateAppKitPrefab()
        {
            GameObject appKitPrefab = Resources.Load<GameObject>("Wallet/Reown AppKit");

            if (appKitPrefab == null)
            {
                Debug.LogError("Reown AppKit prefab not found at Resources/Wallet/Reown AppKit.prefab");
                return;
            }

            m_AppKitPrefab = UnityEngine.Object.Instantiate(appKitPrefab);
        }

        private void OnAccountDisconnected(object sender, Connector.AccountDisconnectedEventArgs e)
        {
            onWalletDisconnected?.Invoke();
        }

        private void OnModalStateChanged(object sender, ModalOpenStateChangedEventArgs e)
        {
            onModalStateChanged?.Invoke(e.IsOpen);
        }

        public async Task<string> GetWalletNativeBalanceAsync()
        {
            if (!AppKit.IsAccountConnected)
            {
                Debug.LogWarning("Wallet not connected!");
                return "0";
            }

            string address = AppKit.Account.Address;
            BigInteger balanceWei = await AppKit.Evm.GetBalanceAsync(address);
            decimal balanceEth = Web3.Convert.FromWei(balanceWei);

            return balanceEth.ToString("F4");
        }
    }
}