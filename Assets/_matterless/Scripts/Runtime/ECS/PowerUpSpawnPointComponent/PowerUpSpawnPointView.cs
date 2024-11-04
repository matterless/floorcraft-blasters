using System;
using System.Collections;
using System.Collections.Generic;
using Matterless.Floorcraft;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class PowerUpSpawnPointView : GameObjectView
{
	
	public Collider capsuleCollider => m_CapsuleCollider;
	public UnityEvent<Collision, EquipmentState, uint> onCollisionEntered => m_OnCollisionEntered;
	public bool cooldown
	{
		get => m_Cooldown;
		set => m_Cooldown = value;
	}

	public EquipmentState equipmentType => m_EquipmentState;
	public float timer
	{
		get => m_CooldownTimer;
		set => m_CooldownTimer = value;
	}
	

	[SerializeField] private Collider m_CapsuleCollider;
	[SerializeField] private UnityEvent<Collision, EquipmentState, uint> m_OnCollisionEntered = new UnityEvent<Collision, EquipmentState, uint>();
	[SerializeField] private TextMeshPro m_Text;
	[SerializeField] private MeshRenderer m_FillRenderer;
	[SerializeField] private GameObject m_GlowObject;
		
	private EquipmentState m_EquipmentState;
	private bool m_Cooldown;
	private float m_CooldownTimer;
	private float m_CooldownDuration;

	public void Init(uint id, EquipmentState equipmentState, float cooldownDuration)
	{
		this.entityId = id;
		m_EquipmentState = equipmentState;
		m_Text.text = m_EquipmentState.ToString();
		m_FillRenderer.material.SetFloat("_FillAmmount",1f);
		m_CooldownDuration = cooldownDuration;
	}

	void Update()
	{
		m_GlowObject.SetActive(!m_Cooldown);
		m_Text.gameObject.SetActive(!m_Cooldown);
		m_CooldownTimer = m_CooldownTimer > 0 ? m_CooldownTimer - Time.deltaTime : 0;
		var fillAmount = Mathf.Abs(m_CooldownTimer / m_CooldownDuration - 1);
		m_FillRenderer.material.SetFloat("_FillAmmount", fillAmount);
	}
	private void OnCollisionStay(Collision collision)
	{
		if (m_Cooldown)
			return;
		
		m_OnCollisionEntered.Invoke(collision, m_EquipmentState, this.entityId);
	}
	//private void OnCollisionEnter(Collision collision)
	//{
	//	if (m_Cooldown)
	//		return;
	//	
	//	m_OnCollisionEntered.Invoke(collision, m_EquipmentState, this.entityId);	
	//}
	public void Dispose()
	{
		Debug.Log("Dispose");
		Destroy(transform.parent.gameObject);
	}
}
