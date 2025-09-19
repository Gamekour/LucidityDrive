using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class Timer : MonoBehaviour
{
    public float interval;
    public bool activateOnStart = false;
    public bool triggerInstantly = false;
    public bool repeating = false;
    public UnityEvent trigger;

    private void Start()
    {
        if (activateOnStart)
            Activate();
    }

    public void Activate()
    {
        StartCoroutine(cycle());
    }

    public void Deactivate()
    {
        StopAllCoroutines();
    }

    IEnumerator cycle()
    {
        if (triggerInstantly)
            trigger.Invoke();

        yield return new WaitForSeconds(interval);
        trigger.Invoke();

        while (repeating)
        {
            yield return new WaitForSeconds(interval);
            trigger.Invoke();
        }
    }
}
