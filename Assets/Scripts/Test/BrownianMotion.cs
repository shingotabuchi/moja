using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrownianMotion : MonoBehaviour
{
    public float d;
    public float gamma;
    Vector3 velocity;
    void FixedUpdate()
    {
        transform.localPosition += velocity * Time.deltaTime;
        velocity += (
            d * (Vector3)Random.insideUnitCircle
            - gamma * velocity
        )*Time.deltaTime;

        if(transform.localPosition.x > 0.5f) transform.localPosition -= new Vector3(1f,0,0);
        else if(transform.localPosition.x < -0.5f) transform.localPosition += new Vector3(1f,0,0);
        else if(transform.localPosition.y > 0.5f) transform.localPosition -= new Vector3(0,1f,0);
        else if(transform.localPosition.y < -0.5f) transform.localPosition += new Vector3(0,1f,0);
    }
}
