using UnityEngine;

public class MoveUpAndDown : MonoBehaviour
{
    [SerializeField] private float moveAmount = 3f;

    private float movedAmount = 0;

    private void Start()
    {
        moveAmount = Random.value > .5f ? -moveAmount : moveAmount;
    }

    private void FixedUpdate()
    {
        moveCloud();
    }

    private void moveCloud()
    {

        float moveIncrement = Random.Range(0, Time.fixedDeltaTime * .5f);
        
        movedAmount += moveIncrement;
        
        Vector3 currentPosition = gameObject.transform.position;
       gameObject.transform.position = new Vector3(
           currentPosition.x, 
           currentPosition.y + (moveAmount > 0 ? moveIncrement : -moveIncrement), 
           currentPosition.z
           );
        
        if (movedAmount >= Mathf.Abs(moveAmount))
        {
            movedAmount = 0;
            moveAmount = - moveAmount;
        }
    }
}
