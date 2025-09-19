using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuickInstantiator : MonoBehaviour
{
    public bool setParent = false;
    public void InstantiatePrefab(GameObject prefab)
    {
        if (setParent)
            Instantiate(prefab, transform);
        else
            Instantiate(prefab, transform.position, transform.rotation);
    }
}
