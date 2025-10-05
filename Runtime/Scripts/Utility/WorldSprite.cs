using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldSprite : MonoBehaviour
{
    public Vector2 UIScale = Vector2.one * 100;
    public Sprite sprite;

    private void OnEnable() => StartCoroutine(WaitToAdd());
    private void OnDisable() => WorldSpriteManager.instance?.RemoveItem(this);

    IEnumerator WaitToAdd()
    {
        while (WorldSpriteManager.instance == null)
            yield return null;
        WorldSpriteManager.instance?.AddItem(this);
    }
}
