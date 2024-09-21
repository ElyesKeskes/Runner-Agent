using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System;
using Unity.VisualScripting;
using Unity.MLAgents.Policies;


public class RunnerAgent : Agent
{
    public float goalLocalZPosition;
    public List<float> obstacleLocalZPositions;
    public List<Transform> obstacleTransforms;
    public List<Transform> rewardTransforms;
    public float distanceToGoal;
    public float distanceToNextObstacle;//Must be observed
    public float distanceToNextReward;//Must be observed
    public float nextRewardXPosition; //Must be observed
    public GameObject nextReward;
    public GameObject nextObstacle;
    [SerializeField] public Transform agentStartLocalPosition;
    Rigidbody rb;
    [SerializeField] public float jumpForce;
    [SerializeField] public float strafeSpeed;
    [SerializeField] public float forwardSpeed;
    [SerializeField] public float pathLength; //Length of the path
    //Pivot point of public the parent gameobject is the local origin, which is located at the center of the path, so at maxZDistance/2 on z-axis and maxXDistance/2 on the x-axis
    [SerializeField] public float pathWidth; //Width of the path
    [SerializeField] public float minDistanceBetweenObstacles;
    [SerializeField] public float minDistanceBetweenRewards;
    [SerializeField] public float minDistanceBetweenRewardsAndObstacles;
    [SerializeField] public Material groundMaterial;
    public Material groundMaterialCopy;
    public bool isGrounded = true; //Must be observed
    public Action episodeFailed;
    public bool noObstacles = false;
    public bool noRewards = false;
    public MeshRenderer groundMeshRenderer;

    void Start()
    {
        episodeFailed += OnFail;
    }
    void Update()
    {
        //Always move forward
        transform.Translate(0, 0, forwardSpeed * Time.deltaTime);
    }

    void OnFail()
    {
        //Set base color of the ground material to red
        groundMaterialCopy.color = Color.red;
        //Add reward 1 x ratio of distance to goal
        // if(distanceToGoal > 0.3f * pathLength)
        // {
        //     AddReward(-0.1f * (distanceToGoal / pathLength));
        // }
        // else
        // {
        //     AddReward(-1);
        // }
    }

    void ResetRewards()
    {
        foreach (var reward in rewardTransforms)
        {
            reward.gameObject.SetActive(true);
        }
    }

    public override void Initialize()
    {
        //Create a copy of the mat and set it
        groundMaterialCopy = new Material(groundMaterial);
        groundMeshRenderer.material = groundMaterialCopy;
        rb = GetComponent<Rigidbody>();
        goalLocalZPosition = GameObject.FindGameObjectWithTag("Goal").transform.localPosition.z;
        obstacleLocalZPositions = new List<float>();
        obstacleTransforms = new List<Transform>();
        //Calculate maxZDistance
        pathLength = goalLocalZPosition - agentStartLocalPosition.localPosition.z;

        var obstacles = GameObject.FindGameObjectsWithTag("Obstacle");
        foreach (var obstacle in obstacles)
        {
            obstacleLocalZPositions.Add(obstacle.transform.localPosition.z);
            obstacleTransforms.Add(obstacle.transform);
        }

        rewardTransforms = new List<Transform>();
        var rewards = GameObject.FindGameObjectsWithTag("Reward");
        foreach (var reward in rewards)
        {
            rewardTransforms.Add(reward.transform);
        }
        if (obstacleTransforms.Count == 0)
        {
            noObstacles = true;
        }
        else
        {
            noObstacles = false;
        }
        if (rewardTransforms.Count == 0)
        {
            noRewards = true;
        }
        else
        {
            noRewards = false;
        }
        GetDistanceToNextObstacle();
        GetDistanceToNextReward();
        GetDistanceToGoal();
    }

    public void GeneratePath()
    {
        RandomizeObstacleZPositions();
        RandomizeRewardXandZPositions();
    }

