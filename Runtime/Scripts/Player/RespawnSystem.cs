using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace LucidityDrive
{
    public class RespawnSystem : MonoBehaviour
    {
        public Transform spawnPoint;
        [SerializeField] private Vector3 startVel;
        [SerializeField] float respawnHeight = -100;
        public UnityEvent OnRespawn;

        private void OnEnable()
        {
            RespawnInterface.OnRespawn.AddListener(OnStaticRespawnEvent);
        }

        private void OnDisable()
        {
            RespawnInterface.OnRespawn.RemoveListener(OnStaticRespawnEvent);
        }

        private void Start()
        {
            if (spawnPoint == null)
                spawnPoint = transform;
            StartCoroutine(WaitToSpawn());
        }

        private void FixedUpdate()
        {
            if (LucidPlayerInfo.pelvis.position.y < respawnHeight)
            {
                RespawnInterface.Respawn(spawnPoint.position, startVel);
            }
        }

        private void OnStaticRespawnEvent()
        {
            OnRespawn.Invoke();
        }

        IEnumerator WaitToSpawn()
        {
            while (!LucidPlayerInfo.animModelInitialized)
                yield return null;

            yield return new WaitForFixedUpdate();
            RespawnInterface.Respawn(spawnPoint.position, startVel);
        }
    }

    public static class RespawnInterface
    {
        public static UnityEvent OnRespawn = new UnityEvent();
        public static void Respawn(Vector3 point)
        {
            LucidPlayerInfo.pelvis.position = point;
            Arms.instance.transform.position = point;
            LucidPlayerInfo.mainBody.velocity = Vector3.zero;
            if (LucidPlayerInfo.vismodelRef != null)
                LucidPlayerInfo.vismodelRef.transform.position = point;
            OnRespawn.Invoke();
        }
        public static void Respawn(Vector3 point, Vector3 velocity)
        {
            LucidPlayerInfo.pelvis.position = point;
            Arms.instance.transform.position = point;
            LucidPlayerInfo.mainBody.velocity = velocity;
            if (LucidPlayerInfo.vismodelRef != null)
                LucidPlayerInfo.vismodelRef.transform.position = point;
            OnRespawn.Invoke();
        }
    }
}