using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor;

public class TileTestScript
{
    [UnityTest]
    public IEnumerator TestIfSumOfChildrenYScaleIsOne()
    {
        // Load Tile prefab
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Tile.prefab");
        // Instantiate a tile
        GameObject tile = GameObject.Instantiate(asset, new Vector3(0, 0, 0), Quaternion.identity);
        
        // Wait for the end of the frame (this makes sure the tile "Start" method is called)
        yield return new WaitForEndOfFrame();

        // The sum of the y-scaling of each child transform should be 1.0
        float sum = 0.0f;
        foreach (Transform child in tile.transform)
        {
            sum += child.localScale.y;
        }
        
        // Allow a small delta, in the case of rounding errors
        Assert.AreEqual(1.0, sum, 0.001);
    }
}
