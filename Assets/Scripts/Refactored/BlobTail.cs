using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum TailMode
{
    Swimming,
    Grabbing,
    Tossing,
}
public class BlobTail : MonoBehaviour 
{
    //Objects that will interact with the tail
    SquishyMesh squishyMesh;
    BlobCharacterController characterController;
    public Transform whatIsHangingFromTheTail;
    Transform itemToGrab;
    
    //Line renderer used to display the tail
    LineRenderer lineRenderer;

    //A list with all tail section
    public List<TailSection> allTailSections = new List<TailSection>();

    //Tail data
    public float tailSectionLength = 0.1f;
    public int tailSectionCount = 50;
    public float tailWidth = 0.2f;
    //Data we can change to change the properties of the tail
    //Spring constant
    public float springConstant = 40f;
    //Damping from tail friction constant
    public float tailFriction = 2f;
    //Damping from air resistance constant
    public float airResistance = 0.05f;
    //Mass of one tail section
    public float tailSectionMass = 1f;

    public float bendForceConstant = 0.1f;

    public int loopCount;
    public int maximumStretchIterations = 2;

    public TailMode tailMode;

    public float maxChangeDistSmall;
    float maxChangeDist = Mathf.Infinity;

    public float grabAcceleration;

    bool tossFromUpperTurnCenter;
    float goingRoundRadius = 2f;
    public float goingRoundForceCoeff = 50f;
    float goingRoundForce = 50f;
    public float goingRoundRadialForceCoeff = 50f;
    float goingRoundRadialForce = 50f;
    public float itemThrowForce = 10f;
    public float itemThrowAngularSpeed = 720f;
    public float tossMaxTime;
    Vector2 turnCenter;
    void Start() 
	{
        tailMode = TailMode.Swimming;
        characterController = GetComponent<BlobCharacterController>();
        squishyMesh = GetComponent<SquishyMesh>();
        //Init the line renderer we use to display the tail
        lineRenderer = GetComponent<LineRenderer>();
        //
        //Create the tail
        //
        //Build the tail from the top
        Vector3 pos = squishyMesh.tailPosition;

        List<Vector2> tailPositions = new List<Vector2>();

        for (int i = 0; i < tailSectionCount; i++)
        {
            tailPositions.Add(pos);
            pos.x += tailSectionLength;
        }

        //But add the tail sections from bottom because it's easier to add
        //more sections to it if we have a winch
        for (int i = tailPositions.Count - 1; i >= 0; i--)
        {
            allTailSections.Add(new TailSection(tailPositions[i]));
        }
    }
	
	void Update() 
	{
        //Display the tail with the line renderer
        DisplayTail();

        //Compare the current length of the tail with the wanted length
        // DebugTailLength();

        if(whatIsHangingFromTheTail!=null)
        {
            //Move what is hanging from the tail to the end of the tail
            whatIsHangingFromTheTail.position = allTailSections[0].pos;

            //Make what's hanging from the tail look at the next to last tail position to make it rotate with the tail
            // whatIsHangingFromTheTail.LookAt(allTailSections[1].pos);
            whatIsHangingFromTheTail.up = (allTailSections[1].pos - allTailSections[0].pos).normalized;
        }
        
    }

    void FixedUpdate()
    {
        if (allTailSections.Count > 0)
        {
            //Time step
            float timeStep = Time.fixedDeltaTime;
            for (int i = 0; i < loopCount; i++)
            {
                UpdateTailSimulation(allTailSections, timeStep);
            }
        }
    }

    //Display the tail with a line renderer
    private void DisplayTail()
    {
        lineRenderer.startWidth = tailWidth;
        lineRenderer.endWidth = tailWidth;

        //An array with all tail section positions
        Vector3[] positions = new Vector3[allTailSections.Count + 1];

        for (int i = 0; i < allTailSections.Count; i++)
        {
            positions[i] = (Vector3)allTailSections[i].pos;
        }
        positions[allTailSections.Count] = transform.position;
        lineRenderer.positionCount = positions.Length;

        lineRenderer.SetPositions(positions);
    }

