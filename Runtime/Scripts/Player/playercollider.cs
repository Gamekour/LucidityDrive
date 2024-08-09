using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CastType
{
    Sphere,
    Box
}

public class playercollider : MonoBehaviour
{
    [SerializeField] HumanBodyBones targetBone;
    [SerializeField] CastType castType;
    [SerializeField] Joint targetJoint;
    [SerializeField] float strength = 1;
    [SerializeField] float friction = 1;
    private Animator targetAnim;
    private RespawnSystem respawnSystem;
    private Rigidbody rb;
    private float sphereRadius;
    private Vector3 boxBounds;
    private Transform target;
    float resetFramesLeft = 0;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        respawn.OnRespawn.AddListener(resetvelocity);
    }

    private void OnDisable()
    {
        respawn.OnRespawn.RemoveListener(resetvelocity);
    }

    private void Start()
    {
        switch (castType)
        {
            case CastType.Sphere:
                sphereRadius = GetComponent<SphereCollider>().radius;
                break;
            case CastType.Box:
                boxBounds = GetComponent<BoxCollider>().bounds.size;
                break;
        }
    }

    private void resetvelocity()
    {
        resetFramesLeft = 0;
    }

    private void FixedUpdate()
    {
        if (target == null)
            return;

        if(resetFramesLeft > 0)
        {
            resetFramesLeft--;
            if (resetFramesLeft == 0)
            {
                rb.velocity = Vector3.zero;
                rb.position = target.position;
                transform.position = target.position;
            }
            else
                return;
        }

        if (PlayerInfo.mainBody.isKinematic) return;

        Vector3 estimate = target.position;
        Vector3 diff = estimate - transform.position;
        bool hit = false;
        RaycastHit hitinfo = new RaycastHit();
        switch (castType)
        {
            case CastType.Sphere:
                hit = Physics.SphereCast(transform.position - (diff.normalized * 0.01f), sphereRadius, diff, out hitinfo, Vector3.Distance(transform.position, estimate), LayerMask.GetMask("Default"));
                break;
            case CastType.Box:
                hit = Physics.BoxCast(transform.position - (diff.normalized * 0.01f), boxBounds * 0.5f, diff, out hitinfo, transform.rotation, Vector3.Distance(transform.position, estimate), LayerMask.GetMask("Default"));
                break;
        }
        if (hit)
        {
            Transform collisionspace = PlayerInfo.collisionspace;
            collisionspace.position = hitinfo.point;
            collisionspace.up = hitinfo.normal;
            Vector3 relativevel = collisionspace.InverseTransformVector(PlayerInfo.mainBody.velocity) * (1/friction);
            float relativeself = collisionspace.InverseTransformPoint(transform.position).y;
            float relativetarget = collisionspace.InverseTransformPoint(target.position).y;
            relativevel.y = (relativeself - relativetarget) * strength;
            PlayerInfo.mainBody.velocity = collisionspace.TransformVector(relativevel);
            PlayerInfo.mainBody.AddForce(-Physics.gravity * Mathf.Clamp01(collisionspace.up.y));
        }
    }
}
