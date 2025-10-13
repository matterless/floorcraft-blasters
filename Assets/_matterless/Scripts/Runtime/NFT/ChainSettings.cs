using UnityEngine;

namespace Matterless.Floorcraft
{
    /// <summary>
    /// NFT blockchain configuration settings.
    /// Configure contract address, RPC endpoint, and API key for blockchain interaction.
    /// Note: Only read operations (no transactions), so mainnet testing is safe and free.
    /// </summary>
    [System.Serializable]
    public class ChainSettings
    {
        [Header("NFT Contract Configuration")]
        [Tooltip("ERC-721 NFT contract address on Base blockchain")]
        [SerializeField] private string m_NftContractAddress = "Get your NFT Contract Address";
        
        [Header("Network Configuration")]
        [Tooltip("RPC endpoint URL (e.g., https://base-mainnet.g.alchemy.com/v2/)")]
        [SerializeField] private string m_RpcEndpoint = "https://base-mainnet.g.alchemy.com/v2/";
        
        [Tooltip("Alchemy API key (get from https://alchemy.com)")]
        [SerializeField] private string m_ApiKey = "Get your Alchemy API Key from https://alchemy.com";
        
        // Public accessors
        public string nftContractAddress => m_NftContractAddress;
        public string rpcEndpoint => m_RpcEndpoint;
        public string apiKey => m_ApiKey;
        public string rpcUrl => $"{m_RpcEndpoint}{m_ApiKey}";
        
        /// <summary>
        /// Check if settings are properly configured
        /// </summary>
        public bool IsConfigured()
        {
            if (string.IsNullOrEmpty(m_NftContractAddress) || m_NftContractAddress.StartsWith("Get your"))
            {
                Debug.LogError("[ChainSettings] NFT contract address is not set!");
                return false;
            }
            
            if (string.IsNullOrEmpty(m_ApiKey) || m_ApiKey.StartsWith("Get your"))
            {
                Debug.LogError("[ChainSettings] Alchemy API key is not set! Get one from https://alchemy.com");
                return false;
            }
            
            if (string.IsNullOrEmpty(m_RpcEndpoint))
            {
                Debug.LogError("[ChainSettings] RPC endpoint is not set!");
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Get display info for debugging
        /// </summary>
        public string GetDebugInfo()
        {
            return $"NFT Contract: {m_NftContractAddress}\n" +
                   $"RPC: {m_RpcEndpoint}***\n" +
                   $"Configured: {IsConfigured()}";
        }
    }
}

