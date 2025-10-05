using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamRoot : MonoBehaviour
{
    private void Update()
    {
        if (Camera.main != null)
        {
            transform.position = Camera.main.transform.position;
            transform.rotation = Camera.main.transform.rotation;
        }
    }
}
