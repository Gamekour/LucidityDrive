using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Animator))]
public class vismodel : MonoBehaviour
{
    [HideInInspector]
    public Animator anim;

    [Header("Options")]
    [SerializeField] bool autoinit = true;
    [SerializeField] float grabspeed = 1;

    [Header("References")]
    [SerializeField] Transform playerMeshParent;

    private modelsync modelSync;
    private Dictionary<string, Quaternion> localBoneRots;
    private float grabweightL = 0;
    private float grabweightR = 0;
    private bool initialized = false;

    private void OnEnable()
    {
        anim = GetComponent<Animator>();
        modelSync = FindObjectOfType<modelsync>();

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
        Vector3 offset = PlayerInfo.pelvis.position - anim.GetBoneTransform(HumanBodyBones.Hips).position;
        transform.position += offset;
        anim.SetBoneLocalRotation(HumanBodyBones.Hips, PlayerInfo.physHips.transform.rotation);
        foreach (HumanBodyBones hb2 in Shortcuts.hb2list)
        {
            string hbstring = Enum.GetName(typeof(HumanBodyBones), hb2);
            if (modelSync.boneRots.ContainsKey(hbstring))
                anim.SetBoneLocalRotation(hb2, modelSync.boneRots[hbstring]);
        }
        if (!PlayerInfo.crawling && LucidInputActionRefs.crawl.ReadValue<float>() == 0 && LucidInputActionRefs.slide.ReadValue<float>() == 0)
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
