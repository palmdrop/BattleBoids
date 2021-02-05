using System;
using System.Collections;
using System.Collections.Generic;
using Map;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor;
using UnityEngine.SceneManagement;

public class MapTestScript
{
    // Makes sure every tile is contained within the bounds
    [UnityTest]
    public IEnumerator TestIfBoundsContainAllGroundTiles()
    {
        // Load scene
        SceneManager.LoadScene("SampleScene");

        yield return new WaitForEndOfFrame();
        
        // Find map
        GameObject map = GameObject.Find("SampleScene/Map");
        Debug.Log(map);
        
        // Retrieve map script, required to calculate bounds
        MapScript mapScript = map.GetComponent<MapScript>();
        
        // Get bounds, will be used to check if all ground tiles are contained
        Rect bounds = mapScript.GetBounds();
        
        // Get min and max coordinates of x and z axes
        float _minX = bounds.x;
        float _minZ = bounds.y;
        float _maxX = _minX + bounds.width;
        float _maxZ = _minZ + bounds.height;
        
        // Get ground component, used to iterate over child tiles
        Component _ground = map.transform.Find("Ground");
        
        yield return new WaitForEndOfFrame();
        
        // Iterate over all children and make sure they are contained within the bounds
        for(int i = 0; i < _ground.transform.childCount; i++)
        {
            // Get child position
            Vector3 position = _ground.transform.GetChild(i).localPosition;
            double cX = position.x;
            double cZ = position.z;
            
            // If child x
            if (cX < _minX || cX > _maxX || cZ < _minZ || cZ > _maxZ)
            {
                Assert.Fail();
            }
        }
        Assert.True(true);
    }
    
    // This test ensures that the bounds is the smallest rectangle possible which contains the children 
    /*[UnityTest]
    public IEnumerator TestIfThereIsATileAtEachEdgeOfBounds()
    {
        
        bool topEdge = false, leftEdge = false, bottomEdge = false, rightEdge = false;
        
        // Iterate over all children and make sure they are contained within the bounds
        for(int i = 0; i < _ground.transform.childCount; i++)
        {
            // Get child position
            Vector3 position = _ground.transform.GetChild(i).localPosition;
            double cX = position.x;
            double cZ = position.z;

            // Check if child position coincides with edge of bounds
            if (!topEdge && cZ == _minZ)
            {
                topEdge = true;
            }
            if (!leftEdge && cX == _minX)
            {
                leftEdge = true;
            }
            if (!bottomEdge && cZ == _maxZ)
            {
                bottomEdge = true;
            }
            if (!rightEdge && cX == _maxX)
            {
                rightEdge = true;
            }
        }
        
        // If a tile coincides with each bound 
        Assert.True(topEdge && leftEdge && bottomEdge && rightEdge);

        yield return null;
    }*/
}
