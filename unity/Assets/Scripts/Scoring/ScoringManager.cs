using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;



namespace PoseTeacher
{
    public enum Scores
    {
        BAD, GOOD, GREAT
    }

    public enum GoalType
    {
        MOTION, POSE
    }

    class ScoringManager : MonoBehaviour
    {
        public static ScoringManager Instance;

        List<Scores> scores;

        // filter
        public double kalmanQ = 0.0001;
        public double kalmanR = 1.0;
        public bool activateKalman = false;
        public List<KalmanFilter> kalmanFilter;

        //Goals
        bool currentlyScoring = false;
        GoalType currentGoalType;
        List<DancePose> currentGoal;
        float currentTimeStamp;
        float goalStartTimeStamp;
        int goalCounter;
        int goalLength;
        List<float> currentScores;

        //constants
        int numberOfComparisons = 8;
        readonly float constDeltaTime = 0.1f;
        public bool alternateDistanceMetric = false;

        public GameObject scoreDisplay;


        public void Awake()
        {
            if (Instance != null)
            {
                Destroy(this);
            } else
            {
                Instance = this;
            }

            // generate kalman filters
            kalmanFilter = new List<KalmanFilter>();
            for (int i = 0; i < numberOfComparisons; i++)
            {
                kalmanFilter.Add(new KalmanFilter(kalmanQ, kalmanR));
                kalmanFilter[i].Reset(1.0);
            }

            scores = new List<Scores>();
        }

        public void Update()
        {
            if (currentlyScoring && DanceManager.Instance.currentSelfPose!=null)
            {
                PoseData currentSelfPose = DanceManager.Instance.currentSelfPose;
                float danceTimeStamp = DanceManager.Instance.songTime;

                bool nextStep = false;

                switch (currentGoalType)
                {
                    case GoalType.POSE:
                        float deltaTime = danceTimeStamp - currentTimeStamp;
                        nextStep = deltaTime >= constDeltaTime;
                        break;
                    case GoalType.MOTION:
                        nextStep = danceTimeStamp >= currentGoal[goalCounter].timestamp + goalStartTimeStamp;
                        break;
                }


                if (nextStep)
                {
                    if (!alternateDistanceMetric)
                    {
                        currentScores.Add(quaternionDistanceScore(currentSelfPose));
                    } else
                    {
                        currentScores.Add(euclideanDistanceScore(currentSelfPose));
                    }

                    currentTimeStamp = danceTimeStamp;
                    goalCounter += 1;
                    if (goalCounter == goalLength)
                    {
                        finishGoal();
                    }
                }

            }
        }


        public void StartNewGoal(GoalType type, List<DancePose> goal, float startTimeStamp)
        {
            if (currentlyScoring) finishGoal();
            currentlyScoring = true;
            currentScores = new List<float>();
            currentGoalType = type;
            currentGoal = goal;
            goalCounter = 0;
            currentTimeStamp = startTimeStamp;
            goalStartTimeStamp = startTimeStamp;
            if (currentGoalType == GoalType.POSE)
            {
                goalLength = 15;
            }
            else
            {
                goalLength = currentGoal.Count;
            }
        }

        private float quaternionDistanceScore(PoseData currentSelfPose)
        {
            float distanceTotal = 0.0f;
            List<Quaternion> selfList = PoseDataToOrientation(currentSelfPose);
            List<Quaternion> goalList;

            if (currentGoalType == GoalType.POSE)
            {
                goalList = DancePoseToOrientation(currentGoal[0]);
            }
            else
            {
                goalList = DancePoseToOrientation(currentGoal[goalCounter]);
            }

            for (int i = 0; i < numberOfComparisons; i++)
            {
                float distance = quaternionDistance(selfList[i], goalList[i]);
                distanceTotal += Mathf.Pow(distance, 2) * scoringWeightsPrioritizeArms[i];
            }
            return Mathf.Sqrt(distanceTotal / TotalWeights(scoringWeightsPrioritizeArms));
        }

        private float euclideanDistanceScore(PoseData currentSelfPose)
        {
            //TODO: Implement EuclideanDistance
            return 0;
        }


        public List<Scores> getFinalScores()
        {
            if (currentlyScoring) finishGoal();
            return scores;
        }

        void finishGoal()
        {
            double tempScore;
            if (currentGoalType == GoalType.POSE)
            {
                //for a pose, take best score in evaluation period
                tempScore = currentScores.Max();

            }
            else
            {
                //for a move, take square average of scores
                tempScore = squaredAverage(currentScores);
            }
            Debug.Log(tempScore);

            if (!alternateDistanceMetric)
            {
                if (tempScore < 0.15)
                {
                    scores.Add(Scores.GREAT);
                }
                else if (tempScore < 0.4)
                {
                    scores.Add(Scores.GOOD);
                }
                else
                {
                    scores.Add(Scores.BAD);
                }
            }
            else
            {
                //TODO: Scoring for EuclideanDistance
            }

            if (scoreDisplay != null)
            {
                scoreDisplay.SendMessage("addScore", scores[scores.Count - 1]);
            }
            currentlyScoring = false;
        }

