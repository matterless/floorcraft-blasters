using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Matterless.Floorcraft
{
    public class SpeederSFX : MonoBehaviour
    {
        #region Inspector
        [SerializeField, FormerlySerializedAs("Sources")] AudioSource[] m_Sources;
        [SerializeField, FormerlySerializedAs("VolumeCurves")] AnimationCurve[] m_VolumeCurves;
        [SerializeField, FormerlySerializedAs("PitchCurves")] AnimationCurve[] m_PitchCurves;
        [SerializeField, FormerlySerializedAs("JetHiss")] AudioSource m_JetHiss;
        [SerializeField, FormerlySerializedAs("SpawnSound")] AudioSource m_SpawnSound;
        [SerializeField, FormerlySerializedAs("NitroSound")] AudioSource m_NitroSound;
        [SerializeField, FormerlySerializedAs("BrakeSound")] AudioSource m_BrakeSound;
        [SerializeField, FormerlySerializedAs("SkidSources")] AudioSource[] m_SkidSources;
        [SerializeField] AudioSource m_LaserCharge;
        [SerializeField] AudioSource m_LaserBlast;
        #endregion

        private int m_SkidSource;
        private float m_SkidVolume;
        private bool m_Skidding;

        public void Set(float value, bool mute = false)
        {
            for (var i = 0; i < m_Sources.Length; i++)
            {
                m_Sources[i].pitch = 1 + m_PitchCurves[i].Evaluate(value);
                m_Sources[i].volume = mute ? 0 : m_VolumeCurves[i].Evaluate(value);
            }
        }

        public void SetSkid(bool b)
        {
            const float MAX_SKID_VOLUME = 0.4f;
            m_SkidVolume = b ? Mathf.MoveTowards(m_SkidVolume, 1, Time.deltaTime * 2) :
             Mathf.MoveTowards(m_SkidVolume, 0, Time.deltaTime * 8);
            for (var i = 0; i < m_SkidSources.Length; i++)
            {
                if (i != m_SkidSource) m_SkidSources[i].volume = Mathf.MoveTowards(m_SkidSources[i].volume, 0, Time.deltaTime * 4);
                else m_SkidSources[i].volume = m_SkidVolume * MAX_SKID_VOLUME;
            }
            if (m_SkidVolume <= Mathf.Epsilon && m_Skidding)
            {
                m_Skidding = false;
                m_SkidSource = (m_SkidSource + 1 + Mathf.FloorToInt(Random.value * (m_SkidSources.Length - 2))) % m_SkidSources.Length;
            }
            else if (m_SkidVolume > 0)
                m_Skidding = true;
        }

        public void Spawn() => m_SpawnSound.Play();

        public void Nitro() => m_NitroSound.Play();
        
        public void Brake() => m_BrakeSound.Play();
        
        public void ChargeLaser() => m_LaserCharge.Play();
        
        public void BlastLaser() => m_LaserBlast.Play();
        
        public void UpdateBoostingValue(float boosting) 
            => m_JetHiss.volume = boosting > 0 ? 1 : 0.5f;
    }
}
