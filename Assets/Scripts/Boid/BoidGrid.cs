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
    private NativeMultiHashMap<GridPoint, int> _grid;// = new NativeMultiHashMap<GridPoint, Boid.BoidInfo>(10, Allocator.TempJob);
    private NativeList<Boid.BoidInfo> _boids;

    private bool allocated;

    // Populates the grid by placing the indices of the boids in cells
    public void Populate(List<Boid> boids)
    {
        if (allocated) return;
        
        _boids = new NativeList<Boid.BoidInfo>(10, Allocator.Persistent);
        _grid = new NativeMultiHashMap<GridPoint, int>(10, Allocator.Persistent);
        float time = Time.time;
        for (int i = 0; i < boids.Count; i++) 
        {
            Boid.BoidInfo info = boids[i].GetInfo();
            _boids.Add(info);
            int xIndex = (int)(math.floor(info.pos.x) / cellWidth);
            int zIndex = (int)(math.floor(info.pos.z) / cellDepth);
            GridPoint gp = new GridPoint(xIndex, zIndex, cellXAmount);
            //_grid.Add(gp, info);
            _grid.Add(gp, i);
        }
        allocated = true;
    }


    // Returns a complete map from boid index to list of neighbouring boid indices, for all boids
    public NativeMultiHashMap<int, int> GetNeighbours()
    {
        NativeMultiHashMap<int, int> neighbours = new NativeMultiHashMap<int, int>(10, Allocator.TempJob);
        for (int i = 0; i < _boids.Length; i++)
        {
            NativeArray<int> neighbourArray = FindBoidsWithinRadius(_boids[i], ClassInfos.infos[(int)_boids[i].type].viewRadius);
            foreach (int j in neighbourArray)
            {
                neighbours.Add(i, j);
            }
            neighbourArray.Dispose();
        }
        return neighbours;
    }


    // Finds indices of boids within the given radius from the given boid (excludes the given boid itself)
    public NativeArray<int> FindBoidsWithinRadius(Boid.BoidInfo boid, float radius)
    {
        // The cell that the current boid is in
        int xIndex = (int)(math.floor(boid.pos.x) / cellWidth);
        int zIndex = (int)(math.floor(boid.pos.z) / cellDepth);
        NativeList<int> boidsInRadius = new NativeList<int>(10, Allocator.Temp);

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
                    foreach (int b in _grid.GetValuesForKey(gp))
                    {
                        float3 horizontalDistance = _boids[b].pos - boid.pos;
                        if (horizontalDistance.x * horizontalDistance.x + horizontalDistance.z * horizontalDistance.z < radius * radius && !boid.Equals(_boids[b]))
                        {
                            boidsInRadius.Add(b);
                        }
                    }
                }
            }
        }



        // Construct resulting array
        NativeArray<int> result = new NativeArray<int>(boidsInRadius.Length, Allocator.Temp);
        for (int i = 0; i < boidsInRadius.Length; i++)
        {
            result[i] = boidsInRadius[i];
        }

        return result;
    }

    public NativeArray<int> FindBoidsWithinRadius(float3 pos, float radius)
    {
        // The cell that the current boid is in
        int xIndex = (int)(math.floor(pos.x) / cellWidth);
        int zIndex = (int)(math.floor(pos.z) / cellDepth);
        NativeList<int> boidsInRadius = new NativeList<int>(10, Allocator.Temp);

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
                    foreach (int b in _grid.GetValuesForKey(gp))
                    {
                        float3 horizontalDistance = _boids[b].pos - pos;
                        if (horizontalDistance.x * horizontalDistance.x + horizontalDistance.z * horizontalDistance.z < radius * radius)
                        {
                            boidsInRadius.Add(b);
                        }
                    }
                }
            }
        }



        // Construct resulting array
        NativeArray<int> result = new NativeArray<int>(boidsInRadius.Length, Allocator.Temp);
        for (int i = 0; i < boidsInRadius.Length; i++)
        {
            result[i] = boidsInRadius[i];
        }

        return result;
    }


    // Disposes of temporary data
    public void Dispose()
    {
        if (!allocated) return;
        _grid.Dispose();
        _boids.Dispose();
        allocated = false;
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
}
