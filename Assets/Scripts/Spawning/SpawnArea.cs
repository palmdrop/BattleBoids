using System;
using System.Collections.Generic;
using UnityEngine;

public class SpawnArea : MonoBehaviour
{
    public GameObject entityToSpawn;
    private GameManager _gameManager;
    private Camera camera;
    private Map.Map map;

    private Player _owner;
    private Collider _collider;

    private int instanceNumber = 1;
    float boidPlaceHeight = 1f;

    private List<Boid> spawned;
    private List<GameObject> holding = new List<GameObject>();
    private List<Vector3> copyingOffsets = new List<Vector3>();
    private bool canPlace = true;
    private bool placing = false;
    private bool copying = false;
    private Vector3 gridStart;

    private bool active = false;

    // Used for bounds creation
    Bounds spawnBounds;
    Vector3 minBoundPosition;
    Vector3 boundSize;

    // Start is called before the first frame update
    void Start()
    {
        _owner = GetComponentInParent<Player>();
        _gameManager = GetComponentInParent<GameManager>();
        _collider = GetComponent<Collider>();
        spawned = _owner.GetFlock();
        camera = _gameManager.GetMainCamera();
        map = _gameManager.GetMap();
    }

    private void Awake()
    {
        // Creates a rectangle bounds around the spawn area
        spawnBounds = transform.GetComponent<Collider>().bounds;
        minBoundPosition = spawnBounds.min;
        boundSize = spawnBounds.extents;
    }

