using System.Collections;
using Map;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class MapTestScript
{
    [SetUp]
    public void Setup()
    {
        // Load scene
        SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
        new WaitForSeconds(1);
    }
    
    // Makes sure every tile is contained within the bounds
    [UnityTest]
    public IEnumerator TestIfBoundsContainAllGroundTiles()
    {
        // Find map
        GameObject map = GameObject.Find("Map");

        // Retrieve map script, required to calculate bounds
        MapScript mapScript = map.GetComponent<MapScript>();
        
        // Get bounds, will be used to check if all ground tiles are contained
        Rect bounds = mapScript.GetBounds();
        
        // Get min and max coordinates of x and z axes
        float minX = bounds.x;
        float minZ = bounds.y;
        float maxX = minX + bounds.width;
        float maxZ = minZ + bounds.height;
        
        // Get ground component, used to iterate over child tiles
        Component ground = map.transform.Find("Ground");
        
        yield return new WaitForEndOfFrame();
        
        // Iterate over all children and make sure they are contained within the bounds
        for(int i = 0; i < ground.transform.childCount; i++)
        {
            // Get child position
            Vector3 position = ground.transform.GetChild(i).localPosition;
            
            double cX = position.x;
            double cZ = position.z;
            
            // If child x
            if (cX < minX || cX > maxX || cZ < minZ || cZ > maxZ)
            {
                Assert.Fail();
            }
        }
        Assert.True(true);
    }
    
    // This test ensures that the bounds is the smallest rectangle possible which contains the children 
    [UnityTest]
    public IEnumerator TestIfThereIsATileAtEachEdgeOfBounds()
    {
        // Find map
        GameObject map = GameObject.Find("Map");
        
        // Retrieve map script, required to calculate bounds
        MapScript mapScript = map.GetComponent<MapScript>();
        
        // Get bounds, will be used to check if all ground tiles are contained
        Rect bounds = mapScript.GetBounds();
        
        // Get min and max coordinates of x and z axes
        float minX = bounds.x;
        float minZ = bounds.y;
        float maxX = minX + bounds.width;
        float maxZ = minZ + bounds.height;
        
        // Get ground component, used to iterate over child tiles
        Component ground = map.transform.Find("Ground");
        
        // Each boolean signifies if a tile has been encountered which coincides with that edge 
        bool topEdge = false, leftEdge = false, bottomEdge = false, rightEdge = false;
        
        // Iterate over all children and make sure they are contained within the bounds
        for(int i = 0; i < ground.transform.childCount; i++)
        {
            // Get child position
            Vector3 position = ground.transform.GetChild(i).localPosition;
            double cX = position.x;
            double cZ = position.z;

            // Check if child position coincides with edge of bounds
            if (!topEdge && cZ == minZ)
            {
                topEdge = true;
            }
            if (!leftEdge && cX == minX)
            {
                leftEdge = true;
            }
            if (!bottomEdge && cZ == maxZ)
            {
                bottomEdge = true;
            }
            if (!rightEdge && cX == maxX)
            {
                rightEdge = true;
            }
        }
        
        // If a tile coincides with each bound 
        Assert.True(topEdge && leftEdge && bottomEdge && rightEdge);

        yield return null;
    }
}
