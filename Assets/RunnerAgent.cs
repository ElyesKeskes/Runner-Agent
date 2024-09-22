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
    public Rigidbody rb;
    [SerializeField] public float jumpForce;
    [SerializeField] public float strafeSpeed;
    [SerializeField] public float forwardSpeed;
    public float distanceToGoal;
    public float distanceToNextObstacle;//Must be observed
    public float distanceToNextReward;//Must be observed
    public float nextRewardXPosition; //Must be observed
    public GameObject nextReward;
    public GameObject nextObstacle;
    public GameObject secondNextObstacle;
    public bool isGrounded = true; //Must be observed
    public TrainingEnvironmentManager trainingEnvironmentManager;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        trainingEnvironmentManager = transform.parent.GetComponent<TrainingEnvironmentManager>();
        GetDistanceToNextObstacle();
        GetDistanceToNextReward();
        GetDistanceToGoal();
        CalculateJumpDistance();
    }


    void Update()
    {
        //Always move forward
        transform.Translate(0, 0, forwardSpeed * Time.deltaTime);
        IsGrounded();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        GetDistanceToNextObstacle();
        GetDistanceToNextReward();
        GetDistanceToGoal();
        

        sensor.AddObservation(distanceToNextObstacle);
        sensor.AddObservation(distanceToNextReward);
        sensor.AddObservation(nextRewardXPosition / trainingEnvironmentManager.pathWidth);
        sensor.AddObservation(transform.localPosition.x / trainingEnvironmentManager.pathWidth);
        sensor.AddObservation(distanceToGoal / trainingEnvironmentManager.pathLength);
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

    public void GetDistanceToNextObstacle() //Must be in front of the agent on the Z axis
    {
        bool found = false;
        if (trainingEnvironmentManager.noObstacles)
        {
            distanceToNextObstacle = 1000f;
            return;
        }
        float minDistance = Mathf.Infinity;
        for (int i = 0; i < trainingEnvironmentManager.obstacleTransforms.Count; i++)
        {
            if (transform.position.z < trainingEnvironmentManager.obstacleTransforms[i].position.z) //If the obstacle is in front of the agent
            {
                found = true;
                float distance = Vector3.Distance(new Vector3(0, 0, transform.localPosition.z), new Vector3(0, 0, trainingEnvironmentManager.obstacleTransforms[i].localPosition.z));
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nextObstacle = trainingEnvironmentManager.obstacleTransforms[i].gameObject;
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

    public void GetDistanceToNextReward()
    {
        bool found = false;
        if (trainingEnvironmentManager.noRewards)
        {
            distanceToNextReward = distanceToGoal;
            nextRewardXPosition = 0f;
            return;
        }
        float minDistance = Mathf.Infinity;
        for (int i = 0; i < trainingEnvironmentManager.rewardTransforms.Count; i++)
        {
            if (transform.position.z < trainingEnvironmentManager.rewardTransforms[i].position.z) //If the reward is in front of the agent
            {
                found = true;
                float distance = Vector3.Distance(new Vector3(transform.localPosition.x, 0, transform.localPosition.z), new Vector3(trainingEnvironmentManager.rewardTransforms[i].localPosition.x, 0, trainingEnvironmentManager.rewardTransforms[i].localPosition.z));
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nextReward = trainingEnvironmentManager.rewardTransforms[i].gameObject;
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

    public void GetDistanceToGoal()
    {
        distanceToGoal = Vector3.Distance(new Vector3(0, 0, transform.localPosition.z), new Vector3(0, 0, trainingEnvironmentManager.goalLocalZPosition));
    }

    void Jump()
    {
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    //Agent Actions: Since it's always running forward without any action, it only moves on the x-axis and jumps (Add force on the y-axis)
    public override void OnActionReceived(ActionBuffers actions)
    {
        //One continuous action for x-axis movement
        var moveX = actions.ContinuousActions[0];
        //One discrete action for jumping
        int jump = actions.DiscreteActions[0];
        // //Movement
        transform.Translate(moveX * strafeSpeed * Time.deltaTime, 0, 0);

        //Jump
        if (jump == 1 && isGrounded)
        {
            Jump();
        }
        // if (jump == 1 && !isGrounded)
        // {
        //     //Shouldn't  jump in the air!
        //     AddReward(-0.1f);
        // }
    }

     void CalculateJumpDistance()
    {
        // Step 1: Calculate initial vertical velocity (v0 = jumpForce / mass)
        float initialVerticalVelocity = jumpForce / rb.mass;

        // Step 2: Calculate time to reach the highest point
        float timeToPeak = initialVerticalVelocity;

        // Step 3: Total air time is twice the time to reach the peak
        float totalAirTime = 2 * timeToPeak;

        // Step 4: Calculate the horizontal distance traveled (forwardSpeed * totalAirTime)
        float horizontalDistance = forwardSpeed * totalAirTime;

        Debug.Log($"Horizontal distance traveled in air: {horizontalDistance} Unity units");
    }


    void IsGrounded()
    {
        isGrounded = Physics.Raycast(transform.position, Vector3.down, 0.6f);
        //Set Y pos to 0
        if (isGrounded && transform.position.y != 0)
        {
            transform.position = new Vector3(transform.position.x, 0, transform.position.z);
        }
    }

    public override void OnEpisodeBegin()
    {

        trainingEnvironmentManager.GeneratePath();
        
        GetDistanceToNextObstacle();
        GetDistanceToNextReward();
        GetDistanceToGoal();
        Debug.Log("<color=yellow>Episode Begin</color>");
        transform.localPosition = trainingEnvironmentManager.agentStartLocalPosition.localPosition;

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
            trainingEnvironmentManager.groundMaterialCopy.color = Color.green;
            EndEpisode();
        }
        if (other.gameObject.CompareTag("Wall")) //Walls on the sides of the path
        {
            AddReward(-1);
            trainingEnvironmentManager.episodeFailed.Invoke();
            EndEpisode();

        }
        //Obstacle and Reward (coin)
        if (other.gameObject.CompareTag("Obstacle"))
        {
            AddReward(-1.5f);
            trainingEnvironmentManager.episodeFailed.Invoke();
            EndEpisode();
        }
        if (other.gameObject.CompareTag("Reward"))
        {
            AddReward(1.5f);
            other.gameObject.SetActive(false);
        }

    }

}
