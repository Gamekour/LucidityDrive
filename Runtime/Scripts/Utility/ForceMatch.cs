using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForceMatch : MonoBehaviour
{
    [SerializeField] HumanBodyBones targetBone;
    [SerializeField] float positionforce = 10;
    [SerializeField] float tolerance = 0.01f;
    private Transform target;
    private ConfigurableJoint joint;
    private Rigidbody rb;

    private void Start()
    {
        joint = GetComponent<ConfigurableJoint>();
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        PlayerInfo.OnAnimModellInitialized.AddListener(OnAnimModelInitialized);
    }

    private void OnDisable()
    {
        PlayerInfo.OnAnimModellInitialized.RemoveListener(OnAnimModelInitialized);
    }

    public void OnAnimModelInitialized()
    {
        target = PlayerInfo.playermodelAnim.GetBoneTransform(targetBone);
    }

    private void FixedUpdate()
    {
        if (target == null || joint == null || rb == null) return;

        float angle = Quaternion.Angle(transform.rotation, target.rotation);
        if( angle < tolerance )
            transform.rotation = target.rotation;
        joint.targetRotation = Quaternion.Inverse(target.rotation) * joint.connectedBody.transform.rotation;
        rb.AddForce((target.position - transform.position) * positionforce * Time.fixedDeltaTime);
    }
}
