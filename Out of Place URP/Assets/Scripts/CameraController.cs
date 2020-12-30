using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

public class CameraController : MonoBehaviour
{
    public Transform FollowTarget;
    public float PositionLerpAmount;
    public float MaxMousePull;

    private Vector3 _targetPosition;
    private Camera _camera;

    private void Awake()
    {
        _camera = Camera.main;
        transform.position = new Vector3(FollowTarget.position.x, FollowTarget.position.y, -10f);
        
        Client.EnteringBuildingMode += ClientOnEnteringBuildingMode;
        Client.ExitingBuildingMode += ClientOnExitingBuildingMode;
    }

    private void ClientOnExitingBuildingMode()
    {
        PixelPerfectCamera pixelPerfectCamera = GetComponent<PixelPerfectCamera>();
        pixelPerfectCamera.assetsPPU = Constants.CAMERA_ASSET_PPU;
    }

    private void ClientOnEnteringBuildingMode()
    {
        PixelPerfectCamera pixelPerfectCamera = GetComponent<PixelPerfectCamera>();
        pixelPerfectCamera.assetsPPU = Constants.CAMERA_ASSET_PPU / 2;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Vector3 mousePos = _camera.ScreenToWorldPoint(Input.mousePosition);
        Vector3 clampedMouseOffset = Vector3.ClampMagnitude(mousePos - FollowTarget.position, MaxMousePull);
        //_targetPosition = (FollowTarget.position + mousePos) / 2f;
        _targetPosition = FollowTarget.position + clampedMouseOffset;
        _targetPosition = new Vector3(_targetPosition.x, _targetPosition.y, -10f);
        transform.position = Vector3.Lerp(transform.position, _targetPosition, PositionLerpAmount);
    }

    private void OnDestroy()
    {
        Client.EnteringBuildingMode -= ClientOnEnteringBuildingMode;
    }
}
