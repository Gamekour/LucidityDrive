using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LucidityDrive.Extras
{
    public class CamRoot : MonoBehaviour
    {
        public bool useHead = false;
        private void Update()
        {
            if (Camera.main != null)
            {
                if (!useHead)
                {
                    transform.position = Camera.main.transform.position;
                    transform.rotation = Camera.main.transform.rotation;
                }
                else if (LucidPlayerInfo.head != null)
                {
                    transform.position = LucidPlayerInfo.head.position;
                    transform.rotation = LucidPlayerInfo.head.rotation;
                }
            }
        }
    }
}