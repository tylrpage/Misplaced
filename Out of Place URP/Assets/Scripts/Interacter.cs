using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interacter : MonoBehaviour
{
    [SerializeField] private Transform LocalPlayerTransform;
    public float MaxInteractRange;
    
    private SpriteRenderer _renderer;
    private Collider2D _previousCollider;
    private bool _active = false; // set to true when an object has an outline
    
    private Camera _mainCamera;
    private Vector3 _mousePos;
    private Vector3 _clampedPos;
    private GridItem _highlightedItem;
    private Client _client;

    public int PointChange;

    void Awake()
    {
        _client = GetComponent<Client>();
        _mainCamera = Camera.main;
    }

    private void Update()
    {
        _mousePos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        _clampedPos =
            Vector3.ClampMagnitude(new Vector3(_mousePos.x, _mousePos.y, LocalPlayerTransform.position.z) - LocalPlayerTransform.position,
                MaxInteractRange) + LocalPlayerTransform.position;
        RaycastHit2D hit = Physics2D.Raycast(_clampedPos, Vector2.zero);

        if (hit.collider != null && hit.transform.CompareTag("Moveable"))
        {
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
                
                // Remove outline from previously selected item if it wasn't clicked
                if (!_highlightedItem.Picked)
                {
                    _renderer.material.SetFloat("Vector1_5D8044E5", 0);
                }

                _renderer = hit.collider.GetComponent<SpriteRenderer>();
                _renderer.material.SetFloat("Vector1_5D8044E5", 1);

                _highlightedItem = hit.collider.GetComponent<GridItem>();
            }
        }
        else if(_active)
        {
            _active = false;
            // Only remove borders on non picked items
            if (!_highlightedItem.Picked)
            {
                _renderer.material.SetFloat("Vector1_5D8044E5", 0);
            }
            _highlightedItem = null;
        }

        if (Input.GetMouseButtonDown(0) && _highlightedItem && !_highlightedItem.Picked)
        {
            _highlightedItem.Picked = true;
            if (_client.IsMyGuessCorrect(_highlightedItem.Id))
            {
                _renderer.material.SetColor("Color_BC0A261F", Color.green);
                PointChange += 1;
            }
            else
            {
                _renderer.material.SetColor("Color_BC0A261F", Color.red);
                PointChange -= 1;
            }
        }
    }
}