        List<Quaternion> PoseDataToOrientation(PoseData pose)
        {
            List<Quaternion> list = new List<Quaternion>();
            Vector3 vector;

            //LeftUpperArm (SHOULDER_LEFT - ELBOW_LEFT)
            vector = pose.data[5].Position - pose.data[6].Position;
            list.Add(Quaternion.LookRotation(vector, Vector3.up));

            //RightUpperArm (SHOULDER_RIGHT - ELBOW_RIGHT)
            vector = pose.data[12].Position - pose.data[13].Position;
            list.Add(Quaternion.LookRotation(vector, Vector3.up));

            //TorsoLeft (SHOULDER_LEFT - HIP_LEFT
            vector = pose.data[5].Position - pose.data[18].Position;
            list.Add(Quaternion.LookRotation(vector, Vector3.up));

            //TorsoRight (SHOULDER_RIGHT - HIP_RIGHT
            vector = pose.data[12].Position - pose.data[22].Position;
            list.Add(Quaternion.LookRotation(vector, Vector3.up));

            //HipStick (HIP_LEFT - HIP_RIGHT)
            vector = pose.data[18].Position - pose.data[22].Position;
            list.Add(Quaternion.LookRotation(vector, Vector3.up));

            //LeftLowerArm (ELBOW_LEFT - WRIST_LEFT)
            vector = pose.data[6].Position - pose.data[7].Position;
            list.Add(Quaternion.LookRotation(vector, Vector3.up));

            //RightLowerArm (ELBOW_RIGHT - WRIST_RIGHT)
            vector = pose.data[13].Position - pose.data[14].Position;
            list.Add(Quaternion.LookRotation(vector, Vector3.up));

            //Shoulders (SHOULDER_LEFT - SHOULDER_RIGHT)
            vector = pose.data[5].Position - pose.data[12].Position;
            list.Add(Quaternion.LookRotation(vector, Vector3.up));

            return list;
        }

        List<Quaternion> DancePoseToOrientation(DancePose pose)
        {
            List<Quaternion> list = new List<Quaternion>();
            Vector3 vector;

            //LeftUpperArm (SHOULDER_LEFT - ELBOW_LEFT)
            vector = pose.positions[5] - pose.positions[6];
            list.Add(Quaternion.LookRotation(vector, Vector3.up));

            //RightUpperArm (SHOULDER_RIGHT - ELBOW_RIGHT)
            vector = pose.positions[12] - pose.positions[13];
            list.Add(Quaternion.LookRotation(vector, Vector3.up));

            //TorsoLeft (SHOULDER_LEFT - HIP_LEFT
            vector = pose.positions[5] - pose.positions[18];
            list.Add(Quaternion.LookRotation(vector, Vector3.up));

            //TorsoRight (SHOULDER_RIGHT - HIP_RIGHT
            vector = pose.positions[12] - pose.positions[22];
            list.Add(Quaternion.LookRotation(vector, Vector3.up));

            //HipStick (HIP_LEFT - HIP_RIGHT)
            vector = pose.positions[18] - pose.positions[22];
            list.Add(Quaternion.LookRotation(vector, Vector3.up));

            //LeftLowerArm (ELBOW_LEFT - WRIST_LEFT)
            vector = pose.positions[6] - pose.positions[7];
            list.Add(Quaternion.LookRotation(vector, Vector3.up));

            //RightLowerArm (ELBOW_RIGHT - WRIST_RIGHT)
            vector = pose.positions[13] - pose.positions[14];
            list.Add(Quaternion.LookRotation(vector, Vector3.up));

            //Shoulders (SHOULDER_LEFT - SHOULDER_RIGHT)
            vector = pose.positions[5] - pose.positions[12];
            list.Add(Quaternion.LookRotation(vector, Vector3.up));

            return list;
        }

        List<int> scoringWeightsPrioritizeArms = new List<int>{3, 3, 1, 1, 1, 3, 3, 1};

        int TotalWeights(List<int> weights)
        {
            int total = 0;
            foreach (int i in weights)
            {
                total += i;
            }
            return total;
        }

        float squaredAverage(List<float> values)
        {
            float squaredTotal = 0f;
            foreach (float f in values)
            {
                squaredTotal += Mathf.Pow(f, 2);
            }
            return Mathf.Sqrt(squaredTotal / values.Count);
        }

        float quaternionDistance(Quaternion a, Quaternion b)
        {
            return 1 - Mathf.Pow(a.w * b.w + a.x * b.x + a.y * b.y + a.z * b.z, 2);
        }
    }
}