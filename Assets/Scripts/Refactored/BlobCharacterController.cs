using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlobCharacterController : MonoBehaviour
{
    public float acceleration;
    public float drag;
    public Vector2 velocity;
    InputMaster controls;
    bool goUp,goDown,goRight,goLeft;
    public int fastLoopCount,slowLoopCount,grabbingLoopCount,tossingLoopCount;
    public float rotationTime;
    public float rotationZ;
    public float rotationVel;

    BlobTail tail;
    
    public float objFindRange;
    public float objGrabRange;

    void Start() 
    {
        tail = GetComponent<BlobTail>();
        controls = new InputMaster();
        controls.Player.MoveUp.started += _ => goUp = true;
        controls.Player.MoveUp.canceled  += _ => goUp = false;
        controls.Player.MoveDown.started += _ => goDown = true;
        controls.Player.MoveDown.canceled  += _ => goDown = false;
        controls.Player.MoveLeft.started += _ => goLeft = true;
        controls.Player.MoveLeft.canceled  += _ => goLeft = false;
        controls.Player.MoveRight.started += _ => goRight = true;
        controls.Player.MoveRight.canceled  += _ => goRight = false;
        controls.Player.Enable();
        rotationZ = transform.rotation.eulerAngles.z;
    }
    void Update() 
    {
        BasicMovement();
        if(Input.GetKeyDown(KeyCode.E))
        {
            if(tail.whatIsHangingFromTheTail==null) StartCoroutine(tail.GrabObj());
            else StartCoroutine(tail.TossObj());
        }
    }

    void BasicMovement()
    {
        transform.position += ((Vector3)velocity) * Time.deltaTime;
        Vector2 forceDirection = Vector2.zero;
        float newRotZ = rotationZ;
        if(goUp) forceDirection += Vector2.up;
        if(goDown) forceDirection -= Vector2.up;
        if(goRight) forceDirection += Vector2.right;
        if(goLeft) forceDirection -= Vector2.right;
        forceDirection.Normalize();
        if(forceDirection != Vector2.zero) newRotZ = 180f*Mathf.Atan2(-forceDirection.x,forceDirection.y)/Mathf.PI-90;

        switch (tail.tailMode)
        {
            case TailMode.Swimming:
            if(forceDirection != Vector2.zero) tail.loopCount = fastLoopCount;
            else tail.loopCount = slowLoopCount;
            break;

            case TailMode.Grabbing:
            tail.loopCount = grabbingLoopCount;
            break;

            case TailMode.Tossing:
            tail.loopCount = tossingLoopCount;
            break;
        }

        rotationZ = Mathf.SmoothDamp(rotationZ, newRotZ, ref rotationVel, rotationTime);
        transform.rotation = Quaternion.Euler(0,0,rotationZ);
        velocity += (forceDirection * acceleration - drag * velocity) * Time.deltaTime; 
    }
}
