using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Interacter : MonoBehaviour
{
    [SerializeField] private Transform LocalPlayerTransform;
    [SerializeField] private TMP_Text StatusText;
    [SerializeField] private AudioClip DingSound;
    [SerializeField] private AudioClip ExplodeSound;
    public float MaxInteractRange;
    
    private SpriteRenderer _renderer;
    private Collider2D _previousCollider;
    private bool _active = false; // set to true when an object has an outline
    
    private Camera _mainCamera;
    private Vector3 _mousePos;
    private Vector3 _clampedPos;
    private GridItem _highlightedItem;
    private Client _client;
    private GrabbyHandBehavior _grabbyHand;
    private AudioSource _audioSource;
    private Animator _playerAnimator;
    private PlayerController _playerController;

    public int CorrectItems;
    public int WrongItems;

    void Awake()
    {
        _client = GetComponent<Client>();
        _grabbyHand = GetComponent<GrabbyHandBehavior>();
        _audioSource = GetComponent<AudioSource>();
        _playerAnimator = LocalPlayerTransform.GetComponentInChildren<Animator>();
        _playerController = LocalPlayerTransform.GetComponent<PlayerController>();
        _mainCamera = Camera.main;
    }

    public void Reset()
    {
        CorrectItems = 0;
        WrongItems = 0;
        UpdateStatusText();
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
            if (Input.GetMouseButtonDown(0))
            {
                _grabbyHand.CloseHand();
            }
            else if (Input.GetMouseButtonUp(0))
            {
                _grabbyHand.OpenHand();
            }
            
            if (_previousCollider == null || !_active)
            {
                _grabbyHand.OpenHand();
                
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
            _grabbyHand.Pointer();
            
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
                CorrectItems += 1; 
                
                _audioSource.PlayOneShot(DingSound);
            }
            else
            {
                _renderer.material.SetColor("Color_BC0A261F", Color.red);
                WrongItems += 1;
                
                _audioSource.PlayOneShot(ExplodeSound);
                _playerAnimator.Play("girl_explode");
                _playerController.enabled = false;
            }
            
            UpdateStatusText();
        }
    }

    // Searching status text
    private void UpdateStatusText()
    {
        StatusText.enabled = true;
        StatusText.text = "Found " + CorrectItems + "/" + _client.MovedItems.Count;
    }
}