    public IEnumerator GrabObj()
    {
        bool applicable = true;
        if(tailMode != TailMode.Swimming) applicable = false;
        if(whatIsHangingFromTheTail != null) applicable = false;

        Collider2D collider = Physics2D.OverlapCircle((Vector2)transform.position,characterController.objFindRange, LayerMask.GetMask("Food"));
        if(collider!=null && applicable)
        {
            tailMode = TailMode.Grabbing;
            itemToGrab = collider.transform;
            while(true)
            {
                if((allTailSections[0].pos-(Vector2)itemToGrab.position).sqrMagnitude < characterController.objGrabRange*characterController.objGrabRange)
                {
                    itemToGrab.GetComponent<Rigidbody2D>().mass = 0;
                    whatIsHangingFromTheTail = itemToGrab;
                    tailMode = TailMode.Swimming;
                    maxChangeDist = maxChangeDistSmall;
                    yield return new WaitForSeconds(0.5f);
                    maxChangeDist = Mathf.Infinity;
                    break;
                }
                
                yield return new WaitForFixedUpdate();
            }
        }
    }

    public IEnumerator TossObj()
    {
        bool applicable = true;
        if(tailMode != TailMode.Swimming) applicable = false;
        if(whatIsHangingFromTheTail == null) applicable = false;

        if(applicable)
        {
            tailMode = TailMode.Tossing;
            tossFromUpperTurnCenter = false;
            
            if(transform.Find("UpperTurnCenter").position.y < transform.Find("LowerTurnCenter").position.y)
            {
                turnCenter = (Vector2)transform.Find("UpperTurnCenter").position;
                tossFromUpperTurnCenter = true;
            }
            else turnCenter = (Vector2)transform.Find("LowerTurnCenter").position;

            Vector2 centerToMouth = (Vector2)transform.Find("MouthPos").position - turnCenter;

            float startRadius = (turnCenter - allTailSections[0].pos).magnitude;
            float endRadius = centerToMouth.magnitude;

            Vector2 vecFromCenter = allTailSections[0].pos - turnCenter;
            Vector2 xVec = centerToMouth.normalized;
            Vector2 yVec = new Vector2(-xVec.y,xVec.x);
            if(tossFromUpperTurnCenter) yVec = new Vector2(xVec.y,-xVec.x);

            float startRadian = Mathf.Atan2(Vector2.Dot(yVec,vecFromCenter),Vector2.Dot(xVec,vecFromCenter));
            if(startRadian < 0)startRadian += 2f * Mathf.PI;
            float katamuki = (startRadius - endRadius)/startRadian;
            goingRoundRadius = startRadius;
            goingRoundForce = goingRoundRadius*goingRoundForceCoeff;
            goingRoundRadialForce = goingRoundRadius*goingRoundRadialForceCoeff;
            bool wentToZero = false;
            float lastRadian = startRadian;
            float time = 0f;
            while(true)
            {
                yield return new WaitForFixedUpdate();

                centerToMouth = (Vector2)transform.Find("MouthPos").position - turnCenter;

                vecFromCenter = allTailSections[0].pos - turnCenter;
                xVec = centerToMouth.normalized;
                yVec = new Vector2(-xVec.y,xVec.x);
                if(tossFromUpperTurnCenter) yVec = new Vector2(xVec.y,-xVec.x);
                float radian = Mathf.Atan2(Vector2.Dot(yVec,vecFromCenter),Vector2.Dot(xVec,vecFromCenter));
                if(radian < 0)radian += 2f * Mathf.PI;

                if(radian > lastRadian){
                    wentToZero = true;
                    if(whatIsHangingFromTheTail != null)
                    {
                        Transform item = whatIsHangingFromTheTail;
                        whatIsHangingFromTheTail = null;
                        Rigidbody2D body = item.GetComponent<Rigidbody2D>();
                        body.velocity = characterController.velocity;
                        body.mass = 1f;
                        body.AddForce(new Vector2(0,itemThrowForce));
                        float impulse = (itemThrowAngularSpeed * Mathf.Deg2Rad) * body.inertia;
                        body.AddTorque(impulse, ForceMode2D.Impulse);
                    }
                }
                time += Time.deltaTime;
                if((wentToZero && radian < startRadian) || time > tossMaxTime) break;

                lastRadian = radian;
                goingRoundRadius = radian * katamuki + endRadius;
                if(goingRoundRadius > 2f) goingRoundRadius = 2f;
                goingRoundForce = goingRoundRadius*goingRoundForceCoeff;
                goingRoundRadialForce = goingRoundRadius*goingRoundRadialForceCoeff;
            }

            tailMode = TailMode.Swimming;
        }
    }

