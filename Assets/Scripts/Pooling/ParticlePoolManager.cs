using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class ParticlePoolManager : MonoBehaviour
{
    public enum Type
    {
        Death,
        Hit
    }

    public static ParticlePoolManager SharedInstance;
    private List<GameObject> pooledDeathAnim;
    private List<GameObject> pooledHitAnim;
    public GameObject DeathAnimPrefab;
    public GameObject HitAnimPrefab;
    public float deathActiveTime;
    public float hitActiveTime;

    private void Awake()
    {
        SharedInstance = this;
        deathActiveTime = DeathAnimPrefab.GetComponent<ParticleSystem>().main.duration;
        hitActiveTime = HitAnimPrefab.GetComponent<ParticleSystem>().main.duration;
        Debug.Log(deathActiveTime);
    }

    public void InstancePoolObjects(int amountToPool, Type type)
    {
        if (type == Type.Death)
        {
            pooledDeathAnim = new List<GameObject>();
            GameObject tmp;
            for (int i = 0; i < amountToPool; i++)
            {
                tmp = Instantiate(DeathAnimPrefab);
                tmp.SetActive(false);
                tmp.transform.parent = gameObject.transform;
                pooledDeathAnim.Add(tmp);
            }
        }
        if (type == Type.Hit)
        {
            pooledHitAnim = new List<GameObject>();
            GameObject tmp;
            for (int i = 0; i < amountToPool; i++)
            {
                tmp = Instantiate(HitAnimPrefab);
                tmp.SetActive(false);
                tmp.transform.parent = gameObject.transform;
                pooledHitAnim.Add(tmp);
            }
        }

    }

    public GameObject getPooledObject(Type type)
    {
        if (type == Type.Death && pooledDeathAnim != null)
        {
            int count = pooledDeathAnim.Count;
            for (int i = 0; i < count; i++)
            {
                if (!pooledDeathAnim[i].gameObject.activeSelf)
                {
                    StartCoroutine(DeactivateAfterTime(pooledDeathAnim[i].gameObject, deathActiveTime));
                    return pooledDeathAnim[i].gameObject;
                }
            }
            return null;
        }

        if (type == Type.Hit)
        {
            int count = pooledHitAnim.Count;
            for (int i = 0; i < count; i++)
            {
                if (!pooledHitAnim[i].gameObject.activeSelf)
                {
                    StartCoroutine(DeactivateAfterTime(pooledHitAnim[i].gameObject, hitActiveTime));
                    return pooledHitAnim[i].gameObject;
                }
            }
            return null;
        }
        return null;
    }

    public IEnumerator DeactivateAfterTime(GameObject o, float time)
    {
        yield return new WaitForSeconds(time);
        o.SetActive(false);
    }

}
