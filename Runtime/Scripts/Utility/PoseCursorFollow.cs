using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoseCursorFollow : MonoBehaviour
{
    public Transform target;

    private void Update()
    {
        Vector3 worldDir = LucidPlayerInfo.pelvis.InverseTransformDirection(target.position - LucidPlayerInfo.pelvis.position);
        transform.forward = transform.parent.TransformDirection(worldDir);
    }
}
