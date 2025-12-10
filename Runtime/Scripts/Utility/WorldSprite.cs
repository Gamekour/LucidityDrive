using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldSprite : MonoBehaviour
{
    public Vector2 UIScale = Vector2.one * 0.05f;
    public Texture image;

    private void OnGUI()
    {
        Rect targetRect = new();
        targetRect.width = Screen.height * UIScale.x;
        targetRect.height = Screen.height * UIScale.y;
        Vector2 screenpos = Camera.main.WorldToScreenPoint(transform.position);
        targetRect.position = new Vector2(screenpos.x - targetRect.width / 2, Screen.height - screenpos.y - targetRect.height / 2);
        GUI.DrawTexture(targetRect, image);
    }
}
