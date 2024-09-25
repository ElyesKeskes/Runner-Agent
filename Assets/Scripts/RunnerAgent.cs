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
    public float nextRewardXPosition;
    public GameObject nextReward;
    public GameObject nextObstacle;
    public GameObject secondNextObstacle;
    public bool isGrounded = true; //Must be observed
    private TrainingEnvironmentManager trainingEnvironmentManager;
    public int coinsGot = 0;
    public int obstaclesAvoided = 0;
    //Ratio values
    public float nextRewardXPositionRatio; //Must be observed
    public float agentXPositionRatio; //Must be observed
    public float distanceToGoalRatio; //Must be observed
    private bool canJump = true;
    private float jumpCooldown = 0.2f; // Cooldown time between jumps
    private float jumpTimer = 0f;


    void Start()
    {
        rb = GetComponent<Rigidbody>();
        trainingEnvironmentManager = transform.parent.GetComponent<TrainingEnvironmentManager>();
        //CalculateJumpDistance();
    }


    void FixedUpdate()
    {

        transform.Translate(0, 0, forwardSpeed * Time.deltaTime);
        IsGrounded();
    }

    void Update()
    {
        Debug.Log(trainingEnvironmentManager.JumpTrigger);
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

    //Agent Actions: Since it's always running forward without any action, it only moves on the x-axis and jumps (Add force on the y-axis)
    public override void OnActionReceived(ActionBuffers actions)
    {
        // Handle jump cooldown
        jumpTimer += Time.deltaTime;
        if (jumpTimer >= jumpCooldown)
        {
            canJump = true;
        }
        //One continuous action for x-axis movement
        //var moveX = actions.ContinuousActions[0];
        //One discrete action for jumping
        int jump = actions.DiscreteActions[0];

        //Jump
        if (jump == 1 && isGrounded && canJump)
        {
            Jump();
            canJump = false;
            jumpTimer = 0f;
        }
        if (jump == 1 && !isGrounded && !canJump)
        {
            Debug.Log("AGENT JUMPED WHILE NOT GROUNDED");
            AddReward(TrainingManager.Instance.rewardAmounts.jumpWhileNotGrounded);
        }
        if(actions.DiscreteActions[1]== 1)
        {
            transform.Translate(1 * strafeSpeed * Time.deltaTime, 0, 0);
        }else if (actions.DiscreteActions[1] == 2)
        {
            transform.Translate(-1 * strafeSpeed * Time.deltaTime, 0, 0);
        }else if (actions.DiscreteActions[1] == 0)
        {
            transform.Translate(0 * strafeSpeed * Time.deltaTime, 0, 0);
        }
      //  transform.Translate(actions.DiscreteActions[1] * strafeSpeed * Time.deltaTime, 0, 0);

    }

    void Jump()
    {
        if (trainingEnvironmentManager.JumpTrigger)
        {
            AddReward(+0.5f);
            Debug.Log("PERFECT JUMP");
        }
        else
        {
            AddReward(-0.5f);
        }
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        Debug.Log("AGENT JUMPED");
    }

    public override void OnEpisodeBegin()
    {
        rb.velocity = Vector3.zero;
        coinsGot = 0;
        obstaclesAvoided = 0;
        trainingEnvironmentManager.GeneratePath();
        InitializeNextObstacles();
        InitializeNextReward();
        GetDistanceToNextObstacles();
        GetDistanceToNextReward();
        GetDistanceToGoal();
        // Debug.Log("<color=yellow>Episode Begin</color>");
        transform.localPosition = trainingEnvironmentManager.agentStartLocalPosition.localPosition;

    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        //var continuousActionsOut = actionsOut.ContinuousActions;
        //continuousActionsOut[0] = Input.GetAxis("Horizontal");
        var discreteActionsOut = actionsOut.DiscreteActions;
        //discreteActionsOut[0] = (Input.GetKey(KeyCode.Space) && isGrounded) ? 1 : 0;
        ActionSegment<int> action = actionsOut.DiscreteActions;
        action[0] = (Input.GetKey(KeyCode.Space) && isGrounded) ? 1 : 0;
        
        if (Input.GetKey(KeyCode.Q))
        {
            action[1] = 2;
        }else if (Input.GetKey(KeyCode.D))
        {
            action[1] = 1;
        }else { action[1] = 0; }

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
                //DEBUG LOG PERFECT RUN
                Debug.Log("<color=green>PERFECT RUN !</color>");
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
            trainingEnvironmentManager.UpdateUI();
            UpdateNextReward();
            trainingEnvironmentManager.updateUI?.Invoke();
        }


    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
        }
    }

    public void OnCoinMissed()
    {
        AddReward(TrainingManager.Instance.rewardAmounts.missCoin);
        //Debug log in red
        Debug.Log("<color=red>Coin Missed</color>");
        UpdateNextReward();
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
        if (coinsGot == trainingEnvironmentManager.activeCoins)
        {
            return true;
        }
        return false;
    }
    public void InitializeNextObstacles()
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
            distanceToNextObstacle = Vector3.Distance(new Vector3(0, 0, transform.localPosition.z + 0.5f), new Vector3(0, 0, nextObstacle.transform.localPosition.z - 0.5f));
        }
        else
        {
            distanceToNextObstacle = 1000f;
        }

        if (secondNextObstacle != null)
        {
            distanceToSecondNextObstacle = Vector3.Distance(new Vector3(0, 0, transform.localPosition.z + 0.5f), new Vector3(0, 0, secondNextObstacle.transform.localPosition.z - 0.5f));
        }
        else
        {
            distanceToSecondNextObstacle = 1000f;
        }
    }
    public void InitializeNextReward()
    {
        nextReward = null;

        foreach (Transform rewardTransform in trainingEnvironmentManager.rewardTransforms)
        {
            if (rewardTransform.gameObject.activeSelf)
            {
                nextReward = rewardTransform.gameObject;
                nextRewardXPosition = nextReward.transform.localPosition.x;
                break;
            }
        }

        if (nextReward == null)
        {
            distanceToNextReward = distanceToGoal;
            nextRewardXPosition = 0f;
        }
    }

    public void UpdateNextReward()
    {
        if (nextReward == null) return;

        int nextIndex = nextReward.transform.GetSiblingIndex() + 1;

        while (nextIndex < trainingEnvironmentManager.rewardTransforms.Count)
        {
            GameObject potentialNextReward = trainingEnvironmentManager.rewardTransforms[nextIndex].gameObject;
            if (potentialNextReward.activeSelf)
            {
                nextReward = potentialNextReward;
                nextRewardXPosition = nextReward.transform.localPosition.x;
                return;
            }
            nextIndex++;
        }

        // No more rewards
        nextReward = null;
        distanceToNextReward = distanceToGoal;
        nextRewardXPosition = 0f;
    }




    public void GetDistanceToNextReward()
    {
        if (nextReward != null)
        {
            distanceToNextReward = Vector3.Distance(
                new Vector3(transform.localPosition.x, 0, transform.localPosition.z),
                new Vector3(nextReward.transform.localPosition.x, 0, nextReward.transform.localPosition.z)
            );
            nextRewardXPosition = nextReward.transform.localPosition.x;
        }
        else
        {
            distanceToNextReward = distanceToGoal;
            nextRewardXPosition = 0f;
        }
    }


    public void GetDistanceToGoal()
    {
        distanceToGoal = Vector3.Distance(new Vector3(0, 0, transform.localPosition.z + 0.5f), new Vector3(0, 0, trainingEnvironmentManager.goalLocalZPosition - 0.5f));
    }





    void IsGrounded()
    {
        isGrounded = Physics.Raycast(transform.position, Vector3.down, 0.6f);
        //Set Y pos to 0
        if (isGrounded && transform.position.y != 0)
        {
            // transform.position = new Vector3(transform.position.x, 0, transform.position.z);
        }
    }

}