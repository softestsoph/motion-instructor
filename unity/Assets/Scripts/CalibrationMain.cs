using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;

namespace PoseTeacher
{

    public class CalibrationMain : MonoBehaviour
    {
        public static CalibrationMain Instance;

        PoseGetter selfPoseInputGetter;

        public GameObject videoCube;

        public GameObject avatarContainerSelf;
        List<AvatarContainer> avatarListSelf;

        private readonly string fake_file = "jsondata/2020_05_27-00_01_59.txt";
        public InputSource selfPoseInputSource = InputSource.KINECT;

        public bool paused = false;

        public PoseData currentSelfPose;


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
            avatarListSelf = new List<AvatarContainer>();
            avatarListSelf.Add(new AvatarContainer(avatarContainerSelf));

            selfPoseInputGetter = getPoseGetter(selfPoseInputSource);
        }

        // Update is called once per frame
        public void Update()
        {
            currentSelfPose = selfPoseInputGetter.GetNextPose();
            AnimateSelf(currentSelfPose);
        }

        public void OnApplicationQuit()
        {
            selfPoseInputGetter.Dispose();
        }

        void AnimateSelf(PoseData live_data)
        {
            // MovePerson() considers which container to move
            foreach (AvatarContainer avatar in avatarListSelf)
            {
                avatar.MovePerson(live_data);
            }
        }

        PoseGetter getPoseGetter(InputSource src)
        {
            switch (src)
            {
                case InputSource.KINECT:
                    return new KinectPoseGetter() { VideoCube = videoCube };
                case InputSource.FILE:
                    return new FilePoseGetter(true) { ReadDataPath = fake_file };
                default:
                    return new FilePoseGetter(true) { ReadDataPath = fake_file };
            }
        }
    }
}