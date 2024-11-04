using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StaticLightHouseView : MonoBehaviour
{
    
    #region Inspector
    [SerializeField] private GameObject m_ScannedLighthousePanel;
    [SerializeField] private GameObject m_AssignUIPanel;
    [SerializeField] private GameObject m_JoinUIPanel;
    [SerializeField] private GameObject m_DoCalibrationPanel;

    private Button m_YesButton;
    private Button m_NoButton;
    private TMP_Text m_Text;
    #endregion
    #region Factory
    public static StaticLightHouseView Create()
        => Instantiate(Resources.Load<StaticLightHouseView>("UIPrefabs/UIP_StaticLightHouseView")).Init();
    #endregion

    private StaticLightHouseView Init()
    {
        return this;
    }

    public void ShowScannedLighthouse()
    {
        // loading screen
        m_ScannedLighthousePanel.SetActive(true);
    }

    public void DisableScannedLighthouse()
    {
        m_ScannedLighthousePanel.SetActive(false);
    }

    public void EnableAssignPanel(Action yesAction,Action noAction)
    {
        m_AssignUIPanel.SetActive(true);
        AddButtonEvent(yesAction, noAction);
    }

    public void DisableAssignPanel()
    {
        m_AssignUIPanel.SetActive(false);
        RemoveButtonEvent();
    }

    public void EnableJoinPanel(string sessionId, Action yesAction, Action noAction)
    {
        m_JoinUIPanel.SetActive(true);
        // TODO : join session text
        m_Text.text = $"Join {sessionId} ?";
        AddButtonEvent(yesAction, noAction);
    }

    public void DisableJoinPanel()
    {
        m_JoinUIPanel.SetActive(false);
        RemoveButtonEvent();
    }

    public void EnableDoCalibration( Action yesAction, Action noAction)
    {
        m_DoCalibrationPanel.SetActive(true);
        AddButtonEvent(yesAction, noAction);
    }

    public void DisableDoCalibration()
    {
        m_DoCalibrationPanel.SetActive(false);
        RemoveButtonEvent();
    }

    private void AddButtonEvent(Action yesAction, Action noAction)
    {
        m_YesButton.onClick.AddListener(()=>yesAction?.Invoke());
        m_NoButton.onClick.AddListener(()=>noAction?.Invoke());
    }

    private void RemoveButtonEvent()
    {
        m_YesButton.onClick.RemoveAllListeners();
        m_NoButton.onClick.RemoveAllListeners();
    }
}
