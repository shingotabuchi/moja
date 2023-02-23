using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollowBlob : MonoBehaviour
{
    public Transform blob;
    public float posZ;

    // Update is called once per frame
    void Update()
    {
        transform.position = new Vector3(blob.position.x,blob.position.y,posZ);
    }
}
