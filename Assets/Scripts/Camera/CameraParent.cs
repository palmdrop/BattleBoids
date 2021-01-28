using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    private Vector2 _mousePositionWhenPressed;
    private Vector2 _mousePositionWhenDragging;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    private void Update()
    {
        Transform cameraParent = transform;
        Vector3 cameraParentPosition = cameraParent.position;
        
        float horizontalSpeed = cameraParentPosition.y * speed * Input.GetAxis("Horizontal");
        float verticalSpeed = cameraParentPosition.y * speed * Input.GetAxis("Vertical");
        float scrollSpeed = Mathf.Log(cameraParentPosition.y) * -zoomSpeed * Input.GetAxis("Mouse ScrollWheel");

        if ((transform.position.y >= MAXHeight) && (scrollSpeed > 0) || (transform.position.y <= MINHeight) && (scrollSpeed < 0)) 
        {
            scrollSpeed = 0;
        } 

        if((transform.position.y + scrollSpeed) > MAXHeight) 
        {
            scrollSpeed = MAXHeight - transform.position.y;
        }
        else if ((transform.position.y + scrollSpeed) < MINHeight) 
        {
            scrollSpeed = MINHeight - transform.position.y;
        }

        Vector3 verticalMove = new Vector3(0, scrollSpeed, 0);
        Vector3 lateralMove = horizontalSpeed * transform.right;
        Vector3 forwardMove = cameraParent.forward;


        forwardMove.y = 0;
        forwardMove.Normalize();
        forwardMove *= verticalSpeed;

        Vector3 move = verticalMove + lateralMove + forwardMove;

        transform.position += move;

        RotateCamera();
    }
    
    private void RotateCamera() 
    {
        Transform childCamera = transform.GetChild(0).transform;
        _yRotate = childCamera.eulerAngles.x;
        
        // Right click when clicked
        if (Input.GetMouseButtonDown(1)) 
        {
            // Safe the position from where right clicked where originally pressed down
            _mousePositionWhenPressed = Input.mousePosition;
        }

        // Right click when held
        if (Input.GetMouseButton(1))
        {
            // Do hide the cursor when the mouse is held down
            Cursor.visible = cursorVisible;
            
            _mousePositionWhenDragging = Input.mousePosition;

            // The change in x and y from where the cursor was originally clicked to where the cursor is right now
            float dx = (_mousePositionWhenDragging - _mousePositionWhenPressed).x * rotateSpeed; 
            float dy = (_mousePositionWhenDragging - _mousePositionWhenPressed).y * rotateSpeed;
            
            // Yaw
            transform.rotation *= Quaternion.Euler(new Vector3(0, reversedControl ? -dx : dx, 0));

            // Pitch
            _yRotate = Mathf.Clamp (reversedControl ? _yRotate + dy : _yRotate - dy, minAngle ,maxAngle);
            childCamera.localRotation = Quaternion.Euler(new Vector3(_yRotate, 0, 0));

        }
        else
        {
            // When the right click is no longer held down, show the cursor 
            Cursor.visible = true;
        }
        

    }

}
