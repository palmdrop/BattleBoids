using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using UnityEngine;

public class BoidManager : MonoBehaviour
{
    [SerializeField] private List<Player> players = new List<Player>();
    [SerializeField] private static readonly float cellWidth = 1f, cellDepth = 1f;
    private static readonly int cellXAmount = 20, cellZAmount = 20;

    // To be replaced by some other data structure
    private List<Boid> _boids = new List<Boid>();
    private static readonly NativeHashMap<GridPoint, NativeList<Boid.BoidInfo>> _grid = new NativeHashMap<GridPoint, NativeList<Boid.BoidInfo>>();

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey("p")) {
            _boids.Clear();
            foreach (Player p in players) {
                foreach (GameObject b in p.GetFlock()) {
                    _boids.Add(b.GetComponent<Boid>());
                }
            }
        }

        _boids.RemoveAll(b => b.dead);

        NativeArray<Boid.BoidInfo> boidInfos = new NativeArray<Boid.BoidInfo>(_boids.Count, Allocator.TempJob);
        NativeArray<float3> forces = new NativeArray<float3>(_boids.Count, Allocator.TempJob);

        for (int i = 0; i < _boids.Count; i++)
        {
            boidInfos[i] = _boids[i].GetInfo();
        }
        
        for (int i = 0; i < boidInfos.Length; i++)
        {
            Boid.BoidInfo info = boidInfos[i];
            int xIndex = (int)(math.floor(info.pos.x) / cellWidth);
            int zIndex = (int)(math.floor(info.pos.z) / cellDepth);
            GridPoint gp = new GridPoint(xIndex, zIndex, cellXAmount);
            if (_grid.ContainsKey(gp))
            {
                _grid[gp].Add(info);
            }
            else
            {
                NativeList<Boid.BoidInfo> toAdd = new NativeList<Boid.BoidInfo>();
                toAdd.Add(info);
                _grid.Add(gp, toAdd);
            }
        }

        BoidStructJob boidJob = new BoidStructJob
            {
                boids = boidInfos,
                forces = forces
            };

        JobHandle jobHandle = boidJob.Schedule(_boids.Count, _boids.Count / 10);
        jobHandle.Complete();

        for (int i = 0; i < _boids.Count; i++)
        {
            _boids[i].UpdateBoid(forces[i]);
        }

        boidInfos.Dispose();
        forces.Dispose();
        foreach (NativeList<Boid.BoidInfo> list in _grid.GetValueArray(Allocator.TempJob))
        {
            list.Dispose();
        }
    }

    // Finds all boids within the given radius from the given boid (excludes the given boid itself)
    public static NativeArray<Boid.BoidInfo> FindBoidsWithinRadius(Boid.BoidInfo boid, float radius)
    {
        int xIndex = (int)(math.floor(boid.pos.x) / cellWidth);
        int zIndex = (int)(math.floor(boid.pos.z) / cellDepth);
        NativeList<Boid.BoidInfo> boidsInRadius = new NativeList<Boid.BoidInfo>();


        int minI = xIndex - (int)math.ceil(radius / cellWidth);
        int maxI = xIndex + (int)math.ceil(radius / cellWidth);
        int minJ = zIndex - (int)math.ceil(radius / cellDepth);
        int maxJ = zIndex + (int)math.ceil(radius / cellDepth);

        for (int i = minI; i <= maxI; i++)
        {
            for (int j = minJ; j <= maxJ; j++)
            {
                GridPoint gp = new GridPoint(i, j, cellXAmount);
                if (_grid.ContainsKey(gp))
                {
                    NativeList<Boid.BoidInfo> gridList = _grid[gp];
                    for (int k = 0; k < gridList.Length; k++)
                    {
                        Boid.BoidInfo b = gridList[k];
                        float3 horizontalDistance = b.pos - boid.pos;
                        if (horizontalDistance.x * horizontalDistance.x + horizontalDistance.z + horizontalDistance.z < radius * radius/* && !b.Equals(boid)*/)
                        {
                            boidsInRadius.Add(b);
                        }
                    }
                }
            }
        }

        NativeArray<Boid.BoidInfo> result = new NativeArray<Boid.BoidInfo>(boidsInRadius.Length, Allocator.TempJob);
        for (int i = 0; i < boidsInRadius.Length; i++)
        {
            result[i] = boidsInRadius[i];
        }
        return result;
    }

    [BurstCompile]
    public struct BoidStructJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Boid.BoidInfo> boids;
        [WriteOnly] public NativeArray<float3> forces;

        public void Execute(int index)
        {
            // Average velocity is used to calculate alignment force
            float3 avgVel = new Vector3(0, 0, 0);

            // Average neighbour position used to calculate cohesion
            float3 avgPosCohesion = new Vector3(0, 0, 0);

            // Average neighbour position used to calculate cohesion
            float3 avgPosSeparation = new Vector3(0, 0, 0);

            // Position of closest enemy
            float3 targetPos = new Vector3(0, 0, 0);
            float targetDist = Mathf.Infinity;

            Boid.BoidInfo boid = boids[index];

            NativeArray<Boid.BoidInfo> neighbours = FindBoidsWithinRadius(boid, boid.classInfo.viewRadius);

            // Iterate over all the neighbours
            int viewCount = 0;
            int separationViewCount = 0;
            for (int i = 0; i < neighbours.Length; i++)
            {
                if (i == index) continue;

                // Compare the distance between this boid and the neighbour using the
                // square of the distance and radius. This avoids costly square root operations
                // And if close enough, add to average position for separation
                float3 vector = (boid.pos - neighbours[i].pos);
                float sqrDist = vector.x * vector.x + vector.y * vector.y + vector.z * vector.z;

                if (neighbours[i].flockId == boid.flockId) {
                    // Friendly boid
                    if (sqrDist < boid.classInfo.separationRadius * boid.classInfo.separationRadius)
                    {
                        // Add to average velocity
                        avgVel += neighbours[i].vel;
                        viewCount++;

                        // Add to average position for cohesion
                        avgPosCohesion += neighbours[i].pos;

                        avgPosSeparation += neighbours[i].pos;
                        separationViewCount++;
                    }
                    else if (sqrDist < boid.classInfo.viewRadius * boid.classInfo.viewRadius)
                    {
                        // Add to average velocity
                        avgVel += neighbours[i].vel;
                        viewCount++;

                        // Add to average position for cohesion
                        avgPosCohesion += neighbours[i].vel;

                    }
                } else {
                    // Enemy boid
                    if (sqrDist < boid.classInfo.viewRadius * boid.classInfo.viewRadius && sqrDist < targetDist) {
                        targetPos = neighbours[i].pos;
                        targetDist = sqrDist;
                    }
                }
            }

            // Calculate alignment force
            Vector3 alignmentForce;
            if (viewCount == 0 || avgVel.Equals(new float3(0, 0, 0))) alignmentForce = new float3(0, 0, 0);
            else alignmentForce = math.normalize(avgVel) * boid.classInfo.alignmentStrength;

            // Calculate cohesion force
            Vector3 cohesionForce;
            if (viewCount == 0) cohesionForce = new float3(0, 0, 0);
            else cohesionForce = math.normalize((avgPosCohesion / viewCount) - boid.pos) * boid.classInfo.cohesionStrength;

            // Calculate separation force
            Vector3 separationForce;
            if (separationViewCount == 0) separationForce = new float3(0, 0, 0);
            else separationForce = math.normalize(boid.pos - (avgPosSeparation / separationViewCount)) * boid.classInfo.separationStrength;

            // Calculate aggression force
            Vector3 aggressionForce;
            if (targetDist == Mathf.Infinity) aggressionForce = new float3(0, 0, 0);
            else aggressionForce = math.normalize(targetPos - boid.pos) * boid.classInfo.aggressionStrength * targetDist;


            forces[index] = alignmentForce + cohesionForce + separationForce + aggressionForce;
        }
    }

    private struct GridPoint : IEquatable<GridPoint>
    {
        public int x, y, w;

        public GridPoint(int x, int y, int w)
        {
            this.x = x;
            this.y = y;
            this.w = w;
        }

        public override int GetHashCode()
        {
            return x + y * w;
        }

        public bool Equals(GridPoint gp)
        {
            return x == gp.x && y == gp.y && w == gp.w;
        }
    }

}
