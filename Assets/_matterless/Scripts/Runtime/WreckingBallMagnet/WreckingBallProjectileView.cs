using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
namespace Matterless.Floorcraft
{
	public class WreckingBallProjectileView : GameObjectView
	{
		public UnityEvent<Collider> triggerEvent => m_TriggerEvent;
		public Collider sphereCollider => m_SphereCollider;
		public Rigidbody rigidBody => m_Rigidbody;
		
		[SerializeField] private SphereCollider m_SphereCollider;
		[SerializeField] private Rigidbody m_Rigidbody;
		[SerializeField] private ParticleSystem m_Explosion;
		
		private UnityEvent<Collider> m_TriggerEvent = new ();
		void OnTriggerEnter(Collider collider) => triggerEvent.Invoke(collider);
		
		public void Explode()
		{
			m_Explosion.transform.position = this.transform.position;
			m_Explosion.transform.SetParent(null);
			m_Explosion.gameObject.SetActive(true);
			m_Explosion.Play();
		}
	}
}