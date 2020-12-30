using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Nametag : MonoBehaviour
{
    [SerializeField] private TextMeshPro NameText;

    private string _name = "null";

    public void SetName(string name)
    {
        NameText.enabled = true;
        _name = name;
        UpdateText();
    }

    private void UpdateText()
    {
        NameText.text = _name;
    }
}
