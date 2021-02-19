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
    private bool playing = false;

    // To be replaced by some other data structure
    private List<Boid> _boids = new List<Boid>();
    private NativeMultiHashMap<GridPoint, Boid.BoidInfo> _grid;// = new NativeMultiHashMap<GridPoint, Boid.BoidInfo>(10, Allocator.Persistent);

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey("p"))
        {
            playing = true;
            _boids.Clear();
            foreach (Player p in players)
            {
                foreach (GameObject b in p.GetFlock())
                {
                    _boids.Add(b.GetComponent<Boid>());
                }
            }
        }

        _boids.RemoveAll(b => b.dead);

        NativeArray<Boid.BoidInfo> boidInfos = new NativeArray<Boid.BoidInfo>(_boids.Count, Allocator.TempJob);
        NativeArray<float3> forces = new NativeArray<float3>(_boids.Count, Allocator.TempJob);
        NativeMultiHashMap<int, Boid.BoidInfo> neighbours = new NativeMultiHashMap<int, Boid.BoidInfo>(_boids.Count, Allocator.TempJob);

        // Create the grid
        _grid = new NativeMultiHashMap<GridPoint, Boid.BoidInfo>(10, Allocator.TempJob);
        
        // Populate the grid
        for (int i = 0; i < boidInfos.Length; i++)
        {
            Boid.BoidInfo info = _boids[i].GetInfo();
            int xIndex = (int)(math.floor(info.pos.x) / cellWidth);
            int zIndex = (int)(math.floor(info.pos.z) / cellDepth);
            GridPoint gp = new GridPoint(xIndex, zIndex, cellXAmount);
            _grid.Add(gp, info);
        }

        // Use the grid
        for (int i = 0; i < _boids.Count; i++)
        {
            boidInfos[i] = _boids[i].GetInfo();
            Boid.BoidInfo[] neighbourArray = FindBoidsWithinRadius(_boids[i].GetInfo(), _boids[i].GetInfo().classInfo.viewRadius);
            foreach (Boid.BoidInfo info in neighbourArray)
            {
                neighbours.Add(i, info);
            }
        }

        BoidStructJob boidJob = new BoidStructJob
        {
            boids = boidInfos,
            forces = forces,
            neighbourArray = neighbours
        };

        JobHandle jobHandle = boidJob.Schedule(_boids.Count, _boids.Count / 10);
        jobHandle.Complete();

        for (int i = 0; i < _boids.Count; i++)
        {
            _boids[i].UpdateBoid(forces[i]);
        }

        boidInfos.Dispose();
        forces.Dispose();
        neighbours.Dispose();
        _grid.Dispose();
    }

    // Finds all boids within the given radius from the given boid (excludes the given boid itself)
    public Boid.BoidInfo[] FindBoidsWithinRadius(Boid.BoidInfo boid, float radius)
    {
        int xIndex = (int)(math.floor(boid.pos.x) / cellWidth);
        int zIndex = (int)(math.floor(boid.pos.z) / cellDepth);
        List<Boid.BoidInfo> boidsInRadius = new List<Boid.BoidInfo>();


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
                    //NativeList<Boid.BoidInfo> gridList = _grid[gp];
                    //for (int k = 0; k < gridList.Length; k++)
                    foreach (Boid.BoidInfo b in _grid.GetValuesForKey(gp))
                    {
                        //Boid.BoidInfo b = gridList[k];
                        float3 horizontalDistance = b.pos - boid.pos;
                        if (horizontalDistance.x * horizontalDistance.x + horizontalDistance.z + horizontalDistance.z < radius * radius/* && !b.Equals(boid)*/)
                        {
                            boidsInRadius.Add(b);
                        }
                    }
                }
            }
        }

        Boid.BoidInfo[] result = new Boid.BoidInfo[boidsInRadius.Count];
        for (int i = 0; i < boidsInRadius.Count; i++)
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
        [ReadOnly] public NativeMultiHashMap<int, Boid.BoidInfo> neighbourArray;

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

            //neighbours = FindBoidsWithinRadius(boid, boid.classInfo.viewRadius);

            // Iterate over all the neighbours
            int viewCount = 0;
            int separationViewCount = 0;
            int i = 0;
            foreach (Boid.BoidInfo other in neighbourArray.GetValuesForKey(index))
            {
                if (i == index) continue;

                //Boid.BoidInfo other = neighbours[i];

                // Compare the distance between this boid and the neighbour using the
                // square of the distance and radius. This avoids costly square root operations
                // And if close enough, add to average position for separation
                float3 vector = (boid.pos - other.pos);
                float sqrDist = vector.x * vector.x + vector.y * vector.y + vector.z * vector.z;

                if (other.flockId == boid.flockId) {
                    // Friendly boid
                    if (sqrDist < boid.classInfo.separationRadius * boid.classInfo.separationRadius)
                    {
                        // Add to average velocity
                        avgVel += other.vel;
                        viewCount++;

                        // Add to average position for cohesion
                        avgPosCohesion += other.pos;

                        avgPosSeparation += other.pos;
                        separationViewCount++;
                    }
                    else if (sqrDist < boid.classInfo.viewRadius * boid.classInfo.viewRadius)
                    {
                        // Add to average velocity
                        avgVel += other.vel;
                        viewCount++;

                        // Add to average position for cohesion
                        avgPosCohesion += other.vel;

                    }
                } else {
                    // Enemy boid
                    if (sqrDist < boid.classInfo.viewRadius * boid.classInfo.viewRadius && sqrDist < targetDist) {
                        targetPos = other.pos;
                        targetDist = sqrDist;
                    }
                }

                i++;
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
