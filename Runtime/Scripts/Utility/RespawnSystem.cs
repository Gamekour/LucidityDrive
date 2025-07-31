using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class RespawnSystem : MonoBehaviour
{
    public Vector3 spawnPoint = Vector3.zero;
    [SerializeField] bool useTransformAsSpawnPoint = true;
    [SerializeField] private Vector3 startVel;
    [SerializeField] float respawnHeight = -100;
    public UnityEvent OnRespawn;

    private void Start()
    {
        if (useTransformAsSpawnPoint)
            spawnPoint = transform.position;
    }

    private void FixedUpdate()
    {
        if (LucidPlayerInfo.pelvis.position.y < respawnHeight)
        {
            RespawnInterface.Respawn(spawnPoint);
            StartCoroutine(RespawnInterface.Unlock());
        }
    }
}

public static class RespawnInterface
{
    public static UnityEvent OnRespawn = new UnityEvent();
    public static void Respawn(Vector3 point)
    {
        if (!LucidPlayerInfo.mainBody.isKinematic)
        {
            LucidPlayerInfo.mainBody.velocity = Vector3.zero;
            LucidPlayerInfo.mainBody.isKinematic = true;
        }
        LucidPlayerInfo.pelvis.position = point;
        if (LucidPlayerInfo.vismodelRef != null)
            LucidPlayerInfo.vismodelRef.transform.position = point;
        OnRespawn.Invoke();
    }
    public static void Respawn(Vector3 point, Vector3 velocity)
    {
        if (!LucidPlayerInfo.mainBody.isKinematic)
        {
            LucidPlayerInfo.mainBody.velocity = velocity;
            LucidPlayerInfo.mainBody.isKinematic = true;
        }
        LucidPlayerInfo.pelvis.position = point;
        if (LucidPlayerInfo.vismodelRef != null)
            LucidPlayerInfo.vismodelRef.transform.position = point;
        OnRespawn.Invoke();
    }
    public static IEnumerator Unlock()
    {
        yield return new WaitForSeconds(0.5f);
        LucidPlayerInfo.mainBody.isKinematic = false;
    }
}
