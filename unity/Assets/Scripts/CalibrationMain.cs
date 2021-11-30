using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using TMPro;

namespace PoseTeacher
{

    public class CalibrationMain : MonoBehaviour
    {
        public static CalibrationMain Instance;

        PoseGetter selfPoseInputGetter;

        public GameObject videoCube;
        public GameObject cube;
        public GameObject text;
        private TextMeshPro textMesh;

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

            textMesh = text.GetComponent<TextMeshPro>();
            textMesh.text = "Go to the cube.";
        }

        // Update is called once per frame
        public void Update()
        {
            currentSelfPose = selfPoseInputGetter.GetNextPose();
            AnimateSelf(currentSelfPose);
            //Debug.Log(avatarListSelf[0].stickContainer.GetReferencePosition());
            check_if_hit_cube();
            
        }

        public void check_if_hit_cube()
        {
            Vector3 cube_position = cube.transform.position;
            Vector3 own_position = avatarListSelf[0].stickContainer.GetReferencePosition();
            bool x_hit = Mathf.Abs(cube_position.x - own_position.x) <= 0.1;
            bool y_hit = Mathf.Abs(cube_position.y - own_position.y) <= 0.2;
            bool z_hit = Mathf.Abs(cube_position.z - own_position.z) <= 0.1;
            Debug.Log("x: " + Mathf.Abs(cube_position.x - own_position.x));
            Debug.Log("y: " + Mathf.Abs(cube_position.y - own_position.y));
            Debug.Log("z: " + Mathf.Abs(cube_position.z - own_position.z));

            if (x_hit && y_hit && z_hit)
            {
                textMesh.text = "Well done!";
            }

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