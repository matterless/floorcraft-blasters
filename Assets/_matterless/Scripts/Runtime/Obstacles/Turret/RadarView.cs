using UnityEngine;
using UnityEngine.Events;

public class RadarView : MonoBehaviour
{
    public UnityEvent<Collider> onTriggerEnter { get; } = new();
    public UnityEvent<Collider> onTriggerExit { get; } = new();

    private void OnTriggerEnter(Collider other)
    {
        onTriggerEnter?.Invoke(other);
    }

    private void OnTriggerExit(Collider other)
    {
        onTriggerExit?.Invoke(other);
    }
}
