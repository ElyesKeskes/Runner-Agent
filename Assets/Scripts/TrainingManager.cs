using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class TrainingManager : Singleton<TrainingManager>
{
    [SerializeField] public RewardAmounts rewardAmounts;

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
    }
}
