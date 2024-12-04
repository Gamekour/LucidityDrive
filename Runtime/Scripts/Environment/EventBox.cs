using UnityEngine;
using UnityEngine.Events;

public class EventBox : MonoBehaviour, IGrabTrigger
{
    public Collider colliderRef;
    [SerializeField] bool doCollisionStayEvents = false;
    private bool initialized = false;
    private bool justCollided = false;
    private int framesSinceCollision = 0;
    private Collision lastCollision = null;

    [Header("Events")]
    public UnityEvent<Collider> onTriggered;
    public UnityEvent<Collision> onCollisionEnter, onCollisionExit, onCollisionStay;
    public UnityEvent onGrabbed, onUngrabbed, onEnabled;


    private void OnEnable()
    {
        colliderRef = GetComponent<Collider>();
        onTriggered ??= new();
        onCollisionEnter ??= new();
        onCollisionExit ??= new();
        onCollisionStay ??= new();
        onGrabbed ??= new();
        onUngrabbed ??= new();
        onEnabled ??= new();
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
        lastCollision = collision;
    }

    private void OnCollisionExit(Collision collision)
    {
        if (!initialized) return;
        onCollisionExit.Invoke(collision);
        framesSinceCollision = 0;
        justCollided = false;
    }

    private void OnCollisionStay(Collision collision)
    {
        if (!initialized) return;
        if (doCollisionStayEvents)
        {
            onCollisionStay.Invoke(collision);
            framesSinceCollision = 0;
            lastCollision = collision;
            justCollided = true;
        }
    }

    private void FixedUpdate()
    {
        framesSinceCollision++;
        if (framesSinceCollision > 1 && justCollided)
        {
            onCollisionExit.Invoke(lastCollision);
            justCollided = false;
        }
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
