using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEditor;

public class GeometryTest : MonoBehaviour
{
    SpriteRenderer spriteR;
    public int lineRes;
    public float area;
    public bool initParticle;
    public int modes;
    public float[] modeCoeffs;
    public float[] modeSinCoeffs;
    public float[] dModeCoeffs;
    public float[] dModeSinCoeffs;
    public float[] modeCoeffsK;
    public float[] modeCoeffsNoise;
    public float[] modeSinCoeffsK;
    public float[] modeSinCoeffsNoise;
    float polygonTheta = 0;
    public float segmentLength = 0.1f;
    public float univK = 1f;
    public float univGamma = 1f;
    public float univNoise = 1f;
    List<Vector3> dotPositions = new List<Vector3>();
    Mesh mesh;
    public GameObject meshObj;
    public bool touchOn;
    public float moveDirection;
    public float acceleration;
    public float drag;
    Vector2 speed;
    InputMaster controls;
    bool goUp,goDown,goRight,goLeft;
    public int ropeCount;
    public GameObject ropePrefab;
    public List<RopeControllerRealistic> ropes = new List<RopeControllerRealistic>();
    public float ropeTheta;
    public int fastLoopCount,slowLoopCount;
    public float rotationTime;
    public float rotationZ;
    public float rotationVel;
    // Start is called before the first frame update
    void OnEnable()
    {
        for (int i = 0; i < ropeCount; i++)
        {
            ropes.Add(Instantiate(ropePrefab,transform).GetComponent<RopeControllerRealistic>());
        }
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
        mesh = new Mesh();
        meshObj.GetComponent<MeshFilter>().mesh = mesh;
        rotationZ = transform.rotation.eulerAngles.z;
    }

    // void OnEnable()
    // {
    //     controls.Player.Enable();
    // }

    // void OnDisable()
    // {
    //     controls.Player.Disable();
    // }

    // Update is called once per frame
    void FixedUpdate()
    {
        transform.position += ((Vector3)speed) * Time.deltaTime;

        Vector2 forceDirection = Vector2.zero;
        float newRotZ = rotationZ;
        if(goUp) forceDirection += Vector2.up;
        if(goDown) forceDirection -= Vector2.up;
        if(goRight) forceDirection += Vector2.right;
        if(goLeft) forceDirection -= Vector2.right;
        forceDirection.Normalize();
        if(forceDirection != Vector2.zero)
        {
            for (int i = 0; i < ropeCount; i++)
            {
                ropes[i].loopCount = fastLoopCount;
                // if(!ropes[i].isGoingRound) ropes[i].loopCount = fastLoopCount;
                // else ropes[i].loopCount = slowLoopCount;
            }
            newRotZ = 180f*Mathf.Atan2(-forceDirection.x,forceDirection.y)/Mathf.PI-90;
        }
        else
        {
            for (int i = 0; i < ropeCount; i++)
            {
                ropes[i].loopCount = slowLoopCount;
            }
        }
        rotationZ = Mathf.SmoothDamp(rotationZ, newRotZ, ref rotationVel, rotationTime);
        transform.rotation = Quaternion.Euler(0,0,rotationZ);
        speed += (forceDirection * acceleration - drag * speed) * Time.deltaTime; 

        for (int i = 1; i < modes; i++)
        {
            dModeCoeffs[i] += (-univGamma*dModeCoeffs[i] -univK*modeCoeffsK[i]*modeCoeffs[i] + univNoise*modeCoeffsNoise[i]*Random.Range(-1f,1f))*Time.deltaTime;
            modeCoeffs[i] += dModeCoeffs[i]*Time.deltaTime;

            dModeSinCoeffs[i] += (-univGamma*dModeSinCoeffs[i] -univK*modeSinCoeffsK[i]*modeSinCoeffs[i] + univNoise*modeSinCoeffsNoise[i]*Random.Range(-1f,1f))*Time.deltaTime;
            modeSinCoeffs[i] += dModeSinCoeffs[i]*Time.deltaTime;
        }
        UpdatePolygonPerimeter();
    }

