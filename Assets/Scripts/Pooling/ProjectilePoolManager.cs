using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class ProjectilePoolManager : MonoBehaviour
{

    public static ProjectilePoolManager SharedInstance;
    public List<RangedProjectile> pooledObjects;
    public GameObject objectToPool;
    private float maxBoidRadius;
    // Start is called before the first frame update

    private void Awake()
    {
        SharedInstance = this;
        maxBoidRadius = 0;
        foreach (Boid.ClassInfo info in ClassInfos.infos)
        {
            maxBoidRadius = maxBoidRadius > info.colliderRadius ? maxBoidRadius : info.colliderRadius;
        }
    }

    public void InstancePoolObjects(int amountToPool)
    {
        pooledObjects = new List<RangedProjectile>();
        GameObject tmp;
        for (int i = 0; i < amountToPool; i++)
        {
            tmp = Instantiate(objectToPool);
            tmp.SetActive(false);
            tmp.transform.parent = gameObject.transform;
            pooledObjects.Add(tmp.GetComponent<RangedProjectile>());
        }
    }

    public GameObject getPooledObject()
    {
        int count = pooledObjects.Count;
        for (int i = 0; i < count; i++)
        {
            if (!pooledObjects[i].gameObject.activeSelf)
            {
                pooledObjects[i].gameObject.GetComponent<TrailRenderer>().Clear();
                return pooledObjects[i].gameObject;
            }
        }
        return null;
    }



}
