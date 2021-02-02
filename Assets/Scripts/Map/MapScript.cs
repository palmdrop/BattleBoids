using UnityEngine;

namespace Map
{
    public class MapScript : MonoBehaviour
    {
        // Children of the map object
        private GameObject _ground;        
        private GameObject _walls;        
        private GameObject _corners;

        // The dimensions of the map. Is calculated using the positions of the corners.
        private Rect _bounds;
        
        void Start()
        {
            // Get children
            _ground  = transform.GetChild(0).gameObject;
            _walls   = transform.GetChild(1).gameObject;
            _corners = transform.GetChild(2).gameObject;

            // Get some of the corners in order to calculate dimensions
            Component cornerNW = _corners.transform.GetChild(0);
            Component cornerNE = _corners.transform.GetChild(1);
            Component cornerSW = _corners.transform.GetChild(2);
            
            _bounds = new Rect(
                // Upper left corner is same as the position of the north west corner
                cornerNW.transform.localPosition,
                // Width and height is calculated using the difference in x and z 
                new Vector2(
                    cornerNE.transform.localPosition.x - cornerNW.transform.localPosition.x,
                    cornerSW.transform.localPosition.z - cornerNW.transform.localPosition.z
                )
            );
        }

        public Rect GetBounds()
        {
            return _bounds;
        }
    }
}
