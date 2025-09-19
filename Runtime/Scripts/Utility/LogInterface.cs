using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LogInterface : MonoBehaviour
{
    public void LogMessage(string message) => Debug.Log(message);
    public void LogWarning(string warning) => Debug.LogWarning(warning);
    public void LogError(string error) => Debug.LogError(error);
}
