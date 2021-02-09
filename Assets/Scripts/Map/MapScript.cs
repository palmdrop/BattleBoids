using System;
using UnityEngine;
using UnityEngine.WSA;

namespace Map
{
    public class MapScript : MonoBehaviour
    {
        // The ground plane of the map
        private GameObject _ground;        
        
        // The dimensions of the map. 
        private Rect _bounds;
        
        // A heightmap representation of the map. The heightmap only contains values for the tiles in "Ground"
        private int _heightmapWidth;
        private int _heightmapHeight;
        private float[] _heightmap;
        
        // The list of all child tiles, placed in a grid corresponding to the heightmap
        private GameObject[] _groundTiles;
        
        void Start()
        {
            // Get ground child, used to calculate bounds and heightmap
            _ground = transform.Find("Ground").gameObject;

            // Calculate the bounds of the map. 
            CalculateBounds();
            
            // Calculate the heightmap representation of the ground of the map 
            CalculateHeightmap();
        }
        
        private void CalculateBounds()
        {
            // Max and min x and z values, used to calculate map dimensions
            float minX = float.MaxValue, maxX = float.MinValue;
            float minZ = float.MaxValue, maxZ = float.MinValue;
            
            // Iterate over all children and calculate max and min xz values
            for(int i = 0; i < _ground.transform.childCount; i++)
            {
                // Get child position
                GameObject child = _ground.transform.GetChild(i).gameObject;
                Vector3 position = child.transform.localPosition;
                
                // Find max and min x value
                minX = Math.Min(minX, position.x);
                maxX = Math.Max(maxX, position.x);
                
                // Find max and min z value
                minZ = Math.Min(minZ, position.z);
                maxZ = Math.Max(maxZ, position.z);
            }

            // The minimum x and y coordinates represents the upper left corner of the bounds
            _bounds = new Rect(
                    // The minimum x and y coordinates represents the upper left corner of the bounds
                new Vector2(minX, minZ), 
                    // The difference between the max and min values represent the width and height
                new Vector2(maxX - minX, maxZ - minZ)
            );
        }

        // Transforms a 3d point into an index. Only uses the x and z component.
        private int PointToIndex(Vector3 point)
        {
            int x = (int) (point.x - _bounds.x + 0.5);
            int y = (int) (point.z - _bounds.y + 0.5);
            return x + y * _heightmapWidth;
        }
        
        private void CalculateHeightmap() 
        {
            // Since the map is tiled based, the heightmap doesn't need any more detail 
            // than the number of tiles direction 
            // NOTE: this assumes that each tile is of unit size and none overlap
            _heightmapWidth = Mathf.CeilToInt(_bounds.width) + 1;
            _heightmapHeight = Mathf.CeilToInt(_bounds.height) + 1;
            
            // Instantiate heightmap
            _heightmap = new float[_heightmapWidth * _heightmapHeight];
            
            // Instantiate tile grid
            _groundTiles = new GameObject[_heightmap.Length];

            for (int i = 0; i < _heightmap.Length; i++) _heightmap[i] = float.MinValue;
            
            for (int i = 0; i < _ground.transform.childCount; i++)
            {
                // Get child tile
                GameObject child = _ground.transform.GetChild(i).gameObject;
                
                // Calculate height and assign value to heightmap index
                Vector3 childPosition = child.transform.localPosition;
                float y = childPosition.y + child.transform.localScale.y / 2.0f;
                
                int index = PointToIndex(childPosition);
                _heightmap[index] = y;
                
                // add tile to grid
                _groundTiles[index] = child;
            }
        }

        public float HeightmapLookup(Vector3 point)
        {
            // Get the corresponding index of the position
            int index = PointToIndex(point);
            
            // If the index is outside the range of the heightmap, this means
            // the position is outside the bounds of the map. Assume there's no ground at this position, and return 
            // the minimum value.
            if (index < 0 || index >= _heightmap.Length) return float.MinValue;
            
            // Otherwise, if valid index, return corresponding heightmap value
            return _heightmap[index];
        }
        
        // Calculates if a given point is inside the map bounds
        public bool PointInsideBounds(Vector3 point)
        {
            return point.x >= _bounds.x && point.x <= _bounds.x + _bounds.width 
                && point.z >= _bounds.y && point.z <= _bounds.y + _bounds.height;
        }

        // Returns the ground tile at a specified position. Returns null if outside bounds or if there's no tile at 
        // the specified position.
        public GameObject GetGroundTileAt(Vector3 point)
        {
            if (PointInsideBounds(point)) return _groundTiles[PointToIndex(point)];
            return null;
        }
        
        public Rect GetBounds()
        {
            return _bounds;
        }
    }
}
