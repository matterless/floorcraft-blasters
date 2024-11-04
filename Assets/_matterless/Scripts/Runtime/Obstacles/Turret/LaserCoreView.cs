using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class LaserCoreView : MonoBehaviour
{
    public UnityEvent<Collider> onTriggerEnter { get; } = new();
    // Start is called before the first frame update
    private void OnTriggerEnter(Collider other)
    {
        onTriggerEnter.Invoke(other);
    }
}
