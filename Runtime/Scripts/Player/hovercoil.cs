using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class hovercoil : MonoBehaviour
{
    [SerializeField] float floatforce = 1;
    [SerializeField] float fallforce = 1;
    [SerializeField] float flightforce = 1;
    private Rigidbody rb;
    private void OnEnable()
    {
        rb = PlayerInfo.mainBody;
    }
    private void FixedUpdate()
    {
        bool space = InputManager.jump.ReadValue<float>() != 0;
        if (space)
            rb.AddForce(Vector3.up * floatforce * Time.fixedDeltaTime, ForceMode.Acceleration);
        else if (!PlayerInfo.grounded)
            rb.AddForce(Vector3.down * fallforce * Time.fixedDeltaTime, ForceMode.Acceleration);

        Vector2 moveVector = InputManager.movement.ReadValue<Vector2>();
        Vector3 moveFlat = Vector3.zero;
        moveFlat.x = moveVector.x;
        moveFlat.z = moveVector.y;
        moveFlat = PlayerInfo.pelvis.TransformVector(moveFlat);

        rb.AddForce(moveFlat * flightforce * Time.fixedDeltaTime);
    }
}