    void UpdatePolygonPerimeter(bool initParticle = false)
    {
        List<Vector3> points = new List<Vector3>();
        float circumferenceProgressPerStep = (float)1/lineRes;
        float TAU = 2*Mathf.PI;
        float radianProgressPerStep = circumferenceProgressPerStep*TAU;
        float scale;
        float sqrdSum = 0f;
        polygonTheta = Mathf.PI * (transform.rotation.eulerAngles.z/180f);
        for (int i = 0; i < lineRes; i++)
        {
            float currentRadian = radianProgressPerStep*i + polygonTheta;
            float newRadius = 0f;
            for (int j = 0; j < modes; j++)
            {
                newRadius += modeCoeffs[j]*Mathf.Cos(j*(currentRadian)) + modeSinCoeffs[j]*Mathf.Sin(j*(currentRadian));
            }
            sqrdSum += newRadius * newRadius;
        }
        scale = Mathf.Sqrt((2*area)/(sqrdSum * lineRes * Mathf.Sin(TAU/lineRes)));

        float perimeterLength = 2f * Mathf.PI * modeCoeffs[0] * scale;
        float theta = 0f;
        float dtheta,r;
        int spawnedDots = 0;
        Vector3 spawnPos;
        while (theta <= 2*Mathf.PI)
        {
            r = 0f;
            for (int i = 0; i < modes; i++)
            {
                r += modeCoeffs[i]*Mathf.Cos(i*(theta)) + modeSinCoeffs[i]*Mathf.Sin(i*(theta));
            }
            r *= scale;
            if(r <= Mathf.Epsilon)
            {
                theta += 0.1f * Mathf.PI;
                continue;
            }
            spawnPos = new Vector3(Mathf.Cos((theta+polygonTheta)), Mathf.Sin((theta+polygonTheta)),0);
            spawnPos *= r;
            if(dotPositions.Count < spawnedDots + 1)
            {
                dotPositions.Add(spawnPos);
            }
            else
            {
                dotPositions[spawnedDots] = spawnPos;
            }
            spawnedDots++;

            dtheta = segmentLength/r;
            theta += dtheta;
        }
        ropeTheta = 0f;
        for (int i = 0; i < ropeCount; i++)
        {
            r = 0f;
            for (int j = 0; j < modes; j++)
            {
                r += modeCoeffs[j]*Mathf.Cos(j*(ropeTheta)) + modeSinCoeffs[j]*Mathf.Sin(j*(ropeTheta));
            }
            r *= scale;
            ropes[i].ropeStartPoint = (new Vector3(Mathf.Cos((ropeTheta+polygonTheta)), Mathf.Sin((ropeTheta+polygonTheta)),0) * r + transform.position)*transform.localScale.x;
            ropes[i].blobCenter = transform.position;
            ropeTheta += 2f*Mathf.PI/ropeCount;
        }
        if(spawnedDots<dotPositions.Count)
        {
            int a = dotPositions.Count;
            for (int i = 0; i < a - spawnedDots; i++)
            {
                dotPositions.RemoveAt(dotPositions.Count-1);
            }
        }
        List<Vector3> linePoints = new List<Vector3>();
        List<int> triangleList = new List<int>();
        for (int i = 0; i < dotPositions.Count; i++)
        {
            if(i<dotPositions.Count-1)
            {
                triangleList.Add(dotPositions.Count);
                triangleList.Add(i);
                triangleList.Add(i+1);
            }
            else
            {
                triangleList.Add(dotPositions.Count);
                triangleList.Add(i);
                triangleList.Add(0);
            }
            float newposx = dotPositions[i].x;
            float newposy = dotPositions[i].y;
            linePoints.Add(new Vector3(
                newposx,
                newposy,
                0
            ));
        }
        mesh.Clear();
        linePoints.Add(new Vector3(
            0,
            0,
            0
        ));
        mesh.vertices = linePoints.ToArray();
        mesh.triangles = triangleList.ToArray();
    }
}
