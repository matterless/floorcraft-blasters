#define VFX_DEBUG

using System.Collections;
using UnityEngine;

namespace Matterless.Floorcraft
{
    public class NPCEnemySpawnPoint : MonoBehaviour
    {
        public Vector3 position => transform.position;
        
        [SerializeField] private ParticleSystem[] m_NonLoopingParticleSystems;
        [SerializeField] private ParticleSystem[] m_LoopingParticleSystems;
        
        private Coroutine m_SpawnCoroutine;
        private float m_WaitTime = 2.0f;

        public void Spawn()
        {
            foreach (var ps in m_LoopingParticleSystems)
            {
                ps.Play();
            }
            
            
            if (m_SpawnCoroutine != null)
            {
                StopCoroutine(m_SpawnCoroutine);
            }
            
            m_SpawnCoroutine = StartCoroutine(SpawnCoroutine());
            
        }

        private IEnumerator SpawnCoroutine()
        {
            foreach (var ps in m_NonLoopingParticleSystems)
            {
                ps.Play();
            }
            
            yield return new WaitForSeconds(m_WaitTime);
            
            foreach (var ps in m_NonLoopingParticleSystems)
            {
                ps.Pause();
            }
            
            foreach (var ps in m_LoopingParticleSystems)
            {
                ps.Stop();
            }
            
            m_SpawnCoroutine = null;
        }

        public void DeSpawn()
        {
            foreach (var ps in m_NonLoopingParticleSystems)
            {
                ps.Play();
            }
        }
    }
}