using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class TrainingManager : Singleton<TrainingManager>
{
    [SerializeField] public RewardAmounts rewardAmounts;
    
     [SerializeField, Range(0.1f, 10f)]
    private float _timeScale = 1f;

    // Property for time scale
    public float timeScale
    {
        get => _timeScale;
        set
        {
            _timeScale = value;
            Time.timeScale = _timeScale;
        }
    }

    // This is called whenever the value is changed in the Inspector
    private void OnValidate()
    {
        // Apply time scale changes in real-time
        Time.timeScale = _timeScale;
    }
    

    //Struct defining the reward amount for: TouchObstacle, TouchCoin, ReachGoal, AvoidObstacle, MissCoin (floats)
    [System.Serializable]
    public struct RewardAmounts
    {
        public float avoidObstacle;
        public float touchObstacle;
        public float touchCoin;
        public float missCoin;
        public float touchGoal;
        public float touchWall;
        public float jumpWhileNotGrounded;
        public float perfectRunBonus;
        public float perfectJumpBonus;
        public float perfectJumpPenalty;

    }


}
