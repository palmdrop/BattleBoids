using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;

public class SelectionManager : MonoBehaviour
{
    // The UI component used to get the active player
    private GameManager _gameManager;
    private GameUI _gameUI;
    
    private LayerMask ground;
    
    private Player activePlayer;
    
    private readonly List<Selectable> selected = new List<Selectable>();
    
    //private static Vector3 min;
    //private static Vector3 max;
    private Selectable _anchorPoint;
    
    int instanceNumber = 1;

    private bool inMoveState = false;
    private bool canPlaceSelected = true;

    private bool inBuyState = false;
    private int buyGridWidth = 0;

    private const float YOffset = 1f;
    
    
    private static RaycastHit mousePositionInWorld;
    // We want to check if the mouse is currently hovering over ground tiles
    private bool mouseOverGround;
    

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
    if (selected.Count == 0) return;
    
    // Update mouse position
    mouseOverGround = Physics.Raycast(_gameManager.GetMainCamera().ScreenPointToRay(Input.mousePosition), out mousePositionInWorld, 1000f, ground);
    
    // Sell the selected entities
    if (Input.GetKeyDown("k"))
    {
        SellSelected();
        return;
    }
    
    // Move entities around the world
    if (Input.GetKeyDown("q"))
    {
        inMoveState = true;
    }
    

    // Move the selected entities
    if (inMoveState)
    {
        MoveSelected();
    }
}

    private void UndoChanges()
    {
        ReturnToOriginalPosition();
        canPlaceSelected = true;
        Deselect();
        
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
        
        /*
        Vector3 selectedPosition = selected.transform.position;
        
        if (selectedPosition.x < min.x)
        {
            min.x = selectedPosition.x;
        } 
        
        if (selectedPosition.x > max.x)
        {
            max.x = selectedPosition.x;
        }

        if (selectedPosition.z < min.z)
        {
            min.z = selectedPosition.z;
        } 
        
        if (selectedPosition.z > max.z)
        {
            max.z = selectedPosition.z;
        }
        */
        
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
            
            // Move the selected with the correct formation to the mouse position
            selectable.transform.position = new Vector3(mousePositionInWorld.point.x + selectable.GetOffset().x, mousePositionInWorld.point.y + YOffset, mousePositionInWorld.point.z + selectable.GetOffset().z);
            
        }
    }

    private void ReturnToOriginalPosition()
    {
        foreach (Selectable selectable in selected)
        {
            selectable.GetComponent<Boid>().SetColor(activePlayer.color);
            selectable.transform.position = selectable.GetPositionBeforeMoved();
        }
    }

    private void SellSelected()
    {
        inMoveState = false;
        
        for (int i = selected.Count; i --> 0; )
        {
            activePlayer.AddBoins(selected[i].GetCost());
            activePlayer.RemoveFromFlock(selected[i]);
            
            Destroy(selected[i].gameObject);
            selected.RemoveAt(i);
            
        }

    }

    public bool IsPlaceable()
    {
        return canPlaceSelected;
    }

    public static RaycastHit MousePositionInWorld => mousePositionInWorld; 

    public GameUI GetGameUI()
    {
        return _gameUI;
    }

    public Camera GetMainCamera()
    {
        return _gameManager.GetMainCamera();
    }
}
