using System;
using UnityEngine;

public class SelectObject : MonoBehaviour
{
    private Camera mainCamera;
    
    private GameObject currentlySelected;

    private void Start()
    {
       
        mainCamera =  Camera.main;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            currentlySelected = RetrieveGameObject();
        }
    }

    public GameObject RetrieveGameObject()
    {
        RaycastHit hit;
        // Sends a ray from the cursor position into the scene
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        // Check if ray intersect with a Collider AND checks if we hit a game object
        if (!Physics.Raycast(ray, out hit, 500f) && !hit.transform)
        {
            return currentlySelected;
        }
        
        // if game object is found, print its name
        return hit.transform.gameObject;
    }

    public GameObject GetCurrentlySelected()
    {
        return currentlySelected;
    }

    public bool IsObjectSelected()
    {
        return currentlySelected;
    }
}
