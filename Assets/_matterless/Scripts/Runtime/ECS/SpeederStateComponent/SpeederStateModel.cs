using System;

namespace Matterless.Floorcraft
{
    [Flags]
    public enum SpeederState : byte
    {
        None = 0,
        Loading = 1,
        Totaled = 2,
        Boosting = 4,
        Braking = 8,
        OverHeat = 16,
        LaserCharge = 32,
        LaserFire = 64,
        Clone = 128,
        FlameThrower = 128,
    }

    public struct SpeederStateModel
    {
        public SpeederStateModel(SpeederState state)
        {
            m_StateId = (byte)state;
        }

        public SpeederStateModel(byte state)
        {
            m_StateId = state;
        }

        private byte m_StateId;
        public SpeederState state => (SpeederState)m_StateId;
        public byte stateId => m_StateId;
    }
}