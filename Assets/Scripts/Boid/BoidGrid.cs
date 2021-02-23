using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using System;
using Unity.Mathematics;

public class BoidGrid
{
    [SerializeField] private static readonly float cellWidth = 1f, cellDepth = 1f;
    private static readonly int cellXAmount = 20;
    private NativeMultiHashMap<GridPoint, Boid.BoidInfo> _grid;// = new NativeMultiHashMap<GridPoint, Boid.BoidInfo>(10, Allocator.TempJob);
    private List<Boid> _boids;


    // TODO: Documentation
    public void Populate(List<Boid> boids)
    {
        _boids = boids;
        _grid = new NativeMultiHashMap<GridPoint, Boid.BoidInfo>(10, Allocator.TempJob);
        foreach (Boid b in boids)
        {
            Boid.BoidInfo info = b.GetInfo();
            int xIndex = (int)(math.floor(info.pos.x) / cellWidth);
            int zIndex = (int)(math.floor(info.pos.z) / cellDepth);
            GridPoint gp = new GridPoint(xIndex, zIndex, cellXAmount);
            _grid.Add(gp, info);
        }
    }


    // TODO: Documentation
    public NativeMultiHashMap<int, Boid.BoidInfo> GetNeighbours()
    {
        NativeMultiHashMap<int, Boid.BoidInfo> neighbours = new NativeMultiHashMap<int, Boid.BoidInfo>(10, Allocator.TempJob);
        for (int i = 0; i < _boids.Count; i++)
        {
            Boid.BoidInfo[] neighbourArray = FindBoidsWithinRadius(_boids[i].GetInfo(), _boids[i].GetInfo().classInfo.viewRadius);
            foreach (Boid.BoidInfo info in neighbourArray)
            {
                neighbours.Add(i, info);
            }
        }
        return neighbours;
    }


    // Finds all boids within the given radius from the given boid (excludes the given boid itself)
    public Boid.BoidInfo[] FindBoidsWithinRadius(Boid.BoidInfo boid, float radius)
    {
        // The cell that the current boid is in
        int xIndex = (int)(math.floor(boid.pos.x) / cellWidth);
        int zIndex = (int)(math.floor(boid.pos.z) / cellDepth);
        List<Boid.BoidInfo> boidsInRadius = new List<Boid.BoidInfo>();

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

        Boid.BoidInfo[] result = new Boid.BoidInfo[boidsInRadius.Count];
        for (int i = 0; i < boidsInRadius.Count; i++)
        {
            result[i] = boidsInRadius[i];
        }

        return result;
    }


    // TODO: Documentation
    public void Dispose()
    {
        _grid.Dispose();
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
