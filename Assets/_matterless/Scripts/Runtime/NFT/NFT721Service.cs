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
    /// High-level NFT service for ERC-721 tokens.
    /// Handles game integration and business logic.
    /// Uses ERC721Contract for blockchain communication.
    /// </summary>
    public class NFT721Service
    {
        private readonly ERC721Contract m_Contract;
        
        /// <summary>
        /// Initialize NFT721 Service
        /// </summary>
        /// <param name="contract">ERC721Contract instance</param>
        public NFT721Service(ERC721Contract contract)
        {
            m_Contract = contract;
        }
        
        /// <summary>
        /// Initialize NFT721 Service with contract address and RPC URL
        /// </summary>
        /// <param name="contractAddress">Your ERC-721 contract address</param>
        /// <param name="rpcUrl">RPC endpoint with API key</param>
        public NFT721Service(string contractAddress, string rpcUrl)
            : this(new ERC721Contract(contractAddress, rpcUrl))
        {
        }
        
        /// <summary>
        /// Check if a specific token is owned by an address.
        /// For ERC-721, we call ownerOf(tokenId) and compare addresses.
        /// </summary>
        /// <param name="ownerAddress">Wallet address to check</param>
        /// <param name="tokenId">Token ID to verify</param>
        /// <returns>True if owner owns the token</returns>
        public async Task<bool> OwnsToken(string ownerAddress, string tokenId)
        {
            try
            {
                var actualOwner = await m_Contract.OwnerOf(BigInteger.Parse(tokenId));
                return actualOwner.Equals(ownerAddress, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NFT721Service] Error checking token ownership: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Check if an address owns ANY token from this collection.
        /// This is the preferred method for ERC-721 gated content.
        /// </summary>
        /// <param name="ownerAddress">Wallet address to check</param>
        /// <returns>True if owner owns at least one NFT from this collection</returns>
        public async Task<bool> OwnsAnyToken(string ownerAddress)
        {
            try
            {
                var balance = await m_Contract.BalanceOf(ownerAddress);
                return balance > 0;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NFT721Service] Error checking if owns any token: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Get the number of NFTs owned by an address
        /// </summary>
        /// <param name="ownerAddress">Wallet address to check</param>
        /// <returns>Number of NFTs owned</returns>
        public async Task<int> GetBalance(string ownerAddress)
        {
            try
            {
                var balance = await m_Contract.BalanceOf(ownerAddress);
                return (int)balance;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NFT721Service] Error getting balance: {ex.Message}");
                return 0;
            }
        }
        
        /// <summary>
        /// Get all token IDs owned by an address.
        /// Requires ERC721Enumerable extension.
        /// </summary>
        /// <param name="ownerAddress">Wallet address to check</param>
        /// <returns>List of token IDs as strings</returns>
        public async Task<List<string>> GetOwnedTokenIds(string ownerAddress)
        {
            var tokenIds = new List<string>();
            
            try
            {
                var balance = await m_Contract.BalanceOf(ownerAddress);
                
                for (int i = 0; i < (int)balance; i++)
                {
                    try
                    {
                        var tokenId = await m_Contract.TokenOfOwnerByIndex(ownerAddress, i);
                        tokenIds.Add(tokenId.ToString());
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[NFT721Service] tokenOfOwnerByIndex failed at index {i}: {ex.Message}");
                        // Contract may not support ERC721Enumerable
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NFT721Service] Error getting owned token IDs: {ex.Message}");
            }
            
            return tokenIds;
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
                return uri;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NFT721Service] Error getting tokenURI for {tokenId}: {ex.Message}");
                return string.Empty;
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
                    Debug.LogWarning($"[NFT721Service] No image URL for token {tokenId}");
                    return null;
                }
                
                Sprite sprite = await LoadImageSprite(imageUrl);
                return sprite;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NFT721Service] Error loading NFT image for token {tokenId}: {ex.Message}");
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
                    Debug.LogWarning($"[NFT721Service] Empty URI for token {tokenId}");
                    return string.Empty;
                }
                
                // Convert IPFS protocol to HTTP gateway
                if (uri.StartsWith("ipfs://"))
                {
                    uri = uri.Replace("ipfs://", "https://ipfs.io/ipfs/");
                }
                
                // Try multiple IPFS gateways
                string[] ipfsGateways = new string[] 
                {
                    "https://cloudflare-ipfs.com/ipfs/",
                    "https://dweb.link/ipfs/",
                    "https://ipfs.io/ipfs/",
                    "https://gateway.pinata.cloud/ipfs/"
                };
                
                // Extract IPFS hash if present
                string ipfsHash = null;
                if (uri.Contains("/ipfs/"))
                {
                    ipfsHash = uri.Substring(uri.IndexOf("/ipfs/") + 6);
                }
                
                // If it's not an IPFS URL, try to fetch directly
                if (ipfsHash == null)
                {
                    return await FetchImageUrlFromMetadata(uri);
                }
                
                // Try each gateway
                foreach (var gateway in ipfsGateways)
                {
                    try
                    {
                        string gatewayUrl = gateway + ipfsHash;
                        string imageUrl = await FetchImageUrlFromMetadata(gatewayUrl);
                        
                        if (!string.IsNullOrEmpty(imageUrl))
                        {
                            return imageUrl;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[NFT721Service] Gateway {gateway} failed: {ex.Message}");
                        // Continue to next gateway
                    }
                }
                
                Debug.LogWarning("[NFT721Service] All IPFS gateways failed");
                return string.Empty;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NFT721Service] Error getting image URL for token {tokenId}: {ex.Message}");
                return string.Empty;
            }
        }
        
        /// <summary>
        /// Fetch the image URL from metadata JSON
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
                    
                    // Handle IPFS URLs in image field
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
                    // No image URL found - check if there's animation_url
                    string uri = await GetTokenURI(tokenId);
                    if (!string.IsNullOrEmpty(uri))
                    {
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
                Debug.LogError($"[NFT721Service] Error checking if video NFT for token {tokenId}: {ex.Message}");
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
                Debug.LogError($"[NFT721Service] Error getting NFT name for token {tokenId}: {ex.Message}");
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
                Debug.LogError($"[NFT721Service] Error loading image from {imageUrl}: {ex.Message}");
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
