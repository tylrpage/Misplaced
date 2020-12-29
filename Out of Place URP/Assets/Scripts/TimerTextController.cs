using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TimerTextController : MonoBehaviour
{
    [SerializeField] private TMP_Text TimerText;

    private float _timeLeft;
    private string _phase;
    private bool _enabled = false;

    private void Update()
    {
        if (_enabled)
        {
            _timeLeft -= Time.deltaTime;
            TimerText.text = "Time Left " + _phase + ": " + Math.Ceiling(_timeLeft) + "s";
        }
    }

    public void SetTimer(string phase, float time)
    {
        _phase = phase;
        _timeLeft = time;
        _enabled = true;
        TimerText.enabled = true;
    }

    public void HideTimer()
    {
        TimerText.enabled = false;
        _enabled = false;
    }
}
