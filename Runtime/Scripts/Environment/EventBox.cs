using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EventBox : MonoBehaviour, GrabTrigger
{
    public Collider colliderRef;
    [SerializeField] bool doCollisionStayEvents = false;

    [Header("Events")]
    public UnityEvent<Collider> onTriggered = new UnityEvent<Collider>();
    public UnityEvent<Collision> onCollisionEnter, onCollisionExit, onCollisionStay = new UnityEvent<Collision>();
    public UnityEvent onGrabbed, onUngrabbed, onEnabled;


    private void OnEnable()
    {
        colliderRef = GetComponent<Collider>();
        onEnabled.Invoke();
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
        if (doCollisionStayEvents)
            onCollisionStay.Invoke(collision);
    }

    public void GrabEvent()
    {
        onGrabbed.Invoke();
    }

    public void UngrabEvent()
    {
        onUngrabbed.Invoke();
    }
}
