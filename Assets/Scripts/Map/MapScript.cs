using UnityEngine;

namespace Map
{
    public class MapScript : MonoBehaviour
    {
        private GameObject _ground;        
        private GameObject _walls;        
        private GameObject _corners;

        private Rect _bounds;
        
        void Start()
        {
            _ground  = transform.GetChild(0).gameObject;
            _walls   = transform.GetChild(1).gameObject;
            _corners = transform.GetChild(2).gameObject;

            Component cornerNW = _corners.transform.GetChild(0);
            Component cornerNE = _corners.transform.GetChild(1);
            Component cornerSW = _corners.transform.GetChild(2);
            
            _bounds = new Rect(
                cornerNW.transform.localPosition,
                new Vector2(
                    cornerNE.transform.localPosition.x - cornerNW.transform.localPosition.x,
                    cornerSW.transform.localPosition.y - cornerNW.transform.localPosition.y
                )
            );

            Debug.Log(_bounds);
        }
    }
}
