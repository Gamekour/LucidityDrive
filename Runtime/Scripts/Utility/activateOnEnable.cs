using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class activateOnEnable : MonoBehaviour
{
    public UnityEvent onTriggered;

    private void OnEnable()
    {
        onTriggered.Invoke();
    }
}
