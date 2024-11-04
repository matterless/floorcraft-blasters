using System.Collections;
using UnityEngine;
using Matterless.UTools;

namespace Matterless.Floorcraft
{
    public class RendererService : IRendererService
    {
        [System.Serializable]
        public class Settings
        {
            [SerializeField] private DimRendererFeature m_DimmRendererFeature;
            [SerializeField] private float m_MaximumDimm = 0.4f;
            [SerializeField] private float m_DimmSpeed = 0.8f;
            
            public DimRendererFeature dimmRendererFeature => m_DimmRendererFeature;
            public float maximumDimm => m_MaximumDimm;
            public float dimmSpeed => m_DimmSpeed;
        }

        private readonly Settings m_Settings;
        private readonly ICoroutineRunner m_CoroutineRunner;
        private Coroutine m_Coroutine = null;
        private bool m_IsDimmed;
        
        public RendererService(ICoroutineRunner coroutineRunner, Settings settings)
        {
            m_Settings = settings;
            m_CoroutineRunner = coroutineRunner;
        }

        public void EnableDimm()
        {
            if (m_IsDimmed)
                return;
        
            if (m_Coroutine != null)
                m_CoroutineRunner.StopUnityCoroutine(m_Coroutine);
            
            m_Coroutine = m_CoroutineRunner.StartUnityCoroutine(DoDimm(0.0f, m_Settings.maximumDimm));
        }

        public void DisableDimm()
        {
            if(!m_IsDimmed)
                return;
            
            if (m_Coroutine != null)
                m_CoroutineRunner.StopUnityCoroutine(m_Coroutine);
            
            m_Coroutine = m_CoroutineRunner.StartUnityCoroutine(UnDimm(m_Settings.maximumDimm, 0.0f));
        }
        private IEnumerator DoDimm(float from, float to)
        {
            m_Settings.dimmRendererFeature.SetActive(true);
            m_IsDimmed = true;
            float t = 0f;
            
            while (t < m_Settings.dimmSpeed)
            {
                m_Settings.dimmRendererFeature.DimAmmount = Mathf.Lerp(from, to, t/m_Settings.dimmSpeed);
                t += Time.deltaTime;
                yield return null;
            }
        }
        private IEnumerator UnDimm(float from, float to)
        {
            float t = 0f;
            m_IsDimmed = false;
            
            while (t < m_Settings.dimmSpeed)
            {
                m_Settings.dimmRendererFeature.DimAmmount = Mathf.Lerp(from, to, t/m_Settings.dimmSpeed);
                t += Time.deltaTime;
                yield return null;
            }
            
            m_Settings.dimmRendererFeature.SetActive(false);
        }
    }
}

