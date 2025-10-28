using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Nethereum.Web3;
using Nethereum.Contracts;
using UnityEngine;

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

namespace Matterless.Floorcraft
{
    /// <summary>
    /// Low-level wrapper for ERC-1155 smart contract calls.
    /// Pure contract interface - no caching, no game logic.
    /// Reusable for any ERC-1155 contract on any EVM chain.
    /// </summary>
    public class ERC1155Contract
    {
        private readonly string m_ContractAddress;
        private readonly string m_RpcUrl;
        private readonly Web3 m_Web3;
        private readonly Contract m_Contract;
        
        public string ContractAddress => m_ContractAddress;
        public string RpcUrl => m_RpcUrl;
        
        /// <summary>
        /// Initialize ERC-1155 contract wrapper
        /// </summary>
        /// <param name="contractAddress">ERC-1155 contract address</param>
        /// <param name="rpcUrl">Blockchain RPC endpoint</param>
        public ERC1155Contract(string contractAddress, string rpcUrl)
        {
            m_ContractAddress = contractAddress;
            m_RpcUrl = rpcUrl.EndsWith("/") ? rpcUrl : rpcUrl + "/";
            
            try
            {
                ConfigureJsonSerialization();
                m_Web3 = new Web3(m_RpcUrl);
                m_Contract = m_Web3.Eth.GetContract(ERC1155ABI.JSON, m_ContractAddress);
            }
            catch (Exception ex)
            {
                Debug.LogError($"ERC1155Contract initialization failed: {ex.Message}");
                throw;
            }
        }
        
        private void ConfigureJsonSerialization()
        {
            // Simple JSON configuration for Unity compatibility
        }
        
        /// <summary>
        /// Get the balance of a specific token for an address
        /// </summary>
        /// <param name="account">Wallet address</param>
        /// <param name="tokenId">Token ID</param>
        /// <returns>Balance of the token</returns>
        public async Task<BigInteger> BalanceOf(string account, BigInteger tokenId)
        {
            try
            {
                // Try Nethereum first
                try
                {
                    var balanceFunction = m_Contract.GetFunction("balanceOf");
                    var balance = await balanceFunction.CallAsync<BigInteger>(account, tokenId);
                    return balance;
                }
                catch (Exception nethereumEx)
                {
                    // Fallback to direct RPC call
                    return await BalanceOfDirectRpc(account, tokenId);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"BalanceOf failed: {ex.Message}");
                return 0;
            }
        }
        
