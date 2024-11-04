using System;

namespace Matterless.Floorcraft
{
    public struct SpawnPointCooldownStateModel
    {
        public SpawnPointCooldownStateModel(bool inCooldown)
        {
            m_InCooldown = inCooldown;
        }
        
        public bool inCooldown => m_InCooldown;
        
        private bool m_InCooldown;
    }
}
