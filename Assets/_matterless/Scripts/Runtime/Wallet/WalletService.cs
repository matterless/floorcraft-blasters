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
        
        // ERC-1155 (Active): Cache whether user owns the specific Auki Domain NFT token
        private bool m_OwnsErc1155NFT = false;
        
        // ERC-721 (Secondary): Cache whether user owns ANY Floorcraft NFT from the collection
        private bool m_OwnsErc721NFT = false;
        
        // Track owned token IDs from each standard
        private readonly List<string> m_OwnedErc1155TokenIds = new List<string>();
        private readonly List<string> m_OwnedErc721TokenIds = new List<string>();
        
        private const string NFT_CACHE_PREFS_KEY = "NFTCacheData";
        private string m_CachedWalletAddress = string.Empty;
        
        [System.Serializable]
        private class NFTCacheData
        {
            public string walletAddress;
            public bool ownsErc1155NFT;
            public bool ownsErc721NFT;
            public List<string> ownedErc1155TokenIds = new List<string>();
            public List<string> ownedErc721TokenIds = new List<string>();
        }
        
        // Public accessors
        public ChainSettings chainSettings => m_ChainSettings;
        public bool hasCachedSession => m_NFTCacheInitialized && !string.IsNullOrEmpty(m_CachedWalletAddress);
        public string cachedWalletAddress => m_CachedWalletAddress;

        public WalletService(WalletSettings walletSettings, ChainSettings chainSettings)
        {
            m_WalletSettings = walletSettings;
            m_ChainSettings = chainSettings;

            RestoreNFTCacheOnStartup();
            
            InstantiateAppKitPrefab();
            InitializeWallet();
        }

        public async void Disconnect()
        {
            if (AppKit.IsAccountConnected)
            {
                await AppKit.DisconnectAsync();
                return;
            }
            
            m_NFTOwnershipCache.Clear();
            m_NFTCacheInitialized = false;
            m_OwnsErc1155NFT = false;
            m_OwnsErc721NFT = false;
            m_OwnedErc1155TokenIds.Clear();
            m_OwnedErc721TokenIds.Clear();
            m_CachedWalletAddress = string.Empty;
            
            ClearNFTCacheFromDisk();
            
            onWalletDisconnected?.Invoke();
        }

        public string GetConnectedAddress()
        {
            if (AppKit.IsAccountConnected)
            {
                return AppKit.Account.Address;
            }
            return m_CachedWalletAddress;
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
                
                if (AppKit.IsAccountConnected)
                {
                    await HandleRestoredSession();
                }
            }
        }
        
        private async Task HandleRestoredSession()
        {
            string walletAddress = AppKit.Account.Address;
            Debug.Log($"[WalletService] Restored wallet session for {walletAddress}");
            
            onWalletConnected?.Invoke();
            
            if (TryLoadNFTCacheFromDisk(walletAddress))
            {
                Debug.Log("[WalletService] NFT cache restored from disk for returning session");
                onNFTsLoaded?.Invoke();
                _ = RefreshNFTCacheInBackground();
                return;
            }
            
            await InitializeNFTCache();
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
            m_NFTOwnershipCache.Clear();
            m_NFTCacheInitialized = false;
            m_OwnsErc1155NFT = false;
            m_OwnsErc721NFT = false;
            m_OwnedErc1155TokenIds.Clear();
            m_OwnedErc721TokenIds.Clear();
            
            onWalletConnected?.Invoke();
            
            string walletAddress = AppKit.Account.Address;
            
            if (TryLoadNFTCacheFromDisk(walletAddress))
            {
                Debug.Log("[WalletService] NFT cache restored from disk");
                onNFTsLoaded?.Invoke();
                _ = RefreshNFTCacheInBackground();
                return;
            }
            
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
            m_NFTOwnershipCache.Clear();
            m_NFTCacheInitialized = false;
            m_OwnsErc1155NFT = false;
            m_OwnsErc721NFT = false;
            m_OwnedErc1155TokenIds.Clear();
            m_OwnedErc721TokenIds.Clear();
            m_CachedWalletAddress = string.Empty;
            
            ClearNFTCacheFromDisk();
            
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
                
            string walletAddress = AppKit.Account.Address;
            
            // === ERC-1155 (Active/Primary) ===
            // Use Alchemy getNFTsForOwner so any token ID from the contract counts (e.g. each domain mint gets a different ID).
            try
            {
                if (m_ChainSettings.IsConfigured())
                {
                    var (ownsAny, ownedTokenIds) = await AlchemyNftApi.GetOwnedNftsFromContractAsync(
                        walletAddress,
                        m_ChainSettings.nftContractAddress,
                        m_ChainSettings.apiKey);
                    m_OwnsErc1155NFT = ownsAny;
                    if (ownsAny && ownedTokenIds != null)
                        m_OwnedErc1155TokenIds.AddRange(ownedTokenIds);
                }
                else
                {
                    // Fallback: single token ID check if Alchemy not configured
                    var nft1155Service = new NFTService(m_ChainSettings.nftContractAddress, m_ChainSettings.rpcUrl);
                    string tokenId = m_ChainSettings.nft1155TokenId;
                    m_OwnsErc1155NFT = await nft1155Service.OwnsToken(walletAddress, tokenId);
                    if (m_OwnsErc1155NFT)
                        m_OwnedErc1155TokenIds.Add(tokenId);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to check ERC-1155 ownership: {ex.Message}");
                m_OwnsErc1155NFT = false;
            }
            
            // === ERC-721 (Secondary) ===
            try
            {
                if (m_ChainSettings.IsERC721Configured())
                {
                    var nft721Service = new NFT721Service(m_ChainSettings.nft721ContractAddress, m_ChainSettings.rpcUrl);
                    
                    // Check if the wallet owns ANY token from the Floorcraft collection
                    m_OwnsErc721NFT = await nft721Service.OwnsAnyToken(walletAddress);
                    
                    if (m_OwnsErc721NFT)
                    {
                        // Try to get specific owned token IDs (requires ERC721Enumerable)
                        var ownedIds = await nft721Service.GetOwnedTokenIds(walletAddress);
                        m_OwnedErc721TokenIds.AddRange(ownedIds);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to check ERC-721 ownership: {ex.Message}");
                m_OwnsErc721NFT = false;
            }
            
            m_NFTCacheInitialized = true;
            
            SaveNFTCacheToDisk();
            
            onNFTsLoaded?.Invoke();
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
        
        /// <summary>
        /// Check if the connected wallet owns any NFT (ERC-1155 or ERC-721).
        /// ERC-1155 is checked first (primary), then ERC-721 (secondary).
        /// </summary>
        /// <param name="tokenId">Token ID (used for ERC-1155 specific checks, ignored for general gating)</param>
        /// <returns>True if wallet owns any supported NFT</returns>
        public bool IsNFTOwned(string tokenId)
        {
            if (!m_NFTCacheInitialized)
            {
                return false;
            }
            
            // ERC-1155 (primary) OR ERC-721 (secondary) ownership unlocks NFT-gated content
            return m_OwnsErc1155NFT || m_OwnsErc721NFT;
        }
        
        /// <summary>
        /// Get combined list of all owned token IDs (ERC-1155 first, then ERC-721)
        /// </summary>
        public List<string> GetOwnedTokenIds()
        {
            var allOwned = new List<string>();
            allOwned.AddRange(m_OwnedErc1155TokenIds);
            allOwned.AddRange(m_OwnedErc721TokenIds);
            return allOwned;
        }
        
        /// <summary>
        /// Get list of owned ERC-1155 token IDs
        /// </summary>
        public List<string> GetOwnedErc1155TokenIds()
        {
            return new List<string>(m_OwnedErc1155TokenIds);
        }
        
        /// <summary>
        /// Get list of owned ERC-721 token IDs
        /// </summary>
        public List<string> GetOwnedErc721TokenIds()
        {
            return new List<string>(m_OwnedErc721TokenIds);
        }
        
        /// <summary>
        /// Get total count of NFTs owned by the connected wallet (both standards)
        /// </summary>
        public int GetOwnedNFTCount()
        {
            return m_OwnedErc1155TokenIds.Count + m_OwnedErc721TokenIds.Count;
        }
        
        private void RestoreNFTCacheOnStartup()
        {
            if (!PlayerPrefs.HasKey(NFT_CACHE_PREFS_KEY))
                return;
            
            try
            {
                string json = PlayerPrefs.GetString(NFT_CACHE_PREFS_KEY);
                var cacheData = JsonConvert.DeserializeObject<NFTCacheData>(json);
                
                if (cacheData == null || string.IsNullOrEmpty(cacheData.walletAddress))
                    return;
                
                m_CachedWalletAddress = cacheData.walletAddress;
                m_OwnsErc1155NFT = cacheData.ownsErc1155NFT;
                m_OwnsErc721NFT = cacheData.ownsErc721NFT;
                m_OwnedErc1155TokenIds.Clear();
                m_OwnedErc1155TokenIds.AddRange(cacheData.ownedErc1155TokenIds ?? new List<string>());
                m_OwnedErc721TokenIds.Clear();
                m_OwnedErc721TokenIds.AddRange(cacheData.ownedErc721TokenIds ?? new List<string>());
                m_NFTCacheInitialized = true;
                
                Debug.Log($"[WalletService] NFT cache restored on startup for {m_CachedWalletAddress}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[WalletService] Failed to restore NFT cache on startup: {ex.Message}");
            }
        }
        
        private void SaveNFTCacheToDisk()
        {
            try
            {
                string address = AppKit.IsAccountConnected ? AppKit.Account.Address : m_CachedWalletAddress;
                m_CachedWalletAddress = address;
                
                var cacheData = new NFTCacheData
                {
                    walletAddress = address,
                    ownsErc1155NFT = m_OwnsErc1155NFT,
                    ownsErc721NFT = m_OwnsErc721NFT,
                    ownedErc1155TokenIds = new List<string>(m_OwnedErc1155TokenIds),
                    ownedErc721TokenIds = new List<string>(m_OwnedErc721TokenIds)
                };
                
                string json = JsonConvert.SerializeObject(cacheData);
                PlayerPrefs.SetString(NFT_CACHE_PREFS_KEY, json);
                PlayerPrefs.Save();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[WalletService] Failed to save NFT cache to disk: {ex.Message}");
            }
        }
        
        private bool TryLoadNFTCacheFromDisk(string walletAddress)
        {
            if (!PlayerPrefs.HasKey(NFT_CACHE_PREFS_KEY))
                return false;
            
            try
            {
                string json = PlayerPrefs.GetString(NFT_CACHE_PREFS_KEY);
                var cacheData = JsonConvert.DeserializeObject<NFTCacheData>(json);
                
                if (cacheData == null || 
                    !string.Equals(cacheData.walletAddress, walletAddress, StringComparison.OrdinalIgnoreCase))
                    return false;
                
                m_OwnsErc1155NFT = cacheData.ownsErc1155NFT;
                m_OwnsErc721NFT = cacheData.ownsErc721NFT;
                m_OwnedErc1155TokenIds.Clear();
                m_OwnedErc1155TokenIds.AddRange(cacheData.ownedErc1155TokenIds ?? new List<string>());
                m_OwnedErc721TokenIds.Clear();
                m_OwnedErc721TokenIds.AddRange(cacheData.ownedErc721TokenIds ?? new List<string>());
                m_NFTCacheInitialized = true;
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[WalletService] Failed to load NFT cache from disk: {ex.Message}");
                return false;
            }
        }
        
        private void ClearNFTCacheFromDisk()
        {
            PlayerPrefs.DeleteKey(NFT_CACHE_PREFS_KEY);
            PlayerPrefs.Save();
        }
        
        /// <summary>
        /// Re-fetches NFT ownership from the network without blocking the user.
        /// If ownership changed, updates in-memory state, saves to disk, and re-fires onNFTsLoaded.
        /// On network failure, the existing cached data is preserved.
        /// </summary>
        private async Task RefreshNFTCacheInBackground()
        {
            if (!AppKit.IsAccountConnected)
                return;
            
            string walletAddress = AppKit.Account.Address;
            
            bool freshOwns1155 = false;
            bool freshOwns721 = false;
            var freshErc1155Ids = new List<string>();
            var freshErc721Ids = new List<string>();
            
            try
            {
                if (m_ChainSettings.IsConfigured())
                {
                    var (ownsAny, ownedTokenIds) = await AlchemyNftApi.GetOwnedNftsFromContractAsync(
                        walletAddress, m_ChainSettings.nftContractAddress, m_ChainSettings.apiKey);
                    freshOwns1155 = ownsAny;
                    if (ownsAny && ownedTokenIds != null)
                        freshErc1155Ids.AddRange(ownedTokenIds);
                }
                else
                {
                    var nft1155Service = new NFTService(m_ChainSettings.nftContractAddress, m_ChainSettings.rpcUrl);
                    freshOwns1155 = await nft1155Service.OwnsToken(walletAddress, m_ChainSettings.nft1155TokenId);
                    if (freshOwns1155)
                        freshErc1155Ids.Add(m_ChainSettings.nft1155TokenId);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[WalletService] Background refresh ERC-1155 failed, keeping cache: {ex.Message}");
                return;
            }
            
            try
            {
                if (m_ChainSettings.IsERC721Configured())
                {
                    var nft721Service = new NFT721Service(m_ChainSettings.nft721ContractAddress, m_ChainSettings.rpcUrl);
                    freshOwns721 = await nft721Service.OwnsAnyToken(walletAddress);
                    if (freshOwns721)
                    {
                        var ownedIds = await nft721Service.GetOwnedTokenIds(walletAddress);
                        freshErc721Ids.AddRange(ownedIds);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[WalletService] Background refresh ERC-721 failed, keeping cache: {ex.Message}");
                return;
            }
            
            bool changed = freshOwns1155 != m_OwnsErc1155NFT || freshOwns721 != m_OwnsErc721NFT;
            
            m_OwnsErc1155NFT = freshOwns1155;
            m_OwnsErc721NFT = freshOwns721;
            m_OwnedErc1155TokenIds.Clear();
            m_OwnedErc1155TokenIds.AddRange(freshErc1155Ids);
            m_OwnedErc721TokenIds.Clear();
            m_OwnedErc721TokenIds.AddRange(freshErc721Ids);
            
            SaveNFTCacheToDisk();
            
            if (changed)
            {
                Debug.Log("[WalletService] NFT ownership changed during background refresh");
                onNFTsLoaded?.Invoke();
            }
        }
        
        /// <summary>
        /// Check if wallet owns any NFT (ERC-1155 primary, ERC-721 secondary).
        /// </summary>
        /// <param name="tokenId">Token ID for ERC-1155 check</param>
        /// <returns>True if wallet owns any supported NFT</returns>
        public async Task<bool> CheckNFTOwnership(string tokenId)
        {
            if (!AppKit.IsAccountConnected)
            {
                Debug.LogWarning("Wallet not connected!");
                return false;
            }

            try
            {
                // Check ERC-1155 first (primary) - use Alchemy "owns any from contract" when configured
                if (m_ChainSettings.IsConfigured())
                {
                    bool owns1155 = await AlchemyNftApi.OwnsAnyNftFromContractAsync(
                        AppKit.Account.Address,
                        m_ChainSettings.nftContractAddress,
                        m_ChainSettings.apiKey);
                    if (owns1155)
                        return true;
                }
                else
                {
                    var nft1155Service = new NFTService(m_ChainSettings.nftContractAddress, m_ChainSettings.rpcUrl);
                    bool owns1155 = await nft1155Service.OwnsToken(AppKit.Account.Address, m_ChainSettings.nft1155TokenId);
                    if (owns1155)
                        return true;
                }

                // Fallback to ERC-721 (secondary)
                if (m_ChainSettings.IsERC721Configured())
                {
                    var nft721Service = new NFT721Service(m_ChainSettings.nft721ContractAddress, m_ChainSettings.rpcUrl);
                    bool owns721 = await nft721Service.OwnsAnyToken(AppKit.Account.Address);
                    return owns721;
                }
                
                return false;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error checking NFT ownership: {ex.Message}");
                return false;
            }
        }
    }
}