using System;
using UnityEngine;

[ExecuteInEditMode]
public class Test : MonoBehaviour
{
    public Camera _Camera;

    public Vector3 _Point;
    // Start is called before the first frame update
    void Start()
    {
        var p = RectTransformUtility.WorldToScreenPoint( _Camera, _Point);
        Debug.Log(p);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnValidate()
    {
        transform.position = _Point;
        var p = RectTransformUtility.WorldToScreenPoint( _Camera, transform.position);
        Debug.Log(p);
    }
}
