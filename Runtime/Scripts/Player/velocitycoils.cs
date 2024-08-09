using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class velocitycoils : MonoBehaviour
{
    [SerializeField] float magnetMult = 1;
    [SerializeField] float staticMoveForce = 1;
    [SerializeField] float staticDecayMult = 1;
    [SerializeField] float sidemoveinfluence = 1;
    private Rigidbody rb;
    private float staticenergy = 0;
    private void OnEnable()
    {
        rb = PlayerInfo.mainBody;
    }
    private void FixedUpdate()
    {
        Vector2 moveVector = InputManager.movement.ReadValue<Vector2>();
        Vector3 moveFlat = Vector3.zero;
        moveFlat.x = moveVector.x * sidemoveinfluence;
        moveFlat.z = moveVector.y;
        Vector3 runamount = moveFlat;
        runamount *= 1 - PlayerInfo.hipspace.up.y;
        rb.AddForce(-PlayerInfo.hipspace.up * (runamount.magnitude * magnetMult));

        staticenergy += Time.fixedDeltaTime * PlayerInfo.currentpush.magnitude;
        staticenergy *= staticDecayMult;

        rb.AddForce(PlayerInfo.hipspace.TransformVector(moveFlat) * (staticenergy * staticMoveForce));
    }
}
