using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class DroneAgent : Agent
{
    
    [Tooltip("force to apply when moving")]
    public float moveForce = 2f;

    [Tooltip("Speed to rotate around the up axis")]
    public float yawSpeed = 100f;

    [Tooltip("the agent's camera")]
    public Camera agentCamera;

    [Tooltip("Whether this is training mode or gameplay mode")]
    public bool trainingMode;
    
    // the ice patch Darea that the agent is in
    private QuadArea quadArea;

    // the nearest ice patch to the agent
    private IcePatch nearestIcePatch;

    // The rigidbody of the agent
    new private Rigidbody rigidbody;

    // Allows for smoother yaw changes
    private float smoothYawChange = 0f;

    // Whether the agent is frozen (intentionally not flying)
    private bool frozen = false; 
    
    /// The amount of ice the agent has obtained this episode
    public float iceObtained { get; private set; }   

    // Initialize the agent
    public override void Initialize()
    {
        rigidbody = GetComponent<Rigidbody>();
        quadArea = GetComponentInParent<QuadArea>();

        // if not training mode, no max step, play forever
        if (!trainingMode) MaxStep = 0;
    }

    //Reset the agent when an episode begins
    public override void OnEpisodeBegin()
    {
        if (trainingMode)
        {
            // Only reset Ice patches in training when there is one agent per area
            quadArea.ResetIce();

        }

        // Reset ice obtained
        iceObtained = 0f;

        // Zero out velocities so that movement stop before a new episode begins
        rigidbody.velocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;

        // Defaults to spawning in front of an ice patch
        bool inFrontOfIcePatch = true;
        if (trainingMode)
        {
            // Spawn in front of ice patch 50% of the time during training
            inFrontOfIcePatch = UnityEngine.Random.value > 0.5f;
        }

        //Move the agent to a new random position
        MoveToSafeRandomPosition(inFrontOfIcePatch);

        //Recalculate the nearest Ice Patch now that the agent has moved
        UpdateNearestIcePatch();

    }

    /// <summary>
    /// Move the agent to a safe random position (i.e. does not collide with anything)
    /// </summary>
    private void MoveToSafeRandomPosition(bool inFrontOfIcePatch)
    {
        bool safePositionFound = false;
        int attemptsRemaining = 100; // Prevent an infinite loop
        Vector3 potentialPosition = Vector3.zero;
        Quaternion potentialRotation = new Quaternion();

        // Loop until a safe position is found or we run out of attempts
        while (!safePositionFound && attemptsRemaining > 0)
        {
            attemptsRemaining--;

            if (inFrontOfIcePatch)
            {
                // Pick a random ice Patch
                IcePatch randomIcePatch = quadArea.IcePatches[UnityEngine.Random.Range(0, quadArea.IcePatches.Count)];

                // Position 10 to 20 cm in front of the ice patch
                float distanceFromIcePatch = UnityEngine.Random.Range(.1f, .2f);

                potentialPosition = randomIcePatch.transform.position * distanceFromIcePatch;


            }
            else
            {
                // Pick a random height from the ground
                float height = UnityEngine.Random.Range(1.2f, 2.5f);

                // Pick a random radius from the center of the area
                float radius = UnityEngine.Random.Range(2f, 7f);

                // Pick a random direction rotated around the y axis
                Quaternion direction = Quaternion.Euler(0f, UnityEngine.Random.Range(-180f, 180f), 0f);

                // Combine height, radius, and direction to pick a potential position
                potentialPosition = quadArea.transform.position + Vector3.up * height + direction * Vector3.forward * radius;

                // Choose and set random starting yaw
                float yaw = UnityEngine.Random.Range(-180f, 180f);
                potentialRotation = Quaternion.Euler(270f, yaw, 0f);
            }
            

            // Check to see if the agent will collide with anything
            Collider[] colliders = Physics.OverlapSphere(potentialPosition, 0.05f);

            // Safe position has been found if no colliders are overlapped
            safePositionFound = colliders.Length == 0;
        }

        Debug.Assert(safePositionFound, "Could not find a safe position to spawn");

        // Set the position and rotation
        transform.position = potentialPosition;
        transform.rotation = potentialRotation;
    }


    /// <summary>
    /// Update the nearest icePatch to the agent
    /// </summary>
    private void UpdateNearestIcePatch()
    {
        foreach (IcePatch icePatch in quadArea.IcePatches)
        {
            if (nearestIcePatch == null && icePatch.HasIce)
            {
                // No current nearest ice patch and this ice patch has ice, so set to this icePatch
                nearestIcePatch = icePatch;
            }
            else if (icePatch.HasIce)
            {
                // Calculate distance to this flower and distance to the current nearest flower
                float distanceToIcePatch = Vector3.Distance(icePatch.transform.position, rigidbody.position);
                float distanceToCurrentNearestIcePatch = Vector3.Distance(nearestIcePatch.transform.position, rigidbody.position);

                // If current nearest flower is empty OR this flower is closer, update the nearest flower
                if (!nearestIcePatch.HasIce || distanceToIcePatch < distanceToCurrentNearestIcePatch)
                {
                    nearestIcePatch = icePatch;
                }
            }
        }
    }

    ///called when an action is received from player input/neural network
    
    /// vectorAction[i] represents:
    /// Index 0: move vector x (+1 = left, -1 = right)
    /// Index 1: move vector y (+1 = up, -1 = down)
    /// Index 2: move vector z (+1 = forward, -1 = backward)
    /// Index 3: yaw angle (+1 = turn right, -1 = turn left)
    public override void OnActionReceived(float[] vectorAction)
    {
        // Don't take actions if frozen
        if (frozen) return;

        // Calculate movement vector
        Vector3 move = new Vector3(vectorAction[0], vectorAction[1], vectorAction[2]);

        // Add force in the direction of the move vector
        rigidbody.AddForce(move * moveForce);


        // Get the current rotation
        Vector3 rotationVector = transform.rotation.eulerAngles;

        //Calculate yaw rotation
        float yawChange = vectorAction[3];

        // Calculate smooth rotation changes
        smoothYawChange = Mathf.MoveTowards(smoothYawChange, yawChange, 2f * Time.fixedDeltaTime);

        //calculate new yaw
        float yaw = rotationVector.y + smoothYawChange * Time.fixedDeltaTime * yawSpeed;

        // Apply the new rotation
        transform.rotation = Quaternion.Euler(270f, yaw, 0f);

    }

    // collect vector observations from environment
    public override void CollectObservations(VectorSensor sensor)
    {
        // If nearestIcePatch is null, observe an empty array and return early
        if (nearestIcePatch == null)
        {
            sensor.AddObservation(new float[9]);
            return;
        }

        // Observe the agent's local rotation (4 observations)
        sensor.AddObservation(transform.localRotation.normalized);

        // Get a vector from the drone rigidbody to the nearest icePatchCollider?
        Vector3 toIcePatch = nearestIcePatch.IcePatchPosition - rigidbody.position;
        // Observe a normalized vector pointing to the nearest ice patch (3 observations)
        sensor.AddObservation(toIcePatch.normalized);


        // Observe a dot product that indicates whether the drone transform is pointing above the ice patch (1 observation)
        sensor.AddObservation(Vector3.Dot(transform.forward.normalized, -nearestIcePatch.IcePatchUpVector.normalized));

        // Observe the relative distance from the drone to the ice patch (1 observation)
        sensor.AddObservation(toIcePatch.magnitude / QuadArea.AreaDiameter);

        // 9 total observations !!!!!!!!!!!!! LOOK AT THIS
    }

    public override void Heuristic(float[] actionsOut)
    {
        //Create placeholders for all movement/turning
        Vector3 left = Vector3.zero;
        Vector3 forward = Vector3.zero;
        Vector3 up = Vector3.zero;
        float yaw = 0f;

        //Convert keyboard inputs to movement and turning
        //All values should be between -1 and +1

        // Forward/backward
        if (Input.GetKey(KeyCode.W)) forward = -transform.up;
        if (Input.GetKey(KeyCode.S)) forward = transform.up;

        // Left/Right
        if (Input.GetKey(KeyCode.A)) left = -transform.right;
        if (Input.GetKey(KeyCode.D)) left = transform.right;

        // Up/Down
        if (Input.GetKey(KeyCode.E)) up = transform.forward;
        if (Input.GetKey(KeyCode.C)) up = -transform.forward;

        // Turn left/right yaw
        if (Input.GetKey(KeyCode.LeftArrow)) yaw = -1f;
        if (Input.GetKey(KeyCode.RightArrow)) yaw = 1f;

        // Combine the movement vectors and normalize
        Vector3 combined = (forward + left + up).normalized;

        // Add the 3 movement values and yaw to the actionsOut array
        actionsOut[0] = combined.x;
        actionsOut[1] = combined.y; 
        actionsOut[2] = combined.z;
        actionsOut[3] = yaw;
    }

    // in play mode, freezes agent
    public void FreezeAgent()
    {
        Debug.Assert(trainingMode == false, "Freeze/Unfreeze not supported in training");
        frozen = true;
        rigidbody.Sleep();
    }

    // Resume agent movement and actions
    public void UnfreezeAgent()
    {
        Debug.Assert(trainingMode == false, "Freeze/Unfreeze not supported in training");
        frozen = false;
        rigidbody.WakeUp();
    }

    //Called when agent's collider enterse a trigger collider
    private void OnTriggerEnter(Collider other)
    {
        TriggerEnterOrStay(other);
    }

    //Called when the agent's collider stays in a trigger collider
    private void OnTriggerStay(Collider other)
    {
        TriggerEnterOrStay(other);
    }

    //Handles when the agent's collider enters or stays in a trigger collider
    private void TriggerEnterOrStay(Collider collider)
    {
        // Check if agent is colliding with icePatchCollider
        if (collider.CompareTag("ice_patch"))
        {
            // Look up ice patch for this collider
            IcePatch icePatch = quadArea.GetIcePatchFromCollider(collider);

            // Attempt to take .5 ice
            // this is per fixed timestep, every 0.2 sec
            float iceReceived = icePatch.Salt(.5f);

            //Keep track of salt dropped, or ice "obtained"
            iceObtained += iceReceived;

            if (trainingMode)
            {
                //Calculate reward for salting ice
                float bonus = .02f;
                    AddReward(.01f + bonus);
            }

            // If icePatch is empty, update the nearest ice patch
            if (!icePatch.HasIce)
            {
                UpdateNearestIcePatch();
            }
        }
    }

    //Called when the agent collides with something solid
    private void OnCollisionEnter(Collision collision)
    {
        if (trainingMode && collision.collider.CompareTag("boundary"))
        {
            //Collided with the area boundary, give a negative reward
            AddReward(-.5f);
        }
    }

    //Called every frame
    private void Update()
    {
        //Draw a line from the drone to nearest ice patch
        if (nearestIcePatch != null)
        {
            Debug.DrawLine(rigidbody.position, nearestIcePatch.IcePatchPosition, Color.green);
        }
    }

    // Called every .02 seconds
    private void FixedUpdate()
    {
        // Avoids scenario where nearest ice patch is stolen by opponent and not updated. Multiplayer?!?!?!?!?!??! since when?
        if (nearestIcePatch != null && !nearestIcePatch.HasIce)
        {
            UpdateNearestIcePatch();
        }

    }

}
