using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Nametag : MonoBehaviour
{
    [SerializeField] private TextMeshPro NameText;
    [SerializeField] private Transform NametagTransform;

    private void LateUpdate()
    {
        NametagTransform.position = new Vector3(Mathf.Floor(transform.position.x * 16) / 16,
            (Mathf.Floor(transform.position.y * 16) / 16) + 1.125f, transform.position.z);
    }

    public void SetText(string name)
    {
        NameText.enabled = true;
        NameText.text = name;
    }
}
