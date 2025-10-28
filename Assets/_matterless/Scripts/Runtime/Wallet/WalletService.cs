using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Reown.AppKit.Unity;
using UnityEngine;
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
        public event Action onNFTsLoaded;

        private readonly WalletSettings m_WalletSettings;
        private readonly ChainSettings m_ChainSettings;
        private GameObject m_AppKitPrefab;
        private bool m_IsInitialized = false;
        
        // NFT ownership cache
        private readonly Dictionary<string, bool> m_NFTOwnershipCache = new Dictionary<string, bool>();
        private bool m_NFTCacheInitialized = false;
        
        // Public accessors
        public ChainSettings chainSettings => m_ChainSettings;

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

        private async void OnAccountConnected(object sender, Connector.AccountConnectedEventArgs e)
        {
            string address = e.Account.Address;

            onWalletConnected?.Invoke();
            
            // Clear cache and reinitialize for new wallet
            m_NFTOwnershipCache.Clear();
            m_NFTCacheInitialized = false;
            await InitializeNFTCache();
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
            // Clear NFT cache when wallet disconnects
            m_NFTOwnershipCache.Clear();
            m_NFTCacheInitialized = false;
            
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
            catch (Exception ex)
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

        private async Task InitializeNFTCache()
        {
            if (m_NFTCacheInitialized || !AppKit.IsAccountConnected)
                return;
                
            try
            {
                // Get vehicle token IDs from the vehicle selector settings
                var vehicleTokenIds = GetVehicleTokenIds();
                
                var nftService = new NFTService(m_ChainSettings.nftContractAddress, m_ChainSettings.rpcUrl);
                
                foreach (string tokenId in vehicleTokenIds)
                {
                    try
                    {
                        bool ownsToken = await nftService.OwnsToken(AppKit.Account.Address, tokenId);
                        m_NFTOwnershipCache[tokenId] = ownsToken;
                    }
                    catch (Exception ex)
                    {
                        m_NFTOwnershipCache[tokenId] = false;
                    }
                }
                
                m_NFTCacheInitialized = true;
                
                // Notify that NFT cache is ready
                onNFTsLoaded?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize NFT cache: {ex.Message}");
            }
        }
        
        private List<string> GetVehicleTokenIds()
        {
            var tokenIds = new List<string>();
            
            try
            {
                // Try to get vehicle settings from the app configs
                var appConfigs = Resources.Load<AppConfigs>("AppConfigs");
                if (appConfigs != null && appConfigs.vehicleSelectorSettings != null)
                {
                    foreach (var vehicle in appConfigs.vehicleSelectorSettings.vehicles)
                    {
                        if (vehicle.requiresNFT)
                        {
                            // Use the NFT token ID field, not the vehicle ID
                            string tokenId = vehicle.nftTokenId.ToString();
                            if (!tokenIds.Contains(tokenId))
                            {
                                tokenIds.Add(tokenId);
                            }
                        }
                    }
                }
                
                // Fallback: if no vehicles found, use default range
                if (tokenIds.Count == 0)
                {
                    for (int i = 1; i <= 10; i++)
                    {
                        tokenIds.Add(i.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                // Fallback to default range
                for (int i = 1; i <= 10; i++)
                {
                    tokenIds.Add(i.ToString());
                }
            }
            
            return tokenIds;
        }
        
        public bool IsNFTOwned(string tokenId)
        {
            if (!m_NFTCacheInitialized)
            {
                return false;
            }
            
            return m_NFTOwnershipCache.TryGetValue(tokenId, out bool owned) && owned;
        }
        
        /// <summary>
        /// Get list of token IDs that are owned by the connected wallet
        /// </summary>
        public List<string> GetOwnedTokenIds()
        {
            var ownedTokens = new List<string>();
            foreach (var kvp in m_NFTOwnershipCache)
            {
                if (kvp.Value)
                {
                    ownedTokens.Add(kvp.Key);
                }
            }
            return ownedTokens;
        }
        
        /// <summary>
        /// Get count of NFTs owned by the connected wallet
        /// </summary>
        public int GetOwnedNFTCount()
        {
            return GetOwnedTokenIds().Count;
        }
        
        public async Task<bool> CheckNFTOwnership(string tokenId)
        {
            if (!AppKit.IsAccountConnected)
            {
                Debug.LogWarning("Wallet not connected!");
                return false;
            }

            try
            {
                // Create NFTService instance for ERC-1155
                var nftService = new NFTService(m_ChainSettings.nftContractAddress, m_ChainSettings.rpcUrl);
                
                
                // Check if user owns the token
                bool ownsToken = await nftService.OwnsToken(AppKit.Account.Address, tokenId);
                
                return ownsToken;
            }
            catch (System.Exception ex)
            {
                return false;
            }
        }
    }
}