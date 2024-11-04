using System;
using UnityEngine;
namespace Matterless.Floorcraft
{
	public class HonkSFX : MonoBehaviour
	{
		[SerializeField] private AudioSource m_AudioSource;

		private float m_Timer;
		private void OnEnable()
		{
			m_AudioSource.Play();
			m_Timer = 0f;
		}

		void Update()
		{
			m_Timer += Time.deltaTime;
			if (m_Timer > 2f)
			{
				Destroy(gameObject);
			}
		}
	}
}