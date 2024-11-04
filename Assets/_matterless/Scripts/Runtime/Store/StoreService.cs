using System;
using Matterless.Audio;
using Matterless.Localisation;
using UnityEngine;

namespace Matterless.Floorcraft
{
    public class StoreService : IStoreService
    {
        private const string PURCHASE_DESCRIPTION_LABEL = "PURCHASE_DESCRIPTION_LABEL";
        
        private readonly StoreView m_View;
        private readonly InAppPurchaseService m_IAPService;
        private readonly ILocalisationService m_LocalisationService;
        private readonly IPlayerPrefsService m_PlayerPrefsService;
        private readonly AudioUiService m_AudioUiService;
        private const bool PREMIUM_ENABLED = false;
        public StoreService(InAppPurchaseService inAppPurchaseService, ILocalisationService localisationService, IPlayerPrefsService playerPrefsService , AudioUiService audioUiService)
        {
            // in editor clear the value every time we start the app
            #if UNITY_EDITOR
            playerPrefsService.SetBool(InAppPurchaseService.BLASTERS_PREMIUM_PRODUCT_ID, false);
            #endif
            
            // restore from local
            isPremiumUnlocked = playerPrefsService.GetBool(InAppPurchaseService.BLASTERS_PREMIUM_PRODUCT_ID, false);
            Debug.Log($"isPremiumUnlocked: {isPremiumUnlocked}");

            if (isPremiumUnlocked)
                return;

            m_IAPService = inAppPurchaseService;
            m_LocalisationService = localisationService;
            m_PlayerPrefsService = playerPrefsService;
            m_AudioUiService = audioUiService;
            m_View = StoreView.Create(PurchasePremium,RestorePremium);
            localisationService.RegisterUnityUIComponents(m_View.gameObject);
            inAppPurchaseService.onPurchaseCompleted += OnPurchaseCompleted;
            inAppPurchaseService.onInitialized += OnIAPInitialized;
            
            Hide();
            m_View.onClose += Hide;
            m_View.onClose += m_AudioUiService.PlayBackSound;
            m_View.onClosePurchaseSuccessful += Hide;
            m_View.onClosePurchaseSuccessful += m_AudioUiService.PlayBackSound;
            // we need this here as well because the view is created in the constructor
            // and the constructor is called in the Application Context
            m_View.Unlock();
        }

        public bool isPremiumUnlocked { get; private set; } = false;
        public bool premiumEnabled => PREMIUM_ENABLED;

        public event Action onPremiumUnlocked;

        public void Show() => m_View.Show(
            m_LocalisationService.Translate(
                PURCHASE_DESCRIPTION_LABEL, 
                m_IAPService.GetPriceString(InAppPurchaseService.BLASTERS_PREMIUM_PRODUCT_ID)));

        public void Hide()
        {
            m_View?.Hide();
        }

        private void OnIAPInitialized()
        {
            // Restoring purchases in the beginning
            RestorePremiumOnInit();
        }

        private void PurchasePremium()
        {
            Debug.Log("PurchasePremium");
            
            m_View.Lock();
            
            m_IAPService.InitiatePurchase(
                InAppPurchaseService.BLASTERS_PREMIUM_PRODUCT_ID,
                OnBlasterPremiumProductPurchased,
                OnBlasterPremiumProductPurchasedFailed
                );
            m_AudioUiService.PlaySelectSound();
        }

        private void OnPurchaseCompleted(string id)
        {
            if(id != InAppPurchaseService.BLASTERS_PREMIUM_PRODUCT_ID)
                return;
            
            Debug.Log("Premium unlocked");
            isPremiumUnlocked = true;
            m_PlayerPrefsService.SetBool(InAppPurchaseService.BLASTERS_PREMIUM_PRODUCT_ID, true);
            onPremiumUnlocked?.Invoke();
        }

        private void OnBlasterPremiumProductPurchased()
        {
            m_View.Unlock();
            m_View.ShowTransitionVFX();
            //m_View.Hide();
            m_AudioUiService.PlayCarSpawnSound();
        }
        
        private void OnBlasterPremiumProductRestored()
        {
            if (m_IAPService.IsPurchased(InAppPurchaseService.BLASTERS_PREMIUM_PRODUCT_ID))
            {
                OnPurchaseCompleted(InAppPurchaseService.BLASTERS_PREMIUM_PRODUCT_ID);
            }

            m_View.Unlock();
        }

        private void OnBlasterPremiumProductPurchasedFailed(string error)
        {
            Debug.Log($"Purchase failed: {error}");
            m_View.Unlock();
            //m_View.Hide();
            m_AudioUiService.PlayBackSound();
        }
        
        private void OnBlasterPremiumProductRestoreFailed(string error)
        {
            Debug.Log($"Restore failed: {error}");
            m_View.Unlock();
            m_AudioUiService.PlayBackSound();
        }
        
        private void RestorePremium()
        {
            m_View.Lock();
            m_IAPService.RestorePurchases(
                OnBlasterPremiumProductRestored,
                OnBlasterPremiumProductRestoreFailed);
            m_AudioUiService.PlaySelectSound();

        }
        
        private void RestorePremiumOnInit()
        {
            m_IAPService.RestorePurchases(
                OnBlasterPremiumProductRestored,
                OnBlasterPremiumProductRestoreFailed);
        }
        
        
    }
}