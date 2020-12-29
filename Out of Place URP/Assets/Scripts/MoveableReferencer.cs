using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Keeps references to all the moveable objects
public class MoveableReferencer : MonoBehaviour
{
    public Dictionary<ushort, GridItem> Moveables { get; private set; } = new Dictionary<ushort, GridItem>();
    
    private void Awake()
    {
        var objects = GameObject.FindGameObjectsWithTag("Moveable");
        foreach (var moveableObject in objects)
        {
            GridItem gridItem = moveableObject.GetComponent<GridItem>();
            Moveables[gridItem.Id] = gridItem;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
