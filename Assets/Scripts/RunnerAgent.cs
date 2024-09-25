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

    [Header("Movement Monitoring")]
    [SerializeField] public float jumpForce;
    [SerializeField] public float strafeSpeed;
    [SerializeField] public float forwardSpeed;
    [Header("Observed Values")]

    public float distanceToNextObstacle;//Must be observed
    public float distanceToSecondNextObstacle; //Must be observed
    public float distanceToNextReward;//Must be observed
    //Ratio values
    public float nextRewardXPositionRatio; //Must be observed
    public float agentXPositionRatio; //Must be observed
    public float distanceToGoalRatio; //Must be observed
    public bool isGrounded = true; //Must be observed
    [Header("Used to calculate observed values")]
    public float distanceToGoal;
    public float nextRewardXPosition;
    public GameObject nextReward;
    public GameObject nextObstacle;
    public GameObject secondNextObstacle;

    private TrainingEnvironmentManager trainingEnvironmentManager;
    [Header("Current run statistics")]
    public int coinsGot = 0;
    public int obstaclesAvoided = 0;
    [Header("Agent movement input")]
    public int movement = 0;
    [Header("Movement Monitoring")]
    public float maxJumpHeight = 0;
    public float agentSpeed;  // Speed in units per second

    private Vector3 lastPosition;  // To store the agent's last position
    private float timeElapsed;     // To track the time elapsed for speed calculation
    private bool canJump = true;
    private float jumpCooldown = 0.2f; // Cooldown time between jumps
    private float jumpTimer = 0f;
    private bool jumpTrigger = false;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        trainingEnvironmentManager = transform.parent.GetComponent<TrainingEnvironmentManager>();
        lastPosition = transform.position;  // Initialize with the current position
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.Space) && isGrounded && canJump)
        {
            Jump();
            canJump = false;
            jumpTimer = 0f;
        }

        //For testing purposes in Default mode if in OnActionReceived jumping logic is commented out: allows automatic jumping

        // if (jumpTrigger)
        // {
        //     if (isGrounded && canJump)
        //     {
        //         //Trigger input spacebar
        //         Jump();

        //     }

        //     jumpTrigger = false;
        // }
    }

    void FixedUpdate()
    {

        transform.Translate(movement * strafeSpeed * Time.fixedDeltaTime, 0, forwardSpeed * Time.fixedDeltaTime);
        IsGrounded();
        RecordMaxJumpHeight();
        MonitorMaxSpeed();
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

        int jump = actions.DiscreteActions[0];
        var moveX = actions.DiscreteActions[1];

        // Handle jump cooldown
        jumpTimer += Time.deltaTime;
        if (jumpTimer >= jumpCooldown)
        {
            canJump = true;
        }

        //Jumping
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

        //1D X-Axis movement
        if (moveX == 1)
        {
            movement = 1;
        }
        else if (moveX == 2)
        {
            movement = -1;
        }
        else if (moveX == 0)
        {
            movement = 0;
        }
    }

    public override void OnEpisodeBegin()
    {
        //Reset & Initialize values
        rb.velocity = Vector3.zero;
        coinsGot = 0;
        obstaclesAvoided = 0;
        trainingEnvironmentManager.GeneratePath();
        InitializeNextObstacles();
        InitializeNextReward();
        GetDistanceToNextObstacles();
        GetDistanceToNextReward();
        GetDistanceToGoal();
        transform.localPosition = trainingEnvironmentManager.agentStartLocalPosition.localPosition;
        // Debug.Log("<color=yellow>Episode Begin</color>");
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<int> action = actionsOut.DiscreteActions;
        // action[0] = (Input.GetKey(KeyCode.Space) && isGrounded) ? 1 : 0;

        // if (Input.GetKey(KeyCode.Q))
        // {
        //     action[1] = 2;
        // }
        // else if (Input.GetKey(KeyCode.D))
        // {
        //     action[1] = 1;
        // }
        // else { action[1] = 0; }

        //For testing purposes in heuristic mode & Demo recorder, allows automatic jumping

        if (jumpTrigger)
        {
            if (isGrounded && canJump)
            {
                action[0] = 1;
            }

            jumpTrigger = false;
        }
        AlignToNextRewardX(actionsOut);

    }

    private void AlignToNextRewardX(ActionBuffers actionsOut)
    {
        ActionSegment<int> action = actionsOut.DiscreteActions;

        // Difference between the agent's X position ratio and the next reward's X position ratio
        float deltaX = nextRewardXPositionRatio - agentXPositionRatio;

        // Tolerance of 1/20 = 0.05 units
        float tolerance = 0.01f;

        // Adjust movement based on the deltaX
        if (deltaX > tolerance)
        {
            // Move right if the next reward is to the right
            action[1] = 1; // Assuming this is the index for right movement
        }
        else if (deltaX < -tolerance)
        {
            // Move left if the next reward is to the left
            action[1] = 2; // Assuming this is the index for left movement
        }
        else
        {
            // No movement if within tolerance
            action[1] = 0;
        }
    }

    void Jump()
    {
        maxJumpHeight = 0; //For monitoring max jump height
        if (trainingEnvironmentManager.JumpTrigger)
        {
            AddReward(TrainingManager.Instance.rewardAmounts.perfectJumpBonus);
            Debug.Log("PERFECT JUMP");
        }
        else
        {
            AddReward(TrainingManager.Instance.rewardAmounts.perfectJumpPenalty);
        }

        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        Debug.Log("AGENT JUMPED");
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
                Debug.Log("<color=green>PERFECT RUN ! Obstacles cleared:" + obstaclesAvoided + "</color>");
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
        if (other.gameObject.CompareTag("JumpTrigger"))
        {
            jumpTrigger = true;
        }
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
        isGrounded = Physics.Raycast(transform.position, Vector3.down, 0.6f, LayerMask.GetMask("Ground"));
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

    public void RecordMaxJumpHeight()
    {
        if (transform.position.y > maxJumpHeight)
        {
            maxJumpHeight = transform.position.y;
        }
    }

    public void MonitorMaxSpeed()
    {
        // Accumulate time each frame
        timeElapsed += Time.fixedDeltaTime;

        // Check if one second has passed
        if (timeElapsed >= 1f)
        {
            // Calculate the distance traveled in the last second
            float distanceTraveled = Vector3.Distance(lastPosition, transform.position);

            // Update the public agentSpeed variable
            agentSpeed = distanceTraveled / timeElapsed;

            // Reset timer and update last position
            timeElapsed = 0f;
            lastPosition = transform.position;
        }
    }

}
