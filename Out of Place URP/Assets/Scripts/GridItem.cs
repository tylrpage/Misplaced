using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridItem : MonoBehaviour
{
    public ushort Id;
    // Size in grid units
    public int Width;
    public int Height;
    // Position of lower left on grid
    public int InitialX;
    public int InitialY;
    
    public int X;
    public int Y;
    public bool Picked;

    private void Awake()
    {
        X = InitialX;
        Y = InitialY;
        transform.position = new Vector3(GridXToWorldPos(X), GridYToWorldPos(Y), 0);
    }

    public void Move(Vector2 pos)
    {
        X = WorldXtoGridPos(pos.x);
        Y = WorldYtoGridPos(pos.y);
        transform.position = new Vector3(GridXToWorldPos(X), GridYToWorldPos(Y), 0);
    }

    public void MoveToGridPos(ushort x, ushort y)
    {
        X = x;
        Y = y;
        transform.position = new Vector3(GridXToWorldPos(x), GridYToWorldPos(y), 0);
    }

    public void ResetPosition()
    {
        X = InitialX;
        Y = InitialY;
        transform.position = new Vector3(GridXToWorldPos(InitialX), GridYToWorldPos(InitialY), 0);
    }

    private int WorldXtoGridPos(float x)
    {
        return (int) (x / Constants.GRID_UNITS);
    }
    
    private int WorldYtoGridPos(float y)
    {
        return (int) (y / Constants.GRID_UNITS);
    }

    private float GridXToWorldPos(int x)
    {
        return x * Constants.GRID_UNITS + ((Constants.GRID_UNITS / 2f) * Width);
    }
    
    private float GridYToWorldPos(int y)
    {
        return y * Constants.GRID_UNITS;
    }
}
