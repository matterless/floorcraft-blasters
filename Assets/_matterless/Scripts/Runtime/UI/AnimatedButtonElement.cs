using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatedButtonElement : MonoBehaviour
{
    [SerializeField] private bool m_ExcludeFromAlphaAnimation = true;
    
    public bool excludeFromAlphaAnimation => m_ExcludeFromAlphaAnimation;
}
