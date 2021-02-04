using System;
using UnityEngine;

namespace Map
{
    public class MapScript : MonoBehaviour
    {
        // Children of the map object
        private GameObject _ground;        
        private GameObject _walls;        

        // The dimensions of the map. 
        private Rect _bounds;
        
        void Start()
        {
            // Get children
            _ground  = transform.GetChild(0).gameObject;
            _walls  = transform.GetChild(1).gameObject;

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

            // Calculate bounds
            _bounds = new Rect(
                new Vector2(minX, minZ), 
                new Vector2(maxX - minX, maxZ - minZ)
            );
        }

        public Rect GetBounds()
        {
            return _bounds;
        }
    }
}
