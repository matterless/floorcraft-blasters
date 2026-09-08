using System;
using System.Numerics;
using System.Threading.Tasks;
using Nethereum.Web3;
using Nethereum.Contracts;
using UnityEngine;

namespace Matterless.Floorcraft
{
    /// <summary>
    /// Low-level wrapper for ERC-721 smart contract calls.
    /// Pure contract interface - no caching, no game logic.
    /// Reusable for any ERC-721 contract on any EVM chain.
    /// </summary>
    public class ERC721Contract
    {
        private readonly string m_ContractAddress;
        private readonly string m_RpcUrl;
        private readonly Web3 m_Web3;
        private readonly Contract m_Contract;
        
        public string ContractAddress => m_ContractAddress;
        public string RpcUrl => m_RpcUrl;
        
        /// <summary>
        /// Initialize ERC-721 contract wrapper
        /// </summary>
        /// <param name="contractAddress">ERC-721 contract address</param>
        /// <param name="rpcUrl">Blockchain RPC endpoint</param>
        public ERC721Contract(string contractAddress, string rpcUrl)
        {
            m_ContractAddress = contractAddress;
            m_RpcUrl = rpcUrl;
            
            // Initialize Nethereum
            m_Web3 = new Web3(m_RpcUrl);
            m_Contract = m_Web3.Eth.GetContract(ERC721ABI.JSON, m_ContractAddress);
        }
        
        /// <summary>
        /// Get the number of NFTs owned by an address
        /// </summary>
        /// <param name="owner">Wallet address</param>
        /// <returns>Number of NFTs owned</returns>
        public async Task<BigInteger> BalanceOf(string owner)
        {
            try
            {
                var balanceFunction = m_Contract.GetFunction("balanceOf");
                var balance = await balanceFunction.CallAsync<BigInteger>(owner);
                return balance;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ERC721Contract] BalanceOf failed: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Get token ID at a given index for an owner (requires ERC721Enumerable)
        /// </summary>
        /// <param name="owner">Wallet address</param>
        /// <param name="index">Index in the owner's token list</param>
        /// <returns>Token ID at that index</returns>
        public async Task<BigInteger> TokenOfOwnerByIndex(string owner, BigInteger index)
        {
            try
            {
                var function = m_Contract.GetFunction("tokenOfOwnerByIndex");
                var tokenId = await function.CallAsync<BigInteger>(owner, index);
                return tokenId;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ERC721Contract] TokenOfOwnerByIndex failed: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Get the owner of a specific token
        /// </summary>
        /// <param name="tokenId">Token ID</param>
        /// <returns>Owner address</returns>
        public async Task<string> OwnerOf(BigInteger tokenId)
        {
            try
            {
                var function = m_Contract.GetFunction("ownerOf");
                var owner = await function.CallAsync<string>(tokenId);
                return owner;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ERC721Contract] OwnerOf failed for token {tokenId}: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Get the metadata URI for a token
        /// </summary>
        /// <param name="tokenId">Token ID</param>
        /// <returns>Metadata URI (usually IPFS link)</returns>
        public async Task<string> TokenURI(BigInteger tokenId)
        {
            try
            {
                var function = m_Contract.GetFunction("tokenURI");
                var uri = await function.CallAsync<string>(tokenId);
                return uri;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ERC721Contract] TokenURI failed for token {tokenId}: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Get the contract name
        /// </summary>
        /// <returns>Contract name</returns>
        public async Task<string> Name()
        {
            try
            {
                var function = m_Contract.GetFunction("name");
                var name = await function.CallAsync<string>();
                return name;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ERC721Contract] Name failed: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Get the contract symbol
        /// </summary>
        /// <returns>Contract symbol</returns>
        public async Task<string> Symbol()
        {
            try
            {
                var function = m_Contract.GetFunction("symbol");
                var symbol = await function.CallAsync<string>();
                return symbol;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ERC721Contract] Symbol failed: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Get the total supply of tokens in the collection.
        /// Note: This requires ERC721Enumerable extension, not all contracts support it.
        /// </summary>
        /// <returns>Total supply, or -1 if not supported</returns>
        public async Task<BigInteger> TotalSupply()
        {
            try
            {
                var function = m_Contract.GetFunction("totalSupply");
                var supply = await function.CallAsync<BigInteger>();
                return supply;
            }
            catch (Exception)
            {
                // totalSupply is optional (ERC721Enumerable), return -1 if not supported
                return -1;
            }
        }
    }
}

