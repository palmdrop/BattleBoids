using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEditor;
using Unity.Collections;
using Unity.Mathematics;

public class GridTest
{
    [UnityTest]
    public IEnumerator TestGrid()
    {
        int testAmount = 100;
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BoidPrefab.prefab");

        List<Boid> boids = new List<Boid>();
        for (int i = 0; i < testAmount; i++)
        {
            GameObject go = GameObject.Instantiate(asset, new Vector3(UnityEngine.Random.Range(0f, 10f), 0, UnityEngine.Random.Range(0f, 10f)), Quaternion.identity);
            Boid b = go.GetComponent<Boid>();
            Player player = new Player();
            player.id = 1;
            b.SetOwner(player);
            boids.Add(b);
        }

        yield return new WaitForEndOfFrame();

        for (int i = 0; i < testAmount; i++)
        {
            Debug.Log(boids[i].GetPos());
        }

        BoidGrid grid = new BoidGrid();
        grid.Populate(boids);

        NativeMultiHashMap<int, Boid.BoidInfo> gridNeighbours = grid.GetNeighbours();

        for (int i = 0; i < testAmount; i++)
        {
            List<Boid.BoidInfo> realNeighbours = new List<Boid.BoidInfo>();
            for (int j = 0; j < testAmount; j++)
            {
                if (i == j) continue;

                float3 horizontalDistance = boids[i].GetInfo().pos - boids[j].GetInfo().pos;
                if (horizontalDistance.x * horizontalDistance.x + horizontalDistance.z * horizontalDistance.z < boids[i].GetInfo().classInfo.viewRadius * boids[i].GetInfo().classInfo.viewRadius)
                {
                    realNeighbours.Add(boids[j].GetInfo());
                }
            }

            Assert.AreEqual(realNeighbours.Count, gridNeighbours.CountValuesForKey(i));

            int k = 0;
            foreach (Boid.BoidInfo neighbourInfo in gridNeighbours.GetValuesForKey(i))
            {
                //Assert.AreEqual(neighbourInfo, realNeighbours[k]);
                Assert.IsTrue(realNeighbours.Contains(neighbourInfo));
                k++;
            }
        }

        grid.Dispose();
    }
}
