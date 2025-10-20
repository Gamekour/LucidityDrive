using UnityEngine;

namespace LucidityDrive
{
    public class Vismodel : MonoBehaviour
    {
        [HideInInspector]
        public Animator anim;

        [Header("Options")]
        public float stanceHeightFactor = 1;
        public float legWidth = 0.1f;
        public float pelvisScaleMult = 1;

        [SerializeField] GameObject[] FirstPersonOnly;
        [SerializeField] GameObject[] NonFirstPersonOnly;
        [SerializeField] float grabSpeed = 1;
        [SerializeField] bool initializeOnStart = true;

        private Transform headRef;
        private float grabWeightL, grabWeightR = 0;
        private bool initialized = false;
        private Vector3 defaultHeadScale;

        private void OnEnable()
        {
            LucidPlayerInfo.onChangeCameraPoint.AddListener(OnCameraPointChanged);
        }

        private void OnDisable()
        {
            if (!initialized) return;

            LucidPlayerInfo.OnRemoveVismodel.Invoke();
            LucidPlayerInfo.vismodelRef = null;
            LucidPlayerInfo.onChangeCameraPoint.RemoveListener(OnCameraPointChanged);
        }

        private void Start()
        {
            anim = GetComponent<Animator>();

            if (initializeOnStart) Init();
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (!initialized) return;
            if (LucidPlayerInfo.animModelInitialized)
                LocalCalc();
        }

        public void Init()
        {
            if (anim.runtimeAnimatorController == null)
                Debug.LogError("Vismodel is missing an animator controller - make sure the runtime animator controller is set to blankAnim (see example in plugin folder)");
            headRef = anim.GetBoneTransform(HumanBodyBones.Head);
            defaultHeadScale = headRef.localScale;
            foreach (SkinnedMeshRenderer smr in GetComponentsInChildren<SkinnedMeshRenderer>())
                smr.updateWhenOffscreen = true;
            initialized = true;
        }

        private void OnCameraPointChanged(int index)
        {
            if (!initialized) return;

            if (LucidPlayerInfo.inFirstPerson)
                headRef.localScale = Vector3.one * 0.001f;
            else
                headRef.localScale = defaultHeadScale;

            foreach (GameObject g in FirstPersonOnly)
                g.SetActive(LucidPlayerInfo.inFirstPerson);
            foreach (GameObject g in NonFirstPersonOnly)
                g.SetActive(!LucidPlayerInfo.inFirstPerson);
        }

        private void FixedUpdate()
        {
            if (initialized && LucidPlayerInfo.vismodelRef == null)
            {
                if (anim.isInitialized)
                {
                    LucidPlayerInfo.vismodelRef = this;
                    LucidPlayerInfo.OnAssignVismodel.Invoke(this);
                }
            }
        }

        //creates a pose for the visual model based on the playermodel's animation, active IK points, and collisions with the ground
        private void LocalCalc()
        {
            if (LucidPlayerInfo.animationModel == null) return;

            Transform animHips = LucidPlayerInfo.animationModel.GetBoneTransform(HumanBodyBones.Hips);

            Transform visHips = anim.GetBoneTransform(HumanBodyBones.Hips);

            Vector3 offset = animHips.position - visHips.position;
            transform.position += offset;
            Quaternion qAnimHips = LucidPlayerInfo.animationModel.bodyRotation;
            Quaternion qAnimHead = LucidPlayerInfo.head.rotation;

            anim.SetBoneLocalRotation(HumanBodyBones.Hips, qAnimHips);
            anim.SetBoneLocalRotation(HumanBodyBones.Head, qAnimHead);

            foreach (HumanBodyBones hb2 in LucidShortcuts.hb2list)
            {
                string hbstring = LucidShortcuts.boneNames[hb2];
                if (AnimationModel.instance.boneRots.ContainsKey(hbstring))
                    anim.SetBoneLocalRotation(hb2, AnimationModel.instance.boneRots[hbstring]);
            }
            bool doSlideIK = LucidPlayerInfo.surfaceAngle < LucidPlayerInfo.slidePushAngleThreshold;
            bool isSliding = LucidInputValueShortcuts.bslide || LucidInputValueShortcuts.slide;
            bool enableFootIK = !(LucidPlayerInfo.crawling || (isSliding && !doSlideIK));
            float footIKWeight = enableFootIK ? 1 : 0;
            anim.SetIKPositionWeight(AvatarIKGoal.LeftFoot, footIKWeight);
            anim.SetIKPositionWeight(AvatarIKGoal.RightFoot, footIKWeight);
            anim.SetIKRotationWeight(AvatarIKGoal.LeftFoot, footIKWeight);
            anim.SetIKRotationWeight(AvatarIKGoal.RightFoot, footIKWeight);
            anim.SetIKPosition(AvatarIKGoal.LeftFoot, LucidPlayerInfo.IK_LF.position);
            anim.SetIKRotation(AvatarIKGoal.LeftFoot, LucidPlayerInfo.IK_LF.rotation);
            anim.SetIKPosition(AvatarIKGoal.RightFoot, LucidPlayerInfo.IK_RF.position);
            anim.SetIKRotation(AvatarIKGoal.RightFoot, LucidPlayerInfo.IK_RF.rotation);

            CalculateHandIK(false);
            CalculateHandIK(true);
        }

        private void CalculateHandIK(bool isRight)
        {
            bool grab = isRight ? LucidPlayerInfo.grabR : LucidPlayerInfo.grabL;
            bool handCollision = isRight ? LucidPlayerInfo.handCollisionR : LucidPlayerInfo.handCollisionL;
            Transform IKTransform = isRight ? LucidPlayerInfo.IK_RH : LucidPlayerInfo.IK_LH;
            AvatarIKGoal IKGoal = isRight ? AvatarIKGoal.RightHand : AvatarIKGoal.LeftHand;
            float grabweight = isRight ? grabWeightR : grabWeightL;

            if (grab || handCollision)
            {
                anim.SetIKPosition(IKGoal, IKTransform.position);
                anim.SetIKPositionWeight(IKGoal, grabweight);

                anim.SetIKRotation(IKGoal, IKTransform.rotation);
                anim.SetIKRotationWeight(IKGoal, grabweight);

                SetGrabWeight(isRight, Mathf.Clamp01(grabweight + (grabSpeed * Time.deltaTime)));
            }
            else if (IKTransform.position != Vector3.zero)
            {
                anim.SetIKPosition(IKGoal, IKTransform.position);
                anim.SetIKPositionWeight(IKGoal, 1);
                SetGrabWeight(isRight, 0);
            }
            else
            {
                anim.SetIKPositionWeight(IKGoal, grabweight);
                anim.SetIKRotationWeight(IKGoal, grabweight);
                SetGrabWeight(isRight, Mathf.Clamp01(grabweight - (grabSpeed * Time.deltaTime)));
            }
        }

        private void SetGrabWeight(bool isRight, float value)
        {
            if (isRight)
                grabWeightR = value;
            else
                grabWeightL = value;
        }
    }
}