using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleAvoidedScript : MonoBehaviour
{
    public BoxCollider avoidedObstacleTrigger;

    public TrainingEnvironmentManager trainingEnvironmentManager;

    void Start()
    {
        avoidedObstacleTrigger = GetComponent<BoxCollider>();
        trainingEnvironmentManager = transform.parent.parent.parent.GetComponent<TrainingEnvironmentManager>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            trainingEnvironmentManager.obstacleAvoided?.Invoke();

        }
    }
}
