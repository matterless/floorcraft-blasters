using System;
using System.Collections.Generic;
using Matterless.Inject;
using System.Threading.Tasks;
using UnityEngine;

namespace Matterless.Floorcraft
{
    public class WalletUiService
    {
        private readonly WalletService m_WalletService;
        private readonly AudioUiService m_AudioUiService;
        private readonly IAnalyticsService m_AnalyticsService;
        private readonly INotificationService m_NotificationService;
        private readonly WalletUiView m_View;
        
        // NFT sprite cache - key is token ID, value is sprite (null for video NFTs)
        private readonly Dictionary<string, Sprite> m_NFTSpriteCache = new Dictionary<string, Sprite>();
        
        // NFT name cache - key is token ID, value is name (for video NFTs that can't display images)
        private readonly Dictionary<string, string> m_NFTNameCache = new Dictionary<string, string>();

        public WalletUiService(WalletService walletService, AudioUiService audioUiService, IAnalyticsService analyticsService, INotificationService notificationService)
        {
            m_WalletService = walletService;
            m_AudioUiService = audioUiService;
            m_AnalyticsService = analyticsService;
            m_NotificationService = notificationService;

            // Create the view
            m_View = WalletUiView.Create("UIPrefabs/UIP_WalletView").Init();

            // Wire up events
            m_View.onConnectWalletButtonClicked += OnConnectWalletButtonClicked;
            m_View.onOpenWalletButtonClicked += OnOpenWalletButtonClicked;
            m_View.onHideWalletButtonClicked += OnHideWalletButtonClicked;
            m_View.onDisconnectWalletButtonClicked += OnDisconnectWalletButtonClicked;

            // Subscribe to wallet state changes
            m_WalletService.onWalletConnected += OnWalletConnected;
            m_WalletService.onWalletDisconnected += OnWalletDisconnected;
            m_WalletService.onModalStateChanged += OnModalStateChanged;
            m_WalletService.onNFTsLoaded += OnNFTsLoaded;

            // Hide by default - will be shown by UiFlowService when in Intro state
            m_View.Hide();

            if (m_WalletService.hasCachedSession)
            {
                m_View.SetConnectButtonVisibility(false);
                m_View.SetOpenWalletButtonVisibility(true);
                m_View.SetWalletAddress(m_WalletService.cachedWalletAddress);
                m_View.SetOpenWalletButtonInteractability(true);
                
                int nftCount = m_WalletService.GetOwnedNFTCount();
                m_View.InitializeNFTContainers(nftCount);
                LoadNFTImages();
            }
            else
            {
                m_View.SetConnectButtonVisibility(true);
                m_View.SetOpenWalletButtonVisibility(false);
            }
        }

        private void OnConnectWalletButtonClicked()
        {
            m_WalletService.Connect();
            m_AudioUiService.PlaySelectSound();
            m_View.SetConnectButtonInteractability(false);
        }

        private void OnOpenWalletButtonClicked()
        {
            // Open wallet info container
            m_AudioUiService.PlaySelectSound();
            m_View.ShowWalletInfo();
            
            // Re-display cached NFT images
            DisplayCachedNFTImages();
        }

        private void OnWalletConnected()
        {
            m_View.SetConnectButtonVisibility(false);
            m_View.SetOpenWalletButtonVisibility(true);

            string address = m_WalletService.GetConnectedAddress();
            m_View.SetWalletAddress(address);

            // Track wallet connection for user analytics
            m_AnalyticsService.SetWalletAddress(address);

            // Show wallet connected notification
            m_NotificationService.ShowMessage(NotificationType.WalletConnected);

            // Do NOT show wallet info automatically - only on Open Wallet button click
            m_View.HideWalletInfo();
            m_View.SetConnectButtonInteractability(true);
            m_View.SetOpenWalletButtonInteractability(true);
            
            ShowBalance();
            
            // NFT containers will be created when onNFTsLoaded fires (after cache is initialized)
        }
        
        /// <summary>
        /// Called when NFT cache has finished initializing (ownership checks complete)
        /// </summary>
        private void OnNFTsLoaded()
        {
            // Create NFT containers based on owned NFT count
            int nftCount = m_WalletService.GetOwnedNFTCount();
            
            
            m_View.InitializeNFTContainers(nftCount);
            
            // Load NFT images/names for display
            LoadNFTImages();
        }

        private void OnWalletDisconnected()
        {
            m_AnalyticsService.ClearWalletAddress();
            m_NotificationService.ShowMessage(NotificationType.WalletDisconnected);

            m_View.ClearNFTContainers();
            m_NFTSpriteCache.Clear();
            m_NFTNameCache.Clear();

            m_View.HideWalletInfo();
            m_View.SetConnectButtonVisibility(true);
            m_View.SetOpenWalletButtonVisibility(false);
            m_View.SetConnectButtonInteractability(true);
            m_View.SetOpenWalletButtonInteractability(false);
            m_View.ResetCanvasSortingOrder();
        }

        private async void ShowBalance(){
            // Fetch ETH balance
            string ethBalance = await m_WalletService.GetWalletNativeBalanceAsync();
            m_View.SetEthBalanceText(ethBalance);
            
            // Fetch AUKI balance
            string aukiBalance = await m_WalletService.GetAukiBalanceAsync();
            m_View.SetAukiBalanceText(aukiBalance);
        }
        
