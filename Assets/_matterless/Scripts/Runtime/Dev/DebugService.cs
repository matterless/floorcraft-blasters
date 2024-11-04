using System;
using System.IO;
using System.Collections.Generic;
using Matterless.Inject;
using UnityEngine;

namespace Matterless.Floorcraft
{
    public class DebugService : ITickable
    {
        private const float TOUCH_DURATION = 3;
        
        private readonly DebugView m_View;
        private readonly ARPlaneOverlayService m_ARPlaneOverlayService;
        private readonly MaterialDebugService m_MaterialDebugService;
        private readonly EquipmentService m_EquipmentService;
        private readonly SpeederService m_SpeederService;
        private readonly SettingMenuService m_SettingMenuService;
        
        private List<string> m_Logs = new ();
        private float m_TouchTimer;
        private bool m_Unlocked = false;
        private bool m_Shown = false;

        private EquipmentState m_ChosenEquipmentState = EquipmentState.None; 
        
        public DebugService(
            ARPlaneOverlayService arPlaneOverlayService, 
            MaterialDebugService materialDebugService,
            DomainAssetPlacementService domainAssetPlacementService,
            EquipmentService equipmentService, 
            SpeederService speederService,
            SettingMenuService settingMenuService,
            EquipmentService.Settings equipmentServiceSettings)
        {
            m_ARPlaneOverlayService = arPlaneOverlayService;
            m_MaterialDebugService = materialDebugService;
            m_EquipmentService = equipmentService;
            m_SpeederService = speederService;
            m_SettingMenuService = settingMenuService;
            
            m_EquipmentService.onComponentAdded += OnComponentAdded;
            m_View = DebugView.Create();
            m_View.RegisterPlanesOverlayToggleAction(m_ARPlaneOverlayService.SetPlaneOverlay);
            m_View.RegisterDomainButtonsAction(domainAssetPlacementService.CreateAsset, domainAssetPlacementService.ClearAllAssets);
#if DEBUG
            m_View.RegisterLogShare(GetLogAndShare);
            Application.logMessageReceived += LogCallback;
#endif
            #region Material Debug
            m_View.RegisterSetEquipmentToggles(SetEquipmentState, equipmentServiceSettings.debugMenuAvailableEqupimentStates);
            m_View.RegisterMaterialTabToggleAction(UpdateMaterialPanel);
            m_View.RegisterGameplayTabToggleAction(UpdateGameplayPanel);
            m_View.RegisterOnDiffuseMatcapToggleAction(m_MaterialDebugService.SetUseDiffuseMatcap);
            m_View.RegisterOnSpecularMatcapToggleAction(m_MaterialDebugService.SetUseSpecularMatcap);
            m_View.RegisterOnDiffuseArrayIndexSliderValueChangeAction(m_MaterialDebugService.SetDiffuseMatcapArrayIndex);
            m_View.RegisterOnSpecularArrayIndexSliderValueChangeAction(m_MaterialDebugService.SetSpecularMatcapArrayIndex);
            m_View.RegisterOnDiffuseMatcapStrengthSliderValueChangeAction(m_MaterialDebugService.SetDiffuseMatcapStrength);
            m_View.RegisterOnSpecularMatcapStrengthSliderValueChangeAction(m_MaterialDebugService.SetSpecularMatcapStrength);
            m_View.RegisterOnDetailNormalMapArrayIndexSliderValueChangeAction(m_MaterialDebugService.SetDetailNormalMapArrayIndex);
            m_View.RegisterOnDetailNormalMapStrengthSliderValueChangeAction(m_MaterialDebugService.SetDetailNormalMapStrength);
            #endregion

            m_View.Hide();
        }
        private void OnComponentAdded(EquipmentStateComponentModel component)
        {
            if (!component.isMine)
                return;
            
            if (m_ChosenEquipmentState != EquipmentState.None)
            {
                m_EquipmentService.SetState(m_SpeederService.serverSpeederEntity, m_ChosenEquipmentState);
            }
        }

        private void GetLogAndShare()
        {
            var filePath = $"{Application.persistentDataPath}/{DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss")}-log.txt";
            File.WriteAllLines(filePath, m_Logs);
            // Native share logic should be added here
        }

        private void LogCallback(string logString, string stacktrace, LogType type)
        {
            m_Logs.Add(logString);
            m_Logs.Add(stacktrace);
            if (m_Logs.Count > 10000)
            {
                m_Logs.RemoveAt(0);
                m_Logs.RemoveAt(0);
            }
        }

#if UNITY_EDITOR
        int _keyRecognition = 0;

        private void EditorHotKey()
        {
            if (Input.GetKeyDown(KeyCode.D) && _keyRecognition == 0)
                _keyRecognition++;
            else if (Input.GetKeyDown(KeyCode.E) && _keyRecognition == 1)
                _keyRecognition++;
            else if (Input.GetKeyDown(KeyCode.V) && _keyRecognition == 2)
            {
                if (m_View.isShown) 
                    m_View.Hide();
                else 
                    m_View.Show();


                _keyRecognition = 0;
            }
        }
#endif

        public void Tick(float deltaTime, float unscaledDeltaTime)
        {

#if UNITY_EDITOR
            EditorHotKey();
#endif

            if (Input.touchCount < 2)
            {
                m_TouchTimer = 0;
                return;
            }
               
            m_TouchTimer += unscaledDeltaTime;

            if (m_TouchTimer > TOUCH_DURATION && !m_View.isShown)
            {
                m_View.Show();
                UpdateGameplayPanel();
            } 
        }


        private void UpdateMaterialPanel()
        {
            MaterialDebugService.MaterialProperties materialProperies = m_MaterialDebugService.GetMaterialProperties();
            m_View.UpdateMaterialPanel(materialProperies);
        }
        
        private void UpdateGameplayPanel()
        {
            if (!m_EquipmentService.TryGetComponentModel(m_SpeederService.serverSpeederEntity, out var component))
                return;

            m_View.UpdateGameplayPanel(component.model.state);
        }

        private void SetEquipmentState(EquipmentState equipmentState)
        {
            m_ChosenEquipmentState = equipmentState;
            
            if (!m_EquipmentService.TryGetComponentModel(m_SpeederService.serverSpeederEntity, out _))
                return;
            
            m_EquipmentService.SetState(m_SpeederService.serverSpeederEntity, equipmentState);
            m_EquipmentService.SetFullQuantity(equipmentState);
        }
    }
}