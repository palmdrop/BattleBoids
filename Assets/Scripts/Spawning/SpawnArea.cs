using System.Collections.Generic;
using UnityEngine;

public class SpawnArea : MonoBehaviour
{
    public GameObject entityToSpawn;
    public Camera camera;
    public Map.Map map;

    private Player _owner;

    int instanceNumber = 1;

    List<Boid> spawned;
    List<GameObject> holding = new List<GameObject>();
    int gridWidth = 0;
    bool canPlace = true;

    bool active = false;
    
    
    // Used for bounds creation
    Bounds spawnBounds;
    Vector3 minBoundPosition;
    Vector3 boundSize;

    // Start is called before the first frame update
    void Start()
    {
        _owner = GetComponentInParent<Player>();
        spawned = _owner.GetFlock();
    }

    private void Awake()
    {
        // Creates a rectangle bounds around the spawn area
        spawnBounds = transform.GetComponent<Collider>().bounds;
        minBoundPosition = spawnBounds.min;
        boundSize = spawnBounds.extents;
    }

    void UpdateGrid(Vector3 gridCenter)
    {
        while (holding.Count < gridWidth * gridWidth) {
            GameObject currentEntity;
            currentEntity = Instantiate(entityToSpawn, new Vector3(0, 0, 0), Quaternion.identity);
            currentEntity.GetComponent<Boid>().SetOwner(_owner);
            currentEntity.name = "Player_" + _owner.id + "_Unit_" + instanceNumber++;
            holding.Add(currentEntity);
        }
        
        while (holding.Count > gridWidth * gridWidth) {
            GameObject currentEntity = holding[0];
            currentEntity.GetComponent<Boid>().Die();
            Destroy(currentEntity);
            holding.RemoveAt(0);
        }
        //float unitWidth = entityToSpawn.GetComponent<Collider>().bounds.size.z;
        float unitWidth = 0.4f;

        float width = gridWidth * unitWidth;
        
        canPlace = true;
        
        for (int x = 0; x < gridWidth; x++) {
            for (int z = 0; z < gridWidth; z++) {
                int i = x * gridWidth + z;
                GameObject currentEntity = holding[i];
                // Find ground height
                Vector3 position = new Vector3(gridCenter.x + x * unitWidth - width / 2, SelectionManager.MousePositionInWorld.point.y + 3f, gridCenter.z + z * unitWidth - width / 2);
                
                /*
                if (map.PointInsideBounds(position))
                {
<<<<<<< Updated upstream
                    position.y =  1.2f;
=======
                    //position.y = map.HeightmapLookup(position) + map.transform.position.y + 1f;
                    position.y = 2f;
>>>>>>> Stashed changes
                }
                else
                {
                    position.y = 0;
                }
                */
                
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
        if (!active) {
            gridWidth = 0;
            UpdateGrid(new Vector3(0, 0, 0));
            return;
        }

        RaycastHit hit;
        Ray ray = camera.ScreenPointToRay(Input.mousePosition);

        // Move current entity to mouse position
        LayerMask mask = LayerMask.GetMask("Ground");
        if (Physics.Raycast(ray, out hit, 1000.0f, mask)) {
            UpdateGrid(hit.point);
        }

        // If left mouse button is pressed
        if (Input.GetMouseButtonDown(0)) {
            if (holding.Count > 0 && canPlace && PurchaseSuccess()) {
                // Place entity
                
                foreach (GameObject currentEntity in holding) {
                    spawned.Add(currentEntity.GetComponent<Boid>());
                }
                holding.Clear();
                gridWidth = 0;
                
                // Tell owner the flock has been updated
                _owner.FlockUpdate = true;
            }
        }
    }

    public bool IsInside(GameObject gameObject)
    {
        Vector3 goTransformPosition = gameObject.transform.position;
        Vector2 goPosition = new Vector2(goTransformPosition.x, goTransformPosition.z);
        
        
        Rect bounds2D = new Rect(minBoundPosition.x, minBoundPosition.z, boundSize.x * 2, boundSize.z * 2);

        Boid boid = gameObject.GetComponent<Boid>();
        
        if (bounds2D.Contains(goPosition))
        {
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

    int SumHoldingCost()
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

    public void ChangeGridWidth(int modifier)
    {
        if (gridWidth + modifier < 0)
        {
            gridWidth = 0;
        }
        else
        {
            gridWidth += modifier;
        }
    }

    public void SetEntityToSpawn(GameObject entity)
    {
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
}
