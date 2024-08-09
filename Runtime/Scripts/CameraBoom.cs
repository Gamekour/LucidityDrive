using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraBoom : MonoBehaviour
{
    [SerializeField] Transform cameraTransform;
    [SerializeField] LayerMask hitMask;
    [SerializeField] float distance = 2;
    [SerializeField] float radius = 0.2f;
    private Vector3 nextPosition = Vector3.zero;

    private void FixedUpdate()
    {
        bool hit = Physics.Raycast(transform.position, transform.forward, out RaycastHit hitInfo, distance, hitMask);
        if (!hit)
            nextPosition = transform.position + (transform.forward * distance);
        else
            nextPosition = hitInfo.point + (hitInfo.normal * radius);
    }

    private void LateUpdate()
    {
        float ratio = Mathf.Clamp01(Time.deltaTime / Time.fixedDeltaTime);
        cameraTransform.position = Vector3.Lerp(cameraTransform.position, nextPosition, 0.5f);
    }
}
