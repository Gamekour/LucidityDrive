using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EventBox : MonoBehaviour
{
    public UnityEvent<Collider> onTriggered = new UnityEvent<Collider>();
    public UnityEvent<Collision> onCollisionEnter = new UnityEvent<Collision>();
    public UnityEvent<Collision> onCollisionExit = new UnityEvent<Collision>();
    [Tooltip("Make sure doCollisionStayEvents is enabled if using this!")]
    public UnityEvent<Collision> onCollisionStay = new UnityEvent<Collision>();
    public Collider colliderRef;
    [SerializeField] bool doCollisionStayEvents = false;
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
        onCollisionEnter.Invoke(collision);
    }

    private void OnCollisionExit(Collision collision)
    {
        onCollisionExit.Invoke(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        if(doCollisionStayEvents)
            onCollisionStay.Invoke(collision);
    }

    private void OnDrawGizmosSelected()
    {
        if(drawGizmo)
            Gizmos.DrawCube(transform.position, transform.localScale);
    }
}
