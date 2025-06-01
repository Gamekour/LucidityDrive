using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EasyForceInterface : MonoBehaviour
{
    public Rigidbody target;
    public float explosionRadius;
    public void AddForceAtPositionForward(float magnitude)
    {
        target.AddForceAtPosition(transform.forward * magnitude, transform.position);
    }
    public void AddForceForward(float magnitude)
    {
        target.AddForce(transform.forward * magnitude);
    }
    public void AddExplosionForce(float magnitude)
    {
        target.AddExplosionForce(magnitude, transform.position, explosionRadius);
    }
    public void AddTorqueRight(float magnitude)
    {
        target.AddTorque(transform.right * magnitude);
    }
}
