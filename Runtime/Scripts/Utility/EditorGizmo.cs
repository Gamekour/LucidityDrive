using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class EditorGizmo : MonoBehaviour
{
    public enum GizmoShape { Box, WireBox, Sphere, WireSphere, Ray, Line, Mesh, WireMesh}
    [SerializeField] bool drawOnSelected = true;
    [SerializeField] GizmoShape gizmoShape;
    [SerializeField] Color gizmoColor;
    [SerializeField] Transform lineTarget;
    [SerializeField] float gizmoSphereRadius;
    [SerializeField] Mesh gizmoMesh;

    private void OnDrawGizmos()
    {
        if (!drawOnSelected)
            gizmodraw();
    }

    private void OnDrawGizmosSelected()
    {
        if(drawOnSelected)
            gizmodraw();
    }

    private void gizmodraw()
    {
        Gizmos.color = gizmoColor;
        switch (gizmoShape)
        {
            case GizmoShape.Box:
                Gizmos.DrawCube(transform.position, transform.lossyScale);
                break;
            case GizmoShape.WireBox:
                Gizmos.DrawWireCube(transform.position, transform.lossyScale);
                return;
            case GizmoShape.Sphere:
                Gizmos.DrawSphere(transform.position, gizmoSphereRadius);
                break;
            case GizmoShape.WireSphere:
                Gizmos.DrawWireSphere(transform.position, gizmoSphereRadius);
                break;
            case GizmoShape.Ray:
                Gizmos.DrawRay(transform.position, transform.forward);
                break;
            case GizmoShape.Line:
                Gizmos.DrawLine(transform.position, lineTarget.position);
                break;
            case GizmoShape.Mesh:
                if (gizmoMesh != null)
                    Gizmos.DrawMesh(gizmoMesh, transform.position, transform.rotation, transform.lossyScale);
                break;
            case GizmoShape.WireMesh:
                if (gizmoMesh != null)
                    Gizmos.DrawWireMesh(gizmoMesh, transform.position, transform.rotation, transform.lossyScale);
                break;
        }
    }
}
