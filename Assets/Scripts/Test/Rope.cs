using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rope : MonoBehaviour
{
    public Vector3 ropeStartPoint;
    public Vector3 blobCenter;
    private LineRenderer lineRenderer;
    private List<RopeSegment> ropeSegments = new List<RopeSegment>();
    public float ropeLength = 10f;
    public int segmentLength = 50;
    public float lineWidth = 0.1f;
    public float grav = 1.5f;
    public float linDrag = 0.1f;
    public float k = 1f;
    float ropeSegLen;
    // Use this for initialization
    void Start()
    {
        ropeSegLen = ropeLength/segmentLength;
        this.lineRenderer = this.GetComponent<LineRenderer>();
        Vector3 segmentStartPoint = ropeStartPoint;
        for (int i = 0; i < segmentLength; i++)
        {
            this.ropeSegments.Add(new RopeSegment(segmentStartPoint));
            segmentStartPoint += ropeSegLen * (ropeStartPoint - blobCenter).normalized;
        }
        this.DrawRope();
        this.Simulate();
    }

    // Update is called once per frame
    void Update()
    {
        this.DrawRope();
    }

    private void FixedUpdate()
    {
        this.Simulate();
        // float length = Vector3.Distance(this.ropeSegments[0].posNow,this.ropeSegments[segmentLength-1].posNow);
        // if(length > ropeLength && balloon != null)
        // {
        //     Vector2 force = ((this.ropeSegments[0].posNow - this.ropeSegments[segmentLength-1].posNow)/length)*k*(length - ropeLength);
        //     balloonBody.AddForceAtPosition(force,balloon.transform.Find("Tukene").position);
        // }
    }

    private void Simulate()
    {
        // SIMULATION
        Vector2 forceGravity = new Vector2(0f, -grav);

        for (int i = 1; i < this.segmentLength; i++)
        {
            RopeSegment firstSegment = this.ropeSegments[i];
            RopeSegment secondSegment = this.ropeSegments[i-1];

            Vector2 velocity = firstSegment.posNow - firstSegment.posOld;
            firstSegment.posOld = firstSegment.posNow;
            firstSegment.posNow += velocity;
            firstSegment.posNow += (forceGravity - velocity*linDrag) * Time.fixedDeltaTime;

            this.ropeSegments[i] = firstSegment;
            this.ropeSegments[i-1] = secondSegment;
        }

        // CONSTRAINTS
        for (int i = 0; i < 50; i++)
        {
            this.ApplyConstraint();
        }
    }

    private void ApplyConstraint()
    {
        RopeSegment firstSegment = this.ropeSegments[0];
        firstSegment.posNow = ropeStartPoint;
        this.ropeSegments[0] = firstSegment;

        for (int i = 0; i < this.segmentLength - 1; i++)
        {
            RopeSegment firstSeg = this.ropeSegments[i];
            RopeSegment secondSeg = this.ropeSegments[i + 1];

            float dist = (firstSeg.posNow - secondSeg.posNow).magnitude;
            float error = Mathf.Abs(dist - this.ropeSegLen);
            Vector2 changeDir = Vector2.zero;

            if (dist > ropeSegLen)
            {
                changeDir = (firstSeg.posNow - secondSeg.posNow).normalized;
            } else if (dist < ropeSegLen)
            {
                changeDir = (secondSeg.posNow - firstSeg.posNow).normalized;
            }

            Vector2 changeAmount = changeDir * error;
            if (i != 0)
            {
                firstSeg.posNow -= changeAmount * 0.5f;
                this.ropeSegments[i] = firstSeg;
                secondSeg.posNow += changeAmount * 0.5f;
                this.ropeSegments[i + 1] = secondSeg;
            }
            else
            {
                secondSeg.posNow += changeAmount;
                this.ropeSegments[i + 1] = secondSeg;
            }
        }
    }

    private void DrawRope()
    {
        float lineWidth = this.lineWidth;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;

        Vector3[] ropePositions = new Vector3[this.segmentLength];
        for (int i = 0; i < this.segmentLength; i++)
        {
            ropePositions[i] = this.ropeSegments[i].posNow;
        }

        lineRenderer.positionCount = ropePositions.Length;
        lineRenderer.SetPositions(ropePositions);
    }

    public struct RopeSegment
    {
        public Vector2 posNow;
        public Vector2 posOld;
        public Vector2 velocity;

        public RopeSegment(Vector2 pos)
        {
            this.posNow = pos;
            this.posOld = pos;
            this.velocity = Vector2.zero;
        }
    }
}