using UnityEngine;

namespace Matterless.Floorcraft
{
    public struct SpeederGameplayModel
    {
        public SpeederState speederState;
        public Vector3 position;
        public Quaternion rotation;
        public float speed;
        public EquipmentState equipmentState;

        public SpeederGameplayModel(SpeederState speederState, Vector3 position, Quaternion rotation, float speed, EquipmentState equipmentState)
        {
            this.speederState = speederState;
            this.position = position;
            this.rotation = rotation;
            this.speed = speed;
            this.equipmentState = equipmentState;
        }
    }
}