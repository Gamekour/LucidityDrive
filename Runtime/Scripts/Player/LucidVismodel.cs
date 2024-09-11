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
    [SerializeField] float collisionTransitionSpeed = 1;

    [Header("References")]
    [SerializeField] Transform playerMeshParent;

    private LucidAnimationModel modelSync;
    private Dictionary<string, Quaternion> localBoneRots;
    private float grabweightL = 0;
    private float grabweightR = 0;
    private float t = 0;
    private bool initialized = false;

    private void OnEnable()
    {
        anim = GetComponent<Animator>();
        modelSync = FindObjectOfType<LucidAnimationModel>();

        if (autoinit) StartCoroutine(InitDelay());
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (initialized)
            LocalCalc();
    }

    public void Init()
    {
        PlayerInfo.vismodelRef = this;
        PlayerInfo.OnAssignVismodel.Invoke(this);
        initialized = true;
    }

    //creates a pose for the visual model based on the playermodel's animation, active IK points, and collisions with the ground
    private void LocalCalc()
    {
        Vector3 offset = PlayerInfo.playermodelAnim.GetBoneTransform(HumanBodyBones.Hips).position - anim.GetBoneTransform(HumanBodyBones.Hips).position;
        transform.position += offset;
        Quaternion qPhysHips = PlayerInfo.physHips.transform.rotation;
        Quaternion qPhysHead = PlayerInfo.physHead.transform.rotation;
        Quaternion qAnimHips = PlayerInfo.playermodelAnim.bodyRotation;
        Quaternion qAnimHead = PlayerInfo.head.rotation;


        if (PlayerInfo.physCollision)
        {
            t = Mathf.Clamp01(t + (Time.deltaTime * collisionTransitionSpeed));
        }
        else
            t = Mathf.Clamp01(t + (Time.deltaTime * collisionTransitionSpeed));

        anim.SetBoneLocalRotation(HumanBodyBones.Hips, Quaternion.Lerp(qAnimHips, qPhysHips, t));
        anim.SetBoneLocalRotation(HumanBodyBones.Head, Quaternion.Lerp(qAnimHead, qPhysHead, t));

        foreach (HumanBodyBones hb2 in Shortcuts.hb2list)
        {
            string hbstring = Enum.GetName(typeof(HumanBodyBones), hb2);
            if (modelSync.boneRots.ContainsKey(hbstring))
                anim.SetBoneLocalRotation(hb2, modelSync.boneRots[hbstring]);
        }
        if (!PlayerInfo.crawling && !LucidInputValueShortcuts.crawl && !LucidInputValueShortcuts.slide)
        {
            anim.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1);
            anim.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1);
            anim.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 1);
            anim.SetIKRotationWeight(AvatarIKGoal.RightFoot, 1);
        }
        else
        {
            anim.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 0);
            anim.SetIKPositionWeight(AvatarIKGoal.RightFoot, 0);
            anim.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 0);
            anim.SetIKRotationWeight(AvatarIKGoal.RightFoot, 0);
        }
        anim.SetIKPosition(AvatarIKGoal.LeftFoot, modelSync.LCast);
        anim.SetIKRotation(AvatarIKGoal.LeftFoot, PlayerInfo.hipspace.rotation);
        anim.SetIKPosition(AvatarIKGoal.RightFoot, modelSync.RCast);
        anim.SetIKRotation(AvatarIKGoal.RightFoot, PlayerInfo.hipspace.rotation);

        if (PlayerInfo.grabL && PlayerInfo.climbtargetL != null)
        {
            anim.SetIKPosition(AvatarIKGoal.LeftHand, PlayerInfo.climbtargetL.position + (PlayerInfo.mainBody.velocity * Time.fixedDeltaTime));
            anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, grabweightL);

            anim.SetIKRotation(AvatarIKGoal.LeftHand, PlayerInfo.climbtargetL.rotation);
            anim.SetIKRotationWeight(AvatarIKGoal.LeftHand, grabweightL);

            grabweightL = Mathf.Clamp01(grabweightL + (grabspeed * Time.deltaTime));
        }
        else if (PlayerInfo.handTargetL != Vector3.zero)
        {
            anim.SetIKPosition(AvatarIKGoal.LeftHand, PlayerInfo.handTargetL);
            anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
            grabweightL = 0;
        }
        else
        {
            anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, grabweightL);
            anim.SetIKRotationWeight(AvatarIKGoal.LeftHand, grabweightL);
            grabweightL = Mathf.Clamp01(grabweightL - (grabspeed * Time.deltaTime));
        }
        if (PlayerInfo.grabR && PlayerInfo.climbtargetR != null)
        {
            anim.SetIKPosition(AvatarIKGoal.RightHand, PlayerInfo.climbtargetR.position + (PlayerInfo.mainBody.velocity * Time.fixedDeltaTime));
            anim.SetIKPositionWeight(AvatarIKGoal.RightHand, grabweightR);

            anim.SetIKRotation(AvatarIKGoal.RightHand, PlayerInfo.climbtargetR.rotation);
            anim.SetIKRotationWeight(AvatarIKGoal.RightHand, grabweightR);

            grabweightR = Mathf.Clamp01(grabweightR + (grabspeed * Time.deltaTime));
        }
        else if (PlayerInfo.handTargetR != Vector3.zero)
        {
            anim.SetIKPosition(AvatarIKGoal.RightHand, PlayerInfo.handTargetR);
            anim.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);
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
