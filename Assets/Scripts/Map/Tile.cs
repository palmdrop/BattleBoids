using System;
using UnityEngine;

namespace Map
{
    public class Tile : MonoBehaviour
    {
        // Variables for adjusting the size of the layers (the sum should add up to 1.0)
        [SerializeField] private float topSize    = 0.1f;
        [SerializeField] private float middleSize = 0.6f;
        [SerializeField] private float bottomSize = 0.3f;

        // The three layers of the tile
        private GameObject _top;
        private GameObject _middle;
        private GameObject _bottom;
    
        // Helper function for constraining a value to a specific range
        private float Constrain(float value, float min, float max)
        {
            return Math.Max(Math.Min(value, max), min);
        }
    
        // The start function will adjust the sizes of the tile layers based on the 
        void Start()
        {
            // Get the children of the tile 
            _top    = transform.GetChild(0).gameObject;
            _middle = transform.GetChild(1).gameObject;
            _bottom = transform.GetChild(2).gameObject;
        
            // Constrain sizes into valid ranges
            // This prevents the sum of the sizes to exceed 1.0 and be less than 0.0
            // However, the values may not add up to 1.0
            topSize    = Constrain(topSize, 0, 1.0f);
            middleSize = Constrain(middleSize, 0, 1.0f - topSize);
            topSize    = Constrain(topSize, 0, 1.0f - topSize - middleSize);

            // Normalize sizes (if they do not sum up to 1.0)
            float sum = topSize + middleSize + bottomSize;
            if (Math.Abs(sum - 1.0) > 0.001) // Use a delta value to avoid 
            {
                // Scale sizes to ensure that their sum adds up to 1.0            
                topSize    /= sum;
                middleSize /= sum;
                bottomSize /= sum;
            }
        
            // Change scale and position of layers
            _top.transform.localPosition    = new Vector3(0, 0.5f - topSize / 2.0f, 0);
            _top.transform.localScale       = new Vector3(1.0f, topSize, 1.0f);

            _middle.transform.localPosition = new Vector3(0, 0.5f - topSize - middleSize / 2.0f, 0);
            _middle.transform.localScale    = new Vector3(1.0f, middleSize, 1.0f);
        
            _bottom.transform.localPosition = new Vector3(0, 0.5f - topSize - middleSize - bottomSize / 2.0f, 0);
            _bottom.transform.localScale    = new Vector3(1.0f, bottomSize, 1.0f);
        }
    }
}
