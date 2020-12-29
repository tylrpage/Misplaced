using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrabbyHandBehavior : MonoBehaviour
{
    private readonly CursorMode CURSOR_MODE = CursorMode.ForceSoftware;
    
    [SerializeField] private Texture2D OpenHandSprite;
    [SerializeField] private Texture2D CloseHandSprite;
    [SerializeField] private Texture2D CursorSprite;
    private Vector2 _hotspot;

    private void Awake()
    {
        _hotspot = new Vector2(OpenHandSprite.width / 2f, OpenHandSprite.height / 2f);

        Pointer();
    }

    public void Pointer()
    {
        Cursor.SetCursor(CursorSprite, Vector2.zero, CURSOR_MODE);
    }

    public void OpenHand()
    {
        Cursor.SetCursor(OpenHandSprite, _hotspot, CURSOR_MODE);
    }
    
    public void CloseHand()
    {
        Cursor.SetCursor(CloseHandSprite, _hotspot, CURSOR_MODE);
    }
}
