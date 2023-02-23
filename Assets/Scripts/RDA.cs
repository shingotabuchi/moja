using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RDA : MonoBehaviour
{
    public static RDSpawner spawner;
    List<Collider2D> bColliders = new List<Collider2D>();
    void Update()
    {
        int bCount = Physics2D.OverlapCircle((Vector2)transform.position, spawner.ASearchRadius, spawner.AContactFilter, bColliders);
        if(bCount >= 2)
        {
            Instantiate(spawner.B,spawner.BParent).transform.position = transform.position;
            Destroy(gameObject);
        }
    }
}
