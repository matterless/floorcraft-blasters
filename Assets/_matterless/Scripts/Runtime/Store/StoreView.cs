using System;
using System.Collections;
using TMPro;
using UnityEngine;
using Button = Matterless.Module.UI.Button;

namespace Matterless.Floorcraft
{
    public class StoreView : UIView<StoreView>
    {
        //[SerializeField] private GameObject m_Panel;
        [SerializeField] private GameObject m_PremiumPopUpPanel;
        [SerializeField] private Button m_PurchaseButton;
        [SerializeField] private Button m_RestoreButton;
        [SerializeField] private Button m_CloseButton;
        [SerializeField] private TextMeshProUGUI m_PurchaseDescriptionLabel;

        [Header("Premium Art")] 
        [SerializeField] RectTransform m_premiumArtRectTransform;
        private Vector2 m_CashedPremiumArtPosition;
        private Coroutine m_premiumArtCoroutine;
        
        [Header("UI Lock")]
        [SerializeField] private GameObject m_LockPanel;
        [SerializeField] private RectTransform m_ProcessingPaymentIcon;
        private Coroutine m_ProcessingPaymentIconCoroutine;

        [Header("Transition")]
        [SerializeField] private GameObject m_PurchaseSuccessfulTransition;
        [SerializeField] private Button m_ClosePurchaseSuccessfulButton;


        
        private Action m_OnPurchase;
        private Action m_OnRestore;
        public event Action onClose;
        public event Action onClosePurchaseSuccessful;

        public static StoreView Create(Action onPurchase, Action onRestore)
            => Instantiate(Resources.Load<StoreView>("UIPrefabs/UIP_StoreView")).Init(onPurchase, onRestore);

        public void Show(string purchaseDescription)
        {
            m_PurchaseDescriptionLabel.text = purchaseDescription;
            base.Show();
            //m_Panel.SetActive(true);
            //m_PremiumPopUpPanel.SetActive(true);
            Unlock();
            
            if (m_premiumArtCoroutine != null)
                StopCoroutine(m_premiumArtCoroutine);
            
            m_premiumArtCoroutine = StartCoroutine(AnimatePremiumArt());
        }
        
        public void Lock()
        {
            m_LockPanel.SetActive(true);
            
            if (m_ProcessingPaymentIconCoroutine != null)
                StopCoroutine(m_ProcessingPaymentIconCoroutine);
            m_ProcessingPaymentIconCoroutine = StartCoroutine(AnimateProcessingPaymentIcon());
        }

        public void Unlock()
        {
            m_LockPanel.SetActive(false);
            
            if (m_ProcessingPaymentIconCoroutine == null)
                return;
            StopCoroutine(m_ProcessingPaymentIconCoroutine);
        }

        private StoreView Init(Action onPurchase, Action onRestore)
        {
            m_OnPurchase = onPurchase;
            m_OnRestore = onRestore;
            m_PurchaseButton.onClick.AddListener(Purchase);
            m_RestoreButton.onClick.AddListener(Restore);
            m_CloseButton.onClick.AddListener(Hide);
            m_CloseButton.onClick.AddListener(OnClose);
            m_ClosePurchaseSuccessfulButton.onClick.AddListener(OnClosePurchaseSuccessful);
            m_CashedPremiumArtPosition = m_premiumArtRectTransform.anchoredPosition;
            Debug.Log(m_CashedPremiumArtPosition);
         
            return this;
        }
        private void OnClosePurchaseSuccessful() => onClosePurchaseSuccessful?.Invoke();

        private void Purchase() => m_OnPurchase.Invoke();
        private void Restore() => m_OnRestore.Invoke();
        
        private void OnClose() => onClose?.Invoke();
        
        private IEnumerator AnimateProcessingPaymentIcon()
        {
            while (true)
            {
                m_ProcessingPaymentIcon.Rotate(0, 0, -5);
                yield return null;
            }
        }
        
        private IEnumerator AnimatePremiumArt()
        {
            //sin animation
            while (true)
            {
                m_premiumArtRectTransform.anchoredPosition = m_CashedPremiumArtPosition + new Vector2(0, Mathf.Sin(Time.time) * 50);
                yield return null;
            }
        }
        
        public void ShowTransitionVFX()
        {
            m_PremiumPopUpPanel.SetActive(false);
            m_PurchaseSuccessfulTransition.SetActive(true);
        }
    }
}