    void UpdateGrid(Vector3 gridEnd)
    {
        float unitWidth = 0.4f;
        int gridSizeX = (int) Math.Abs((gridEnd.x - gridStart.x) / unitWidth) + 1;
        int gridSizeY = (int) Math.Abs((gridEnd.z - gridStart.z) / unitWidth) + 1;
        int dirX = Math.Sign(gridEnd.x - gridStart.x);
        int dirY = Math.Sign(gridEnd.z - gridStart.z);
        while (holding.Count < gridSizeX * gridSizeY) {
            GameObject currentEntity;
            currentEntity = Instantiate(entityToSpawn, new Vector3(0, 0, 0), Quaternion.identity, _owner.gameObject.transform);
            currentEntity.name = "Player_" + _owner.id + "_Unit_" + instanceNumber++;
            holding.Add(currentEntity);
        }

        while (holding.Count > gridSizeX * gridSizeY) {
            GameObject currentEntity = holding[0];
            Destroy(currentEntity);
            holding.RemoveAt(0);
        }

        canPlace = true;

        LayerMask groundMask = LayerMask.GetMask("Ground");

        for (int x = 0; x < gridSizeX; x++) {
            for (int z = 0; z < gridSizeY; z++) {
                int i = x * gridSizeY + z;
                GameObject currentEntity = holding[i];

                Vector3 position = new Vector3(gridStart.x + dirX * x * unitWidth, 0, gridStart.z + dirY * z * unitWidth);

                // Find correct y-position
                float y = 0;
                Ray ray = new Ray(position + new Vector3(0f, 10f, 0f), Vector3.down);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, 1000f, groundMask)) y = hit.point.y;
                position.y = y + boidPlaceHeight;
                currentEntity.transform.position = position;

                // Check if within spawn area
                if (IsInside(currentEntity))
                {
                    currentEntity.GetComponent<Boid>().SetColor(new Color(_owner.color.r, _owner.color.g, _owner.color.b, 0.5f));
                } else
                {
                    currentEntity.GetComponent<Boid>().SetColor(new Color(1.0f, 1.0f, 1.0f, 0.5f));
                    canPlace = false;
                }
            }
        }
    }


    // Update is called once per frame
    void Update()
    {

        if (!active || !(placing || copying)) {
            ClearHolding();
            return;
        }

        RaycastHit hit;
        Ray ray = camera.ScreenPointToRay(Input.mousePosition);

        // Move current entity to mouse position
        LayerMask mask = LayerMask.GetMask("Ground");
        if (Physics.Raycast(ray, out hit, 1000.0f, mask)) {

            if (placing)
            {
                // If left mouse button is released
                if (Input.GetMouseButtonUp(0) &&
                    holding.Count > 0 && canPlace && PurchaseSuccess())
                {
                    // Place entity

                    foreach (GameObject currentEntity in holding)
                    {
                        spawned.Add(currentEntity.GetComponent<Boid>());
                    }
                    holding.Clear();

                    // Tell owner the flock has been updated
                    _owner.FlockUpdate = true;
                    if (!Input.GetKey(KeyCode.LeftControl))
                        this.placing = false;
                }
                else if (!Input.GetMouseButton(0))
                {
                    gridStart = hit.point;
                }
                UpdateGrid(hit.point);
            }
            else if (copying)
            {
                if (Input.GetMouseButtonDown(0) &&
                    holding.Count > 0 && canPlace && PurchaseSuccess())
                {
                    foreach (GameObject currentEntity in holding)
                    {
                        spawned.Add(currentEntity.GetComponent<Boid>());
                    }

                    // Tell owner the flock has been updated
                    _owner.FlockUpdate = true;

                    //If ctrl is held down we want to keep placing units.
                    if (Input.GetKey(KeyCode.LeftControl))
                    {
                        copying = false;
                        List<Selectable> tmp = CopyBoidsToSelectable(holding);
                        holding.Clear();

                        //A bit of a hack but works. Makes new copies of the previously held entities.
                        PressedCopySelectedKey(tmp);
                    }
                    else
                    {
                        holding.Clear();
                        copying = false;
                    }
                }
                else
                {
                    MoveCopies();
                }

            }
        }
    }

    public bool IsInPlacingPhase()
    {
        return placing || copying;
    }

    public void CancelPlacingPhase()
    {
        placing = false;
        copying = false;
    }

    private void MoveCopies()
    {

        // If the mouse is not hovering over the ground mask, return
        if (!SelectionManager.SharedInstance.IsMouseOverGround()) return;


        // Assume they are all in the spawn area
        canPlace = true;

        // Make sure cursor is not visible
        if (Cursor.visible)
        {
            Cursor.visible = false;
        }

        // Move the selectable to the mouse cursor while respecting the distance it currently have to other selected entities
        foreach (GameObject o in holding)
        {
            Selectable selectable = o.GetComponent<Boid>();
            // If one of them are not in the spawn area, you can't place any
            bool isInsideSpawnArea = IsInside(selectable.gameObject);

            if (isInsideSpawnArea)
            {
                o.GetComponent<Boid>().SetColor(new Color(_owner.color.r, _owner.color.g, _owner.color.b, 0.5f));
            }
            else
            {
                o.GetComponent<Boid>().SetColor(new Color(1.0f, 1.0f, 1.0f, 0.5f));
                canPlace = false;
            }

            // Find correct y-position
            Vector3 mousePositionInWorld = SelectionManager.SharedInstance.GetMousePosInWorld();
            float y = mousePositionInWorld.y;
            Ray ray = new Ray(selectable.transform.position + new Vector3(0f, 10f, 0f), Vector3.down);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 1000f, LayerMask.GetMask("Ground"))) y = hit.point.y;

            // Move the selected with the correct formation to the mouse position
            selectable.transform.position = new Vector3(mousePositionInWorld.x + selectable.GetOffset().x, y + boidPlaceHeight, mousePositionInWorld.z + selectable.GetOffset().z);

        }
    }

    public List<Selectable> CopyBoidsToSelectable(List<GameObject> boids)
    {
        List<Selectable> selected = new List<Selectable>();
        foreach (GameObject o in boids)
        {
            selected.Add(o.GetComponent<Boid>());
        }
        return selected;
    }

    public void PressedCopySelectedKey(List<Selectable> selected)
    {
        //Copy the selected units and put them into holding
        if (selected.Count > 0 && !copying)
        {
            copying = true;
            Cursor.visible = false;
            holding = new List<GameObject>();
            
            for (int i = 0; i < selected.Count; i++)
            {

                string name = ((Boid)selected[i]).GetType().ToString();
                GameObject prefab = GameUI.SharedInstance.FindUnitByName(name);

                GameObject currentEntity = Instantiate(prefab, new Vector3(0, 0, 0), Quaternion.identity, _owner.gameObject.transform);
                currentEntity.name = "Player_" + _owner.id + "_Unit_" + instanceNumber++;
                currentEntity.GetComponent<Boid>().SetOffset(-selected[i].GetOffset());
                currentEntity.GetComponent<Boid>().SetColor(new Color(_owner.color.r, _owner.color.g, _owner.color.b, 0.5f));
                holding.Add(currentEntity);
                selected[i].SetSelectionIndicator(false);
            }

        }
        else
        {
            Cursor.visible = true;
            copying = false;
        }
    }


    public bool IsInside(GameObject currentEntity)
    {
        Ray ray = new Ray(currentEntity.transform.position + new Vector3(0f, 100f, 0f), Vector3.down);
        RaycastHit hit;
        Boid boid = currentEntity.GetComponent<Boid>();
        if (_collider.Raycast(ray, out hit, 1000f)) {
            boid.SetColor(_owner.color);
            return true;
        }

        boid.SetColor(new Color(1.0f, 1.0f, 1.0f, 0.5f));
        return false;
    }

    bool PurchaseSuccess()
    {
        return _owner.RemoveBoins(SumHoldingCost());
    }

    void PurchaseReturn()
    {
        _owner.AddBoins(SumHoldingCost());
    }

    public int SumHoldingCost()
    {
        Boid boid;
        int sum = 0;
        foreach (GameObject gameObject in holding)
        {
            boid = gameObject.GetComponent<Boid>();
            sum += boid.GetCost();
        }
        return sum;
    }

    public void ClearHolding()
    {
        holding.ForEach(b => Destroy(b));
        holding.Clear();
    }

    public void SetEntityToSpawn(GameObject entity)
    {
        ClearHolding();
        entityToSpawn = entity;
    }

    public void Activate() {
        active = true;
    }

    public void Deactivate() {
        active = false;
    }

    public bool isHolding()
    {
        return holding.Count > 0;
    }

    public void SetPlacing(bool placing)
    {
        this.placing = placing;
    }

    public void SetCopying(bool copying)
    {
        this.copying = copying;
    }
}
