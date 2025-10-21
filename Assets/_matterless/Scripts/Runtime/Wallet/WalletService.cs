using System;
using System.Linq;
using System.Threading.Tasks;
using Reown.AppKit.Unity;
using UnityEngine;
using Matterless.Inject;
using Matterless.UTools;
using Nethereum.Web3;
using System.Numerics;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace Matterless.Floorcraft
{
    [System.Serializable]
    public class RpcRequest
    {
        public string jsonrpc = "2.0";
        public string method;
        public object[] @params;
        public int id;
    }

    [System.Serializable]
    public class RpcResponse
    {
        public string jsonrpc;
        public string result;
        public RpcError error;
        public int id;
    }

    [System.Serializable]
    public class RpcError
    {
        public int code;
        public string message;
    }

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
            Debug.Log($"Tomicz: OnAccountConnected called with address: {address}");

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

        public async Task<string> GetAukiBalanceAsync()
        {
            if (!AppKit.IsAccountConnected) 
            { 
                Debug.LogWarning("Wallet not connected!"); 
                return "0"; 
            }

            try
            {
                // AUKI token contract address on Base
                string aukiContractAddress = "0xf9569cfb8fd265e91aa478d86ae8c78b8af55df4";
                // Get AUKI balance using eth_call
                string balanceHex = await GetTokenBalanceDirectRpcAsync(aukiContractAddress, AppKit.Account.Address);
                
                if (!string.IsNullOrEmpty(balanceHex) && balanceHex != "0x0")
                {
                    // Convert hex to BigInteger using manual parsing to ensure unsigned
                    var hexString = balanceHex.Substring(2); // Remove "0x"
                    var balanceWei = ParseHexToUnsignedBigInteger(hexString);

                    // Convert Wei to AUKI (divide by 10^18)
                    var balanceAuki = (double)balanceWei / Math.Pow(10, 18);
                    
                    // Format to 4 decimal places without rounding
                    return balanceAuki.ToString("0.####");
                }
                
                return "0.0000";
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error fetching AUKI balance: {ex.Message}");
                return "Error";
            }
        }

        public async Task<string> GetWalletNativeBalanceAsync()
        {
            if (!AppKit.IsAccountConnected)
            {
                Debug.LogWarning("Wallet not connected!");
                return "0";
            }
            
            try
            {
                // Try direct HTTP RPC call first
                string balanceHex = await GetBalanceDirectRpcAsync(AppKit.Account.Address);
                
                if (!string.IsNullOrEmpty(balanceHex) && balanceHex != "0x0")
                {
                    // Convert hex to BigInteger using manual parsing to ensure unsigned
                    var hexString = balanceHex.Substring(2); // Remove "0x"
                    var balanceWei = ParseHexToUnsignedBigInteger(hexString);

                    // Convert Wei to ETH (divide by 10^18)
                    var balanceEth = (double)balanceWei / Math.Pow(10, 18);
                    
                    // Format to 4 decimal places without rounding
                    return balanceEth.ToString("0.####");
                }
                
                return "0.0000";
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error fetching balance: {ex.Message}");
                return "Error";
            }
        }

        /// <summary>
        /// Converts a hexadecimal string to BigInteger using manual parsing to ensure unsigned interpretation.
        /// This avoids issues with BigInteger.Parse() treating hex values as signed integers.
        /// </summary>
        /// <param name="hexString">Hex string without "0x" prefix (e.g., "e1ab9886571c7")</param>
        /// <returns>BigInteger representing the unsigned hex value</returns>
        private BigInteger ParseHexToUnsignedBigInteger(string hexString)
        {
            var result = new BigInteger(0);
            
            for (int i = 0; i < hexString.Length; i++)
            {
                result *= 16; // Shift left by 4 bits (multiply by 16)
                char c = hexString[i];
                
                // Convert hex character to decimal digit
                int digit = c >= '0' && c <= '9' ? c - '0' :           // 0-9
                           c >= 'A' && c <= 'F' ? c - 'A' + 10 :       // A-F
                           c >= 'a' && c <= 'f' ? c - 'a' + 10 : 0;    // a-f
                
                result += digit;
            }
            
            return result;
        }

        private async Task<string> GetTokenBalanceDirectRpcAsync(string contractAddress, string walletAddress)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    // ERC-20 balanceOf function signature: balanceOf(address)
                    // Function selector: 0x70a08231
                    string functionSelector = "0x70a08231";
                    
                    // Pad wallet address to 32 bytes (64 hex characters)
                    string paddedAddress = walletAddress.Substring(2).PadLeft(64, '0');
                    string data = functionSelector + paddedAddress;
                    
                    var rpcRequest = new RpcRequest
                    {
                        method = "eth_call",
                        @params = new object[] 
                        { 
                            new { to = contractAddress, data = data }, 
                            "latest" 
                        },
                        id = 2
                    };

                    string jsonRequest = JsonConvert.SerializeObject(rpcRequest);

                    var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
                    var response = await httpClient.PostAsync(m_ChainSettings.rpcUrl, content);
                    
                    string responseContent = await response.Content.ReadAsStringAsync();

                    var rpcResponse = JsonConvert.DeserializeObject<RpcResponse>(responseContent);
                    
                    if (rpcResponse.error != null)
                    {
                        Debug.LogError($"AUKI RPC Error: {rpcResponse.error.code} - {rpcResponse.error.message}");
                        return null;
                    }
                    
                    return rpcResponse.result;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"AUKI Direct RPC call failed: {ex.Message}");
                return null;
            }
        }

        private async Task<string> GetBalanceDirectRpcAsync(string address)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var rpcRequest = new RpcRequest
                    {
                        method = "eth_getBalance",
                        @params = new object[] { address, "latest" },
                        id = 1
                    };

                    string jsonRequest = JsonConvert.SerializeObject(rpcRequest);

                    var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
                    var response = await httpClient.PostAsync(m_ChainSettings.rpcUrl, content);
                    
                    string responseContent = await response.Content.ReadAsStringAsync();

                    var rpcResponse = JsonConvert.DeserializeObject<RpcResponse>(responseContent);
                    
                    if (rpcResponse.error != null)
                    {
                        Debug.LogError($"RPC Error: {rpcResponse.error.code} - {rpcResponse.error.message}");
                        return null;
                    }
                    
                    return rpcResponse.result;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Direct RPC call failed: {ex.Message}");
                return null;
            }
        }
    }
}