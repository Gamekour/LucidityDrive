using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Animator))]
public class LucidVismodel : MonoBehaviour
{
    [HideInInspector]
    public Animator anim;

    [Header("Options")]
    [SerializeField] bool autoinit = true;
    [SerializeField] float grabspeed = 1;
    [SerializeField] float collisionTransitionTime = 1;

    [Header("References")]
    public Transform playerMeshParent;
    public BoxCollider bodyCollider;
    public SphereCollider headCollider;

    //internal variables
    private LucidAnimationModel modelSync;
    private float grabweightL = 0;
    private float grabweightR = 0;
    private bool initialized = false;
    private Quaternion hipDeriv = Quaternion.identity;
    private Quaternion headDeriv = Quaternion.identity;

    private void OnEnable()
    {
        anim = GetComponent<Animator>();
        modelSync = FindObjectOfType<LucidAnimationModel>();

        if (autoinit) StartCoroutine(InitDelay());
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (initialized && PlayerInfo.animModelInitialized)
            LocalCalc();
    }

    public void Init()
    {
        PlayerInfo.vismodelRef = this;
        PlayerInfo.OnAssignVismodel.Invoke(this);
        initialized = true;
    }

    private void OnDisable()
    {
        PlayerInfo.OnRemoveVismodel.Invoke();
    }

    //creates a pose for the visual model based on the playermodel's animation, active IK points, and collisions with the ground
    private void LocalCalc()
    {
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
            string hbstring = Enum.GetName(typeof(HumanBodyBones), hb2);
            if (modelSync.boneRots.ContainsKey(hbstring))
                anim.SetBoneLocalRotation(hb2, modelSync.boneRots[hbstring]);
        }
        bool enableFootIK = !(PlayerInfo.crawling || LucidInputValueShortcuts.crawl || LucidInputValueShortcuts.slide || PlayerInfo.airtime > modelSync.airtimeThreshold);
        float footIKWeight = enableFootIK ? 1 : 0;
        anim.SetIKPositionWeight(AvatarIKGoal.LeftFoot, footIKWeight);
        anim.SetIKPositionWeight(AvatarIKGoal.RightFoot, footIKWeight);
        anim.SetIKRotationWeight(AvatarIKGoal.LeftFoot, footIKWeight);
        anim.SetIKRotationWeight(AvatarIKGoal.RightFoot, footIKWeight);
        anim.SetIKPosition(AvatarIKGoal.LeftFoot, PlayerInfo.IK_LF.position);
        anim.SetIKRotation(AvatarIKGoal.LeftFoot, PlayerInfo.IK_LF.rotation);
        anim.SetIKPosition(AvatarIKGoal.RightFoot, PlayerInfo.IK_RF.position);
        anim.SetIKRotation(AvatarIKGoal.RightFoot, PlayerInfo.IK_RF.rotation);

        if (PlayerInfo.grabL || PlayerInfo.forceIK_LH)
        {
            anim.SetIKPosition(AvatarIKGoal.LeftHand, PlayerInfo.IK_LH.position + (PlayerInfo.mainBody.velocity * Time.fixedDeltaTime));
            anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, grabweightL);

            anim.SetIKRotation(AvatarIKGoal.LeftHand, PlayerInfo.IK_LH.rotation);
            anim.SetIKRotationWeight(AvatarIKGoal.LeftHand, grabweightL);

            grabweightL = Mathf.Clamp01(grabweightL + (grabspeed * Time.deltaTime));
        }
        else if (PlayerInfo.IK_LH.position != Vector3.zero)
        {
            anim.SetIKPosition(AvatarIKGoal.LeftHand, PlayerInfo.IK_LH.position);
            anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
            grabweightL = 0;
        }
        else
        {
            anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, grabweightL);
            anim.SetIKRotationWeight(AvatarIKGoal.LeftHand, grabweightL);
            grabweightL = Mathf.Clamp01(grabweightL - (grabspeed * Time.deltaTime));
        }
        if (PlayerInfo.grabR || PlayerInfo.forceIK_RH)
        {
            anim.SetIKPosition(AvatarIKGoal.RightHand, PlayerInfo.IK_RH.position + (PlayerInfo.mainBody.velocity * Time.fixedDeltaTime));
            anim.SetIKPositionWeight(AvatarIKGoal.RightHand, grabweightR);

            anim.SetIKRotation(AvatarIKGoal.RightHand, PlayerInfo.IK_RH.rotation);
            anim.SetIKRotationWeight(AvatarIKGoal.RightHand, grabweightR);

            grabweightR = Mathf.Clamp01(grabweightR + (grabspeed * Time.deltaTime));
        }
        else if (PlayerInfo.IK_RH.position != Vector3.zero)
        {
            anim.SetIKPosition(AvatarIKGoal.RightHand, PlayerInfo.IK_RH.position);
            anim.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);
            anim.SetIKRotation(AvatarIKGoal.RightHand, PlayerInfo.IK_RH.rotation);
            anim.SetIKRotationWeight(AvatarIKGoal.RightHand, 1);
            grabweightR = 0;
        }
        else
        {
            anim.SetIKPositionWeight(AvatarIKGoal.RightHand, grabweightR);
            anim.SetIKRotationWeight(AvatarIKGoal.RightHand, grabweightR);
            grabweightR = Mathf.Clamp01(grabweightR - (grabspeed * Time.deltaTime));
        }
    }

    IEnumerator InitDelay()
    {
        yield return new WaitForEndOfFrame();
        Init();
    }
}
