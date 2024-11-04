using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Matterless.Floorcraft
{
    
    public class ExplosionView : MonoBehaviour
    {
        [SerializeField] private ParticleSystem m_FieryDebris;

        public void SetScaleDependentValues(float scale)
        {
            var main = m_FieryDebris.main;
            
            
            var emission = m_FieryDebris.emission;
            var rateOverDistance = emission.rateOverDistance.constant;
            rateOverDistance = rateOverDistance * 1.0f / scale;
            emission.rateOverDistance = rateOverDistance;
            
        }

    }
}
