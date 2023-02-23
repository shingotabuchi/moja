using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RDSpawner : MonoBehaviour
{
    public GameObject A,B;
    public Transform AParent,BParent;
    public int ACount,BCount;
    public float BSpawnRadius;
    public float ASearchRadius;
    public float feed,kill;
    public ContactFilter2D AContactFilter = new ContactFilter2D();
    void Start()
    {
        // AContactFilter.SetLayerMask(LayerMask.GetMask("B"));
        RDA.spawner = this;
        for (int i = 0; i < ACount; i++)
        {
            Instantiate(A,AParent).transform.localPosition = new Vector3(Random.Range(-0.5f,0.5f),Random.Range(-0.5f,0.5f),0);
        }
        for (int i = 0; i < BCount; i++)
        {
            Instantiate(B,BParent).transform.localPosition = (Vector3)(Random.insideUnitCircle * BSpawnRadius);
        }
    }

    void Update()
    {
        foreach (Transform b in BParent)
        {
            if(Random.Range(0f,1f) <= (feed *ACount)/AParent.childCount)
            {
                // Instantiate(A,AParent).transform.position = b.position + 2f * ASearchRadius * (Vector3)Random.insideUnitCircle;
                Instantiate(A,AParent).transform.localPosition = new Vector3(Random.Range(-0.5f,0.5f),Random.Range(-0.5f,0.5f),0);
            }
            if(Random.Range(0f,1f) <= kill)
            {
                Destroy(b.gameObject);
            }
        }
    }
}
