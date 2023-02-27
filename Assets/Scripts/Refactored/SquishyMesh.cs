using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEditor;
using UnityEngine.U2D;

public class SquishyMesh : MonoBehaviour
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
    public Vector3 tailPosition;

    public SpriteShapeController spriteShapeController;
    Spline spline;
    void Start()
    {
        spline = spriteShapeController.spline;
        spline.Clear();
        // for (int i = 0; i < lineRes; i++)
        // {
        //     spline.InsertPointAt(i, new Vector3(Mathf.Cos((2f*Mathf.PI*i)/lineRes),Mathf.Sin((2f*Mathf.PI*i)/lineRes),0));
        // }
        // spriteShapeController.RefreshSpriteShape();
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
    }
    void FixedUpdate()
    {

        // for (int i = 0; i < allMembraneSegments.Count; i++)
        // {
        //     int j = allMembraneSegments.Count - i - 1;
        //     GameObject membraneSegment = allMembraneSegments[j];
        //     Vector3 position = membraneSegment.transform.position;
        //     Quaternion rotation = membraneSegment.transform.rotation;

        //     spline.InsertPointAt(i, position);
        //     spline.SetTangentMode(i, ShapeTangentMode.Continuous);
        //     spline.SetRightTangent(i, rotation * Vector3.down * tangentLength);
        //     spline.SetLeftTangent(i, rotation * Vector3.up * tangentLength);
        // }

        // spriteShapeController.RefreshSpriteShape();
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

        float tailTheta = 0f;
        r = 0f;
        for (int j = 0; j < modes; j++)
        {
            r += modeCoeffs[j]*Mathf.Cos(j*(tailTheta)) + modeSinCoeffs[j]*Mathf.Sin(j*(tailTheta));
        }
        r *= scale;
        tailPosition = (new Vector3(Mathf.Cos((tailTheta+polygonTheta)), Mathf.Sin((tailTheta+polygonTheta)),0) * r + transform.position)*transform.localScale.x;
        // tailTheta += 2f*Mathf.PI;

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
        for (int i = dotPositions.Count; i < spline.GetPointCount(); i++)
        {
            spline.RemovePointAt(i);
        }

        for (int i = 0; i < dotPositions.Count; i++)
        {
            if(spline.GetPointCount() < i+1) spline.InsertPointAt(i,dotPositions[i]);
            else spline.SetPosition(i,dotPositions[i]);

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