        private async Task<BigInteger> BalanceOfDirectRpc(string account, BigInteger tokenId)
        {
            try
            {
                using (var httpClient = new System.Net.Http.HttpClient())
                {
                    // ERC-1155 balanceOf function signature: balanceOf(address,uint256)
                    // Function selector: 0x00fdd58e
                    string functionSelector = "0x00fdd58e";
                    
                    // Pad account address to 32 bytes (64 hex characters)
                    string paddedAccount = account.Substring(2).PadLeft(64, '0');
                    
                    // Pad tokenId to 32 bytes (64 hex characters)
                    string paddedTokenId = tokenId.ToString("X").PadLeft(64, '0');
                    
                    string data = functionSelector + paddedAccount + paddedTokenId;
                    
                    var rpcRequest = new
                    {
                        jsonrpc = "2.0",
                        method = "eth_call",
                        @params = new object[] 
                        { 
                            new { to = m_ContractAddress, data = data }, 
                            "latest" 
                        },
                        id = 1
                    };

                    string jsonRequest = Newtonsoft.Json.JsonConvert.SerializeObject(rpcRequest);
                    var content = new System.Net.Http.StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json");
                    var response = await httpClient.PostAsync(m_RpcUrl, content);
                    
                    string responseContent = await response.Content.ReadAsStringAsync();
                    
                    var rpcResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<RpcResponse>(responseContent);
                    
                    if (rpcResponse.error != null)
                    {
                        Debug.LogError($"RPC Error: {rpcResponse.error}");
                        return 0;
                    }
                    
                    string balanceHex = rpcResponse.result;
                    if (string.IsNullOrEmpty(balanceHex) || balanceHex == "0x0")
                    {
                        return 0;
                    }
                    
                    // Convert hex to BigInteger
                    var balance = new BigInteger(System.Convert.ToInt64(balanceHex.Substring(2), 16));
                    return balance;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Direct RPC BalanceOf failed: {ex.Message}");
                return 0;
            }
        }
        
        /// <summary>
        /// Get the URI for a specific token
        /// </summary>
        /// <param name="tokenId">Token ID</param>
        /// <returns>Token URI</returns>
        public async Task<string> URI(BigInteger tokenId)
        {
            try
            {
                // Try Nethereum first
                try
                {
                    var function = m_Contract.GetFunction("uri");
                    var uri = await function.CallAsync<string>(tokenId);
                    return uri;
                }
                catch (Exception nethereumEx)
                {
                    Debug.LogWarning($"Nethereum failed, trying direct RPC: {nethereumEx.Message}");
                    // Fallback to direct RPC call
                    return await URIDirectRpc(tokenId);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"ERC1155Contract URI failed for token {tokenId}: {ex.Message}");
                throw;
            }
        }
        
        private async Task<string> URIDirectRpc(BigInteger tokenId)
        {
            try
            {
                using (var httpClient = new System.Net.Http.HttpClient())
                {
                    // ERC-1155 uri function signature: uri(uint256)
                    // Function selector: 0x0e89341c
                    string functionSelector = "0x0e89341c";
                    
                    // Pad tokenId to 32 bytes (64 hex characters)
                    string paddedTokenId = tokenId.ToString("X").PadLeft(64, '0');
                    
                    string data = functionSelector + paddedTokenId;
                    
                    var rpcRequest = new
                    {
                        jsonrpc = "2.0",
                        method = "eth_call",
                        @params = new object[] 
                        { 
                            new { to = m_ContractAddress, data = data }, 
                            "latest" 
                        },
                        id = 1
                    };

                    string jsonRequest = Newtonsoft.Json.JsonConvert.SerializeObject(rpcRequest);
                    var content = new System.Net.Http.StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json");
                    var response = await httpClient.PostAsync(m_RpcUrl, content);
                    
                    string responseContent = await response.Content.ReadAsStringAsync();
                    
                    var rpcResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<RpcResponse>(responseContent);
                    
                    if (rpcResponse.error != null)
                    {
                        Debug.LogError($"RPC Error for uri(): {rpcResponse.error}");
                        return string.Empty;
                    }
                    
                    string resultHex = rpcResponse.result;
                    
                    if (string.IsNullOrEmpty(resultHex) || resultHex == "0x")
                    {
                        Debug.LogWarning($"Empty result for uri() token {tokenId}");
                        return string.Empty;
                    }
                    
                    // Decode ABI-encoded string
                    string decodedUri = DecodeAbiEncodedString(resultHex);
                    
                    return decodedUri;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Direct RPC URI failed: {ex.Message}");
                return string.Empty;
            }
        }
        
        private string DecodeAbiEncodedString(string hexData)
        {
            try
            {
                // Remove '0x' prefix
                string hex = hexData.StartsWith("0x") ? hexData.Substring(2) : hexData;
                
                // First 64 characters (32 bytes) are the offset - skip it
                // Next 64 characters (32 bytes) are the length of the string
                if (hex.Length < 128)
                {
                    Debug.LogError($"Invalid hex data length: {hex.Length}");
                    return string.Empty;
                }
                
                string lengthHex = hex.Substring(64, 64);
                int stringLength = System.Convert.ToInt32(lengthHex, 16);
                
                // Then comes the actual string data (padded to 32-byte boundary)
                string dataHex = hex.Substring(128);
                
                // Convert hex to bytes manually
                byte[] bytes = new byte[stringLength];
                for (int i = 0; i < stringLength; i++)
                {
                    if (i * 2 + 1 < dataHex.Length)
                    {
                        string hexByte = dataHex.Substring(i * 2, 2);
                        bytes[i] = System.Convert.ToByte(hexByte, 16);
                    }
                }
                
                // Convert bytes to string
                string result = System.Text.Encoding.UTF8.GetString(bytes);
                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to decode ABI string: {ex.Message}");
                return string.Empty;
            }
        }
        
        /// <summary>
        /// Get balances for multiple tokens (batch query)
        /// </summary>
        /// <param name="accounts">Array of wallet addresses</param>
        /// <param name="tokenIds">Array of token IDs to check</param>
        /// <returns>Array of balances</returns>
        public async Task<BigInteger[]> BalanceOfBatch(string[] accounts, BigInteger[] tokenIds)
        {
            try
            {
                var function = m_Contract.GetFunction("balanceOfBatch");
                var balances = await function.CallAsync<BigInteger[]>(accounts, tokenIds);
                return balances;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ERC1155Contract] BalanceOfBatch failed: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Check if an account owns any tokens (useful for vehicle unlock)
        /// </summary>
        /// <param name="account">Wallet address</param>
        /// <param name="maxTokenId">Maximum token ID to check (e.g., 1000)</param>
        /// <returns>True if account owns any tokens</returns>
        public async Task<bool> OwnsAnyTokens(string account, int maxTokenId = 1000)
        {
            try
            {
                // Check first 1000 token IDs (adjust as needed)
                var tokenIds = new BigInteger[maxTokenId];
                var accounts = new string[maxTokenId];
                
                for (int i = 0; i < maxTokenId; i++)
                {
                    tokenIds[i] = i + 1; // Token IDs usually start from 1
                    accounts[i] = account;
                }
                
                var balances = await BalanceOfBatch(accounts, tokenIds);
                
                foreach (var balance in balances)
                {
                    if (balance > 0)
                    {
                        return true;
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ERC1155Contract] OwnsAnyTokens failed: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Get all token IDs owned by an account (up to maxTokenId)
        /// Note: This method is not implemented as it requires complex batch operations
        /// Use individual BalanceOf calls instead for better reliability
        /// </summary>
        /// <param name="account">Wallet address</param>
        /// <param name="maxTokenId">Maximum token ID to check</param>
        /// <returns>List of owned token IDs</returns>
        public async Task<List<string>> GetOwnedTokenIds(string account, int maxTokenId = 1000)
        {
            // This method is intentionally not implemented
            // Use individual BalanceOf calls for specific token IDs instead
            Debug.LogWarning("GetOwnedTokenIds is not implemented. Use individual BalanceOf calls instead.");
            return new List<string>();
        }
    }
}

