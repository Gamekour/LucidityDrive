using UnityEngine;
using UnityEngine.Events;

public class EventBox : MonoBehaviour, IGrabTrigger
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
        onTriggered = new();
        onCollisionEnter = new();
        onCollisionExit = new();
        onCollisionStay = new();
        onGrabbed = new();
        onUngrabbed = new();
        onEnabled = new();
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
