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

        if(Input.GetKeyDown(KeyCode.E))
        {
            if(!grab)
            {
                grab = true;
                StartCoroutine(GetComponent<GeometryTest>().ropes[0].GrabItem(food));
            }
            else if(GetComponent<GeometryTest>().ropes[0].whatIsHangingFromTheRope != null)
            {
                grab = false;
                GetComponent<GeometryTest>().ropes[0].whatIsHangingFromTheRope = null;
            }
        }
    }
}
