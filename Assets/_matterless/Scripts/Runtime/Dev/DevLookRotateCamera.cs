using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace Matterless.Floorcraft
{
    public class DevLookRotateCamera : MonoBehaviour
    {
        [SerializeField] private float speed = 10.0f;
        void Start () {
            transform.LookAt(Vector3.zero);
        }
        void Update () {
            transform.RotateAround (Vector3.zero,new Vector3(0.0f,1.0f,0.0f),1 * Time.deltaTime * speed);
        }
    }
}
