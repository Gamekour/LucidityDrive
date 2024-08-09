using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Animator))]
public class vismodel : MonoBehaviour
{
    public UnityEvent UpdateBoneRoots = new UnityEvent();
    [HideInInspector]
    public Animator anim;

    [Header("Options")]
    [SerializeField] bool autoinit = true;
    [SerializeField] float grabspeed = 1;

    [Header("References")]
    [SerializeField] Transform playerMeshParent;
    [SerializeField] Transform armatureRoot;
    [SerializeField] Renderer[] colorableClothes;
    [SerializeField] Transform[] IKTransforms;
    [SerializeField] Vector3[] IKOffsetEulers;

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

    private void FixedUpdate()
    {
        if(initialized)
            UpdateBoneRoots.Invoke();
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (initialized)
            LocalCalc();
        else if (IKTransforms.Length > 0)
            ObserverIK();
    }

    public void UnhideFirstPerson()
    {
        foreach (Transform t in playerMeshParent.GetComponentsInChildren<Transform>())
        {
            if (t.gameObject.layer == 17)
                t.gameObject.layer = 0;
        }
    }

    public void InitLocal()
    {
        foreach (Transform t in playerMeshParent.GetComponentsInChildren<Transform>())
        {
            if (t.gameObject.layer != 17)
                t.gameObject.layer = 15;
        }
    }

    public void InitOwner()
    {
        PlayerInfo.vismodelRef = this;
        localBoneRots = modelSync.boneRots;
        PlayerInfo.OnAssignVismodel.Invoke(this);
        foreach (Collider c in armatureRoot.GetComponentsInChildren<Collider>())
            c.gameObject.layer = 10;
        Cursor.lockState = CursorLockMode.Locked;

        initialized = true;
    }

    private void LocalCalc()
    {
        Vector3 offset = PlayerInfo.pelvis.position - anim.GetBoneTransform(HumanBodyBones.Hips).position;
        transform.position += offset;
        anim.SetBoneLocalRotation(HumanBodyBones.Hips, PlayerInfo.physHips.transform.rotation);
        //anim.SetBoneLocalRotation(HumanBodyBones.Head, PlayerInfo.physHead.rotation);
        foreach (HumanBodyBones hb2 in Shortcuts.hb2list)
        {
            string hbstring = Enum.GetName(typeof(HumanBodyBones), hb2);
            if (localBoneRots.ContainsKey(hbstring))
                anim.SetBoneLocalRotation(hb2, localBoneRots[hbstring]);
        }
        if (!PlayerInfo.crawling && InputManager.crawl.ReadValue<float>() == 0 && InputManager.slide.ReadValue<float>() == 0)
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

    private void ObserverIK()
    {
        anim.SetIKPosition(AvatarIKGoal.LeftHand, IKTransforms[0].position);
        anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
        anim.SetIKRotation(AvatarIKGoal.LeftHand, IKTransforms[0].rotation * Quaternion.Euler(IKOffsetEulers[0]));
        anim.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1);

        anim.SetIKPosition(AvatarIKGoal.RightHand, IKTransforms[1].position);
        anim.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);
        anim.SetIKRotation(AvatarIKGoal.RightHand, IKTransforms[1].rotation * Quaternion.Euler(IKOffsetEulers[1]));
        anim.SetIKRotationWeight(AvatarIKGoal.RightHand, 1);

        anim.SetIKPosition(AvatarIKGoal.LeftFoot, IKTransforms[2].position);
        anim.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1);
        anim.SetIKRotation(AvatarIKGoal.LeftFoot, IKTransforms[2].rotation * Quaternion.Euler(IKOffsetEulers[2]));
        anim.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 1);

        anim.SetIKPosition(AvatarIKGoal.RightFoot, IKTransforms[3].position);
        anim.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1);
        anim.SetIKRotation(AvatarIKGoal.RightFoot, IKTransforms[3].rotation * Quaternion.Euler(IKOffsetEulers[3]));
        anim.SetIKRotationWeight(AvatarIKGoal.RightFoot, 1);

        anim.SetBoneLocalRotation(HumanBodyBones.Hips, IKTransforms[4].transform.rotation);

        anim.SetLookAtWeight(1);
        anim.SetLookAtPosition(IKTransforms[5].transform.position);

    }

    public void ChangeClothingColor(Color color)
    {
        foreach(Renderer r in colorableClothes)
        {
            r.material.color = color;
        }
    }

    IEnumerator InitDelay()
    {
        yield return new WaitForEndOfFrame();
        InitOwner();
        InitLocal();
    }
}
