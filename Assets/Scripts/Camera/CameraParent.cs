using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using System.Runtime.InteropServices;

public class CameraParent : MonoBehaviour
{
    private const float MINHeight = 4f;
    private const float MAXHeight = 40f;

    [SerializeField] private float speed = .1f;
    [SerializeField] private float zoomSpeed = 5f;
    [SerializeField] private float rotateSpeed = .1f;

    [SerializeField] private bool reversedControl = false;
    
    //Rotation Sensitivity
    [SerializeField] public float minAngle = 0f;
    [SerializeField] public float maxAngle = 90f;
    private float _yRotate;

    private Vector2 _previousCursorPosition;
    private Vector2 _currentCursorPosition;
    
    private Transform _parentCamera;
    private Transform _childCamera;
    
    private Vector3 _cameraParentPosition;

    private bool _rightMouseButtonHeld = false;

    // Start is called before the first frame update
    private void Start()
    {
        _parentCamera = transform;
        _childCamera = _parentCamera.GetChild(0).transform;
        
    }

    // Update is called once per frame
    private void Update()
    {
        // Checks if you clicked the button 
        if (Input.GetMouseButtonDown(1))
        {
            // Saves the position where you originally clicked the button
            _previousCursorPosition = Input.mousePosition;
        }
        
        if (Input.GetMouseButton(1))
        {
            _currentCursorPosition = Input.mousePosition;
            // Hide the cursor when the mouse is held down
            _rightMouseButtonHeld = true;
        }
        
        // Only move the camera container around the world
        // Yaw: rotate camera container, pitch: rotate child camera locally
    }
    
    private void FixedUpdate()
    {
        MoveCamera(_parentCamera);
        RotateCamera(_parentCamera, _childCamera);
    }

    private void MoveCamera(Transform cameraParent)
    {
        _cameraParentPosition = cameraParent.position;
                
        // Make the movement speed dependent on y coordinate (the more we zoom out,the faster we move)
        float horizontalSpeed = _cameraParentPosition.y * speed * Input.GetAxis("Horizontal");
        float verticalSpeed = _cameraParentPosition.y * speed * Input.GetAxis("Vertical");
        float scrollSpeed = Mathf.Log(_cameraParentPosition.y) * -zoomSpeed * Input.GetAxis("Mouse ScrollWheel");
                
        
        Vector3 verticalMove = new Vector3(0, scrollSpeed, 0);
        Vector3 lateralMove = horizontalSpeed * _parentCamera.right;
        Vector3 forwardMove = _parentCamera.forward;

        forwardMove.y = 0;
        forwardMove.Normalize();
        forwardMove *= verticalSpeed;
                
        Vector3 move = verticalMove + lateralMove + forwardMove;
        Vector3 currentPosition = _parentCamera.position;

        _parentCamera.position = new Vector3(
            move.x + currentPosition.x,
            Mathf.Clamp(move.y + currentPosition.y, MINHeight, MAXHeight),
            move.z + currentPosition.z
        );

    }

    
    // Parent camera remains orthogonal to the world normal, while the child camera pitch is local to the parent camera
    private void RotateCamera(Transform parentCamera, Transform childCamera)
    {
        if (!_rightMouseButtonHeld)
        {
            return;
        }

        // The change in x and y from where the cursor was originally clicked to where the cursor is right now
        float dx = (_currentCursorPosition.x - _previousCursorPosition.x) * rotateSpeed; 
        float dy = (_currentCursorPosition.y - _previousCursorPosition.y) * rotateSpeed;
        
        // Yaw
        parentCamera.rotation *= Quaternion.Euler(new Vector3(0, reversedControl ? -dx : dx, 0));

        // Pitch
        _yRotate = childCamera.eulerAngles.x;
        _yRotate = Mathf.Clamp (reversedControl ? _yRotate + dy : _yRotate - dy, minAngle ,maxAngle);
        
        childCamera.localRotation = Quaternion.Euler(new Vector3(_yRotate, 0, 0));
        
        _previousCursorPosition = _currentCursorPosition;
    }
}
