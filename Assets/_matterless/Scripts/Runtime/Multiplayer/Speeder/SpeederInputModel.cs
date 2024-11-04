using UnityEngine;

namespace Matterless.Floorcraft
{
    public struct SpeederInputModel
    {
        public Vector3? target;
        public Vector3 position;
        public Vector3 floorNormal;
        public Quaternion rotation;
        public float speed;
        public bool brake;
        public bool input;
        public SpeederState speederState;
        public EquipmentState equipmentState;
        public bool crownKeeper;
        public float worldScale;
    }
}