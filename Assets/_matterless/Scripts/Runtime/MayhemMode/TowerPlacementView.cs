using System;
using System.Collections;
using UnityEngine;

public class TowerPlacementView : MonoBehaviour
{
    [SerializeField] private GameObject m_PlacementMarker;
    [SerializeField] private Material m_Material;
    
    // NOTE (Marko) : _SummonIsDespawnIsCancelled packed as Vector3;
    // x -> Summon
    // y -> IsDespawn (bool) 0 or 1
    // z -> IsCancelled (bool) 0 or 1
    private static readonly int SummonIsDespawnIsCancelled = Shader.PropertyToID("_SummonIsDespawnIsCancelled");
    private Vector3 m_SummonIsDespawnIsCancelled;
    private bool m_IsDespawn;
    private bool m_IsCancelled;
    private Coroutine m_AnimationCoroutine;

    public void OnShowTowerPlacementVisals(Action onAnimationFinished)
    {
        m_IsDespawn = false;
        m_IsCancelled = false;
        
        if (m_AnimationCoroutine != null)
            StopCoroutine(m_AnimationCoroutine);
        
        m_AnimationCoroutine = StartCoroutine(Animate(true, onAnimationFinished));
    }

    public void OnTowerPlaced(Action onAnimationFinished)
    {
        m_IsDespawn = true;
        m_IsCancelled = false;
        
        
        if (m_AnimationCoroutine != null)
            StopCoroutine(m_AnimationCoroutine);
        
        m_AnimationCoroutine = StartCoroutine(Animate(false, onAnimationFinished));
    }

    public void OnTowerPlacementCanceled(Action onAnimationFinished)
    {
        m_IsDespawn = true;
        m_IsCancelled = true;
        
        if (m_AnimationCoroutine != null)
            StopCoroutine(m_AnimationCoroutine);
        
        m_AnimationCoroutine = StartCoroutine(Animate(false, onAnimationFinished));
    }
    
    
    private IEnumerator Animate(bool show, Action callback)
    {
        m_SummonIsDespawnIsCancelled.y = m_IsDespawn ? 1.0f : 0.0f;
        m_SummonIsDespawnIsCancelled.z = m_IsCancelled ? 1.0f : 0.0f;
        
        float time = 0;
        while (time < 1.5f)
        {
            
            float t = time/1.5f;
            
            m_SummonIsDespawnIsCancelled.x = show ? t : 1.0f - t;
            m_Material.SetVector(SummonIsDespawnIsCancelled, m_SummonIsDespawnIsCancelled);
            
            // Jokes aside. Show marker after 2/3 of the animation
            bool showMarker = show ? t > 0.666f : t < 0.333f;
            m_PlacementMarker.SetActive(showMarker); 
            
            time += Time.deltaTime;
            yield return null;
        }
        m_SummonIsDespawnIsCancelled.x = show ? 1 : 0;
        
        m_Material.SetVector(SummonIsDespawnIsCancelled, m_SummonIsDespawnIsCancelled);
        callback?.Invoke();
        m_AnimationCoroutine = null;
    }
}
