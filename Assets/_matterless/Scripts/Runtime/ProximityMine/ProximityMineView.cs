using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Matterless.Floorcraft
{
    public class ProximityMineView : MonoBehaviour
    {
        public UnityEvent<Collision> collisionEvent => m_CollisionEvent;
        public Collider sphereCollider => m_SphereCollider;
        
        private UnityEvent<Collision> m_CollisionEvent = new ();
        
        [SerializeField] private GameObject m_Mesh; 
        [SerializeField] private GameObject m_Circle;
        [SerializeField] private GameObject m_Outline;
        [SerializeField] private GameObject m_RedCircle;
        [SerializeField] private GameObject m_RedOutline;
        [SerializeField] private ParticleSystem m_ExplosionParticles;
        [SerializeField] private SphereCollider m_SphereCollider;
        
        
        private void OnCollisionEnter(Collision collision) => collisionEvent.Invoke(collision);
        
        public void SetRadius(float radius)
        {
            m_SphereCollider.radius = radius * 20f;
            m_Circle.transform.localScale = radius * 1266.67f * Vector3.one;
            m_Outline.transform.localScale = radius * 200f * Vector3.one;
        }
        public void ActivateExplosionSFX()
        {
            m_ExplosionParticles.transform.SetParent(null);
            m_ExplosionParticles.Play();
            m_Mesh.SetActive(false);
            m_Circle.SetActive(false);
            m_Outline.SetActive(false);
            m_RedCircle.SetActive(false);
            m_RedOutline.SetActive(false);
        }
        
        public void SetOutline(bool isLocal)
        {
            m_Circle.SetActive(isLocal);
            m_Outline.SetActive(isLocal);
            m_RedCircle.SetActive(!isLocal);
            m_RedOutline.SetActive(!isLocal);
        }
    }
}