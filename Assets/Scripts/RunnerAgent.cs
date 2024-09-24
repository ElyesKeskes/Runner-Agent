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
    public float distanceToSecondNextObstacle; //Must be observed
    public float distanceToNextReward;//Must be observed
    public float nextRewardXPosition; //Must be observed
    public GameObject nextReward;
    public GameObject nextObstacle;
    public GameObject secondNextObstacle;
    public bool isGrounded = true; //Must be observed
    private TrainingEnvironmentManager trainingEnvironmentManager;
    public int coinsGot = 0;
    public int obstaclesAvoided = 0;
    //Ratio values
    public float nextRewardXPositionRatio;
    public float agentXPositionRatio;
    public float distanceToGoalRatio;


    void Start()
    {
        rb = GetComponent<Rigidbody>();
        trainingEnvironmentManager = transform.parent.GetComponent<TrainingEnvironmentManager>();
        //CalculateJumpDistance();
    }


    void Update()
    {
        //Always move forward
        transform.Translate(0, 0, forwardSpeed * Time.deltaTime);
        IsGrounded();
    }




   




    public override void CollectObservations(VectorSensor sensor)
    {
        GetDistanceToNextObstacles();
        GetDistanceToNextReward();
        GetDistanceToGoal();

        //Calculate ratios

        nextRewardXPositionRatio = nextRewardXPosition / trainingEnvironmentManager.pathWidth;
        distanceToGoalRatio = distanceToGoal / trainingEnvironmentManager.pathLength;
        agentXPositionRatio = transform.localPosition.x / trainingEnvironmentManager.pathWidth;

        sensor.AddObservation(distanceToNextObstacle);
        sensor.AddObservation(distanceToSecondNextObstacle);
        sensor.AddObservation(distanceToNextReward);
        sensor.AddObservation(nextRewardXPosition);
        sensor.AddObservation(agentXPositionRatio);
        sensor.AddObservation(distanceToGoalRatio);
        sensor.AddObservation(isGrounded);
    }


    public void OnCoinMissed()
    {
        AddReward(TrainingManager.Instance.rewardAmounts.missCoin);
        //Debug log in red
        Debug.Log("<color=red>Coin Missed</color>");
    }

    public void OnObstacleAvoided()
    {
        AddReward(TrainingManager.Instance.rewardAmounts.avoidObstacle);
        //Debug log in green
        Debug.Log("<color=green>Obstacle Avoided</color>");
        obstaclesAvoided++;
        UpdateNextObstacles();
        trainingEnvironmentManager.updateUI?.Invoke();
    }

    bool CheckIfAllCoinsCollected()
    {
        //Counts total of coins in scene only the ones that are active
        int activeCoins = 0;
        foreach (var reward in trainingEnvironmentManager.rewardTransforms)
        {
            if (reward.gameObject.activeSelf)
            {
                activeCoins++;
            }
        }
        if (coinsGot == activeCoins)
        {
            return true;
        }
        return false;
    }
    public void InitializeNextObstacle()
    {
        if (trainingEnvironmentManager.obstacleTransforms.Count > 1)
        {
            nextObstacle = trainingEnvironmentManager.obstacleTransforms[0].gameObject; // Closest to the goal
            secondNextObstacle = trainingEnvironmentManager.obstacleTransforms[1].gameObject;
        }
        else
        {
            nextObstacle = null;
            secondNextObstacle = null;
            distanceToNextObstacle = 1000f; // No obstacles
        }
    }
     public void UpdateNextObstacles()
    {
        if (nextObstacle == null) return;

        int nextIndex = nextObstacle.transform.GetSiblingIndex() + 1;
        GameObject potentialNextObstacle = null;
        GameObject potentialSecondNextObstacle = null;

        // Find the next active obstacle for nextObstacle
        while (nextIndex < trainingEnvironmentManager.obstacleTransforms.Count)
        {
            potentialNextObstacle = trainingEnvironmentManager.obstacleTransforms[nextIndex].gameObject;

            if (potentialNextObstacle.activeInHierarchy)
            {
                nextObstacle = potentialNextObstacle;
                break;
            }

            nextIndex++;
        }

        // Update secondNextObstacle: Find the second next active obstacle
        nextIndex = nextObstacle != null ? nextObstacle.transform.GetSiblingIndex() + 1 : nextIndex;

        while (nextIndex < trainingEnvironmentManager.obstacleTransforms.Count)
        {
            potentialSecondNextObstacle = trainingEnvironmentManager.obstacleTransforms[nextIndex].gameObject;

            if (potentialSecondNextObstacle.activeInHierarchy)
            {
                secondNextObstacle = potentialSecondNextObstacle;
                break;
            }

            nextIndex++;
        }

        // If no more active obstacles found, set null
        if (potentialNextObstacle == null || !potentialNextObstacle.activeInHierarchy)
        {
            nextObstacle = null;
            distanceToNextObstacle = 1000f;
        }

        if (potentialSecondNextObstacle == null || !potentialSecondNextObstacle.activeInHierarchy)
        {
            secondNextObstacle = null;
            distanceToSecondNextObstacle = 1000f;
        }
    }
    public void GetDistanceToNextObstacles() //Must be in front of the agent on the Z axis
    {
        if (nextObstacle != null)
        {
            distanceToNextObstacle = Vector3.Distance(new Vector3(0, 0, transform.localPosition.z), new Vector3(0, 0, nextObstacle.transform.localPosition.z));
        }
        else
        {
            distanceToNextObstacle = 1000f;
        }

        if (secondNextObstacle != null)
        {
            distanceToSecondNextObstacle = Vector3.Distance(new Vector3(0, 0, transform.localPosition.z), new Vector3(0, 0, secondNextObstacle.transform.localPosition.z));
        }
        else
        {
            distanceToSecondNextObstacle = 1000f;
        }
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
        if (jump == 1 && !isGrounded)
        {
            //Shouldn't  jump in the air!
            AddReward(TrainingManager.Instance.rewardAmounts.jumpWhileNotGrounded);
        }
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
        trainingEnvironmentManager.ResetUI();
        InitializeNextObstacle();
        GetDistanceToNextObstacles();
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
            AddReward(TrainingManager.Instance.rewardAmounts.touchGoal);
            //Set base color of the ground material to green
            trainingEnvironmentManager.groundMaterialCopy.color = Color.green;
            if (CheckIfAllCoinsCollected())
            {
                AddReward(TrainingManager.Instance.rewardAmounts.perfectRunBonus);
            }
            EndEpisode();
        }
        if (other.gameObject.CompareTag("Wall")) //Walls on the sides of the path
        {
            AddReward(TrainingManager.Instance.rewardAmounts.touchWall);
            trainingEnvironmentManager.episodeFailed.Invoke();
            EndEpisode();

        }

        if (other.gameObject.CompareTag("Obstacle"))
        {
            AddReward(TrainingManager.Instance.rewardAmounts.touchObstacle);
            trainingEnvironmentManager.episodeFailed.Invoke();
            EndEpisode();
        }
        if (other.gameObject.CompareTag("Reward"))
        {
            AddReward(TrainingManager.Instance.rewardAmounts.touchCoin);
            other.gameObject.SetActive(false);
            coinsGot++;
            trainingEnvironmentManager.updateUI?.Invoke();
        }

    }

}
