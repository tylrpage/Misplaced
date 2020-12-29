﻿using System;
using System.Collections.Generic;
using UnityEngine;

public class Builder : MonoBehaviour
{
    public static event Action<ushort, int, int> ObjectMoved; 
    
    private Camera _mainCamera;
    private Vector3 _mousePos;

    // Outline stuff
    private SpriteRenderer _renderer;
    private Collider2D _previousCollider;
    private bool _active = false; // set to true when an object has an outline

    private bool[,] _grid;
    private GridItem _highlightedItem;
    private bool _movingItem;
    private bool _currentlyInValidPosition;
    private GrabbyHandBehavior _grabbyHand;
    private ushort _numberOfMovedObjects = 0;
    private HashSet<ushort> _movedObjects;
    
    // Start is called before the first frame update
    void Start()
    {
        _mainCamera = Camera.main;
        
        _grid = PopulateBoolGrid();
        _grabbyHand = GetComponent<GrabbyHandBehavior>();
    }

    public void Reset()
    {
        _numberOfMovedObjects = 0;
        _movedObjects = new HashSet<ushort>();
    }

    // Update is called once per frame
    void Update()
    {
        _mousePos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(_mousePos, Vector2.zero);
        if (!_movingItem)
        {
            if (hit.collider != null && hit.transform.CompareTag("Moveable"))
            {
                // Open grabby hand
                _grabbyHand.OpenHand();
                
                if (_previousCollider == null || !_active)
                {
                    _active = true;
                
                    _previousCollider = hit.collider;
                
                    _renderer = hit.collider.GetComponent<SpriteRenderer>();
                    _renderer.material.SetFloat("Vector1_5D8044E5", 1);
                
                    _highlightedItem = hit.collider.GetComponent<GridItem>();
                }
                else if (_previousCollider != hit.collider)
                {
                    _active = true;
                    _previousCollider = hit.collider;
                
                    // Remove outline from previously selected item
                    _renderer.material.SetFloat("Vector1_5D8044E5", 0);
                
                    _renderer = hit.collider.GetComponent<SpriteRenderer>();
                    _renderer.material.SetFloat("Vector1_5D8044E5", 1);

                    _highlightedItem = hit.collider.GetComponent<GridItem>();
                }
            }
            else if(_active)
            {
                _active = false;
                _renderer.material.SetFloat("Vector1_5D8044E5", 0);
                _highlightedItem = null;
                
                _grabbyHand.Pointer();
            }
        }
        else
        {
            _highlightedItem.Move(_mousePos);
            _currentlyInValidPosition = IsPositionValid(_grid, _highlightedItem.X, _highlightedItem.Y,
                _highlightedItem.Width,
                _highlightedItem.Height);
            if (_currentlyInValidPosition)
            {
                _renderer.material.SetColor("Color_BC0A261F", Color.green);
            }
            else
            {
                _renderer.material.SetColor("Color_BC0A261F", Color.red);
            }
        }
        

        if (Input.GetMouseButtonDown(0))
        {
            // Check if a object is being hovered
            if (_highlightedItem != null)
            {
                // Check if we are currently holding something
                if (_movingItem)
                {
                    // Reset the object's position is placed in an invalid location
                    if (!_currentlyInValidPosition || OverMovingLimit())
                    {
                        _highlightedItem.ResetPosition();
                    }
                    else
                    {
                        // Update the collision grid then update the objects initial positions
                        // so that it will reset to this spot next time and can update the grid
                        // correctly if moved again
                        UpdateGrid(_grid, _highlightedItem);
                        // _highlightedItem.InitialX = _highlightedItem.X;
                        // _highlightedItem.InitialY = _highlightedItem.Y;
                        
                        _grabbyHand.OpenHand();
                        _numberOfMovedObjects += 1;

                        ObjectMoved?.Invoke(_highlightedItem.Id, _highlightedItem.X, _highlightedItem.Y);
                    }
                    
                    // Either way, drop it
                    _highlightedItem = null;
                    _renderer.material.SetColor("Color_BC0A261F", Color.white);
                }
                else
                {
                    // Close/Grab with grabby hand
                    _grabbyHand.CloseHand();
                }
                
                _movingItem = !_movingItem;
            }
        }
    }

    private bool IsPositionValid(bool[,] grid, int x, int y, int width, int height)
    {
        if (x < 0 || x + width > grid.GetLength(0) || y < 0 || y + height > grid.GetLength(1))
        {
            return false;
        }
        else
        {
            bool valid = true;
            for (int x_i = 0; x_i < width; x_i++)
            {
                for (int y_i = 0; y_i < height; y_i++)
                {
                    if (grid[x_i + x, y_i + y] == true)
                    {
                        valid = false;
                    }
                }
            }

            return valid;
        }
    }

    private void UpdateGrid(bool[,] grid, GridItem changedItem)
    {
        // clear initial area
        for (int x = 0; x < changedItem.Width; x++)
        {
            for (int y = 0; y < changedItem.Height; y++)
            {
                grid[x + changedItem.InitialX, y + changedItem.InitialY] = false;
            }
        }
        
        // Set new positions to true
        for (int x = 0; x < changedItem.Width; x++)
        {
            for (int y = 0; y < changedItem.Height; y++)
            {
                grid[x + changedItem.X, y + changedItem.Y] = true;
            }
        }
    }

    private bool[,] PopulateBoolGrid()
    {
        bool[,] grid = new bool[Constants.GRID_WIDTH, Constants.GRID_HEIGHT];
        GameObject[] moveables = GameObject.FindGameObjectsWithTag("Moveable");
        GridItem gridItem;
        foreach (var moveable in moveables)
        {
            gridItem = moveable.GetComponent<GridItem>();
            for (int y = 0; y < gridItem.Height; y++)
            {
                for (int x = 0; x < gridItem.Width; x++)
                {
                    grid[gridItem.X + x, gridItem.Y + y] = true;
                }
            }
        }

        return grid;
    }

    // True when moving an object we haven't moved before and we moved max amount of objects
    private bool OverMovingLimit()
    {
        return _numberOfMovedObjects >= Constants.MAX_SHIFTED_OBJECTS && !_movedObjects.Contains(_highlightedItem.Id);
    }
}
