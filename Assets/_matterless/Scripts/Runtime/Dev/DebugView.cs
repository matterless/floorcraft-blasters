using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace Matterless.Floorcraft
{
    public class DebugView : MonoBehaviour
    {
        #region Factory
        public static DebugView Create() => Instantiate(Resources.Load<DebugView>("UIPrefabs/UIP_DebugView")).Init();
        #endregion

        public bool isShown => m_Panel.gameObject.active;
        
        #region Inspector
        [SerializeField, FormerlySerializedAs("m_Pane")] private GameObject m_Panel;
        [SerializeField] private Button m_CloseButton;
        [SerializeField] private Button m_LogShareButton;

        [Header("Tabs")] [SerializeField] private List<Toggle> m_Toggles;
        [SerializeField] private List<GameObject> m_Panels;

        [Header("Overlays")] [SerializeField] private Text m_PlaneDetectionMode;
        [SerializeField] private Toggle m_PlanesOverlayToggle;

        [Header("Domains")]
        [SerializeField] private Button m_CreateDomainAssetButton;
        [SerializeField] private Button m_DeleteAllDomainAssetsButton;

        [Header("Materials")]
        [SerializeField] private Toggle m_SeeThroughToggle;
        [SerializeField] private Toggle m_UseDiffuseMatcapToggle;
        [SerializeField] private Toggle m_UseSpecularMatcapToggle;
        [SerializeField] private Slider m_DiffuseArrayIndexSlider;
        [SerializeField] private Slider m_SpecularArrayIndexSlider;
        [SerializeField] private Slider m_DiffuseMatcapStrengthSlider;
        [SerializeField] private Slider m_SpecularMatcapStrengthSlider;
        [SerializeField] private Slider m_DetailNormalMapArrayIndexSlider;
        [SerializeField] private Slider m_DetailNormalMapStrengthSlider;

        [SerializeField] private List<PowerUpToggleView> m_PowerUpToggleViews; 
        [SerializeField] private Toggle m_GameplayTabToggle;
        [SerializeField] private PowerUpToggleView m_PowerUpToggleViewTemplate;
        [SerializeField] private ToggleGroup m_PowerUpToggleGroup;
        
        #endregion
        
        private ARPlaneManager m_ARPlaneManager;
        private Action m_OnMaterialTabToggle;
        private Action m_OnGameplayTabToggle;
        private Action<bool> m_OnPlanesOverlayToggle;
        private Action<bool> m_OnUseDiffuseMatcapToggle;
        private Action<bool> m_OnUseSpecularMatcapToggle;
        private Action<int>  m_OnDiffuseArrayIndexSliderValueChangeAction;
        private Action<int>  m_OnSpecularArrayIndexSliderValueChangeAction;
        private Action<float> m_OnDiffuseMatcapStrengthSliderValueChangeAction;
        private Action<float> m_OnSpecularMatcapStrengthSliderValueChangeAction;
        private Action<int>  m_OnDetailNormalMapArrayIndexSliderValueChangeAction;
        private Action<float> m_OnDetailNormalMapStrengthSliderValueChangeAction;
        private Action m_LogShare;

        private Action m_OpenSettingMenuAction;

        public void RegisterDomainButtonsAction(Action createAsset, Action deleteAll)
        {
            m_CreateDomainAssetButton.onClick.AddListener(() => createAsset.Invoke());
            m_DeleteAllDomainAssetsButton.onClick.AddListener(() => deleteAll.Invoke());
        }

        public void RegisterSetEquipmentToggles(Action<EquipmentState> action, List<EquipmentState> equpimentStates)
        {
            foreach (var equpimentState in equpimentStates)
            {
                var powerUpToggleView = Instantiate(m_PowerUpToggleViewTemplate,m_PowerUpToggleGroup.transform);
                powerUpToggleView.equipmentState = equpimentState;
                powerUpToggleView.toggle.group = m_PowerUpToggleGroup;
                powerUpToggleView.toggle.onValueChanged.AddListener((isOn) => powerUpToggleView.OnValueChanged(isOn, action));
                powerUpToggleView.labelView.text = equpimentState.ToString();
            }
        }
         
        public void RegisterPlanesOverlayToggleAction(Action<bool> action) => m_OnPlanesOverlayToggle = action;
        public void RegisterMaterialTabToggleAction(Action action) => m_OnMaterialTabToggle = action;
        public void RegisterGameplayTabToggleAction(Action action) => m_OnGameplayTabToggle = action;
        public void RegisterOnDiffuseMatcapToggleAction(Action<bool> action) => m_OnUseDiffuseMatcapToggle = action;
        public void RegisterOnSpecularMatcapToggleAction(Action<bool> action) => m_OnUseSpecularMatcapToggle = action;
        public void RegisterOnDiffuseArrayIndexSliderValueChangeAction(Action<int> action) => m_OnDiffuseArrayIndexSliderValueChangeAction = action;
        public void RegisterOnSpecularArrayIndexSliderValueChangeAction(Action<int> action) => m_OnSpecularArrayIndexSliderValueChangeAction = action;
        public void RegisterOnDiffuseMatcapStrengthSliderValueChangeAction(Action<float> action) => m_OnDiffuseMatcapStrengthSliderValueChangeAction = action;
        public void RegisterOnSpecularMatcapStrengthSliderValueChangeAction(Action<float> action) => m_OnSpecularMatcapStrengthSliderValueChangeAction = action;
        public void RegisterOnDetailNormalMapArrayIndexSliderValueChangeAction(Action<int> action) => m_OnDetailNormalMapArrayIndexSliderValueChangeAction = action;
        public void RegisterOnDetailNormalMapStrengthSliderValueChangeAction(Action<float> action) => m_OnDetailNormalMapStrengthSliderValueChangeAction = action;
        public void RegisterLogShare(Action action) => m_LogShare = action;
        public void RegisterOpenSettingMenu(Action action) => m_OpenSettingMenuAction = action;
        
        private void Update()
        {
            PlaneDetectionModeUpdate();
        }

        private void PlaneDetectionModeUpdate()
        {
            if (m_ARPlaneManager == null)
                m_ARPlaneManager = FindObjectOfType<ARPlaneManager>();
            
            if(m_ARPlaneManager == null)
                return;

            //m_ARPlaneManager.requestedDetectionMode = PlaneDetectionMode.
            m_PlaneDetectionMode.text = m_ARPlaneManager.currentDetectionMode.ToString();
        }

        private DebugView Init()
        {
            m_CloseButton.onClick.AddListener(Hide);
            m_LogShareButton.onClick.AddListener(() => m_LogShare.Invoke());

            for (int i = 0; i < m_Toggles.Count; i++)
            {
                var panel = m_Panels[i];
                panel.SetActive(false);
                m_Toggles[i].onValueChanged.AddListener(panel.SetActive);
            }

            m_Toggles[0].isOn = true;
            m_Panels[0].SetActive(true);
            

            // overlays init
            m_PlanesOverlayToggle.onValueChanged.AddListener((x)=> m_OnPlanesOverlayToggle(x));
            
            // m_MaterialTabToggle.onValueChanged.AddListener((x) =>
            // {
            //     m_MaterialPanelBody.gameObject.SetActive(x);
            //     if (x)
            //     {
            //         m_OnMaterialTabToggle();
            //         if (m_SeeThroughToggle.isOn)
            //             SetSeeThrough(true);
            //         return;
            //     }
            //     
            //     SetSeeThrough(false);
            //     
            // });

            // material init
            //m_SeeThroughToggle.onValueChanged.AddListener((x) => SetSeeThrough(x));
            m_UseDiffuseMatcapToggle.onValueChanged.AddListener((x) => m_OnUseDiffuseMatcapToggle(x));
            m_UseSpecularMatcapToggle.onValueChanged.AddListener((x) => m_OnUseSpecularMatcapToggle(x));
            m_DiffuseArrayIndexSlider.onValueChanged.AddListener((x) => m_OnDiffuseArrayIndexSliderValueChangeAction((int) x));
            m_SpecularArrayIndexSlider.onValueChanged.AddListener((x) => m_OnSpecularArrayIndexSliderValueChangeAction((int) x));
            m_DiffuseMatcapStrengthSlider.onValueChanged.AddListener((x) => m_OnDiffuseMatcapStrengthSliderValueChangeAction(x));
            m_SpecularMatcapStrengthSlider.onValueChanged.AddListener((x) => m_OnSpecularMatcapStrengthSliderValueChangeAction(x));
            m_DetailNormalMapArrayIndexSlider.onValueChanged.AddListener((x) => m_OnDetailNormalMapArrayIndexSliderValueChangeAction((int) x));
            m_DetailNormalMapStrengthSlider.onValueChanged.AddListener((x) => m_OnDetailNormalMapStrengthSliderValueChangeAction(x));
            return this;
        }

        [ContextMenu("Show")]
        public void Show()
        {
            m_Panel.SetActive(true);
        }

        public void Hide()
        {
            m_Panel.SetActive(false);
        }
        
        public void UpdateMaterialPanel(MaterialDebugService.MaterialProperties materialProperties)
        {
            m_UseDiffuseMatcapToggle.SetIsOnWithoutNotify(materialProperties.UseDiffuseMatcap);
            m_UseSpecularMatcapToggle.SetIsOnWithoutNotify(materialProperties.UseSpecularMatcap);
            m_DiffuseArrayIndexSlider.SetValueWithoutNotify(materialProperties.DiffuseMatcapArrayIndex);
            m_SpecularArrayIndexSlider.SetValueWithoutNotify(materialProperties.SpecularMatcapArrayIndex);
            m_DiffuseMatcapStrengthSlider.SetValueWithoutNotify(materialProperties.DiffuseMatcapStrength);
            m_SpecularMatcapStrengthSlider.SetValueWithoutNotify(materialProperties.SpecularMatcapStrength);
            m_DetailNormalMapArrayIndexSlider.SetValueWithoutNotify(materialProperties.DetailNormalMapArrayIndex);
            m_DetailNormalMapStrengthSlider.SetValueWithoutNotify(materialProperties.DetailNormalMapStrength);
        }
        
        public void UpdateGameplayPanel(EquipmentState modelState)
        {
            foreach (var powerUpToggleView in m_PowerUpToggleViews)
            {
                powerUpToggleView.toggle.isOn = false;
            }
            if (TryGetToggleView(modelState, out var toggleView))
            {
                toggleView.toggle.isOn = true;
            }
        }
        
        private bool TryGetToggleView(EquipmentState equipmentState, out PowerUpToggleView toggle)
        {
            foreach (var powerUpToggleView in m_PowerUpToggleViews)
            {
                if (powerUpToggleView.equipmentState == equipmentState)
                {
                    toggle = powerUpToggleView;
                    return true;
                }
            }
            toggle = null;
            return false;
        }
        
        // private void SetSeeThrough(bool on)
        // {
        //     Transform parent = m_MaterialPanelBody.transform;
        //     while (!parent.name.StartsWith("UIP_DebugView"))
        //     {
        //         if (parent.TryGetComponent(out Image image))
        //         {
        //             image.enabled = !on;
        //         }
        //         parent = parent.transform.parent;
        //     }
        // }
    }
}