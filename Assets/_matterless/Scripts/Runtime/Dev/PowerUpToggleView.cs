using System;
using UnityEngine;
using UnityEngine.UI;
namespace Matterless.Floorcraft
{
	public class PowerUpToggleView : MonoBehaviour
	{
		public void OnValueChanged(bool isOn, Action<EquipmentState> action)
		{
			if (isOn) action(m_EquipmentState);
		}
		public Toggle toggle => m_Toggle;
		public EquipmentState equipmentState
		{
			get => m_EquipmentState;
			set => m_EquipmentState = value;
		}
		public Text labelView => m_LabelView;
		[SerializeField] private Toggle m_Toggle;
		[SerializeField] private EquipmentState m_EquipmentState;
		[SerializeField] private Text m_LabelView;
	} 
}