    private void UpdateTailSimulation(List<TailSection> allTailSections, float timeStep)
    {
        //Move the last position, which is the top position, to what the tail is attached to
        TailSection lastTailSection = allTailSections[allTailSections.Count - 1];

        lastTailSection.pos = squishyMesh.tailPosition;

        allTailSections[allTailSections.Count - 1] = lastTailSection;


        //
        //Calculate the next pos and vel with Forward Euler
        //
        //Calculate acceleration in each tail section which is what is needed to get the next pos and vel
        List<Vector2> accelerations = CalculateAccelerations(allTailSections);

        List<TailSection> nextPosVelForwardEuler = new List<TailSection>();

        //Loop through all line segments (except the last because it's always connected to something)
        for (int i = 0; i < allTailSections.Count - 1; i++)
        {
            TailSection thisTailSection = TailSection.zero;

            //Forward Euler
            //vel = vel + acc * t
            thisTailSection.vel = allTailSections[i].vel + accelerations[i] * timeStep;

            //pos = pos + vel * t
            thisTailSection.pos = allTailSections[i].pos + allTailSections[i].vel * timeStep;

            //Save the new data in a temporarily list
            nextPosVelForwardEuler.Add(thisTailSection);
        }

        //Add the last which is always the same because it's attached to something
        nextPosVelForwardEuler.Add(allTailSections[allTailSections.Count - 1]);


        //
        //Calculate the next pos with Heun's method (Improved Euler)
        //
        //Calculate acceleration in each tail section which is what is needed to get the next pos and vel
        List<Vector2> accelerationFromEuler = CalculateAccelerations(nextPosVelForwardEuler);

        List<TailSection> nextPosVelHeunsMethod = new List<TailSection>();

        //Loop through all line segments (except the last because it's always connected to something)
        for (int i = 0; i < allTailSections.Count - 1; i++)
        {
            TailSection thisTailSection = TailSection.zero;

            //Heuns method
            //vel = vel + (acc + accFromForwardEuler) * 0.5 * t
            thisTailSection.vel = allTailSections[i].vel + (accelerations[i] + accelerationFromEuler[i]) * 0.5f * timeStep;

            //pos = pos + (vel + velFromForwardEuler) * 0.5f * t
            thisTailSection.pos = allTailSections[i].pos + (allTailSections[i].vel + nextPosVelForwardEuler[i].vel) * 0.5f * timeStep;
            if(tailMode == TailMode.Tossing) thisTailSection.pos += characterController.velocity * timeStep/loopCount;
            //Save the new data in a temporarily list
            nextPosVelHeunsMethod.Add(thisTailSection);
        }

        //Add the last which is always the same because it's attached to something
        nextPosVelHeunsMethod.Add(allTailSections[allTailSections.Count - 1]);



        //From the temp list to the main list
        for (int i = 0; i < allTailSections.Count; i++)
        {
            allTailSections[i] = nextPosVelHeunsMethod[i];

            //allTailSections[i] = nextPosVelForwardEuler[i];
        }


        //Implement maximum stretch to avoid numerical instabilities
        //May need to run the algorithm several times
        if(tailMode == TailMode.Swimming)
        {
            for (int i = 0; i < maximumStretchIterations; i++)
            {
                ImplementMaximumStretch(allTailSections);
            }
        }
    }

