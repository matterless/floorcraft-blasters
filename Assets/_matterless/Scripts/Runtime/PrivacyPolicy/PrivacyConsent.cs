using System;

namespace Matterless.Floorcraft
{
    /// <summary>
    /// Whether the player has accepted the terms and privacy policy. Services that send data
    /// off the device (analytics, crash reporting, Auki authentication) wait for it, so nothing
    /// leaves the device before the first-launch consent screen is accepted.
    /// </summary>
    public class PrivacyConsent
    {
        private Action m_OnGranted;

        public bool granted { get; private set; }

        /// <summary>Runs the action now if consent was given, otherwise once it is.</summary>
        public void WhenGranted(Action action)
        {
            if (granted)
                action();
            else
                m_OnGranted += action;
        }

        public void Grant()
        {
            if (granted)
                return;

            granted = true;
            var onGranted = m_OnGranted;
            m_OnGranted = null;
            onGranted?.Invoke();
        }
    }
}
