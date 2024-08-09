using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Launcher : MonoBehaviour
{
    [SerializeField] Transform dir;
    [SerializeField] float magnitude;

    public void LaunchLocalPlayer()
    {
        PlayerInfo.mainBody.velocity = dir.forward * magnitude;
    }
}
