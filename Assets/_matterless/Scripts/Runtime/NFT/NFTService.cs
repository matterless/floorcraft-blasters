using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using UnityEngine;

namespace Matterless.Floorcraft
{
    /// <summary>
    /// High-level NFT service for Floorcraft Blasters.
    /// Handles game integration and business logic.
    /// Uses ERC721Contract for blockchain communication.
    /// </summary>
    public class NFTService
    {
        private readonly ERC721Contract m_Contract;
        
        /// <summary>
        /// Initialize NFT Service
        /// </summary>
        /// <param name="contract">ERC721Contract instance</param>
        public NFTService(ERC721Contract contract)
        {
            m_Contract = contract;
            Debug.Log($"[NFT Service] Initialized with contract: {m_Contract.ContractAddress}");
        }
        
        /// <summary>
        /// Initialize NFT Service with contract address and RPC URL
        /// </summary>
        /// <param name="contractAddress">Your ERC-721 contract address (e.g., "0xABC...")</param>
        /// <param name="rpcUrl">RPC endpoint with API key (e.g., "https://base-mainnet.g.alchemy.com/v2/YOUR_KEY")</param>
        public NFTService(string contractAddress, string rpcUrl)
            : this(new ERC721Contract(contractAddress, rpcUrl))
        {
        }
        
        /// <summary>
        /// Get all NFT token IDs owned by an address
        /// Uses ERC721Enumerable extension (tokenOfOwnerByIndex)
        /// </summary>
        /// <param name="ownerAddress">Wallet address to check</param>
        /// <returns>List of token IDs as strings</returns>
        public async Task<List<string>> GetOwnedTokenIds(string ownerAddress)
        {
            try
            {
                Debug.Log($"[NFT Service] Querying ownership for: {ownerAddress}");
                
                // Step 1: Get balance (how many NFTs owned)
                var balance = await m_Contract.BalanceOf(ownerAddress);
                
                Debug.Log($"[NFT Service] Balance: {balance}");
                
                var ownedTokenIds = new List<string>();
                
                // Step 2: Get each token ID
                if (balance > 0)
                {
                    for (int i = 0; i < (int)balance; i++)
                    {
                        try
                        {
                            var tokenId = await m_Contract.TokenOfOwnerByIndex(ownerAddress, i);
                            ownedTokenIds.Add(tokenId.ToString());
                            Debug.Log($"[NFT Service] Found token: {tokenId}");
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"[NFT Service] Failed to get token at index {i}: {ex.Message}");
                        }
                    }
                }
                
                Debug.Log($"[NFT Service] Total NFTs owned: {ownedTokenIds.Count}");
                return ownedTokenIds;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NFT Service] Error querying NFTs: {ex.Message}");
                return new List<string>();
            }
        }
        
        /// <summary>
        /// Get metadata URI for a specific token
        /// </summary>
        /// <param name="tokenId">Token ID</param>
        /// <returns>Metadata URI (usually IPFS link)</returns>
        public async Task<string> GetTokenURI(string tokenId)
        {
            try
            {
                var uri = await m_Contract.TokenURI(BigInteger.Parse(tokenId));
                Debug.Log($"[NFT Service] Token {tokenId} URI: {uri}");
                return uri;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NFT Service] Error getting tokenURI for {tokenId}: {ex.Message}");
                return string.Empty;
            }
        }
        
        /// <summary>
        /// Get the owner of a specific token
        /// </summary>
        /// <param name="tokenId">Token ID</param>
        /// <returns>Owner address</returns>
        public async Task<string> GetOwnerOf(string tokenId)
        {
            try
            {
                var owner = await m_Contract.OwnerOf(BigInteger.Parse(tokenId));
                return owner;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NFT Service] Error getting owner of {tokenId}: {ex.Message}");
                return string.Empty;
            }
        }
        
        /// <summary>
        /// Get contract name
        /// </summary>
        public async Task<string> GetContractName()
        {
            try
            {
                return await m_Contract.Name();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NFT Service] Error getting contract name: {ex.Message}");
                return string.Empty;
            }
        }
        
        /// <summary>
        /// Get contract symbol
        /// </summary>
        public async Task<string> GetContractSymbol()
        {
            try
            {
                return await m_Contract.Symbol();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NFT Service] Error getting contract symbol: {ex.Message}");
                return string.Empty;
            }
        }
        
        /// <summary>
        /// Get total supply of NFTs in collection
        /// </summary>
        public async Task<int> GetTotalSupply()
        {
            try
            {
                var supply = await m_Contract.TotalSupply();
                return (int)supply;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NFT Service] Error getting total supply: {ex.Message}");
                return 0;
            }
        }
        
        /// <summary>
        /// Check if a specific token is owned by an address
        /// </summary>
        /// <param name="ownerAddress">Wallet address to check</param>
        /// <param name="tokenId">Token ID to verify</param>
        /// <returns>True if owner owns the token</returns>
        public async Task<bool> OwnsToken(string ownerAddress, string tokenId)
        {
            var tokens = await GetOwnedTokenIds(ownerAddress);
            return tokens.Contains(tokenId);
        }
    }
}

