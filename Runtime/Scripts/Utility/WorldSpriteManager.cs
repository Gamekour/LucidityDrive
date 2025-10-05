using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WorldSpriteManager : MonoBehaviour
{
    public static WorldSpriteManager instance;
    private Dictionary<WorldSprite, RectTransform> spritePairs = new();

    private void Awake() => instance = this;

    public void AddItem(WorldSprite worldSprite)
    {
        // Create a new GameObject for the UI sprite
        GameObject go = new GameObject("WorldSpriteUI", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(transform, false);

        // Set up RectTransform
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = worldSprite.UIScale;

        // Set up Image (no sprite assigned yet)
        Image img = go.GetComponent<Image>();
        img.raycastTarget = false;

        img.sprite = worldSprite.sprite;

        spritePairs.Add(worldSprite, rect);
    }

    public void RemoveItem(WorldSprite worldSprite)
    {
        RectTransform rect = spritePairs[worldSprite];
        spritePairs.Remove(worldSprite);
        Destroy(rect.gameObject);
    }

    private void Update()
    {
        foreach(WorldSprite ws in spritePairs.Keys)
        {
            spritePairs[ws].position = Camera.main.WorldToScreenPoint(ws.transform.position);
        }
    }
}
