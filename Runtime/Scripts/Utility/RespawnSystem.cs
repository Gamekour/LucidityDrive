using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class RespawnSystem : MonoBehaviour
{
    public Vector3 spawnpoint = Vector3.zero;
    [SerializeField] Vector3 startVel = Vector3.zero;
    [SerializeField] float respawnHeight = -100;

    private void Start()
    {
        spawnpoint = transform.position;
    }

    private void FixedUpdate()
    {
        if (PlayerInfo.pelvis.position.y < respawnHeight)
        {
            respawn.Respawn(spawnpoint);
            StartCoroutine(respawn.Unlock());
        }
    }
}

public static class respawn
{
    public static UnityEvent OnRespawn = new UnityEvent();
    public static void Respawn(Vector3 point)
    {
        PlayerInfo.physBody.isTrigger = true;
        PlayerInfo.physHead.isTrigger = true;
        Rigidbody rbHips = PlayerInfo.physBody.GetComponent<Rigidbody>();
        Rigidbody rbHead = PlayerInfo.physBody.GetComponent<Rigidbody>();
        if (!rbHips.isKinematic)
        {
            PlayerInfo.physBody.GetComponent<Rigidbody>().velocity = Vector3.zero;
            PlayerInfo.physBody.GetComponent<Rigidbody>().isKinematic = true;
        }
        if(!rbHead.isKinematic)
        {
            PlayerInfo.physHead.GetComponent<Rigidbody>().velocity = Vector3.zero;
            PlayerInfo.physHead.GetComponent<Rigidbody>().isKinematic = true;
        }
        PlayerInfo.physBody.transform.position = point;
        PlayerInfo.physHead.transform.position = point;
        if (!PlayerInfo.mainBody.isKinematic)
        {
            PlayerInfo.mainBody.velocity = Vector3.zero;
            PlayerInfo.mainBody.isKinematic = true;
        }
        PlayerInfo.pelvis.position = point;
        OnRespawn.Invoke();
    }
    public static void Respawn(Vector3 point, Vector3 velocity)
    {
        PlayerInfo.physBody.isTrigger = true;
        PlayerInfo.physHead.isTrigger = true;
        Rigidbody rbHips = PlayerInfo.physBody.GetComponent<Rigidbody>();
        Rigidbody rbHead = PlayerInfo.physBody.GetComponent<Rigidbody>();
        if (!rbHips.isKinematic)
        {
            PlayerInfo.physBody.GetComponent<Rigidbody>().velocity = velocity;
            PlayerInfo.physBody.GetComponent<Rigidbody>().isKinematic = true;
        }
        if (!rbHead.isKinematic)
        {
            PlayerInfo.physHead.GetComponent<Rigidbody>().velocity = velocity;
            PlayerInfo.physHead.GetComponent<Rigidbody>().isKinematic = true;
        }
        PlayerInfo.physBody.transform.position = point;
        PlayerInfo.physHead.transform.position = point;
        if (!PlayerInfo.mainBody.isKinematic)
        {
            PlayerInfo.mainBody.velocity = velocity;
            PlayerInfo.mainBody.isKinematic = true;
        }
        PlayerInfo.pelvis.position = point;
        OnRespawn.Invoke();
    }
    public static IEnumerator Unlock()
    {
        yield return new WaitForSeconds(0.5f);
        PlayerInfo.physBody.isTrigger = false;
        PlayerInfo.physHead.isTrigger = false;
        PlayerInfo.physBody.GetComponent<Rigidbody>().isKinematic = false;
        PlayerInfo.physHead.GetComponent<Rigidbody>().isKinematic = false;
        PlayerInfo.mainBody.isKinematic = false;
    }
}
