using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EventBox : MonoBehaviour
{
    public UnityEvent<Collider> onTriggered = new UnityEvent<Collider>();
    public UnityEvent<Collision> onCollided = new UnityEvent<Collision>();
    public Collider colliderRef;
    [SerializeField] bool drawGizmo = false;

    private void OnEnable()
    {
        colliderRef = GetComponent<Collider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        onTriggered.Invoke(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        onCollided.Invoke(collision);
    }

    private void OnDrawGizmosSelected()
    {
        if(drawGizmo)
            Gizmos.DrawCube(transform.position, transform.localScale);
    }
}
