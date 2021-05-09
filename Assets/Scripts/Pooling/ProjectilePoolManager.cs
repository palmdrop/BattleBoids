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
    private float maxAliveTime = 3f;
    // Start is called before the first frame update

    private void Awake()
    {
        //Trick for easy reference if there is only one script per scene.
        SharedInstance = this;
        maxBoidRadius = 0;
        foreach (Boid.ClassInfo info in ClassInfos.infos)
        {
            maxBoidRadius = maxBoidRadius > info.colliderRadius ? maxBoidRadius : info.colliderRadius;
        }
    }

    public void InstancePoolObjects(int amountToPool)
    {
        //Handles pre-instancing.
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
        //Return any pooled object as long as it's not active.
        int count = pooledObjects.Count;
        for (int i = 0; i < count; i++)
        {
            if (!pooledObjects[i].gameObject.activeSelf)
            {
                pooledObjects[i].gameObject.GetComponent<TrailRenderer>().Clear();
                StartCoroutine(DeactivateAfterTime(pooledObjects[i].gameObject, maxAliveTime));
                pooledObjects[i].created = Time.time;
                return pooledObjects[i].gameObject;
            }
        }
        return null;
    }

    public IEnumerator DeactivateAfterTime(GameObject o, float time)
    {
        //We can safely deactivate the projectile if it falls out of the map after some time.
        yield return new WaitForSeconds(time);
        if ((Time.time - o.GetComponent<RangedProjectile>().created) >= time)
            o.SetActive(false);
    }

    public void FixedUpdate()
    {
        int activeCount = 0;
        int count = pooledObjects.Count;
        for (int i = 0; i < count; i++)
        {
            if (pooledObjects[i].gameObject.activeSelf)
            {
                activeCount++;
            }
        }

        //Info about projectiles and boids in burst-compatible arrays.
        NativeArray<float3> projectilePositions = new NativeArray<float3>(activeCount, Allocator.TempJob);
        NativeArray<float3> projectileVelocities = new NativeArray<float3>(activeCount, Allocator.TempJob);
        int indexCount = 0;
        for (int i = 0; i < count; i++)
        {
            if (pooledObjects[i].gameObject.activeSelf)
            {
                projectilePositions[indexCount] = pooledObjects[i].transform.position;
                projectileVelocities[indexCount] = pooledObjects[i].GetVel() * Time.fixedDeltaTime * 2;
                indexCount++;
            }
        }


        List<Boid> boids = BoidManager.SharedInstance.GetBoids();
        NativeArray<float3> boidPositionsArray = new NativeArray<float3>(boids.Count, Allocator.TempJob);
        for (int i = 0; i < boids.Count; i++)
        {
            boidPositionsArray[i] = boids[i].GetPos();
        }


        NativeArray<Boid.ClassInfo> DisposableBoidClassInfos = new NativeArray<Boid.ClassInfo>(ClassInfos.infos, Allocator.TempJob);


        NativeArray<int> DisposableTypeInfos = new NativeArray<int>(boids.Count, Allocator.TempJob);
        for (int i = 0; i < boids.Count; i++)
        {
            DisposableTypeInfos[i] = (int)boids[i].GetType();
        }

        NativeArray<int> hitIndexes = new NativeArray<int>(activeCount, Allocator.TempJob);

   
        ProjectileCollisionJob collisionJob = new ProjectileCollisionJob()
        {
            projectiles = projectilePositions,
            projectileVelocities = projectileVelocities,
            boidPositions = boidPositionsArray,
            projectileRadius = RangedProjectile.projectileRadius,
            maxBoidRadius = this.maxBoidRadius,
            grid = BoidManager.SharedInstance.GetGrid(),
            boidClassInfos = DisposableBoidClassInfos,
            typeInfo = DisposableTypeInfos,
            cw = BoidManager.SharedInstance.GetPhysicsWorld().PhysicsWorld.CollisionWorld,
            hitIndexes = hitIndexes


        };

        // Schedule job 
        JobHandle jobHandle = collisionJob.Schedule(activeCount, activeCount / 10);
        jobHandle.Complete();

        //Update projectiles and damage hit boids etc.
        int tmpCount = 0;
        for (int i = 0; i < pooledObjects.Count; i++)
        {
            RangedProjectile p = pooledObjects[i];
            if (p.gameObject.activeSelf)
            {
                int collisionIndex = hitIndexes[tmpCount++];
                if (collisionIndex == -2)
                    p.gameObject.SetActive(false);
                else if (collisionIndex != -1)
                    p.ManagedOnTriggerEnter(boids[collisionIndex]);
                p.ManagedFixedUpdate();
            }
        }

        //Dispose to avoid errors.
        projectilePositions.Dispose();
        projectileVelocities.Dispose();
        boidPositionsArray.Dispose();
        DisposableBoidClassInfos.Dispose();
        DisposableTypeInfos.Dispose();
        hitIndexes.Dispose();
    }

    [BurstCompile]
    private struct ProjectileCollisionJob : IJobParallelFor
    {
        //To know if we have collided with a boid, all boids nearby have to be checked.
        //The problem is identical to the boid neighbor-checking so we might as well use
        //the already implemented grid.
        [ReadOnly] public NativeArray<float3> projectiles;
        [ReadOnly] public NativeArray<float3> projectileVelocities;
        [ReadOnly] public NativeArray<float3> boidPositions;
        [ReadOnly] public float projectileRadius;
        [ReadOnly] public float maxBoidRadius;
        [ReadOnly] public BoidGrid grid;
        [ReadOnly] public NativeArray<Boid.ClassInfo> boidClassInfos;
        [ReadOnly] public NativeArray<int> typeInfo;
        [ReadOnly] public Unity.Physics.CollisionWorld cw;
        [WriteOnly] public NativeArray<int> hitIndexes;

        public void Execute(int index)
        {
            NativeArray<int> neighbours = grid.FindBoidsWithinRadius(projectiles[index], projectileRadius + maxBoidRadius);
            bool hit = false;
            for (int i = 0; i < neighbours.Length; i++)
            {
                int boid = neighbours[i];
                float boidRadius = boidClassInfos[typeInfo[boid]].colliderRadius;
                float3 offset = boidPositions[boid] - projectiles[index];
                float collisionDistance = boidRadius + projectileRadius;

                //Only checks distance. A spherecast would be optimal but this works as long as the speeds are not too high.
                if (math.dot(offset, offset) < collisionDistance * collisionDistance)
                {
                    hitIndexes[index] = boid;
                    hit = true;
                    break;
                }
            }

            //We also check if the projectile is about to collide with the ground or other obstacles
            Unity.Physics.RaycastInput ray = new Unity.Physics.RaycastInput
            {
                Start = projectiles[index],
                End = projectiles[index] + projectileVelocities[index],
                Filter = new Unity.Physics.CollisionFilter
                {
                    BelongsTo = boidClassInfos[typeInfo[0]].groundMask,
                    CollidesWith = boidClassInfos[typeInfo[0]].groundMask
                }
            };
            Unity.Physics.RaycastHit rayhit;
            if (cw.CastRay(ray, out rayhit))   //Cast rays to nearby boundaries
            {
                hit = true;
                hitIndexes[index] = -2;
            }

            if (!hit)
            {
                hitIndexes[index] = -1;
            }

        }
    }

}