    void GetDistanceToNextObstacle() //Must be in front of the agent on the Z axis
    {
        bool found = false;
        if (noObstacles)
        {
            distanceToNextObstacle = 1000f;
            return;
        }
        float minDistance = Mathf.Infinity;
        for (int i = 0; i < obstacleTransforms.Count; i++)
        {
            if (transform.position.z < obstacleTransforms[i].position.z) //If the obstacle is in front of the agent
            {
                found = true;
                float distance = Vector3.Distance(new Vector3(0, 0, transform.localPosition.z), new Vector3(0, 0, obstacleTransforms[i].localPosition.z));
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nextObstacle = obstacleTransforms[i].gameObject;
                }
            }
        }
        if (!found)
        {
            distanceToNextObstacle = 1000f;
            nextObstacle = null;
            return;
        }
        distanceToNextObstacle = minDistance;
    }

    void GetDistanceToNextReward()
    {   bool found = false;
        if (noRewards)
        {
            distanceToNextReward = distanceToGoal;
            nextRewardXPosition = 0f;
            return;
        }
        float minDistance = Mathf.Infinity;
        for (int i = 0; i < rewardTransforms.Count; i++)
        {
            if (transform.position.z < rewardTransforms[i].position.z) //If the reward is in front of the agent
            {   found = true;
                float distance = Vector3.Distance(new Vector3(transform.localPosition.x, 0, transform.localPosition.z), new Vector3(rewardTransforms[i].localPosition.x, 0, rewardTransforms[i].localPosition.z));
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nextReward = rewardTransforms[i].gameObject;
                }
            }
        }
        if (!found)
        {
            distanceToNextReward = distanceToGoal;
            nextRewardXPosition = 0f;
            nextReward = null;
            return;
        }
        distanceToNextReward = minDistance;
        nextRewardXPosition = nextReward.transform.localPosition.x;
    }

    void GetDistanceToGoal()
    {
        distanceToGoal = Vector3.Distance(new Vector3(0, 0, transform.localPosition.z), new Vector3(0, 0, goalLocalZPosition));
    }

    void RandomizeObstacleZPositions()
    {
        if (noObstacles)
        {
            return;
        }
        // Determine the Z position range for the obstacles
        float minZPosition = agentStartLocalPosition.localPosition.z + pathLength * 0.2f; // 20% of the path length
        float maxZPosition = goalLocalZPosition - minDistanceBetweenObstacles * (obstacleTransforms.Count - 1);

        List<float> obstaclePositions = new List<float>();

        for (int i = 0; i < obstacleTransforms.Count; i++)
        {
            bool positionValid = false;
            float newZ = 0f;

            while (!positionValid)
            {
                // Randomize the Z position within the specified range
                newZ = UnityEngine.Random.Range(minZPosition, maxZPosition);

                // Check if the new position is valid
                positionValid = true;

                // Check minimum distance to other obstacles
                for (int j = 0; j < obstaclePositions.Count; j++)
                {
                    if (Mathf.Abs(newZ - obstaclePositions[j]) < minDistanceBetweenObstacles)
                    {
                        positionValid = false;
                        break; // Break and try again
                    }
                }
            }

            // Add the valid position to the list
            obstaclePositions.Add(newZ);

            // Set the valid position for the obstacle
            obstacleTransforms[i].localPosition = new Vector3(obstacleTransforms[i].localPosition.x, obstacleTransforms[i].localPosition.y, newZ);
        }
    }



    void RandomizeRewardXandZPositions()
    {
        if (noRewards)
        {
            return;
        }
        float minZPosition = agentStartLocalPosition.localPosition.z + pathLength * 0.1f; // 10% of the path length
        float maxZPosition = goalLocalZPosition;
        float halfPathWidth = pathWidth / 2f; // X positions will be in the range [-halfPathWidth, halfPathWidth]

        for (int i = 0; i < rewardTransforms.Count; i++)
        {
            bool isValidPosition = false;

            while (!isValidPosition)
            {
                // Randomize X and Z positions for the reward
                float randomX = UnityEngine.Random.Range(-halfPathWidth, halfPathWidth);
                float randomZ = UnityEngine.Random.Range(minZPosition, maxZPosition);

                // Check distance constraints
                isValidPosition = true;

                // Check distance to all other rewards
                for (int j = 0; j < i; j++)
                {
                    float rewardDistance = Vector3.Distance(new Vector3(randomX, 0, randomZ), rewardTransforms[j].localPosition);
                    if (rewardDistance < minDistanceBetweenRewards)
                    {
                        isValidPosition = false;
                        break;
                    }
                }

                // Check distance to all obstacles
                if (isValidPosition)
                {
                    for (int k = 0; k < obstacleTransforms.Count; k++)
                    {
                        float obstacleDistance = Vector3.Distance(new Vector3(randomX, 0, randomZ), obstacleTransforms[k].localPosition);
                        if (obstacleDistance < minDistanceBetweenRewardsAndObstacles)
                        {
                            isValidPosition = false;
                            break;
                        }
                    }
                }

                // If valid position is found, assign it to the reward
                if (isValidPosition)
                {
                    rewardTransforms[i].localPosition = new Vector3(randomX, rewardTransforms[i].localPosition.y, randomZ);
                }
            }
        }
    }



    public override void CollectObservations(VectorSensor sensor)
    {  
        GetDistanceToNextObstacle();
        GetDistanceToNextReward();
        GetDistanceToGoal();
        IsGrounded();

        sensor.AddObservation(distanceToNextObstacle);
        sensor.AddObservation(distanceToNextReward);
        sensor.AddObservation(nextRewardXPosition/pathWidth);
        sensor.AddObservation(transform.localPosition.x/pathWidth);
        sensor.AddObservation(distanceToGoal/pathLength);
        sensor.AddObservation(isGrounded);
    }

    void CheckIfMissedNextReward()
    {
        if (nextReward != null)
        {
            if (transform.position.z > nextReward.transform.position.z)
            {
                AddReward(-0.1f);
            }
        }
    }

    void CheckIfAvoidedNextObstacle()
    {
        if (nextObstacle != null)
        {
            if (transform.position.z > nextObstacle.transform.position.z)
            {
                AddReward(0.1f);
            }
        }
    }

    void Jump()
    {
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        // //Debug log in pink color AGENT JUMPED
        // Debug.Log("<color=pink>AGENT JUMPED</color>");
    }
    //Agent Actions: Since it's always running forward without any action, it only moves on the x-axis and jumps (Add force on the y-axis)
    public override void OnActionReceived(ActionBuffers actions)
    {
        //One continuous action for x-axis movement
        //Two descrete branches: One for movement on x axis and one for jumping: First branch size is 3 (don't move sideways, move left, move right) and second branch size is 2 (jump, don't jump)
        //var moveX = 2f * Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
        var moveX = actions.ContinuousActions[0];

        
        int jump = actions.DiscreteActions[0];
        // //Movement
       transform.Translate(moveX * strafeSpeed * Time.deltaTime, 0, 0);

        //Jump
        if (jump == 1 && isGrounded)
        {
            Jump();
        }
        if (jump == 1 && !isGrounded)
        {
            //Shouldn't be able to jump in the air
            AddReward(-0.1f);
        }
    }

    void IsGrounded()
    {
        isGrounded = Physics.Raycast(transform.position, Vector3.down, 0.6f);
    }

    public override void OnEpisodeBegin()
    {
        GeneratePath();
        ResetRewards();
        GetDistanceToNextObstacle();
        GetDistanceToNextReward();
        GetDistanceToGoal();
        Debug.Log("<color=yellow>Episode Begin</color>");
        transform.localPosition = agentStartLocalPosition.localPosition;

    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Horizontal");
        var discreteActionsOut = actionsOut.DiscreteActions;
        discreteActionsOut[0] = (Input.GetKey(KeyCode.Space) && isGrounded) ? 1 : 0;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Goal")) //Goal at the end of the path
        {
            AddReward(3f);
            //Set base color of the ground material to green
            groundMaterialCopy.color = Color.green;
            EndEpisode();
        }
        if (other.gameObject.CompareTag("Wall")) //Walls on the sides of the path
        {
            AddReward(-1);
             episodeFailed.Invoke();
             EndEpisode();

        }
        //Obstacle and Reward (coin)
        if (other.gameObject.CompareTag("Obstacle"))
        {
            AddReward(-1.5f);
            episodeFailed.Invoke();
            EndEpisode();
        }
        if (other.gameObject.CompareTag("Reward"))
        {
            AddReward(1.5f);
            other.gameObject.SetActive(false);
        }

    }

    // void OnTriggerStay(Collider other)
    // {
    //     if (other.gameObject.CompareTag("Wall")) //Walls on the sides of the path
    //     {
    //         AddReward(-0.001f);
            
    //     }
    // }

}
