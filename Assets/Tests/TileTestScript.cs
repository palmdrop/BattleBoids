using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Map;
using UnityEditor;

public class TileTestScript
{
    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    // `yield return null;` to skip a frame.
    [UnityTest]
    public IEnumerator TileTestScriptWithEnumeratorPasses()
    {
        //GameObject tileGameObject = MonoBehaviour.Instantiate(Resources.Load<GameObject>("Prefabs/Tile"));
        //TileScript tile = tileGameObject.GetComponent<TileScript>();
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Tile.prefab");
        GameObject tile = GameObject.Instantiate(asset, new Vector3(0, 0, 0), Quaternion.identity);

        float sum = 0.0f;
        foreach (Transform child in tile.transform)
        {
            sum += child.localScale.y;
        }
        
        Assert.AreEqual(1.0, sum, 0.001);
        yield return null;
    }
}
