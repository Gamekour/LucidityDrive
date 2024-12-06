using UnityEngine;

[RequireComponent(typeof(Animator))]
public class LucidVismodel : MonoBehaviour
{
    [HideInInspector]
    public Animator anim;

    [Header("Options")]
    [SerializeField] bool initializeOnStart = true;
    [SerializeField] float grabSpeed = 1;
    [SerializeField] float collisionTransitionTime = 1;

    [Header("References")]
    public Transform playerMeshParent;
    public BoxCollider bodyCollider;
    public SphereCollider headCollider;

    private LucidAnimationModel modelSync;
    private float grabWeightL, grabWeightR = 0;
    private bool initialized = false;
    private Quaternion hipDeriv = Quaternion.identity;
    private Quaternion headDeriv = Quaternion.identity;

    private void Start()
    {
        anim = GetComponent<Animator>();
        modelSync = FindObjectOfType<LucidAnimationModel>();

        if (initializeOnStart) Init();
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (initialized && PlayerInfo.animModelInitialized)
            LocalCalc();
    }

    public void Init()
    {
        initialized = true;
    }

    private void OnDisable()
    {
        PlayerInfo.OnRemoveVismodel.Invoke();
    }

    private void FixedUpdate()
    {
        if (initialized && PlayerInfo.vismodelRef == null)
        {
            if (anim.isInitialized)
            {
                PlayerInfo.vismodelRef = this;
                PlayerInfo.OnAssignVismodel.Invoke(this);
            }
        }
    }

    //creates a pose for the visual model based on the playermodel's animation, active IK points, and collisions with the ground
    private void LocalCalc()
    {
        if (PlayerInfo.playermodelAnim == null) return;

        Transform tHips = PlayerInfo.playermodelAnim.GetBoneTransform(HumanBodyBones.Hips);

        Vector3 offset = tHips.position - anim.GetBoneTransform(HumanBodyBones.Hips).position;
        transform.position += offset;
        Quaternion qPhysHips = PlayerInfo.physBody.transform.rotation;
        Quaternion qPhysHead = PlayerInfo.physHead.transform.rotation;
        Quaternion qAnimHips = PlayerInfo.playermodelAnim.bodyRotation;
        Quaternion qAnimHead = PlayerInfo.head.rotation;

        Quaternion qHips2 = PlayerInfo.physCollision ? qAnimHips : qPhysHips;
        Quaternion qHips1 = PlayerInfo.physCollision ? qPhysHips : qAnimHips;
        Quaternion qHead2 = PlayerInfo.physCollision ? qAnimHead : qPhysHead;
        Quaternion qHead1 = PlayerInfo.physCollision ? qPhysHead : qAnimHead;

        anim.SetBoneLocalRotation(HumanBodyBones.Hips, QuaternionUtil.SmoothDamp(qHips1, qHips2, ref hipDeriv, collisionTransitionTime));
        anim.SetBoneLocalRotation(HumanBodyBones.Head, QuaternionUtil.SmoothDamp(qHead1, qHead2, ref headDeriv, collisionTransitionTime));

        foreach (HumanBodyBones hb2 in Shortcuts.hb2list)
        {
            string hbstring = Shortcuts.boneNames[hb2];
            if (modelSync.boneRots.ContainsKey(hbstring))
                anim.SetBoneLocalRotation(hb2, modelSync.boneRots[hbstring]);
        }
        bool doSlideIK = PlayerInfo.surfaceAngle < PlayerInfo.slidePushAngleThreshold;
        bool isSliding = LucidInputValueShortcuts.bslide || LucidInputValueShortcuts.slide;
        bool enableFootIK = !(PlayerInfo.crawling || (isSliding && !doSlideIK));
        float footIKWeight = enableFootIK ? 1 : 0;
        anim.SetIKPositionWeight(AvatarIKGoal.LeftFoot, footIKWeight);
        anim.SetIKPositionWeight(AvatarIKGoal.RightFoot, footIKWeight);
        anim.SetIKRotationWeight(AvatarIKGoal.LeftFoot, footIKWeight);
        anim.SetIKRotationWeight(AvatarIKGoal.RightFoot, footIKWeight);
        anim.SetIKPosition(AvatarIKGoal.LeftFoot, PlayerInfo.IK_LF.position);
        anim.SetIKRotation(AvatarIKGoal.LeftFoot, PlayerInfo.IK_LF.rotation);
        anim.SetIKPosition(AvatarIKGoal.RightFoot, PlayerInfo.IK_RF.position);
        anim.SetIKRotation(AvatarIKGoal.RightFoot, PlayerInfo.IK_RF.rotation);

        CalculateHandIK(false);
        CalculateHandIK(true);
    }

    private void CalculateHandIK(bool isRight)
    {
        bool grab = isRight ? PlayerInfo.grabR : PlayerInfo.grabL;
        bool handCollision = isRight ? PlayerInfo.handCollisionR : PlayerInfo.handCollisionL;
        Transform IKTransform = isRight ? PlayerInfo.IK_RH : PlayerInfo.IK_LH;
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
