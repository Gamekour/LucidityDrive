using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EventBox : MonoBehaviour, GrabTrigger
{
    public Collider colliderRef;
    [SerializeField] bool doCollisionStayEvents = false;
    private bool initialized = false;

    [Header("Events")]
    public UnityEvent<Collider> onTriggered;
    public UnityEvent<Collision> onCollisionEnter, onCollisionExit, onCollisionStay;
    public UnityEvent onGrabbed, onUngrabbed, onEnabled;


    private void OnEnable()
    {
        colliderRef = GetComponent<Collider>();
        onTriggered = new UnityEvent<Collider>();
        onCollisionEnter = new UnityEvent<Collision>();
        onCollisionExit = new UnityEvent<Collision>();
        onCollisionStay = new UnityEvent<Collision>();
        onGrabbed = new UnityEvent();
        onUngrabbed = new UnityEvent();
        onEnabled = new UnityEvent();
        onEnabled.Invoke();
        initialized = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!initialized) return;
        onTriggered.Invoke(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!initialized) return;
        onCollisionEnter.Invoke(collision);
    }

    private void OnCollisionExit(Collision collision)
    {
        if (!initialized) return;
        onCollisionExit.Invoke(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (!initialized) return;
        if (doCollisionStayEvents)
            onCollisionStay.Invoke(collision);
    }

    public void GrabEvent()
    {
        if (!initialized) return;
        onGrabbed.Invoke();
    }

    public void UngrabEvent()
    {
        if (!initialized) return;
        onUngrabbed.Invoke();
    }
}
