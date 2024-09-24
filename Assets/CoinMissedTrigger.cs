using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoinMissedTrigger : MonoBehaviour
{
    public BoxCollider missedCoinTrigger;

    public TrainingEnvironmentManager trainingEnvironmentManager;

    void Start()
    {
        missedCoinTrigger = GetComponent<BoxCollider>();
        trainingEnvironmentManager = transform.parent.parent.parent.GetComponent<TrainingEnvironmentManager>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            trainingEnvironmentManager.coinMissed?.Invoke();
        }
    }
}
