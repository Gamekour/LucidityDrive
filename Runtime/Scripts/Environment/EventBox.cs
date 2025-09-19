using UnityEngine;
using UnityEngine.Events;

public class EventBox : MonoBehaviour, IGrabTrigger
{
    public Collider colliderRef;
    public LayerMask ignoreLayers;
    public bool doCollisionStayEvents = false;
    private bool initialized = false;
    private bool justCollided = false;
    private int framesSinceCollision = 0;
    private Collision lastCollision = null;

    [Header("Events")]
    public UnityEvent<Collider> onTriggered;
    public UnityEvent<Collision> onCollisionEnter, onCollisionExit, onCollisionStay;
    public UnityEvent<GameObject> onParticleCollision;
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
        bool isIgnoredLayer = ignoreLayers == (ignoreLayers | (1 << other.gameObject.layer));
        if (!initialized || isIgnoredLayer) return;
        onTriggered.Invoke(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        bool isIgnoredLayer = ignoreLayers == (ignoreLayers | (1 << collision.gameObject.layer));
        if (!initialized || isIgnoredLayer) return;
        onCollisionEnter.Invoke(collision);
        lastCollision = collision;
    }

    private void OnCollisionExit(Collision collision)
    {
        bool isIgnoredLayer = ignoreLayers == (ignoreLayers | (1 << collision.gameObject.layer));
        if (!initialized || isIgnoredLayer) return;
        onCollisionExit.Invoke(collision);
        framesSinceCollision = 0;
        justCollided = false;
    }

    private void OnCollisionStay(Collision collision)
    {
        bool isIgnoredLayer = ignoreLayers == (ignoreLayers | (1 << collision.gameObject.layer));
        if (!initialized || isIgnoredLayer) return;
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

    private void OnParticleCollision(GameObject other)
    {
        onParticleCollision.Invoke(other);
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
