using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.MLAgents;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class MoveScript : Agent
{
    [SerializeField] private float MoveSpeed = 5f;
    [SerializeField] private float RotationSpeed = 200f;
    private Rigidbody rb;
    private int coins = 0;
    [SerializeField] private Material winmat, losemat;
    [SerializeField] private MeshRenderer floor;
    [SerializeField] private List<GameObject> coinsList;
    [SerializeField] private List<GameObject> obstaclesList;
    [SerializeField] private GameObject closestCoin;
    [SerializeField] private float previousDistanceToCoin;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnEpisodeBegin()
    {
        transform.localPosition = Vector3.zero;
        ResetCoins();
        ResetObstacles();
        coins = 0;
        closestCoin = FindClosestActiveCoin();
        previousDistanceToCoin = closestCoin != null ? Vector3.Distance(transform.position, closestCoin.transform.position) : float.MaxValue;
    }

    private void ResetCoins()
    {
        foreach (GameObject coin in coinsList)
        {
            coin.SetActive(false);
        }

        HashSet<int> selectedCoins = new HashSet<int>();
        while (selectedCoins.Count < 7)
        {
            int randomIndex = Random.Range(0, coinsList.Count);
            selectedCoins.Add(randomIndex);
        }

        foreach (int index in selectedCoins)
        {
            coinsList[index].SetActive(true);
        }
    }

    private void ResetObstacles()
    {
        foreach (GameObject obstacle in obstaclesList)
        {
            obstacle.SetActive(false);
        }

        HashSet<int> selectedObstacles = new HashSet<int>();
        while (selectedObstacles.Count < 4)
        {
            int randomIndex = Random.Range(0, obstaclesList.Count);
            selectedObstacles.Add(randomIndex);
        }

        foreach (int index in selectedObstacles)
        {
            obstaclesList[index].SetActive(true);
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition);
        if (closestCoin != null)
        {
                sensor.AddObservation(closestCoin.transform.localPosition);
                Vector3 directionToCoin = (closestCoin.transform.position - transform.position).normalized;
            sensor.AddObservation(directionToCoin);
        }
        else
        {
            sensor.AddObservation(Vector3.zero);
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveZ = actions.ContinuousActions[0]; // Forward/backward
        float rotateY = actions.ContinuousActions[1]; // Rotation

        // Ensure forward movement
        if (moveZ > 0)
        {
            Vector3 moveDirection = transform.forward * moveZ * MoveSpeed * Time.deltaTime;
            rb.MovePosition(transform.position + moveDirection);
        }

        // Limit rotation to a maximum angle
        if (rotateY != 0)
        {
            transform.Rotate(0, rotateY * RotationSpeed * Time.deltaTime, 0);
        }

        UpdateRewardForCoinDistance();

        // Check for win condition
        if (coins >= 7)
        {
            SetReward(500f);
            floor.material = winmat;
            EndEpisode();
        }
        else
        {
            AddReward(-0.01f);
        }
    }

    private void UpdateRewardForCoinDistance()
    {
        closestCoin = FindClosestActiveCoin();
        if (closestCoin != null && closestCoin.activeSelf)
        {
            float currentDistanceToCoin = Vector3.Distance(transform.position, closestCoin.transform.position);

            // Reward for getting closer to the coin
           

            // Reward for facing the coin
            Vector3 directionToCoin = (closestCoin.transform.position - transform.position).normalized;
            float dotProduct = Vector3.Dot(transform.forward, directionToCoin);
            if (dotProduct > 0.9f)
            {
                if ((int)currentDistanceToCoin < (int)previousDistanceToCoin)
                {
                    AddReward(1f); // Increased reward for getting closer
                }
              //  AddReward(0.2f); // Reward for facing the coin
            }

            // Optional: You can also give a smaller reward for being generally in the direction of the coin
            //else if (dotProduct > 0.5f) // If facing somewhat towards the coin
            //{
            //    if ((int)currentDistanceToCoin < (int)previousDistanceToCoin)
            //    {
            //        AddReward(0.05f); // Increased reward for getting closer
            //    }
            //   // AddReward(0.1f); // Smaller reward for being in the right direction
            //}

            previousDistanceToCoin = currentDistanceToCoin; // Update the previous distance
        }
    }


    private GameObject FindClosestActiveCoin()
    {
        return coinsList
            .Where(coin => coin.activeSelf)
            .OrderBy(coin => Vector3.Distance(transform.position, coin.transform.position))
            .FirstOrDefault();
    }

    public override void Heuristic(in ActionBuffers actionOut)
    {
        ActionSegment<float> continuousActions = actionOut.ContinuousActions;
        continuousActions[0] = Input.GetKey(KeyCode.W) ? 1f : 0f; // Move forward
        continuousActions[1] = Input.GetKey(KeyCode.A) ? -1f : Input.GetKey(KeyCode.D) ? 1f : 0f; // Rotate
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("ObsTrig"))
        {
            AddReward(-0.01f);
        }
        if (other.CompareTag("Coin"))
        {
            coins++;
            AddReward(+20f);
            other.gameObject.SetActive(false);
            UpdateRewardForCoinDistance();

            closestCoin = FindClosestActiveCoin();
            previousDistanceToCoin = closestCoin != null ? Vector3.Distance(transform.position, closestCoin.transform.position) : float.MaxValue;
        }
        if (other.CompareTag("Wall") || other.CompareTag("Obstacle"))
        {
            SetReward(-10f);
            floor.material = losemat;
            EndEpisode();
        }
    }
}
