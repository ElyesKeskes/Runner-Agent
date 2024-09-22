using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class TrainingEnvironmentManager : MonoBehaviour
{
    public float goalLocalZPosition;
    public List<float> obstacleLocalZPositions;
    public List<Transform> obstacleTransforms;
    public List<Transform> rewardTransforms;

    [SerializeField] public float pathLength;
    [SerializeField] public float pathWidth;
    [SerializeField] public float minDistanceBetweenObstacles;
    [SerializeField] public float minDistanceBetweenRewards;
    [SerializeField] public float minDistanceBetweenRewardsAndObstacles;
    [SerializeField] public Material groundMaterial;
    public Material groundMaterialCopy;
    [SerializeField] public Transform agentStartLocalPosition;
    public Action episodeFailed;
    public bool noObstacles = false;
    public bool noRewards = false;
    public MeshRenderer groundMeshRenderer;
    public RunnerAgent agent;

    void Start()
    {
        episodeFailed += OnFail;
        Initialize();
    }

    public void Initialize()
    {
        groundMaterialCopy = new Material(groundMaterial);
        groundMeshRenderer.material = groundMaterialCopy;

        agent.rb = GetComponent<Rigidbody>();
        goalLocalZPosition = transform.GetChild(2).localPosition.z;
        pathLength = goalLocalZPosition - agentStartLocalPosition.localPosition.z;

        InitializeObstacles();
        InitializeRewards();
    }

    private void InitializeObstacles()
    {
        obstacleTransforms = new List<Transform>();
        var obstaclesParent = transform.GetChild(0);
        foreach (Transform child in obstaclesParent)
        {
            obstacleLocalZPositions.Add(child.localPosition.z);
            obstacleTransforms.Add(child);
        }
        noObstacles = obstacleTransforms.Count == 0;
    }

    public void ResetRewards()
    {
        foreach (var reward in rewardTransforms)
        {
            reward.gameObject.SetActive(true);
        }
    }

    public void ResetObstacles()
    {
        foreach (var obstacle in obstacleTransforms)
        {
            obstacle.gameObject.SetActive(true);
        }
    }

    private void InitializeRewards()
    {
        rewardTransforms = new List<Transform>();
        var rewardParent = transform.GetChild(1);
        foreach (var child in rewardParent)
        {
            rewardTransforms.Add((Transform)child);
        }
        noRewards = rewardTransforms.Count == 0;
    }

    public void GeneratePath()
    {
        RandomizeObstacleZPositions();
        RandomizeRewardXandZPositions();
    }

    public void OnFail()
    {
        groundMaterialCopy.color = Color.red;
    }
    public void RandomizeObstacleZPositions()
{
    if (noObstacles) return;
    ResetObstacles();
    float minZPosition = agentStartLocalPosition.localPosition.z + pathLength * 0.1f;
    float maxZPosition = goalLocalZPosition;

    List<float> obstaclePositions = new List<float>();

    float previousZ = minZPosition;
    foreach (var obstacle in obstacleTransforms)
    {
        // Define the maximum possible Z position for this obstacle
        float remainingDistance = maxZPosition - previousZ;
        float availableSpace = remainingDistance / (obstacleTransforms.Count - obstaclePositions.Count);

        // Get the new Z position for the obstacle
        float newZ = previousZ + UnityEngine.Random.Range(minDistanceBetweenObstacles, availableSpace);

        // If placing the obstacle would violate the minimum distance to the goal, deactivate it
        if (newZ + minDistanceBetweenObstacles > maxZPosition)
        {
            obstacle.gameObject.SetActive(false);
            continue;
        }

        // Place the obstacle and update the list of placed obstacles
        obstaclePositions.Add(newZ);
        obstacle.localPosition = new Vector3(obstacle.localPosition.x, obstacle.localPosition.y, newZ);

        // Update the previous obstacle's Z position
        previousZ = newZ;
    }
}


    public void RandomizeRewardXandZPositions()
    {
        if (noRewards) return;
        ResetRewards();
        float minZPosition = agentStartLocalPosition.localPosition.z + pathLength * 0.05f;
        float maxZPosition = goalLocalZPosition;
        float halfPathWidth = pathWidth / 2f;

        int maxRewards = Mathf.FloorToInt((maxZPosition - minZPosition) / minDistanceBetweenRewards);

        // Deactivate rewards if there isn't enough space
        for (int i = 0; i < rewardTransforms.Count; i++)
        {
            if (i < maxRewards)
            {
                rewardTransforms[i].gameObject.SetActive(true);
            }
            else
            {
                rewardTransforms[i].gameObject.SetActive(false);
            }
        }

        float availableLength = maxZPosition - minZPosition;
        float sectionLength = availableLength / Mathf.Min(rewardTransforms.Count, maxRewards);

        for (int i = 0; i < rewardTransforms.Count && i < maxRewards; i++)
        {
            float sectionMinZ = minZPosition + i * sectionLength;
            float sectionMaxZ = sectionMinZ + sectionLength;

            float newZ = UnityEngine.Random.Range(sectionMinZ, sectionMaxZ);
            float randomX = UnityEngine.Random.Range(-halfPathWidth, halfPathWidth);

            if (IsTooCloseToObstacle(newZ))
            {
                rewardTransforms[i].gameObject.SetActive(false); // Deactivate if too close to obstacles
            }
            else
            {
                rewardTransforms[i].localPosition = new Vector3(randomX, rewardTransforms[i].localPosition.y, newZ);
            }
        }
    }

    private bool IsTooCloseToObstacle(float newZ)
    {
        foreach (var obstacle in obstacleTransforms)
        {
            if (obstacle.gameObject.activeSelf && Vector3.Distance(new Vector3(0, 0, newZ), new Vector3(0, 0, obstacle.localPosition.z)) < minDistanceBetweenRewardsAndObstacles)
            {
                return true;
            }
        }
        return false;
    }

}
