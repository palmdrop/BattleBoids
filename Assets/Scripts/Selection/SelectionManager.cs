using System;
using System.Collections.Generic;
using UnityEngine;

public class SelectionManager : MonoBehaviour
{
    // The UI component used to get the active player
    private GameManager _gameManager;
    private GameUI _gameUI;
    
    private LayerMask ground;
    
    private Player activePlayer;
    
    private readonly List<Selectable> selected = new List<Selectable>();
    
    private Selectable _anchorPoint;
    

    private bool inMoveState = false;
    private bool canPlaceSelected = true;
    private bool canSelect = true;


    private const float YOffset = 1f;
    
    
    private static RaycastHit _mousePositionInWorld;
    // We want to check if the mouse is currently hovering over ground tiles
    private bool mouseOverGround;
    
    
    private void Awake()
    {
        CommandManager.RegisterPressedAction(KeyCode.K, PressedSellKey, "Sell the selected boids");
        CommandManager.RegisterPressedAction(KeyCode.Q, PressedMoveSelectedKey, "Move the selected boids");
    }
    

    private void Start()
    { 
        ground = LayerMask.GetMask("Ground");
        _gameManager = GetComponentInParent<GameManager>();
        _gameUI = _gameManager.GetGameUI();
        activePlayer = _gameUI.GetActivePlayer();
        
    }

    private void Update()
    {
        // You can't edit if the game is currently running
        if (_gameUI.AllPlayersReady())
        {
            if (selected.Count > 0)
            {
                UndoChanges();
            }
            
            return;
        }

        // Undo every non-confirmed changes when the player change
        if (!activePlayer.Equals(_gameUI.GetActivePlayer()))
        {
            UndoChanges();
        }
        
        // Stop if nothing is selected 
        if (selected.Count == 0) 
        {
            Cursor.visible = true;
            return; 
        } 
        // Update mouse position
        mouseOverGround = Physics.Raycast(_gameManager.GetMainCamera().ScreenPointToRay(Input.mousePosition), out _mousePositionInWorld, 1000f, ground);
        
        // Sell the selected entities
        /*
        if (Input.GetKeyDown("k"))
        {
            SellSelected();
            return;
        }
        */


        // Move the selected entities
        if (inMoveState)
        {
            MoveSelected();
        }
    }

    private void UndoChanges()
    {
        ReturnToOriginalPosition();
        Deselect();
        
        canPlaceSelected = true;
        canSelect = true;
        
        // In case the player changed
        activePlayer = _gameUI.GetActivePlayer();
    }

    public void Select(Selectable selectable)
    {
       // This is used if the commands get interrupted or canceled
        selectable.SetPositionBeforeMoved();
        
        selected.Add(selectable);
        
        
        // TODO: update the anchor point to be in the same location(?)
        if (selected.Count == 1)
        {
            _anchorPoint = selectable;
        }
        
        // Sets the offset in relation to the first selectable registered in the _selected array
        selectable.SetOffset(_anchorPoint.transform.position);
        
    }

    public void Deselect()
    {

        inMoveState = false;
        // Remove the visual indicator from the deselected entities
        foreach (Selectable selected in GETSelectedEntities())
        {
            selected.SetSelectionIndicator(false);
        }

        selected.Clear();
    }


    public List<Selectable> GETSelectedEntities()
    {
        return selected;
    }

    private void MoveSelected()
    {

        // If the mouse is not hovering over the ground mask, return
        if (!mouseOverGround) return;


        // Assume they are all in the spawn area
        canPlaceSelected = true;
        
        // Move the selectable to the mouse cursor while respecting the distance it currently have to other selected entities
        foreach (Selectable selectable in selected)
        {
            // If one of them are not in the spawn area, you can't place any
            bool isInsideSpawnArea = activePlayer.GetSpawnArea().IsInside(selectable.gameObject);
            
            if (!isInsideSpawnArea)
            {
                canPlaceSelected = false;
            }

            // Find correct y-position
            float y = _mousePositionInWorld.point.y;
            Ray ray = new Ray(selectable.transform.position + new Vector3(0f, 10f, 0f), Vector3.down);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 1000f, ground)) y = hit.point.y;

            // Move the selected with the correct formation to the mouse position
            selectable.transform.position = new Vector3(_mousePositionInWorld.point.x + selectable.GetOffset().x, y + YOffset, _mousePositionInWorld.point.z + selectable.GetOffset().z);
            
        }
    }

    private void ReturnToOriginalPosition()
    {
        foreach (Selectable selectable in selected)
        {
            selectable.transform.position = selectable.GetPositionBeforeMoved();
        }
    }

    private void SellSelected()
    {
        inMoveState = false;
        Debug.Log(selected.Count.ToString());

        for (int i = selected.Count; i --> 0; )
        {
            activePlayer.AddBoins(selected[i].GetCost());
            activePlayer.RemoveFromFlock(selected[i]);
            
            Destroy(selected[i].gameObject);
            selected.RemoveAt(i);
            
        }

        canSelect = true;
    }

    public bool IsPlaceable()
    {
        return canPlaceSelected;
    }

    public static RaycastHit MousePositionInWorld => _mousePositionInWorld; 

    public GameUI GetGameUI()
    {
        return _gameUI;
    }

    public Camera GetMainCamera()
    {
        return _gameManager.GetMainCamera();
    }

    public bool CanSelect
    {
        get => canSelect;
        set => canSelect = value;
    }

    private void PressedSellKey()
    {
        if (selected.Count > 0)
        {
            SellSelected();
        }
    }

    private void PressedMoveSelectedKey()
    {
        if (selected.Count > 0)
        {
            inMoveState = true;
            Cursor.visible = false;
        }
        else
        {
            inMoveState = false;
        }
    }

}
