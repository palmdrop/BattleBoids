using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using System.Runtime.InteropServices;
using Map;

public class CameraParent : MonoBehaviour
{
    private const float MINHeight = 5f;
    private const float MAXHeight = 40f;

    private const float CameraOffset = 15f;

    [SerializeField] private float speed = .1f;
    [SerializeField] private float zoomSpeed = 1f;
    [SerializeField] private float rotateSpeed = .1f;

    [SerializeField] private bool reversedControl = false;
    
    //Rotation Sensitivity
    [SerializeField] public float minAngle = 0f;
    [SerializeField] public float maxAngle = 90f;
    private float _cameraPitch;

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
            // Saves the position where the cursor currently is when holding right click down
            _currentCursorPosition = Input.mousePosition;
            _rightMouseButtonHeld = true;
        }

        ZoomCamera(_parentCamera);

        
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
                
        
        Vector3 lateralMove = horizontalSpeed * _parentCamera.right;
        Vector3 forwardMove = _parentCamera.forward;

        forwardMove.y = 0;
        forwardMove.Normalize();
        forwardMove *= verticalSpeed;
                
        // How much the camera should move in the x and z plane
        Vector3 move = lateralMove + forwardMove;

        // Makes sure that there is no continous movement when right mouse button is held down
        Vector3 currentPosition = _parentCamera.position;

        Rect bounds = gameObject.GetComponentInParent<Map.MapScript>().GetBounds();

        _parentCamera.position = new Vector3(
            Mathf.Clamp(move.x + currentPosition.x, bounds.xMin - CameraOffset, bounds.xMax + CameraOffset),
            currentPosition.y,
            Mathf.Clamp(move.z + currentPosition.z, bounds.yMin - CameraOffset, bounds.yMax + CameraOffset)
        );

    }

    private void ZoomCamera(Transform parentCamera)
    {
        float scrollSpeed = -zoomSpeed * NormalizeScrollMultiplier(Input.GetAxis("Mouse ScrollWheel"));

        // If the user is not scrolling, do nothing
        if (scrollSpeed == 0) return;
        _cameraParentPosition = parentCamera.position;
        Vector3 currentPosition = _parentCamera.position;

        // else increment the height of the camera with the zoomSpeed and limit its height
        _parentCamera.position = new Vector3(
            currentPosition.x,
            Mathf.Clamp(scrollSpeed + currentPosition.y, MINHeight, MAXHeight),
            currentPosition.z
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
        _cameraPitch = childCamera.eulerAngles.x;
        _cameraPitch = Mathf.Clamp (reversedControl ? _cameraPitch + dy : _cameraPitch - dy, minAngle ,maxAngle);
        
        childCamera.localRotation = Quaternion.Euler(new Vector3(_cameraPitch, 0, 0));
        
        _previousCursorPosition = _currentCursorPosition;
    }

    // This is used to make sure mouse scroll speed is consistent between a trackpad and an external mouse 
    private float NormalizeScrollMultiplier(float value) 
    {
        if (value > 0) return 1f;
        else if (value < 0) return -1f;
        else return 0;
    }
}

