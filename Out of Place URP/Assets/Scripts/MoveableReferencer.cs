using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Keeps references to all the moveable objects
public class MoveableReferencer : MonoBehaviour
{
    public Dictionary<ushort, GridItem> Moveables { get; private set; } = new Dictionary<ushort, GridItem>();
    public Dictionary<ushort, SpriteRenderer> MoveablesRenderers = new Dictionary<ushort, SpriteRenderer>();
    
    private void Awake()
    {
        var objects = GameObject.FindGameObjectsWithTag("Moveable");
        foreach (var moveableObject in objects)
        {
            GridItem gridItem = moveableObject.GetComponent<GridItem>();
            Moveables[gridItem.Id] = gridItem;
            MoveablesRenderers[gridItem.Id] = moveableObject.GetComponent<SpriteRenderer>();
        }
    }

    public void ResetMoveables()
    {
        foreach (var moveableObject in Moveables.Values)
        {
            moveableObject.HardResetPosition();
        }

        foreach (var renderers in MoveablesRenderers.Values)
        {
            renderers.material.SetFloat("Vector1_5D8044E5", 0);
            renderers.material.SetColor("Color_BC0A261F", Color.white);
        }
    }
}
