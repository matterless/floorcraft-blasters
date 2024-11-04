using System;

namespace Matterless.Floorcraft
{
    public enum EquipmentState : byte
    {
        None = 0,
        Magnet = 1,
        MagnetAndWreckingBall = 2,
        Dash = 3,
        Laser = 4,
        Flamethrower = 5,
        ProximityMines = 6,
        Clone = 8,
    }

    public struct EquipmentStateModel
    {
        public EquipmentStateModel(EquipmentState state)
        {
            m_State = state;
        }

        public EquipmentState state => m_State;

        private EquipmentState m_State;
    }
}