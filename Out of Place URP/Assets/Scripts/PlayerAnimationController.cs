using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    private Animator _animator;
    private Vector3 _previousPosition;
    private string _currentAnimationState;
    private SpriteRenderer _spriteRenderer;
    

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Start()
    {
        _previousPosition = transform.position;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        Vector3 difference = (transform.position - _previousPosition) / Time.deltaTime;
        if (difference.magnitude > 0.01f)
        {
            ChangeAnimationState("girl_run");

            if (difference.x < -0.01f)
            {
                _spriteRenderer.flipX = true;
            }
            else if (difference.x > 0.01f)
            {
                _spriteRenderer.flipX = false;
            }
        }
        else
        {
            ChangeAnimationState("girl_idle");
        }

        _previousPosition = transform.position;
    }
    
    private void ChangeAnimationState(string newState)
    {
        if (_currentAnimationState == newState) return;
        
        _animator.Play(newState);
        _currentAnimationState = newState;
    }
}
