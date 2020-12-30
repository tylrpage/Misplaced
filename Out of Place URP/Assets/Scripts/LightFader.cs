using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using Random = UnityEngine.Random;

public class LightFader : MonoBehaviour
{
    public float MinIntensity;
    public float MaxIntensity;
    public float Speed;
    [SerializeField] private Light2D Light2D;
    [SerializeField] private Transform LightObject;
    private float _randomStart;

    private void Awake()
    {
        _randomStart = Random.Range(0, 1f);
        Debug.Log(_randomStart);
    }

    // Update is called once per frame
    void Update()
    {
        float func = Mathf.Abs(Mathf.Cos((Time.time) * Mathf.PI * Speed) + _randomStart);
        Light2D.intensity = func * (MaxIntensity - MinIntensity) + MinIntensity;
        transform.localScale = new Vector3(func + 1, func + 1, 1);
    }
}
