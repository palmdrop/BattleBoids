using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BoxSelection : MonoBehaviour
{
    // The camera that is used for the projection from world view to screen view
    [SerializeField] private Camera selectionCamera;
    
    // This is the selection area displayed on the UI
    [SerializeField] private RectTransform selectionAreaUI;
    
    // The UI component used to get the active player flock
    [SerializeField] private GameUI gameUI;

    // This is the selection invisible selection area in the game
    private Rect _selectionArea;
    // The position of the boid projected on to the screen
    private Vector3 entityScreenPosition;
    
    // Start and end position for the selection area
    private Vector2 _startSelectionPosition;
    private Vector2 _endSelectionPosition;
    

    private void Start()
    {
        ResetDrawUISelectionArea();
    }

    private void Update()
    {
        // When you click the left mouse button
        if (Input.GetMouseButtonDown(0))
        {
            DeselectEntities();

            // Get the position of the mouse once
            _startSelectionPosition = Input.mousePosition;

            _selectionArea = new Rect();
        }

        // When the left mouse button is held down
        if (Input.GetMouseButton(0))
        {
            // Continuously update the position of the mouse
            _endSelectionPosition = Input.mousePosition;

            DrawUISelectionArea();
            
            /*
             * Calculate the selection area in the world view
             */
            
            // When we are on the left side of the mouse position
            if (Input.mousePosition.x < _startSelectionPosition.x)
            {
                _selectionArea.xMin = Input.mousePosition.x;
                _selectionArea.xMax = _startSelectionPosition.x;
            }
            //  Right side...
            else
            {
                _selectionArea.xMin =  _startSelectionPosition.x;
                _selectionArea.xMax = Input.mousePosition.x;
            }
            
            // For y...
            if (Input.mousePosition.y < _startSelectionPosition.y)
            {
                _selectionArea.yMin = Input.mousePosition.y;
                _selectionArea.yMax = _startSelectionPosition.y;
            }
            else
            {
                _selectionArea.yMin =  _startSelectionPosition.y;
                _selectionArea.yMax = Input.mousePosition.y;
            }
        }

        // When the left mouse button is released
        if (Input.GetMouseButtonUp(0))
        {
            SelectPlayerFlockEntities();
            ResetDrawUISelectionArea();
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
        List<GameObject> activePlayerFlock = gameUI.GetActivePlayer().GetFlock();
        
        foreach (GameObject entity in activePlayerFlock)
        {
            Selectable selected = entity.GetComponent<Selectable>();
            
            entityScreenPosition = selectionCamera.WorldToScreenPoint(selected.transform.position);
            
            if (_selectionArea.Contains(entityScreenPosition))
            {
                SelectionManager.AddToSelected(selected);
                selected.SetSelectionIndicator(true);
            }
        }

    }
    
    private void DeselectEntities()
    {
        
        SelectionManager.RemoveSelected();
    }
    
}