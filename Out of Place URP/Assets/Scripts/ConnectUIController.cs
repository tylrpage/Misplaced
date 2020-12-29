﻿using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConnectUIController : MonoBehaviour
{
    [SerializeField] private TMP_InputField NameInput;
    [SerializeField] private Button ConnectButton;
    
    public string DisplayName { get; private set; }

    public void OnNameInputChanged(string newName)
    {
        Regex rg = new Regex(@"^[a-zA-Z0-9]*$");
        if (rg.IsMatch(newName))
        {
            DisplayName = newName;
            ConnectButton.interactable = true;
        }
        else
        {
            ConnectButton.interactable = false;
        }
    }
}
