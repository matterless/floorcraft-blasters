using System.Collections;
using System.Collections.Generic;
using Matterless.Haptics;
using Matterless.UTools;
using UnityEngine;

namespace Matterless.Floorcraft
{
    public class HapticService : IHapticService
    {
        private ICoroutineRunner m_CoroutineRunner;
        public HapticService(ICoroutineRunner coroutineRunner)
        {
            m_CoroutineRunner = coroutineRunner;
        }
        public void PlayHeavyHapticsTwice()
        {
            m_CoroutineRunner.StartUnityCoroutine(WaitSecondHeavyHaptic(0.4f,0.1f));
        }

        private IEnumerator WaitSecondHeavyHaptic(float time,float second)
        {
            PlayHeavyHaptics();

            float timer = 0;
            while (time > timer)
            {
                yield return new WaitForSeconds(second);
                PlayHeavyHaptics();
                timer += second;
            }
        }
        public void PlayHeavyHaptics()
        {
            HapticManager.StartHapticFeedback(Matterless.Haptics.HapticFeedbackTypes.Heavy);
        }
        public void PlayLightHaptics()
        {
            HapticManager.StartHapticFeedback(Matterless.Haptics.HapticFeedbackTypes.Light);
        }
        public void PlayMediumHaptics()
        {
            HapticManager.StartHapticFeedback(Matterless.Haptics.HapticFeedbackTypes.Medium);
        }
        public void PlayRigidHaptics()
        {
            HapticManager.StartHapticFeedback(Matterless.Haptics.HapticFeedbackTypes.Rigid);
        }
        public void PlaySoftHaptics()
        {
            HapticManager.StartHapticFeedback(Matterless.Haptics.HapticFeedbackTypes.Soft);
        }
    }
}

