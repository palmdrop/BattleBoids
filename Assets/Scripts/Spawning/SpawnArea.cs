using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnArea : MonoBehaviour
{
    public GameObject entityToSpawn;
    public Camera camera;
    public Player owner;

    int instanceNumber = 1;

    List<GameObject> spawned;
    List<GameObject> holding = new List<GameObject>();
    int gridWidth = 0;
    bool canPlace = true;

    bool holdingKey = false;

    // Start is called before the first frame update
    void Start()
    {
        spawned = owner.GetFlock();
    }

    void UpdateGrid(Vector3 gridCenter) {
        while (holding.Count < gridWidth * gridWidth) {
            GameObject currentEntity;
            currentEntity = Instantiate(entityToSpawn, new Vector3(0, 0, 0), Quaternion.identity);
            currentEntity.name = "toSpawn" + instanceNumber++;
            currentEntity.GetComponent<Renderer>().material.color = new Color(1, 1, 1, 0.5f);
            holding.Add(currentEntity);
        }
        while (holding.Count > gridWidth * gridWidth) {
            GameObject currentEntity = holding[0];
            Destroy(currentEntity);
            holding.RemoveAt(0);
        }
        float unitWidth = entityToSpawn.GetComponent<MeshRenderer>().bounds.size.z;
        float width = gridWidth * unitWidth;
        canPlace = true;
        for (int x = 0; x < gridWidth; x++) {
            for (int z = 0; z < gridWidth; z++) {
                int i = x * gridWidth + z;
                GameObject currentEntity = holding[i];
                // Find ground height
                Vector3 position = new Vector3(gridCenter.x + x * unitWidth - width / 2, 1000f, gridCenter.z + z * unitWidth - width / 2);
                RaycastHit hit;
                LayerMask mask = LayerMask.GetMask("Ground");
                if (Physics.Raycast(position, transform.TransformDirection(Vector3.down), out hit, 2000f, mask)) {
                    position.y = hit.point.y;
                } else {
                    position.y = 0;
                }
                currentEntity.transform.position = position;
                // Check if within spawn area
                if (this.GetComponent<Collider>().Raycast(new Ray(new Vector3(position.x, 1000f, position.z), transform.TransformDirection(Vector3.down)), out hit, 2000f)) {
                    currentEntity.GetComponent<Renderer>().material.color = new Color(1, 1, 1, 0.5f);
                } else {
                    currentEntity.GetComponent<Renderer>().material.color = new Color(1, 0, 0, 0.5f);
                    canPlace = false;
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        RaycastHit hit;
        Ray ray = camera.ScreenPointToRay(Input.mousePosition);

        // Move current entity to mouse position
        LayerMask mask = LayerMask.GetMask("Ground");
        if (Physics.Raycast(ray, out hit, 1000.0f, mask)) {
            UpdateGrid(hit.point);
        }

        if (Input.GetMouseButtonDown(0)) {
            if (holding.Count > 0 && canPlace) {
                // Place entity
                foreach (GameObject currentEntity in holding) {
                    spawned.Add(currentEntity);
                    currentEntity.GetComponent<Renderer>().material.color = new Color(1, 1, 1, 1);
                }
                holding.Clear();
                gridWidth = 0;
            } else if (Physics.Raycast(ray, out hit)) {
                // Pick up entity
                if (spawned.Contains(hit.collider.gameObject)) {
                    spawned.Remove(hit.collider.gameObject);
                    GameObject currentEntity = hit.collider.gameObject;
                    currentEntity.GetComponent<Renderer>().material.color = new Color(1, 1, 1, 0.5f);
                    holding.Add(currentEntity);
                    gridWidth = 1;
                }
            }

        }

        if (Input.GetKey("n")) {
            if (!holdingKey) {
                // Create new entity
                gridWidth += 1;
            }
            holdingKey = true;
        } else if (Input.GetKey("x")) {
            // Remove entity
            if (!holdingKey) {
                if (gridWidth > 0)
                    gridWidth -= 1;
            }
            holdingKey = true;
        } else {
            holdingKey = false;
        }

    }
}
