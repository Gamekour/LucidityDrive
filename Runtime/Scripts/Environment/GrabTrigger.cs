using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GrabTrigger : MonoBehaviour
{
    public UnityEvent onTriggered;

    public void GrabEvent()
    {
        onTriggered.Invoke();
    }
}
