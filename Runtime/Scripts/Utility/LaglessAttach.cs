using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaglessAttach : MonoBehaviour
{
    public Transform target;

    public bool followPosition = true;
    public bool followRotation = false;

    private Coroutine followCoroutine;

    void OnEnable()
    {
        if (target != null)
            followCoroutine = StartCoroutine(FollowRoutine());
    }

    void OnDisable()
    {
        if (followCoroutine != null)
            StopCoroutine(followCoroutine);
    }

    IEnumerator FollowRoutine()
    {
        while (true)
        {
            if (target != null)
            {
                if (followPosition)
                    transform.position = target.position;
                if (followRotation)
                    transform.rotation = target.rotation;
            }
            yield return new WaitForEndOfFrame();
        }
    }
}
