using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Blob : MonoBehaviour
{
    public bool grab;
    public Transform food;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(grab)
        {
            grab = false;
            StartCoroutine(GetComponent<GeometryTest>().ropes[0].GrabItem(food));
        }
    }
}
