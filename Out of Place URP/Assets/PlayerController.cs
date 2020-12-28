using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float Speed;
    
    private SpriteRenderer _spriteRenderer;
    private Vector3 _inputDir;
    private Rigidbody2D _rb;
    
    private Animator _animator;
    private string _currentState;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponentInChildren<Animator>();
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        _inputDir = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

        if (_inputDir.magnitude > 0)
        {
            ChangeAnimationState("girl_run");
        }
        else
        {
            ChangeAnimationState("girl_idle");
        }
        
        if (_inputDir.x < 0)
        {
            _spriteRenderer.flipX = true;
        }
        else if (_inputDir.x > 0)
        {
            _spriteRenderer.flipX = false;
        }
    }

    private void FixedUpdate()
    {
        _rb.velocity = _inputDir * Speed;
    }

    private void ChangeAnimationState(string newState)
    {
        if (_currentState == newState) return;
        
        _animator.Play(newState);
        _currentState = newState;
    }
}
