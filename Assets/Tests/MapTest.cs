using System;
using System.Collections;
using System.Collections.Generic;
using Map;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class MapTest
{
    [SetUp]
    public void Setup()
    {
        // Load scene
        SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
        new WaitForSeconds(2);
    }
    
    // Makes sure every tile is contained within the bounds
    [UnityTest]
    public IEnumerator TestIfBoundsContainAllGroundTiles()
    {
        // Find map
        GameObject map = GameObject.Find("Map");
        
        yield return new WaitForEndOfFrame();

        // Retrieve map script, required to calculate bounds
        Map.Map mapScript = map.GetComponent<Map.Map>();
        
        yield return new WaitForEndOfFrame();
        
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

        yield return new WaitForEndOfFrame();
        
        // Retrieve map script, required to calculate bounds
        Map.Map mapScript = map.GetComponent<Map.Map>();
        
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
    }

    [UnityTest]
    public IEnumerator TestIfGroundTileHeightCorrespondsToHeightmapValue()
    {
        // Find map, map script and ground component
        GameObject map = GameObject.Find("Map");
        yield return new WaitForEndOfFrame();
        Map.Map mapScript = map.GetComponent<Map.Map>();
        Component ground = map.transform.Find("Ground");

        // Iterate over all children
        for (int i = 0; i < ground.transform.childCount; i++)
        {
            Transform child = ground.transform.GetChild(i);
            
            // Calculate the height of the child
            float tileHeight = child.position.y + child.localScale.y / 2.0f;

            // Iterate over all corners of the children (and the center), to make sure the heightmap has the correct
            // value for every point on the child.
            for (float dx = -0.50f; dx <= 0.50f; dx += 0.50f)
            {
                for (float dy = -0.50f; dy <= 0.50; dy += 0.50f)
                {
                    float heightmapValue = mapScript.HeightmapLookup(child.localPosition 
                                                                    // scale the offset slightly to avoid problems where the tiles are adjacent
                                                                     + new Vector3(dx, 0.0f, dy) * 0.999f); 
                    
                    // allow a small delta to account for precision errors 
                    if (Mathf.Abs(heightmapValue - tileHeight) > 0.000001)
                    {
                        Assert.Fail();
                    }
                }
            }
        }

        Assert.Pass();
    }

    [UnityTest]
    public IEnumerator TestIfEveryTileIsAtCorrectGridPosition()
    {
        // Find map, mapScript and ground
        GameObject map = GameObject.Find("Map");
        yield return new WaitForEndOfFrame();
        Map.Map mapScript = map.GetComponent<Map.Map>();
        Component ground = map.transform.Find("Ground");

        // Iterate over all children 
        for (int i = 0; i < ground.transform.childCount; i++)
        {
            GameObject child = ground.transform.GetChild(i).gameObject;

            // Make sure the child is the same as the tile saved in the internal map grid at that position
            if (child != mapScript.GetGroundTileAt(child.transform.localPosition))
            {
                Assert.Fail();
            }
        }

        Assert.Pass();
    }

    [UnityTest]
    public IEnumerator TestIfEveryNullTileHasHeightmapValueOfMinValue()
    {
        // Find map, mapScript and ground
        GameObject map = GameObject.Find("Map");
        yield return new WaitForEndOfFrame();
        Map.Map mapScript = map.GetComponent<Map.Map>();
        Rect bounds = mapScript.GetBounds();
        
        // Iterate over all discrete bound positions 
        for (float x = bounds.x; x <= bounds.x + bounds.width; x += 1.0f)
        {
            for (float z = bounds.y; z <= bounds.y + bounds.height; z += 1.0f)
            {
                Vector3 position = new Vector3(x, 0.0f, z);

                GameObject tile = mapScript.GetGroundTileAt(position);
                
                // Check if the tile at the corresponding position is null
                if (tile == null)
                {
                    // If null, make sure the heightmap value is float.MinValue
                    float heightmapValue = mapScript.HeightmapLookup(position);
                    if (heightmapValue != float.MinValue)
                    {
                        Assert.Fail();
                    }
                }
            }
        }

        Assert.Pass();
    }

    [UnityTest]
    public IEnumerator TestIfEveryEdgeIsCoveredByAWall()
    {
        // Find map, mapScript and ground
        GameObject map = GameObject.Find("Map");
        yield return new WaitForEndOfFrame();
        Map.Map mapScript = map.GetComponent<Map.Map>();
        Component ground = map.transform.Find("Ground");

        // Find walls object and create a hashset for easy wall location lookup
        Component walls = map.transform.Find("Walls");
        HashSet<Vector2> wallPositions = new HashSet<Vector2>();

        // Iterate over all the walls and save their xz position
        // This will be used to verify that there's a wall adjacent to any edge tile
        for (int i = 0; i < walls.transform.childCount; i++)
        {
            GameObject wall = walls.transform.GetChild(i).gameObject;
            Vector3 wallPosition = wall.transform.localPosition;
            wallPositions.Add(new Vector2(wallPosition.x, wallPosition.z));
        }
        
        // Iterate over all the children
        for (int i = 0; i < ground.transform.childCount; i++)
        {
            // Get current tile and position 
            GameObject tile = ground.transform.GetChild(i).gameObject;
            Vector3 position = tile.transform.localPosition;
            
            // Iterate over neighbour positions
            for (float dx = -1.0f; dx <= 1.0f; dx += 1.0f)
            {
                float startStop = Math.Abs(dx) == 1.0 ? 0.0f : 1.0f;
                for (float dz = -startStop; dz <= startStop; dz += 1.0f)
                {
                    float x = position.x + dx;
                    float z = position.z + dz;

                    // Check the corresponding heightmap value of that position. If float.MinValue, this means the tile
                    // has no neighbour at that location. Then check if a wall exists.
                    float heightValue = mapScript.HeightmapLookup(new Vector3(x, 0, z));
                    if (heightValue == float.MinValue && !wallPositions.Contains(new Vector2(x, z)))                       
                    {
                        Assert.Fail();
                    }
                }
            }
        }

        Assert.Pass();
    }
    
}
