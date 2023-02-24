using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class RopeControllerRealistic : MonoBehaviour 
{
    //Objects that will interact with the rope
    public Vector3 ropeStartPoint;
    public Vector3 blobCenter;
    public Transform whatIsHangingFromTheRope;
    
    //Line renderer used to display the rope
    LineRenderer lineRenderer;

    //A list with all rope section
    public List<RopeSection> allRopeSections = new List<RopeSection>();

    //Rope data
    public float ropeSectionLength = 0.1f;
    public int ropeSectionCount = 50;
    public float ropeWidth = 0.2f;
    //Data we can change to change the properties of the rope
    //Spring constant
    public float springConstant = 40f;
    //Damping from rope friction constant
    public float ropeFriction = 2f;
    //Damping from air resistance constant
    public float airResistance = 0.05f;
    //Mass of one rope section
    public float ropeSectionMass = 1f;

    public float grav = 9;

    public float bendForceConstant = 0.1f;

    public int loopCount;
    public int maximumStretchIterations = 2;

    public float grabAcceleration;
    public bool isGrabbing;
    public bool isGoingRound;
    public float goingRoundRadius = 2f;
    public float goingRoundForceCoeff = 50f;
    float goingRoundForce = 50f;
    public float goingRoundRadialForceCoeff = 50f;
    float goingRoundRadialForce = 50f;
    public float itemGrabDist;
    public Transform itemToGrab;

    float maxChangeDist = Mathf.Infinity;
    public float maxChangeDistSmall = 0.005f;
    bool upperTurn;
    Vector2 turnCenter;
    void Start() 
	{
        //Init the line renderer we use to display the rope
        lineRenderer = GetComponent<LineRenderer>();
        //
        //Create the rope
        //
        //Build the rope from the top
        Vector3 pos = ropeStartPoint;

        List<Vector2> ropePositions = new List<Vector2>();

        for (int i = 0; i < ropeSectionCount; i++)
        {
            ropePositions.Add(pos);
            pos.x += ropeSectionLength;
        }

        //But add the rope sections from bottom because it's easier to add
        //more sections to it if we have a winch
        for (int i = ropePositions.Count - 1; i >= 0; i--)
        {
            allRopeSections.Add(new RopeSection(ropePositions[i]));
        }
    }
	
	void Update() 
	{
        //Display the rope with the line renderer
        DisplayRope();

        //Compare the current length of the rope with the wanted length
        // DebugRopeLength();

        if(whatIsHangingFromTheRope!=null)
        {
            //Move what is hanging from the rope to the end of the rope
            whatIsHangingFromTheRope.position = allRopeSections[0].pos;

            //Make what's hanging from the rope look at the next to last rope position to make it rotate with the rope
            // whatIsHangingFromTheRope.LookAt(allRopeSections[1].pos);
            whatIsHangingFromTheRope.up = (allRopeSections[1].pos - allRopeSections[0].pos).normalized;
        }
        
    }

    public IEnumerator GrabItem(Transform item)
    {
        itemToGrab = item;
        isGrabbing = true;
        while(true)
        {
            if((allRopeSections[0].pos-(Vector2)item.position).sqrMagnitude < itemGrabDist*itemGrabDist)
            {
                item.GetComponent<Rigidbody2D>().mass = 0;
                whatIsHangingFromTheRope = item;
                isGrabbing = false;
                maxChangeDist = maxChangeDistSmall;
                yield return new WaitForSeconds(0.5f);
                maxChangeDist = Mathf.Infinity;
                break;
            }
            yield return new WaitForFixedUpdate();
        }
    }

    public IEnumerator GoRound()
    {
        isGoingRound = true;
        upperTurn = false;
        if(transform.parent.Find("UpperTurnCenter").position.y < transform.parent.Find("LowerTurnCenter").position.y)
        {
            turnCenter = (Vector2)transform.parent.Find("UpperTurnCenter").position;
            upperTurn = true;
        }
        else turnCenter = (Vector2)transform.parent.Find("LowerTurnCenter").position;

        Vector2 centerToMouth = (Vector2)transform.parent.Find("MouthPos").position - turnCenter;

        float startRadius = (turnCenter - allRopeSections[0].pos).magnitude;
        float endRadius = centerToMouth.magnitude;

        Vector2 vecFromCenter = allRopeSections[0].pos - turnCenter;
        Vector2 xVec = centerToMouth.normalized;
        Vector2 yVec = new Vector2(-xVec.y,xVec.x);
        if(upperTurn) yVec = new Vector2(xVec.y,-xVec.x);

        float startRadian = Mathf.Atan2(Vector2.Dot(yVec,vecFromCenter),Vector2.Dot(xVec,vecFromCenter));
        if(startRadian < 0)startRadian += 2f * Mathf.PI;
        float katamuki = (startRadius - endRadius)/startRadian;
        goingRoundRadius = startRadius;
        goingRoundForce = goingRoundRadius*goingRoundForceCoeff;
        goingRoundRadialForce = goingRoundRadius*goingRoundRadialForceCoeff;
        bool wentToZero = false;
        float lastRadian = startRadian;
        while(true)
        {
            yield return new WaitForFixedUpdate();

            centerToMouth = (Vector2)transform.parent.Find("MouthPos").position - turnCenter;

            vecFromCenter = allRopeSections[0].pos - turnCenter;
            xVec = centerToMouth.normalized;
            yVec = new Vector2(-xVec.y,xVec.x);
            if(upperTurn) yVec = new Vector2(xVec.y,-xVec.x);
            float radian = Mathf.Atan2(Vector2.Dot(yVec,vecFromCenter),Vector2.Dot(xVec,vecFromCenter));
            if(radian < 0)radian += 2f * Mathf.PI;

            if(radian > lastRadian) wentToZero = true;
            if(wentToZero && radian < startRadian) break;

            lastRadian = radian;
            goingRoundRadius = radian * katamuki + endRadius;
            goingRoundForce = goingRoundRadius*goingRoundForceCoeff;
            goingRoundRadialForce = goingRoundRadius*goingRoundRadialForceCoeff;
        }
        isGoingRound = false;
    }

    void FixedUpdate()
    {
        if (allRopeSections.Count > 0)
        {
            //Time step
            float timeStep = Time.fixedDeltaTime;
            for (int i = 0; i < loopCount; i++)
            {
                UpdateRopeSimulation(allRopeSections, timeStep);
            }
        }
    }

    //Display the rope with a line renderer
    private void DisplayRope()
    {
        lineRenderer.startWidth = ropeWidth;
        lineRenderer.endWidth = ropeWidth;

        //An array with all rope section positions
        Vector3[] positions = new Vector3[allRopeSections.Count];

        for (int i = 0; i < allRopeSections.Count; i++)
        {
            positions[i] = (Vector3)allRopeSections[i].pos;
        }

        lineRenderer.positionCount = positions.Length;

        lineRenderer.SetPositions(positions);
    }

    private void UpdateRopeSimulation(List<RopeSection> allRopeSections, float timeStep)
    {
        //Move the last position, which is the top position, to what the rope is attached to
        RopeSection lastRopeSection = allRopeSections[allRopeSections.Count - 1];

        lastRopeSection.pos = ropeStartPoint;

        allRopeSections[allRopeSections.Count - 1] = lastRopeSection;


        //
        //Calculate the next pos and vel with Forward Euler
        //
        //Calculate acceleration in each rope section which is what is needed to get the next pos and vel
        List<Vector2> accelerations = CalculateAccelerations(allRopeSections);

        List<RopeSection> nextPosVelForwardEuler = new List<RopeSection>();

        //Loop through all line segments (except the last because it's always connected to something)
        for (int i = 0; i < allRopeSections.Count - 1; i++)
        {
            RopeSection thisRopeSection = RopeSection.zero;

            //Forward Euler
            //vel = vel + acc * t
            thisRopeSection.vel = allRopeSections[i].vel + accelerations[i] * timeStep;

            //pos = pos + vel * t
            thisRopeSection.pos = allRopeSections[i].pos + allRopeSections[i].vel * timeStep;

            //Save the new data in a temporarily list
            nextPosVelForwardEuler.Add(thisRopeSection);
        }

        //Add the last which is always the same because it's attached to something
        nextPosVelForwardEuler.Add(allRopeSections[allRopeSections.Count - 1]);


        //
        //Calculate the next pos with Heun's method (Improved Euler)
        //
        //Calculate acceleration in each rope section which is what is needed to get the next pos and vel
        List<Vector2> accelerationFromEuler = CalculateAccelerations(nextPosVelForwardEuler);

        List<RopeSection> nextPosVelHeunsMethod = new List<RopeSection>();

        //Loop through all line segments (except the last because it's always connected to something)
        for (int i = 0; i < allRopeSections.Count - 1; i++)
        {
            RopeSection thisRopeSection = RopeSection.zero;

            //Heuns method
            //vel = vel + (acc + accFromForwardEuler) * 0.5 * t
            thisRopeSection.vel = allRopeSections[i].vel + (accelerations[i] + accelerationFromEuler[i]) * 0.5f * timeStep;

            //pos = pos + (vel + velFromForwardEuler) * 0.5f * t
            thisRopeSection.pos = allRopeSections[i].pos + (allRopeSections[i].vel + nextPosVelForwardEuler[i].vel) * 0.5f * timeStep;

            //Save the new data in a temporarily list
            nextPosVelHeunsMethod.Add(thisRopeSection);
        }

        //Add the last which is always the same because it's attached to something
        nextPosVelHeunsMethod.Add(allRopeSections[allRopeSections.Count - 1]);



        //From the temp list to the main list
        for (int i = 0; i < allRopeSections.Count; i++)
        {
            allRopeSections[i] = nextPosVelHeunsMethod[i];

            //allRopeSections[i] = nextPosVelForwardEuler[i];
        }


        //Implement maximum stretch to avoid numerical instabilities
        //May need to run the algorithm several times
        if(!isGrabbing && !isGoingRound)
        {
            for (int i = 0; i < maximumStretchIterations; i++)
            {
                ImplementMaximumStretch(allRopeSections);
            }
        }
    }

    //Calculate accelerations in each rope section which is what is needed to get the next pos and vel
    private List<Vector2> CalculateAccelerations(List<RopeSection> allRopeSections)
    {
        List<Vector2> accelerations = new List<Vector2>();

        //Spring constant
        float k = springConstant;
        //Damping constant
        float d = ropeFriction;
        //Mass of one rope section
        float m = ropeSectionMass;
        //How long should the rope section be
        float wantedLength = ropeSectionLength;


        //Calculate all forces once because some sections are using the same force but negative
        List<Vector2> allSpringForces = new List<Vector2>();
        List<Vector2> allBendingForces = new List<Vector2>();
        List<float> allBendingAngles = new List<float>();

        for (int i = 0; i < allRopeSections.Count - 1; i++)
        {
            //From Physics for game developers book
            //The force exerted on body 1
            //pos1 (above) - pos2
            Vector2 vectorBetween = allRopeSections[i + 1].pos - allRopeSections[i].pos;

            float distanceBetween = vectorBetween.magnitude;

            Vector2 dir = vectorBetween.normalized;

            float springForce = k * (distanceBetween - wantedLength);


            //Damping from rope friction 
            //vel1 (above) - vel2
            float frictionForce = d * ((Vector2.Dot(allRopeSections[i + 1].vel - allRopeSections[i].vel, vectorBetween)) / distanceBetween);


            //The total force on the spring
            Vector2 springForceVec = -(springForce + frictionForce) * dir;

            //This is body 2 if we follow the book because we are looping from below, so negative
            springForceVec = -springForceVec;

            allSpringForces.Add(springForceVec);

            Vector2 nextVectorBetween = (Vector2)blobCenter - allRopeSections[i+1].pos;
            if(i < allRopeSections.Count - 2)
            {
                nextVectorBetween = allRopeSections[i + 2].pos - allRopeSections[i+1].pos;
            }
            float bentAngle = Vector2.SignedAngle(nextVectorBetween,vectorBetween);
            float bendForceNorm = bentAngle * bendForceConstant;
            Vector2 bendForceDirection = new Vector2(-dir.y,dir.x);
            Vector2 bendForce = bendForceNorm * bendForceDirection;
            allBendingForces.Add(bendForce);
            allBendingAngles.Add(bentAngle);
        }


        //Loop through all line segments (except the last because it's always connected to something)
        //and calculate the acceleration
        for (int i = 0; i < allRopeSections.Count - 1; i++)
        {
            Vector2 springForce = Vector2.zero;
            Vector2 bendForce = Vector2.zero;

            //Spring 1 - above
            springForce += allSpringForces[i];
            bendForce += allBendingForces[i];

            //Spring 2 - below
            //The first spring is at the bottom so it doesnt have a section below it
            if (i != 0)
            {
                springForce -= allSpringForces[i - 1];
            }
            if (i>=2)
            {
                bendForce += (Vector2)(Quaternion.Euler(0f,0f, allBendingAngles[i-2]) * (Vector3)allBendingForces[i-2]);
            }
            
            //Damping from air resistance, which depends on the square of the velocity
            float vel = allRopeSections[i].vel.magnitude;

            Vector2 dampingForce = airResistance * vel * vel * allRopeSections[i].vel.normalized;

            //The mass attached to this spring
            float springMass = m;

            //end of the rope is attached to a box with a mass
            if (i == 0 && whatIsHangingFromTheRope != null)
            {
                springMass += whatIsHangingFromTheRope.GetComponent<Rigidbody2D>().mass;
            }

            //Force from gravity
            Vector2 gravityForce = springMass * new Vector2(0f, -grav);

            //The total force on this spring
            Vector2 totalForce = springForce + gravityForce - dampingForce + bendForce;

            //Calculate the acceleration a = F / m
            Vector2 acceleration = totalForce / springMass;

            if(i==0)
            {
                if(isGrabbing) acceleration += ((Vector2)itemToGrab.position - allRopeSections[0].pos).normalized * grabAcceleration;
                if(isGoingRound) 
                {
                    Vector2 vecFromCenter = (Vector2)blobCenter - allRopeSections[0].pos;
                    Vector2 tangentialVec = new Vector2(-vecFromCenter.y,vecFromCenter.x);
                    if(upperTurn) tangentialVec = new Vector2(vecFromCenter.y,-vecFromCenter.x);
                    acceleration += tangentialVec.normalized * goingRoundForce;
                    float radius = vecFromCenter.magnitude;
                    if(radius > goingRoundRadius) acceleration += (vecFromCenter/radius) *  goingRoundRadialForce;
                    if(radius < goingRoundRadius) acceleration -= (vecFromCenter/radius) *  goingRoundRadialForce;
                }
            }


            accelerations.Add(acceleration);
        }

        //The last line segment's acc is always 0 because it's attached to something
        accelerations.Add(Vector2.zero);


        return accelerations;
    }

    //Implement maximum stretch to avoid numerical instabilities
    private void ImplementMaximumStretch(List<RopeSection> allRopeSections)
    {
        //Make sure each spring are not less compressed than 90% nor more stretched than 110%
        float maxStretch = 1.1f;
        float minStretch = 0.9f;

        //Loop from the end because it's better to adjust the top section of the rope before the bottom
        //And the top of the rope is at the end of the list
        for (int i = allRopeSections.Count - 1; i > 0; i--)
        {
            RopeSection topSection = allRopeSections[i];

            RopeSection bottomSection = allRopeSections[i - 1];

            //The distance between the sections
            float dist = (topSection.pos - bottomSection.pos).magnitude;

            //What's the stretch/compression
            float stretch = dist / ropeSectionLength;

            if (stretch > maxStretch)
            {
                //How far do we need to compress the spring?
                float compressLength = dist - (ropeSectionLength * maxStretch);
                if(compressLength > maxChangeDist) compressLength = maxChangeDist;

                //In what direction should we compress the spring?
                Vector2 compressDir = (topSection.pos - bottomSection.pos).normalized;

                Vector2 change = compressDir * compressLength;

                MoveSection(change, i - 1);
            }
            else if (stretch < minStretch)
            {
                //How far do we need to stretch the spring?
                float stretchLength = (ropeSectionLength * minStretch) - dist;
                if(stretchLength > maxChangeDist) stretchLength = maxChangeDist;

                //In what direction should we compress the spring?
                Vector2 stretchDir = (bottomSection.pos - topSection.pos).normalized;

                Vector2 change = stretchDir * stretchLength;

                MoveSection(change, i - 1);
            }
        }
    }

    //Move a rope section based on stretch/compression
    private void MoveSection(Vector2 finalChange, int listPos)
    {
        RopeSection bottomSection = allRopeSections[listPos];

        //Move the bottom section
        Vector2 pos = bottomSection.pos;

        pos += finalChange;

        bottomSection.pos = pos;

        allRopeSections[listPos] = bottomSection;
    }

    //Compare the current length of the rope with the wanted length
    private void DebugRopeLength()
    {
        float currentLength = 0f;

        for (int i = 1; i < allRopeSections.Count; i++)
        {
            float thisLength = (allRopeSections[i].pos - allRopeSections[i - 1].pos).magnitude;

            currentLength += thisLength;
        }

        float wantedLength = ropeSectionLength * (float)(allRopeSections.Count - 1);
    }
}