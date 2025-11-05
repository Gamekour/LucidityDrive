using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LucidityDrive.Extras
{
    public class TempLayerOverride : MonoBehaviour
    {
        public int newLayer = 2;
        private int prevLayer = 0;

        private void OnEnable() => StartCoroutine(StartOverride());

        private void OnDisable() => gameObject.layer = prevLayer;

        IEnumerator StartOverride()
        {
            yield return new WaitForEndOfFrame();
            prevLayer = gameObject.layer;
            gameObject.layer = newLayer;
        }
    }
}

