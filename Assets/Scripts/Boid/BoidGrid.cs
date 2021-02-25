using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using System;
using Unity.Mathematics;

public struct BoidGrid
{
    [SerializeField] private static readonly float cellWidth = 1f, cellDepth = 1f;
    private static readonly int cellXAmount = 100;
    private NativeMultiHashMap<GridPoint, Boid.BoidInfo> _grid;// = new NativeMultiHashMap<GridPoint, Boid.BoidInfo>(10, Allocator.TempJob);
    private NativeList<Boid.BoidInfo> _boids;


    // TODO: Documentation
    public void Populate(List<Boid> boids)
    {
        _boids = new NativeList<Boid.BoidInfo>(10, Allocator.TempJob);
        _grid = new NativeMultiHashMap<GridPoint, Boid.BoidInfo>(10, Allocator.TempJob);
        foreach (Boid b in boids)
        {
            Boid.BoidInfo info = b.GetInfo();
            _boids.Add(info);
            int xIndex = (int)(math.floor(info.pos.x) / cellWidth);
            int zIndex = (int)(math.floor(info.pos.z) / cellDepth);
            GridPoint gp = new GridPoint(xIndex, zIndex, cellXAmount);
            _grid.Add(gp, info);
        }
    }


    // TODO: Documentation
    public NativeMultiHashMap<int, IndexBoidPair> GetNeighbours()
    {
        NativeMultiHashMap<int, IndexBoidPair> neighbours = new NativeMultiHashMap<int, IndexBoidPair>(10, Allocator.TempJob);
        for (int i = 0; i < _boids.Length; i++)
        {
            NativeArray<Boid.BoidInfo> neighbourArray = FindBoidsWithinRadius(_boids[i], _boids[i].classInfo.viewRadius);
            foreach (Boid.BoidInfo info in neighbourArray)
            {
                neighbours.Add(i, new IndexBoidPair(i, info));
            }
            neighbourArray.Dispose();
        }
        return neighbours;
    }


    // Finds all boids within the given radius from the given boid (excludes the given boid itself)
    public NativeArray<Boid.BoidInfo> FindBoidsWithinRadius(Boid.BoidInfo boid, float radius)
    {
        // The cell that the current boid is in
        int xIndex = (int)(math.floor(boid.pos.x) / cellWidth);
        int zIndex = (int)(math.floor(boid.pos.z) / cellDepth);
        NativeList<Boid.BoidInfo> boidsInRadius = new NativeList<Boid.BoidInfo>(10, Allocator.Temp);

        // The cells that cover the view radius of the boid
        int minI = xIndex - (int)math.ceil(radius / cellWidth);
        int maxI = xIndex + (int)math.ceil(radius / cellWidth);
        int minJ = zIndex - (int)math.ceil(radius / cellDepth);
        int maxJ = zIndex + (int)math.ceil(radius / cellDepth);

        // Iterate over surrounding cells
        for (int i = minI; i <= maxI; i++)
        {
            for (int j = minJ; j <= maxJ; j++)
            {

                GridPoint gp = new GridPoint(i, j, cellXAmount);
                if (_grid.ContainsKey(gp))
                {
                    // Iterate over the boids in surrounding cells
                    foreach (Boid.BoidInfo b in _grid.GetValuesForKey(gp))
                    {
                        float3 horizontalDistance = b.pos - boid.pos;
                        if (horizontalDistance.x * horizontalDistance.x + horizontalDistance.z * horizontalDistance.z < radius * radius && !boid.Equals(b))
                        {
                            boidsInRadius.Add(b);
                        }
                    }
                }
            }
        }

        NativeArray<Boid.BoidInfo> result = new NativeArray<Boid.BoidInfo>(boidsInRadius.Length, Allocator.Temp);
        for (int i = 0; i < boidsInRadius.Length; i++)
        {
            result[i] = boidsInRadius[i];
        }

        return result;
    }


    // TODO: Documentation
    public void Dispose()
    {
        _grid.Dispose();
        _boids.Dispose();
    }


    // Struct for storing coordinates of a grid
    private struct GridPoint : IEquatable<GridPoint>
    {
        // Cell coordinates
        public int x, y;

        // "Width" of the grid, used for hashing
        public int w;

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


    public struct IndexBoidPair
    {
        public IndexBoidPair(int index, Boid.BoidInfo boid)
        {
            this.index = index;
            this.boid = boid;
        }

        public readonly int index;
        public readonly Boid.BoidInfo boid;
    }
}
