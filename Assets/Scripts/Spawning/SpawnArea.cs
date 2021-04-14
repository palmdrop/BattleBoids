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

    private List<Boid> spawned;
    private List<GameObject> holding = new List<GameObject>();
    private bool canPlace = true;
    private bool placing = false;
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

                // Find correct y-position
                float y = 0;
                Ray ray = new Ray(currentEntity.transform.position + new Vector3(0f, 10f, 0f), Vector3.down);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, 1000f, groundMask)) y = hit.point.y;
                Vector3 position = new Vector3(gridStart.x + dirX * x * unitWidth, y + 1f, gridStart.z + dirY * z * unitWidth);
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
        if (!active || !placing) {
            ClearHolding();
            return;
        }

        RaycastHit hit;
        Ray ray = camera.ScreenPointToRay(Input.mousePosition);

        // Move current entity to mouse position
        LayerMask mask = LayerMask.GetMask("Ground");
        if (Physics.Raycast(ray, out hit, 1000.0f, mask)) {

            // If left mouse button is released
            if (Input.GetMouseButtonUp(0) &&
                holding.Count > 0 && canPlace && PurchaseSuccess()) {
                // Place entity

                foreach (GameObject currentEntity in holding) {
                    spawned.Add(currentEntity.GetComponent<Boid>());
                }
                holding.Clear();

                // Tell owner the flock has been updated
                _owner.FlockUpdate = true;

                this.placing = false;
            } else if (!Input.GetMouseButton(0)) {
                gridStart = hit.point;
            }
            UpdateGrid(hit.point);
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
}
