using System.Collections.Generic;
using UnityEngine;

public class Selection : MonoBehaviour
{
    private SelectionManager _selectionManager;

    // The camera that is used for the projection from world view to screen view
    private Camera _selectionCamera;

    // This is the selection area displayed on the UI
    [SerializeField] private RectTransform selectionAreaUI;

    // The UI component used to get the active player flock
    private GameUI _gameUI;

    // This is the selection invisible selection area in the game
    private Rect _selectionArea;

    // The position of the boid projected on to the screen
    private Vector3 entityScreenPosition;

    // Start and end position for the selection area
    private Vector2 _startSelectionPosition;
    private Vector2 _endSelectionPosition;

    // Offset for finding closest boid when clicking
    private const float Offset = 6f;

    
    
    private void Start()
    {
        _selectionManager = GetComponentInParent<SelectionManager>();
        _selectionCamera = _selectionManager.GetMainCamera();
        
        _gameUI = _selectionManager.GetGameUI();
        ResetDrawUISelectionArea();
    }

    private void Update()
    {
        // You can't select if you are currently purchasing new units
        if (_gameUI.GetActivePlayer().GetSpawnArea().isHolding())
        {
            _selectionManager.CanSelect = false;
            return;     
        }
        

        if (_selectionManager.GETSelectedEntities().Count > 0)
        {
            _selectionManager.CanSelect = false;
        }
        
        // If you are currently moving entities around, you can't select new ones before they are placed
        if (!_selectionManager.IsPlaceable())
        {
            return;
        }
        
        // When you click the left mouse button
        if (Input.GetMouseButtonDown(0))
        {
            _selectionManager.Deselect();

            // Get the position of the mouse once
            _startSelectionPosition = Input.mousePosition;

            _selectionArea = new Rect();
        }

        

        // When the left mouse button is held down
        if (Input.GetMouseButton(0) && _selectionManager.CanSelect)
        {
            
            
            // Continuously update the position of the mouse
            _endSelectionPosition = Input.mousePosition;
            
           

            DrawUISelectionArea();
            
            /*
             * Calculate the selection area in the world view
             */
            
            _selectionArea.xMin = Mathf.Min(Input.mousePosition.x, _startSelectionPosition.x);
            _selectionArea.yMin = Mathf.Min(Input.mousePosition.y, _startSelectionPosition.y);
            
            _selectionArea.xMax = Mathf.Max(Input.mousePosition.x, _startSelectionPosition.x);
            _selectionArea.yMax = Mathf.Max(Input.mousePosition.y, _startSelectionPosition.y);
            
        }

        // When the left mouse button is released
        if (Input.GetMouseButtonUp(0))
        {
            if (_selectionManager.CanSelect)
            {
                // Detects if the mouse is clicked in without moving
                if (_startSelectionPosition == _endSelectionPosition)
                {
                    _selectionArea.xMin = Input.mousePosition.x - Offset;
                    _selectionArea.xMax = Input.mousePosition.x + Offset;
                    
                    _selectionArea.yMin = Input.mousePosition.y - Offset;
                    _selectionArea.yMax = Input.mousePosition.y + Offset;
                }
                
                SelectPlayerFlockEntities();
                ResetDrawUISelectionArea();
            }

            _selectionManager.CanSelect = true;
        }
}

    private void ResetDrawUISelectionArea()
    {
            // Reset the selection area positions and graphical representation
            _startSelectionPosition = _endSelectionPosition = Vector2.zero;
            DrawUISelectionArea();
    }

    // The selection area that will be drawn on the canvas
    private void DrawUISelectionArea()
    {
        Vector2 center = (_startSelectionPosition + _endSelectionPosition) / 2;

        selectionAreaUI.position = center;

        float selectionAreaUIWidth = Mathf.Abs(_startSelectionPosition.x - _endSelectionPosition.x);
        float selectionAreaUIHeight = Mathf.Abs(_startSelectionPosition.y - _endSelectionPosition.y);

        // Draws the selection area in relation to the set anchor points
        selectionAreaUI.sizeDelta = new Vector2(selectionAreaUIWidth, selectionAreaUIHeight);
    }

    // Select the active player's boids inside the selection area
    private void SelectPlayerFlockEntities()
    {
        // The current players flock
        List<Boid> activePlayerFlock = _gameUI.GetActivePlayer().GetFlock();
        
        foreach (Boid entity in activePlayerFlock)
        {
            if (entity != null)
            {
                Selectable selected = entity.GetComponent<Selectable>();

                entityScreenPosition = _selectionCamera.WorldToScreenPoint(selected.transform.position);

                if (_selectionArea.Contains(entityScreenPosition))
                {
                    selected.SetSelectionIndicator(true);
                    _selectionManager.Select(selected);

                }
            }

            
        }

    }
    
}
