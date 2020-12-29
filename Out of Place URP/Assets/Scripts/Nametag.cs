using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Nametag : MonoBehaviour
{
    [SerializeField] private TextMeshPro NameText;
    [SerializeField] private Transform NametagTransform;

    private string _name = "null";
    private int _pts = 0;
    
    private void LateUpdate()
    {
        NametagTransform.position = new Vector3(Mathf.Floor(transform.position.x * 16) / 16,
            (Mathf.Floor(transform.position.y * 16) / 16) + 1.125f, transform.position.z);
    }

    public void SetName(string name)
    {
        NameText.enabled = true;
        _name = name;
        UpdateText();
    }

    public void SetPts(int pts)
    {
        _pts = pts;
        UpdateText();
    }

    private void UpdateText()
    {
        NameText.text = name + "\npts: " + _pts;
    }
}
