using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using Microsoft.MixedReality.Toolkit.UI;

namespace PoseTeacher
{

    public class ScoringTestScene : MonoBehaviour
    {
        public static ScoringTestScene Instance;

        PoseGetter selfPoseInputGetter;

        public GameObject videoCube;
        public DancePerformanceScriptableObject DancePerformanceObjectTarget;
        public DancePerformanceScriptableObject DancePerformanceObjectTest;

        public GameObject avatarContainerTarget, avatarContainerTest;
        List<AvatarContainer> avatarListTarget, avatarListTest;

        private readonly string target_file = "jsondata/2020_05_27-00_01_59.txt";
        private readonly string test_file = "jsondata/2020_05_27-00_01_59.txt";

        public bool paused = false;

        public InputSource PoseInputSource = InputSource.FILE;
        public PoseData currentPose;
        public DancePose testPose;

        private DanceData danceDataTarget;
        private DanceData danceDataTest;

        private AudioClip song;
        private AudioSource audioSource;

        readonly List<(float, DanceData)> goals = new List<(float, DanceData)>();

        public float songTime => audioSource?.time ?? 0;

        int currentId = 0;

        public void Awake()
        {
            if (Instance != null)
            {
                Destroy(this);
            }
            else
            {
                Instance = this;
            }
        }

        // Start is called before the first frame update
        public void Start()
        {
            avatarListTarget = new List<AvatarContainer>();
            avatarListTest = new List<AvatarContainer>();
            avatarListTarget.Add(new AvatarContainer(avatarContainerTarget));
            avatarListTest.Add(new AvatarContainer(avatarContainerTest));

            audioSource = GetComponent<AudioSource>();
            song = DancePerformanceObjectTarget.SongObject.SongClip;
            audioSource.clip = song;
            danceDataTarget = DancePerformanceObjectTarget.danceData.LoadDanceDataFromScriptableObject();
            danceDataTest = DancePerformanceObjectTest.danceData.LoadDanceDataFromScriptableObject();

            for (int i = 0; i < DancePerformanceObjectTarget.goals.Count; i++)
            {
                goals.Add((DancePerformanceObjectTarget.goalStartTimestamps[i], DancePerformanceObjectTarget.goals[i]));
            }

            selfPoseInputGetter = getPoseGetter(PoseInputSource);

            audioSource.Play();

            Debug.Log("Successfull start initialization.");
        }

        // Update is called once per frame
        public void Update()
        {
            float timeOffset = audioSource.time - danceDataTarget.poses[currentId].timestamp;
            //currentPose = selfPoseInputGetter.GetNextPose();
            int fakeID;
            testPose = danceDataTest.GetInterpolatedPose(currentId, out fakeID, timeOffset);

            AnimateSelf(testPose.toPoseData());
            if (goals.Count > 0 && audioSource.time >= goals[0].Item1)
            {
                ScoringManager.Instance.StartNewGoal(goals[0].Item2.poses, 0f);
                goals.RemoveAt(0);
            }
            AnimateTeacher(danceDataTarget.GetInterpolatedPose(currentId, out currentId, timeOffset).toPoseData());


            if (audioSource.time > danceDataTarget.poses[danceDataTarget.poses.Count - 1].timestamp)
            {
                audioSource.Stop();
                List<Scores> finalScores = ScoringManager.Instance.getFinalScores();
                Debug.Log(string.Format("final #{0} scores:", finalScores.Count));
                foreach(Scores s in finalScores)
                {
                    switch (s)
                    {
                        case Scores.BAD:
                            Debug.Log("bad");
                            break;

                        case Scores.GOOD:
                            Debug.Log("good");
                            break;

                        case Scores.GREAT:
                            Debug.Log("great");
                            break;

                        default:
                            Debug.Log("different score.." + s);
                            break;
                    }
                }
                //TODO: Add final score screen
            }
        }

        public void OnApplicationQuit()
        {
            selfPoseInputGetter.Dispose();

        }

        void AnimateSelf(PoseData live_data)
        {
            // MovePerson() considers which container to move
            foreach (AvatarContainer avatar in avatarListTarget)
            {
                avatar.MovePerson(live_data);
            }
        }
        // Animates all teacher avatars based on the JointData provided
        void AnimateTeacher(PoseData recorded_data)
        {
            foreach (AvatarContainer avatar in avatarListTest)
            {
                avatar.MovePerson(recorded_data);
            }
        }

        PoseGetter getPoseGetter(InputSource src)
        {

            PoseGetter poseGetter;

            switch (src)
            {

                case InputSource.KINECT:
                    poseGetter = new KinectPoseGetter() { VideoCube = videoCube };
                    break;
                case InputSource.FILE:
                    poseGetter = new FilePoseGetter(true) { ReadDataPath = test_file };
                    break;
                default:
                    poseGetter = new FilePoseGetter(true) { ReadDataPath = test_file };
                    break;
            }

            if (poseGetter != null)
            {
                Debug.Log("created posegetter: " + poseGetter);
                return poseGetter;
            }
            else
            {
                Debug.Log("Could not create posegetter.");
                return null;
            }

        }



    }


}