        private async void LoadNFTImages()
        {
            try
            {
                var erc1155TokenIds = m_WalletService.GetOwnedErc1155TokenIds();
                var erc721TokenIds = m_WalletService.GetOwnedErc721TokenIds();
                
                if (erc1155TokenIds.Count == 0 && erc721TokenIds.Count == 0)
                {
                    return;
                }
                
                // === Load ERC-1155 NFTs (Active/Primary) ===
                if (erc1155TokenIds.Count > 0)
                {
                    var nft1155Service = new NFTService(m_WalletService.chainSettings.nftContractAddress, m_WalletService.chainSettings.rpcUrl);
                    
                    foreach (var tokenId in erc1155TokenIds)
                    {
                        await LoadSingleNFT(tokenId, nft1155Service);
                    }
                }
                
                // === Load ERC-721 NFTs (Secondary) ===
                if (erc721TokenIds.Count > 0)
                {
                    var nft721Service = new NFT721Service(m_WalletService.chainSettings.nft721ContractAddress, m_WalletService.chainSettings.rpcUrl);
                    
                    foreach (var tokenId in erc721TokenIds)
                    {
                        await LoadSingleNFT(tokenId, nft721Service);
                    }
                }
                
                // Display cached images/names after all downloads complete
                DisplayCachedNFTImages();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading NFT images: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Load a single ERC-1155 NFT image directly. Falls back to text name only if image fails.
        /// </summary>
        private async Task LoadSingleNFT(string tokenId, NFTService nftService)
        {
            try
            {
                if (m_NFTSpriteCache.ContainsKey(tokenId) || m_NFTNameCache.ContainsKey(tokenId))
                {
                    return;
                }
                Sprite sprite = await nftService.LoadNFTImage(tokenId);
                
                if (sprite != null)
                {
                    m_NFTSpriteCache[tokenId] = sprite;
                }
                else
                {
                    string nftName = await nftService.GetNFTName(tokenId);
                    m_NFTNameCache[tokenId] = nftName;
                    Debug.LogWarning($"Failed to load image for ERC-1155 token {tokenId}, using name: {nftName}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load ERC-1155 NFT data for token {tokenId}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Load a single NFT using the ERC-721 service (handles video/image/text fallback)
        /// </summary>
        private async Task LoadSingleNFT(string tokenId, NFT721Service nftService)
        {
            try
            {
                if (m_NFTSpriteCache.ContainsKey(tokenId) || m_NFTNameCache.ContainsKey(tokenId))
                {
                    return;
                }
                
                // Check if this is a video NFT
                bool isVideo = await nftService.IsVideoNFT(tokenId);
                
                if (isVideo)
                {
                    string nftName = await nftService.GetNFTName(tokenId);
                    m_NFTNameCache[tokenId] = nftName;
                }
                else
                {
                    Sprite sprite = await nftService.LoadNFTImage(tokenId);
                    if (sprite != null)
                    {
                        m_NFTSpriteCache[tokenId] = sprite;
                    }
                    else
                    {
                        string nftName = await nftService.GetNFTName(tokenId);
                        m_NFTNameCache[tokenId] = nftName;
                        Debug.LogWarning($"Failed to load sprite for ERC-721 token {tokenId}, using name: {nftName}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load ERC-721 NFT data for token {tokenId}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Display cached NFT images or names in containers
        /// </summary>
        private void DisplayCachedNFTImages()
        {
            try
            {
                var ownedTokenIds = m_WalletService.GetOwnedTokenIds();
                
                if (ownedTokenIds.Count == 0 || m_View.m_InstantiatedContainersPublic.Count == 0)
                {
                    return;
                }
                
                int displayCount = Mathf.Min(ownedTokenIds.Count, m_View.m_InstantiatedContainersPublic.Count);
                
                for (int i = 0; i < displayCount; i++)
                {
                    string tokenId = ownedTokenIds[i];
                    
                    // Check if sprite is cached (image NFT)
                    if (m_NFTSpriteCache.TryGetValue(tokenId, out Sprite sprite))
                    {
                        m_View.SetNFTImage(i, sprite);
                    }
                    // Check if name is cached (video NFT or failed image load)
                    else if (m_NFTNameCache.TryGetValue(tokenId, out string nftName))
                    {
                        m_View.SetNFTText(i, nftName);
                    }
                    else
                    {
                        Debug.LogWarning($"No cached data for token {tokenId}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error displaying cached NFT data: {ex.Message}");
            }
        }

        public void Show()
        {
            m_View.Show();
        }

        public void Hide()
        {
            m_View.Hide();
        }

        private void OnHideWalletButtonClicked()
        {
            // Hide wallet info container
            m_AudioUiService.PlaySelectSound();
            m_View.HideWalletInfo();
        }

        private void OnDisconnectWalletButtonClicked()
        {
            m_AudioUiService.PlaySelectSound();
            m_View.HideWalletInfo();
            m_WalletService.Disconnect();
        }

        private void OnModalStateChanged(bool isOpen)
        {
            m_View.SetConnectButtonInteractability(!isOpen);
            m_View.SetOpenWalletButtonInteractability(!isOpen);
        }
    }
}