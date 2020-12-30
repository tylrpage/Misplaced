using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float Speed;
    [SerializeField] private AudioClip StepSound;
    public float StepInterval;
    
    private Vector3 _inputDir;
    private Vector3 _inputDirRaw;
    private Rigidbody2D _rb;
    private AudioSource _audioSource;
    private float _timeTillNextStep = 0;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _audioSource = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        _inputDir = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        _inputDirRaw = new Vector3(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        
        if (_inputDirRaw != Vector3.zero && Time.time >= _timeTillNextStep)
        {
            _audioSource.PlayOneShot(StepSound);
            _timeTillNextStep = Time.time + StepInterval;
        }
    }

    private void FixedUpdate()
    {
        _rb.velocity = _inputDir * Speed;
    }
}
