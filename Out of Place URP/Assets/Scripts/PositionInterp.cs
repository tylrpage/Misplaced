using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PositionInterp : MonoBehaviour
{
    private Vector3 _previousPosition;
    private Vector3 _nextPosition;
    private float _t;

    private void Start()
    {
        _t = 0;
        _previousPosition = transform.position;
        _nextPosition = transform.position;
    }

    private void Update()
    {
        _t += Time.deltaTime * Constants.SERVER_TICKRATE;
        transform.position = Vector3.Lerp(_previousPosition, _nextPosition, _t);
    }

    public void PushNewPosition(Vector3 position)
    {
        _t = 0;
        _previousPosition = _nextPosition;
        _nextPosition = position;
    }

    public void Teleport(Vector3 position)
    {
        _t = 0;
        _previousPosition = position;
        _nextPosition = position;
    }
}
