using System.Collections.Generic;
using UnityEngine;

public class WindCube : MonoBehaviour
{
    [SerializeField] Vector3 relativeForce;
    [SerializeField] float threshold, matchSmoothness, drag;
    [SerializeField] bool multiplicative, subVel = false;
    [SerializeField] ForceMode fm = ForceMode.Force;
    private readonly List<Rigidbody> targets = new();

    private void OnTriggerEnter(Collider other)
    {
        if(other.attachedRigidbody != null)
            targets.Add(other.attachedRigidbody);
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.attachedRigidbody != null && targets.Contains(other.attachedRigidbody))
            targets.Remove(other.attachedRigidbody);
    }
    private void FixedUpdate()
    {
        for (int i = 0; i < targets.Count; i++)
        {
            Rigidbody target = targets[i];
            Vector3 force = transform.TransformVector(relativeForce);
            if (target != null)
            {
                Vector3 localvel = transform.InverseTransformVector(target.velocity);
                if ((localvel.y > threshold) || threshold == 0)
                {
                    if (multiplicative)
                    {
                        force = Vector3.Scale(force, transform.up * Mathf.Clamp(localvel.y, 0, Mathf.Infinity));
                    }
                    if (subVel)
                    {
                        force -= target.velocity;
                        force /= Time.fixedDeltaTime;
                        force *= (1 - matchSmoothness);
                    }
                    target.AddForce(force, fm);
                    target.velocity /= drag + 1;
                }
            }
            else
            {
                targets.RemoveAt(i);
                i--;
            }
        }
    }
}
