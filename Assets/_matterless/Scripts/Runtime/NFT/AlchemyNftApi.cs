using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Matterless.Floorcraft
{
    /// <summary>
    /// Alchemy NFT API helper for "owns any ERC-1155 from contract" checks.
    /// Uses getNFTsForOwner so any token ID from the contract counts.
    /// </summary>
    public static class AlchemyNftApi
    {
        private const string BaseMainnetNftBaseUrl = "https://base-mainnet.g.alchemy.com/nft/v3";

        /// <summary>
        /// Check if an address owns any ERC-1155 (or ERC-721) from the given contract.
        /// Uses Alchemy's getNFTsForOwner; token ID does not matter.
        /// </summary>
        /// <param name="ownerAddress">Wallet address (e.g. 0x...)</param>
        /// <param name="contractAddress">ERC-1155 contract address</param>
        /// <param name="apiKey">Alchemy API key</param>
        /// <returns>True if owner has at least one NFT from the contract</returns>
        public static async Task<bool> OwnsAnyNftFromContractAsync(string ownerAddress, string contractAddress, string apiKey)
        {
            var result = await GetOwnedNftsFromContractAsync(ownerAddress, contractAddress, apiKey);
            return result.OwnsAny;
        }

        /// <summary>
        /// Get owned NFT token IDs from a contract for an address (via Alchemy getNFTsForOwner).
        /// </summary>
        /// <param name="ownerAddress">Wallet address</param>
        /// <param name="contractAddress">Contract address</param>
        /// <param name="apiKey">Alchemy API key</param>
        /// <returns>OwnsAny and list of owned token IDs (can be empty)</returns>
        public static async Task<(bool OwnsAny, List<string> TokenIds)> GetOwnedNftsFromContractAsync(
            string ownerAddress,
            string contractAddress,
            string apiKey)
        {
            var tokenIds = new List<string>();
            if (string.IsNullOrEmpty(ownerAddress) || string.IsNullOrEmpty(contractAddress) ||
                string.IsNullOrEmpty(apiKey) || apiKey.StartsWith("Get your"))
            {
                return (false, tokenIds);
            }

            try
            {
                string url = $"{BaseMainnetNftBaseUrl}/{apiKey.Trim()}/getNFTsForOwner" +
                    $"?owner={Uri.EscapeDataString(ownerAddress)}" +
                    $"&contractAddresses[]={Uri.EscapeDataString(contractAddress)}" +
                    "&withMetadata=false";

                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(15);
                    string json = await httpClient.GetStringAsync(url);
                    var root = JObject.Parse(json);

                    int totalCount = root["totalCount"]?.Value<int>() ?? 0;
                    var ownedNfts = root["ownedNfts"] as JArray;
                    if (ownedNfts != null)
                    {
                        foreach (var nft in ownedNfts)
                        {
                            var tokenId = nft["tokenId"]?.ToString();
                            if (!string.IsNullOrEmpty(tokenId))
                                tokenIds.Add(tokenId);
                        }
                    }

                    return (totalCount > 0 || tokenIds.Count > 0, tokenIds);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AlchemyNftApi] getNFTsForOwner failed: {ex.Message}");
                return (false, tokenIds);
            }
        }
    }
}
