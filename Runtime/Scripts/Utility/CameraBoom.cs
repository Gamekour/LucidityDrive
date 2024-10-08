using UnityEngine;

public class CameraBoom : MonoBehaviour
{
    [SerializeField] Transform cameraTransform;
    [SerializeField] LayerMask hitMask;
    [SerializeField] float distance = 2f;
    [SerializeField] float radius = 0.2f;
    [SerializeField] float smoothTime = 0.1f;

    private Vector3 currentVelocity;
    private Vector3 targetPosition;
    private Vector3 lastFixedUpdatePosition;

    private void FixedUpdate()
    {
        // Calculate the desired position
        Vector3 desiredPosition = transform.position + transform.forward * distance;

        // Check for obstacles
        if (Physics.SphereCast(transform.position, radius, transform.forward, out RaycastHit hitInfo, distance, hitMask))
        {
            desiredPosition = hitInfo.point + hitInfo.normal * radius;
        }

        lastFixedUpdatePosition = desiredPosition;
    }

    private void LateUpdate()
    {
        // Smoothly move the camera towards the target position
        targetPosition = Vector3.SmoothDamp(targetPosition, lastFixedUpdatePosition, ref currentVelocity, smoothTime);
        cameraTransform.position = targetPosition;
    }
}