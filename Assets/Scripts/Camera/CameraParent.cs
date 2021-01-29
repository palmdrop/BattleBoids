using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

public class CameraParent : MonoBehaviour
{
    
    private const float MINHeight = 4f;
    private const float MAXHeight = 40f;

    [SerializeField] private float speed = 0.06f;
    [SerializeField] private float zoomSpeed = 10.0f;
    [SerializeField] private float rotateSpeed = 0.01f;



    [SerializeField] private bool cursorVisible = false;
    [SerializeField] private bool reversedControl = false;
    //Rotation Sensitivity
    [SerializeField] public float minAngle = 0f;
    [SerializeField] public float maxAngle = 90f;
    private float _yRotate;

    private Vector2 _cursorStartPosition;
    private Vector2 _cursorEndPosition;
    
    private Transform _cameraParent;
    private Transform _childCamera;
    
    private Vector3 _cameraParentPosition;
    // Start is called before the first frame update
    void Start()
    {
        _cameraParent = transform;
        _childCamera = _cameraParent.GetChild(0).transform;
        
    }

    // Update is called once per frame
    private void Update()
    {
        
        // Only move the camera container around the world
        MoveCamera(_cameraParent);
        // Yaw: rotate camera container, pitch: rotate child camera locally
        RotateCamera(_cameraParent, _childCamera);
    }

    private void MoveCamera(Transform cameraParent)
    {
        
        _cameraParentPosition = cameraParent.position;
                
        // Make the movement speed dependent on y coordinate (the more we zoom out,the faster we move)
        float horizontalSpeed = _cameraParentPosition.y            * speed      * Input.GetAxis("Horizontal");
        float verticalSpeed   = _cameraParentPosition.y            * speed      * Input.GetAxis("Vertical");
        float scrollSpeed     = Mathf.Log(_cameraParentPosition.y) * -zoomSpeed * Input.GetAxis("Mouse ScrollWheel");
                
        
        Vector3 verticalMove = new Vector3(0, scrollSpeed, 0);
        Vector3 lateralMove = horizontalSpeed * _cameraParent.right;
        Vector3 forwardMove = _cameraParent.forward;

        forwardMove.y = 0;
        forwardMove.Normalize();
        forwardMove *= verticalSpeed;
                
        Vector3 move = verticalMove + lateralMove + forwardMove;
        Vector3 currentPosition = _cameraParent.position;
        
        _cameraParent.position = new Vector3(
                move.x + currentPosition.x, 
                Mathf.Clamp(move.y + currentPosition.y, 4, 40),
                move.z + currentPosition.z
            );

    }
    
    // Parent camera remains orthogonal to the world normal, while the child camera pitch is local to the parent camera
    private void RotateCamera(Transform parentCamera, Transform childCamera) 
    {
        _yRotate = childCamera.eulerAngles.x;
        
        // Right click when clicked
        if (Input.GetMouseButtonDown(1)) 
        {
            // Safe the position from where right clicked where originally pressed down
            _cursorStartPosition = Input.mousePosition;
        }

        // Right click when held
        if (Input.GetMouseButton(1))
        {
            // Do hide the cursor when the mouse is held down
            Cursor.visible = cursorVisible;
            
            _cursorEndPosition = Input.mousePosition;

            // The change in x and y from where the cursor was originally clicked to where the cursor is right now
            float dx = (_cursorEndPosition - _cursorStartPosition).x * rotateSpeed; 
            float dy = (_cursorEndPosition - _cursorStartPosition).y * rotateSpeed;
            
            // Yaw
            parentCamera.rotation *= Quaternion.Euler(new Vector3(0, reversedControl ? -dx : dx, 0));

            // Pitch
            _yRotate = Mathf.Clamp (reversedControl ? _yRotate + dy : _yRotate - dy, minAngle ,maxAngle);
            childCamera.localRotation = Quaternion.Euler(new Vector3(_yRotate, 0, 0));


            _cursorStartPosition = _cursorEndPosition;
        }
        else
        {
            // When the right click is no longer held down, show the cursor 
            Cursor.visible = true;
        }
    }
}
