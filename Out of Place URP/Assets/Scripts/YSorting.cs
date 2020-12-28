using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class YSorting : MonoBehaviour
{
    private Transform _transform;
    
    // Start is called before the first frame update
    void Start()
    {
        _transform = transform;
    }

    // Update is called once per frame
    void Update()
    {
        var position = _transform.position;
        transform.position = new Vector3(position.x, position.y, position.y);
        //_position = new Vector3(_position.x, _position.y, _position.y);
    }
}
