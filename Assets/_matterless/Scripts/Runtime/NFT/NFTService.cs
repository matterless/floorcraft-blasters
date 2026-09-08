using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using UnityEngine;
using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace Matterless.Floorcraft
{
    /// <summary>
    /// High-level NFT service for Floorcraft Blasters.
    /// Handles game integration and business logic.
    /// Uses ERC1155Contract for blockchain communication.
    /// </summary>
    public class NFTService
    {
        private readonly ERC1155Contract m_Contract;
        
        /// <summary>
        /// Initialize NFT Service
        /// </summary>
        /// <param name="contract">ERC1155Contract instance</param>
        public NFTService(ERC1155Contract contract)
        {
            m_Contract = contract;
        }
        
        /// <summary>
        /// Initialize NFT Service with contract address and RPC URL
        /// </summary>
        /// <param name="contractAddress">Your ERC-1155 contract address (e.g., "0xABC...")</param>
        /// <param name="rpcUrl">RPC endpoint with API key (e.g., "https://base-mainnet.g.alchemy.com/v2/YOUR_KEY")</param>
        public NFTService(string contractAddress, string rpcUrl)
            : this(new ERC1155Contract(contractAddress, rpcUrl))
        {
        }
        
        /// <summary>
        /// Get all NFT token IDs owned by an address
        /// Uses ERC-1155 batch query method
        /// </summary>
        /// <param name="ownerAddress">Wallet address to check</param>
        /// <returns>List of token IDs as strings</returns>
        public async Task<List<string>> GetOwnedTokenIds(string ownerAddress)
        {
            try
            {
                // Use ERC-1155 batch query method
                var ownedTokenIds = await m_Contract.GetOwnedTokenIds(ownerAddress);
                
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
                var uri = await m_Contract.URI(BigInteger.Parse(tokenId));
                return uri;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NFT Service] Error getting tokenURI for {tokenId}: {ex.Message}");
                return string.Empty;
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
            try
            {
                var balance = await m_Contract.BalanceOf(ownerAddress, BigInteger.Parse(tokenId));
                return balance > 0;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NFT Service] Error checking token ownership: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Load NFT image as Sprite from token metadata
        /// </summary>
        /// <param name="tokenId">Token ID</param>
        /// <returns>Sprite loaded from NFT image</returns>
        public async Task<Sprite> LoadNFTImage(string tokenId)
        {
            try
            {
                string imageUrl = await GetTokenImageUrl(tokenId);
                
                if (string.IsNullOrEmpty(imageUrl))
                {
                    Debug.LogWarning($"No image URL for token {tokenId}");
                    return null;
                }
                
                Sprite sprite = await LoadImageSprite(imageUrl);
                return sprite;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading NFT image for token {tokenId}: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Get image URL from token metadata
        /// </summary>
        private async Task<string> GetTokenImageUrl(string tokenId)
        {
            try
            {
                string uri = await GetTokenURI(tokenId);
                
                if (string.IsNullOrEmpty(uri))
                {
                    return string.Empty;
                }
                
                // Handle ERC-1155 URIs with {id} placeholder
                if (uri.Contains("{id}"))
                {
                    BigInteger tokenIdBigInt = BigInteger.Parse(tokenId);
                    // Use lowercase hex, strip leading zeros, pad to 64 chars
                    string hexTokenId = tokenIdBigInt.ToString("x").TrimStart('0').PadLeft(64, '0');
                    uri = uri.Replace("{id}", hexTokenId);
                }
                
                // Convert IPFS protocol to HTTP
                if (uri.StartsWith("ipfs://"))
                {
                    uri = uri.Replace("ipfs://", "https://ipfs.io/ipfs/");
                }
                
                // For non-IPFS URLs (regular HTTP), fetch metadata directly
                if (!uri.Contains("/ipfs/"))
                {
                    return await FetchImageUrlFromMetadata(uri);
                }
                
                // For IPFS URLs, try multiple gateways
                string ipfsHash = uri.Substring(uri.IndexOf("/ipfs/") + 6);
                
                string[] ipfsGateways = new string[] 
                {
                    "https://ipfs.io/ipfs/",
                    "https://cloudflare-ipfs.com/ipfs/",
                    "https://dweb.link/ipfs/",
                    "https://gateway.pinata.cloud/ipfs/"
                };
                
                foreach (var gateway in ipfsGateways)
                {
                    try
                    {
                        string imageUrl = await FetchImageUrlFromMetadata(gateway + ipfsHash);
                        if (!string.IsNullOrEmpty(imageUrl))
                        {
                            return imageUrl;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"IPFS gateway {gateway} failed: {ex.Message}");
                    }
                }
                
                Debug.LogWarning($"All gateways failed for token {tokenId}");
                return string.Empty;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error getting image URL for token {tokenId}: {ex.Message}");
                return string.Empty;
            }
        }
        
        /// <summary>
        /// Fetch the image URL from metadata JSON at the given URL
        /// </summary>
        private async Task<string> FetchImageUrlFromMetadata(string metadataUrl)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.Timeout = TimeSpan.FromSeconds(10);
                string metadataJson = await httpClient.GetStringAsync(metadataUrl);
                
                if (!string.IsNullOrEmpty(metadataJson))
                {
                    var metadata = JObject.Parse(metadataJson);
                    string imageUrl = metadata["image"]?.ToString();
                    
                    if (imageUrl != null && imageUrl.StartsWith("ipfs://"))
                    {
                        imageUrl = imageUrl.Replace("ipfs://", "https://ipfs.io/ipfs/");
                    }
                    
                    return imageUrl;
                }
            }
            
            return string.Empty;
        }
        
        /// <summary>
        /// Check if the NFT has video content instead of an image
        /// </summary>
        /// <param name="tokenId">Token ID</param>
        /// <returns>True if the NFT content is a video</returns>
        public async Task<bool> IsVideoNFT(string tokenId)
        {
            try
            {
                string imageUrl = await GetTokenImageUrl(tokenId);
                
                if (string.IsNullOrEmpty(imageUrl))
                {
                    // No image URL found - check if there's animation_url in metadata
                    string uri = await GetTokenURI(tokenId);
                    if (!string.IsNullOrEmpty(uri))
                    {
                        // Handle {id} placeholder for ERC-1155
                        if (uri.Contains("{id}"))
                        {
                            BigInteger tokenIdBigInt = BigInteger.Parse(tokenId);
                            string hexTokenId = tokenIdBigInt.ToString("x").TrimStart('0').PadLeft(64, '0');
                            uri = uri.Replace("{id}", hexTokenId);
                        }
                        
                        string metadataUrl = uri.StartsWith("ipfs://") 
                            ? uri.Replace("ipfs://", "https://ipfs.io/ipfs/") 
                            : uri;
                        
                        using (var httpClient = new HttpClient())
                        {
                            httpClient.Timeout = TimeSpan.FromSeconds(10);
                            string metadataJson = await httpClient.GetStringAsync(metadataUrl);
                            
                            if (!string.IsNullOrEmpty(metadataJson))
                            {
                                var metadata = JObject.Parse(metadataJson);
                                string animationUrl = metadata["animation_url"]?.ToString();
                                if (!string.IsNullOrEmpty(animationUrl))
                                {
                                    return true; // Has animation_url, likely video
                                }
                            }
                        }
                    }
                    return false;
                }
                
                // Check if the image URL points to a video file
                string lowerUrl = imageUrl.ToLower();
                return lowerUrl.EndsWith(".mp4") || 
                       lowerUrl.EndsWith(".webm") || 
                       lowerUrl.EndsWith(".mov") ||
                       lowerUrl.EndsWith(".avi") ||
                       lowerUrl.Contains("video");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NFTService] Error checking if video NFT for token {tokenId}: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Get NFT name from metadata
        /// </summary>
        /// <param name="tokenId">Token ID</param>
        /// <returns>NFT name from metadata, or token ID if not found</returns>
        public async Task<string> GetNFTName(string tokenId)
        {
            try
            {
                string uri = await GetTokenURI(tokenId);
                
                if (string.IsNullOrEmpty(uri))
                {
                    return $"NFT #{tokenId}";
                }
                
                // Handle {id} placeholder for ERC-1155
                if (uri.Contains("{id}"))
                {
                    BigInteger tokenIdBigInt = BigInteger.Parse(tokenId);
                    string hexTokenId = tokenIdBigInt.ToString("x").TrimStart('0').PadLeft(64, '0');
                    uri = uri.Replace("{id}", hexTokenId);
                }
                
                // Convert IPFS protocol to HTTP gateway
                string metadataUrl = uri.StartsWith("ipfs://") 
                    ? uri.Replace("ipfs://", "https://ipfs.io/ipfs/") 
                    : uri;
                
                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(10);
                    string metadataJson = await httpClient.GetStringAsync(metadataUrl);
                    
                    if (!string.IsNullOrEmpty(metadataJson))
                    {
                        var metadata = JObject.Parse(metadataJson);
                        string name = metadata["name"]?.ToString();
                        
                        if (!string.IsNullOrEmpty(name))
                        {
                            return name;
                        }
                    }
                }
                
                return $"NFT #{tokenId}";
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NFTService] Error getting NFT name for token {tokenId}: {ex.Message}");
                return $"NFT #{tokenId}";
            }
        }
        
        /// <summary>
        /// Load image as Texture2D from URL
        /// </summary>
        private async Task<Texture2D> LoadImageTexture(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
                return null;
                
            try
            {
                using (var httpClient = new HttpClient())
                {
                    byte[] imageData = await httpClient.GetByteArrayAsync(imageUrl);
                    
                    Texture2D texture = new Texture2D(2, 2);
                    texture.LoadImage(imageData);
                    
                    return texture;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading image from {imageUrl}: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Load image as Sprite from URL
        /// </summary>
        private async Task<Sprite> LoadImageSprite(string imageUrl)
        {
            Texture2D texture = await LoadImageTexture(imageUrl);
            if (texture == null)
                return null;
                
            return Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new UnityEngine.Vector2(0.5f, 0.5f)
            );
        }
    }
}