    //Calculate accelerations in each tail section which is what is needed to get the next pos and vel
    private List<Vector2> CalculateAccelerations(List<TailSection> allTailSections)
    {
        List<Vector2> accelerations = new List<Vector2>();

        //Spring constant
        float k = springConstant;
        //Damping constant
        float d = tailFriction;
        //Mass of one tail section
        float m = tailSectionMass;
        //How long should the tail section be
        float wantedLength = tailSectionLength;


        //Calculate all forces once because some sections are using the same force but negative
        List<Vector2> allSpringForces = new List<Vector2>();
        List<Vector2> allBendingForces = new List<Vector2>();
        List<float> allBendingAngles = new List<float>();

        for (int i = 0; i < allTailSections.Count - 1; i++)
        {
            //From Physics for game developers book
            //The force exerted on body 1
            //pos1 (above) - pos2
            Vector2 vectorBetween = allTailSections[i + 1].pos - allTailSections[i].pos;

            float distanceBetween = vectorBetween.magnitude;

            Vector2 dir = vectorBetween.normalized;

            float springForce = k * (distanceBetween - wantedLength);


            //Damping from tail friction 
            //vel1 (above) - vel2
            float frictionForce = d * ((Vector2.Dot(allTailSections[i + 1].vel - allTailSections[i].vel, vectorBetween)) / distanceBetween);


            //The total force on the spring
            Vector2 springForceVec = -(springForce + frictionForce) * dir;

            //This is body 2 if we follow the book because we are looping from below, so negative
            springForceVec = -springForceVec;

            allSpringForces.Add(springForceVec);

            Vector2 nextVectorBetween = (Vector2)transform.position - allTailSections[i+1].pos;
            if(i < allTailSections.Count - 2)
            {
                nextVectorBetween = allTailSections[i + 2].pos - allTailSections[i+1].pos;
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
        for (int i = 0; i < allTailSections.Count - 1; i++)
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
            float vel = allTailSections[i].vel.magnitude;

            Vector2 dampingForce = airResistance * vel * vel * allTailSections[i].vel.normalized;

            //The mass attached to this spring
            float springMass = m;

            //end of the tail is attached to a box with a mass
            if (i == 0 && whatIsHangingFromTheTail != null)
            {
                springMass += whatIsHangingFromTheTail.GetComponent<Rigidbody2D>().mass;
            }


            //The total force on this spring
            Vector2 totalForce = springForce - dampingForce + bendForce;

            //Calculate the acceleration a = F / m
            Vector2 acceleration = totalForce / springMass;
            if(i == 0)
            {
                if(tailMode == TailMode.Grabbing)
                {
                    acceleration += ((Vector2)itemToGrab.position - allTailSections[0].pos).normalized * grabAcceleration;
                }
                if(tailMode == TailMode.Tossing) 
                {
                    // Vector2 vecFromCenter = (Vector2)transform.position - allTailSections[0].pos;
                    Vector2 vecFromCenter = turnCenter - allTailSections[0].pos;
                    Vector2 tangentialVec = new Vector2(-vecFromCenter.y,vecFromCenter.x);
                    if(tossFromUpperTurnCenter) tangentialVec = new Vector2(vecFromCenter.y,-vecFromCenter.x);
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
    private void ImplementMaximumStretch(List<TailSection> allTailSections)
    {
        //Make sure each spring are not less compressed than 90% nor more stretched than 110%
        float maxStretch = 1.1f;
        float minStretch = 0.9f;

        //Loop from the end because it's better to adjust the top section of the tail before the bottom
        //And the top of the tail is at the end of the list
        for (int i = allTailSections.Count - 1; i > 0; i--)
        {
            TailSection topSection = allTailSections[i];

            TailSection bottomSection = allTailSections[i - 1];

            //The distance between the sections
            float dist = (topSection.pos - bottomSection.pos).magnitude;

            //What's the stretch/compression
            float stretch = dist / tailSectionLength;

            if (stretch > maxStretch)
            {
                //How far do we need to compress the spring?
                float compressLength = dist - (tailSectionLength * maxStretch);
                if(compressLength > maxChangeDist) compressLength = maxChangeDist;

                //In what direction should we compress the spring?
                Vector2 compressDir = (topSection.pos - bottomSection.pos).normalized;

                Vector2 change = compressDir * compressLength;

                MoveSection(change, i - 1);
            }
            else if (stretch < minStretch)
            {
                //How far do we need to stretch the spring?
                float stretchLength = (tailSectionLength * minStretch) - dist;
                if(stretchLength > maxChangeDist) stretchLength = maxChangeDist;

                //In what direction should we compress the spring?
                Vector2 stretchDir = (bottomSection.pos - topSection.pos).normalized;

                Vector2 change = stretchDir * stretchLength;

                MoveSection(change, i - 1);
            }
        }
    }

    //Move a tail section based on stretch/compression
    private void MoveSection(Vector2 finalChange, int listPos)
    {
        TailSection bottomSection = allTailSections[listPos];

        //Move the bottom section
        Vector2 pos = bottomSection.pos;

        pos += finalChange;

        bottomSection.pos = pos;

        allTailSections[listPos] = bottomSection;
    }

    //Compare the current length of the tail with the wanted length
    private void DebugTailLength()
    {
        float currentLength = 0f;

        for (int i = 1; i < allTailSections.Count; i++)
        {
            float thisLength = (allTailSections[i].pos - allTailSections[i - 1].pos).magnitude;

            currentLength += thisLength;
        }

        float wantedLength = tailSectionLength * (float)(allTailSections.Count - 1);
    }
}