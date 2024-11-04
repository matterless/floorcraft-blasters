using UnityEngine;

namespace Matterless.Floorcraft
{
    /// <summary>
    /// Model
    /// </summary>
    public struct TransformModel
    {
        public TransformModel(Vector3 position, Quaternion rotetion, float speed)
        {
            this.position = position;
            this.rotation = rotetion;
            this.speed = speed;
        }

        public Vector3 position { get; set; }
        public Quaternion rotation { get; set; }
        public float speed { get; set; }
    }
}