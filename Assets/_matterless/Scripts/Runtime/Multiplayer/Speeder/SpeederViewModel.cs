using UnityEngine;

namespace Matterless.Floorcraft
{
    public struct SpeederViewModel
    {
        public uint entityId;
        public Vector3 groundPosition;
        public Quaternion orientation;
        public SpeederState speederState;
        public float speed;
        public float age;
        public float braking;
        public Vector3 floorNormal;
        public float boosting;
        public bool crownKeeper;
        public EquipmentState equipmentState;

        public SpeederViewModel
        (uint entityId,
            Vector3 groundPosition,
            Quaternion orientation,
            SpeederState speederState,
            float speed,
            float age,
            float braking,
            Vector3 floorNormal,
            float boosting,
            EquipmentState equipmentState,
            bool crownKeeper)
        {
            this.entityId = entityId;
            this.groundPosition = groundPosition;
            this.orientation = orientation;
            this.speederState = speederState;
            this.speed = speed;
            this.age = age;
            this.braking = braking;
            this.floorNormal = floorNormal;
            this.boosting = boosting;
            this.equipmentState = equipmentState;
            this.crownKeeper = crownKeeper;
        }
    }
}