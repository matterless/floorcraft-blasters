using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

namespace Matterless.Floorcraft
{
    public class InAppPurchaseService : IStoreListener
    {
        public static string BLASTERS_PREMIUM_PRODUCT_ID = "floorcraft.blasters.premium";
        
        private IStoreController m_StoreController;
        private IExtensionProvider m_ExtensionProvider;
        private string m_CurrentProductId;
        private Action m_OnSucceed;
        private Action<string> m_OnFailed;
        public Action onInitialized;
        private bool m_IsInitialized = false;
        
        public ProductCollection products => m_StoreController.products;
        public string GetPriceString(string productId) => products.WithID(productId).metadata.localizedPriceString;
        public bool isInitialized => m_IsInitialized;
        
        public InAppPurchaseService()
        {
            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
            builder.AddProduct(BLASTERS_PREMIUM_PRODUCT_ID, ProductType.NonConsumable, new IDs
            {
                {BLASTERS_PREMIUM_PRODUCT_ID, AppleAppStore.Name},
                {BLASTERS_PREMIUM_PRODUCT_ID, GooglePlay.Name}
            });
            // Initialize Unity IAP...
            UnityPurchasing.Initialize (this, builder);

#if UNITY_EDITOR
            StandardPurchasingModule.Instance().useFakeStoreUIMode = FakeStoreUIMode.DeveloperUser;
            StandardPurchasingModule.Instance().useFakeStoreAlways = true;
#endif
        }

        public event Action<string> onPurchaseCompleted;

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            m_StoreController = controller;
            m_ExtensionProvider = extensions;
            m_IsInitialized = true;
            onInitialized?.Invoke();
        }

        public void RestorePurchases(Action onSuccess, Action<string> onFail)
        {
            m_ExtensionProvider.GetExtension<IAppleExtensions>().RestoreTransactions(
                (result, error) =>
                {
                    if (result)
                    {
                        // This restores everything. It will also be a success if nothing restored but the request hasn't failed.
                        onSuccess.Invoke();
                    }
                    else
                        onFail(error);
                });
        }

        public void OnInitializeFailed(InitializationFailureReason error)
        {
            throw new Exception("IAPService initialize failed");
        }

        public void OnInitializeFailed(InitializationFailureReason error, string? message)
        {
            throw new Exception($"IAPService initialize failed: {message}");
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs purchaseEvent)
        {
            Debug.Log($"ProcessPurchase: {purchaseEvent.purchasedProduct.definition.id}");
            
            //TODO:: implement on purchase action
            m_OnSucceed?.Invoke();
            m_OnSucceed = null;
            m_OnFailed = null;
            onPurchaseCompleted?.Invoke(purchaseEvent.purchasedProduct.definition.id);
            
            return PurchaseProcessingResult.Complete;
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            m_OnFailed.Invoke(failureReason.ToString());
            m_OnSucceed = null;
            m_OnFailed = null;
        }

        // =========================================================================================

        public bool IsPurchased(string productId)
        {
            if (m_StoreController != null)
            {
                // Fetch the currency Product reference from Unity Purchasing
                Product product = m_StoreController.products.WithID(productId);
                if (product != null)
                {
                    
                    return product.hasReceipt;
                }
            }

            return false;
        }
        
        public void InitiatePurchase(Product product, string payload)
        {
            m_StoreController.InitiatePurchase(product, payload);
        }

        public void InitiatePurchase(string productId, string payload)
        {
            m_StoreController.InitiatePurchase(productId, payload);
        }

        public void InitiatePurchase(Product product)
        {
            m_StoreController.InitiatePurchase(product);
        }

        private void InitiatePurchase(string productId)
        {
            m_StoreController.InitiatePurchase(productId);
        }

        public void InitiatePurchase(string productId, Action onSucceed, Action<string> onFailed)
        {
            m_CurrentProductId = productId;
            m_OnSucceed = onSucceed;
            m_OnFailed = onFailed;
            InitiatePurchase(productId);
        }
    }
}