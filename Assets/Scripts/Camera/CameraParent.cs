using System;
using Unity.Physics;
using UnityEngine;
using UnityEngine.UIElements;
using Collider = UnityEngine.Collider;
using ContactPoint = UnityEngine.ContactPoint;

public class CameraParent : MonoBehaviour
{
    // Used to limit where the camera can move
    private Rect _mapBounds;
    [SerializeField] private float allowedMapBoundOffset = 100f;
    
    // Scripts
    private Map.Map map;
    private SelectObject selectObject;

    //Controls the speed of different interactions 
    [SerializeField] private float speed = .1f;
    [SerializeField] private float zoomSpeed = 1f;
    [SerializeField] private float rotateSpeed = .1f;
    
    // Flips the controllers for rotation
    [SerializeField] private bool reversedControl = false;
    
    // How much you can pitch the camera
    [SerializeField] public float minAngle = 0f;
    [SerializeField] public float maxAngle = 90f;
    private float _cameraPitch;

    // Used to track mouse movement when rotating
    private Vector2 _previousCursorPosition;
    private Vector2 _currentCursorPosition;
    
    // Yaw: parent camera
    // Pitch: child camera
    private Rigidbody _parentCamera;
    private Transform _childCamera;
    

    private bool _rightMouseButtonHeld = false;
    private bool _cameraFollowGameObject = false;

    private Transform selectedObject;
    
    private Vector3 _selectedGameObjectPosition;
    private Vector3 _parentCameraPosition;


    private GameManager _gameManager;

    // Start is called before the first frame update
    private void Start()
    {
        _parentCamera = GetComponent<Rigidbody>();
        _childCamera = transform.GetChild(0).transform;
        _gameManager = FindObjectOfType<GameManager>();
        
        // Map script used to find map bounds
        map = FindObjectOfType<Map.Map>();
        
        // SelecObject script used to select a object you can follow around
        selectObject = FindObjectOfType<SelectObject>();
    }

    // Update is called once per frame
    private void Update()
    {
        
        _mapBounds = map.GetBounds();

        // If left mouse click is pressed
        // Retrieve the object clicked on

        if (selectObject.IsObjectSelected() && _cameraFollowGameObject)
        {
            selectedObject = selectObject.GetCurrentlySelected().transform;
        }
            

        // If there is a selected GameObject
        if (selectedObject)
        {
            // Get its positions, this is used to track where the object is
            Vector3 selectedObjectPosition = selectedObject.position;
            _selectedGameObjectPosition = new Vector3(
                selectedObjectPosition.x,
                _parentCamera.position.y,
                selectedObjectPosition.z
            ); 
        }
        
        // Checks if you clicked the button 
        if (Input.GetMouseButtonDown(1) && !_gameManager.IsPaused())
        {
            // Saves the position where you originally clicked the button
            _previousCursorPosition = Input.mousePosition;
        }
        
        // If right mouse button is held
        if (Input.GetMouseButton(1) && !_gameManager.IsPaused())
        {
            // Saves the position where the cursor currently is when holding right click down
            _currentCursorPosition = Input.mousePosition;
            _rightMouseButtonHeld = true;
        }


        // When you press "f", you toggle if you want to follow the selected GameObject
        if (Input.GetKeyDown("f"))
        {
            _cameraFollowGameObject = !_cameraFollowGameObject;
        }

        
    }

    private void FixedUpdate()
    {
        LockCameraToGameObject();
        RotateCamera();
        MoveCamera();
    }

    private void OnCollisionEnter(Collision other)
    {
        // We don't want the camera to collide with the invisible wall surrounding the map
        if (other.gameObject.name == "Wall")
        {
            Physics.IgnoreCollision(other.collider, GetComponent<Collider>());
        }
    }


    private void MoveCamera()
    {
        if (_gameManager.IsPaused())
            return;
        _parentCameraPosition = transform.position;

        float zoomOutMultiplier = Mathf.Max(2f, _parentCameraPosition.y/2);
                
        // Make the movement speed dependent on y coordinate (the more we zoom out,the faster we move)
        float horizontalSpeed = zoomOutMultiplier * speed * Input.GetAxis("Horizontal");
        float verticalSpeed = zoomOutMultiplier * speed * Input.GetAxis("Vertical");
                
        
        Vector3 lateralMove = horizontalSpeed * transform.right;
        Vector3 forwardMove = transform.forward;

        Vector3 upDownMoveAmount = transform.up * (NormalizeScrollMultiplier(Input.GetAxis("Mouse ScrollWheel")) * zoomSpeed * zoomOutMultiplier);

        
        // Y is set to zero to avoid moving up and down with movement key, we want to restrict it to scroll
        forwardMove.y = 0;
        forwardMove.Normalize();
        forwardMove *= verticalSpeed;
        
                
        // How much the camera should move in the x, y and z plane
        Vector3 move = lateralMove + forwardMove + upDownMoveAmount;

        // Moves the camera
        _parentCamera.velocity = move;
        
        // Constrains the camera to a defined offset
        transform.position = new Vector3(
            Mathf.Clamp(_parentCameraPosition.x, _mapBounds.xMin - allowedMapBoundOffset, _mapBounds.xMax + allowedMapBoundOffset),
            Mathf.Clamp(_parentCameraPosition.y,  -allowedMapBoundOffset, allowedMapBoundOffset),
            Mathf.Clamp( _parentCameraPosition.z, _mapBounds.yMin - allowedMapBoundOffset, _mapBounds.yMax + allowedMapBoundOffset)
        );

    }

    
    // Parent camera remains orthogonal to the world normal, while the child camera pitch is local to the parent camera
    private void RotateCamera()
    {
        if (!_rightMouseButtonHeld)
        {
            return;
        }

        // The change in x and y from where the cursor was originally clicked to where the cursor is right now
        float dx = (_currentCursorPosition.x - _previousCursorPosition.x) * rotateSpeed; 
        float dy = (_currentCursorPosition.y - _previousCursorPosition.y) * rotateSpeed;
        
        // Yaw
        _parentCamera.rotation *= Quaternion.Euler(new Vector3(0, reversedControl ? -dx : dx, 0));

        // Pitch
        _cameraPitch = _childCamera.eulerAngles.x;
        _cameraPitch = Mathf.Clamp (reversedControl ? _cameraPitch + dy : _cameraPitch - dy, minAngle ,maxAngle);
        
        _childCamera.localRotation = Quaternion.Euler(new Vector3(_cameraPitch, 0, 0));

        _previousCursorPosition = _currentCursorPosition;
    }

    private void LockCameraToGameObject()
    {
        // If the selected object is invalid or you are not currently in the following state
        if (!selectedObject || !_cameraFollowGameObject)
        {
            _cameraFollowGameObject = false;
            return;
        }
        
        // Else transition smoothly to the objects location
        _parentCamera.position = Vector3.Lerp(_parentCameraPosition, _selectedGameObjectPosition, .2f);
    }

    // This is used to make sure mouse scroll speed is consistent between a trackpad and an external mouse 
    private float NormalizeScrollMultiplier(float value) 
    {
        if (value > 0) return 1f;
        if (value < 0) return -1f; 
        return 0;
    }
}

