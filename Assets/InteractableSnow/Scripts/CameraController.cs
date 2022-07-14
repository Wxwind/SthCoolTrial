using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public GameObject _lookAt;
    public float _distance;
    public float _mouseSensitivity;
    public float _height;
    public bool isNeedPosDamping;
    public bool isNeedRotateDamping;
    public float _moveDamping = 2.5f;
    public float _rotateDamping = 2.5f;
    private const float MINRX=-2f;
    private const float MAXRX=89f;

    private Vector3 _camForward
    {
        get => transform.forward;
    }
    public Vector3 _camUp{ get; private set; }
    public Vector3 _camRight { get; private set; }
    public Vector3 _castForward;
    private Camera _camera;
    private float _mouseRx, _mouseRy;


    private void Start()
    {
        Application.targetFrameRate = 60;
        _camUp = Vector3.up;
        ReCalCastForward();
        Cursor.visible = false;
    }

    void FixedUpdate()
    {
        _camera = GetComponent<Camera>();
        _mouseRy += Input.GetAxis("Mouse X") * _mouseSensitivity * 0.1f;
        _mouseRx -= Input.GetAxis("Mouse Y") * _mouseSensitivity * 0.1f;
        Debug.Log(_mouseRy);
        _mouseRx = math.clamp(_mouseRx,MINRX, MAXRX);

        Quaternion newCamRotation = Quaternion.Euler(_mouseRx, _mouseRy, 0);
        Vector3 newCamPos = _lookAt.transform.position - _camForward * _distance + _height * Vector3.up;
        if (isNeedPosDamping)
        {
            transform.position = Vector3.Lerp(transform.position, newCamPos, Time.fixedDeltaTime * _moveDamping);
        }
        else
        {
            transform.position =newCamPos;
        }
        if (isNeedRotateDamping)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, newCamRotation, Time.fixedDeltaTime * _rotateDamping);
        }
        else
        {
            transform.rotation = newCamRotation;
        }
        //摄像机update频率更新情况下
        //position  rotation   result  
        //  no     lerp      摄像机高速移动抽搐（以update频率影响position和rotation）/(鼠标移动不连续导致抽搐)（需要position插值）
        //  lerp    lerp     角色跑步抽搐/（摄像机update更新，角色刚体fixedupdate更新，频率不一致，角色更新速度一次，摄像机可能更新位置一次或者多次）
        //  no       no      摄像机高速移动抽搐/(鼠标移动不连续导致抽搐)（需要插值）
        //  lerp     no      角色跑步抽搐 且 摄像机高速移动抽搐（不明显） （需要rotation插值）
        //摄像机fixedupdate频率更新情况下
        //position  rotation  result  
        //  no     lerp      摄像机高速移动抽搐(不明显)/(鼠标移动不连续导致抽搐)（需要position插值）
        //  lerp    lerp     正常
        //  no       no      摄像机高速移动抽搐/(鼠标移动不连续导致抽搐)（需要position和rotation插值）
        //  lerp     no      摄像机高速移动抽搐/(鼠标移动不连续导致抽搐)（需要rotation插值）
        //总结
        //rotation和position未插值 导致摄像机高速移动导致画面抽搐
        //角色刚体更新频率和摄像机更新频率不一致 导致角色跑步画面抽搐
        ReCalCastForward();
    }

    private void OnDestroy()
    {
        Cursor.visible = true;
    }

    private void ReCalCastForward()
    {
        var left = Vector3.Cross(_camForward, _camUp).normalized;
        _camRight = -left;
        var castForward = Vector3.Cross(_camUp, left).normalized;
        _castForward=castForward;
    }
}