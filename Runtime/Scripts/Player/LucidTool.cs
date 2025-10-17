using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace LucidityDrive
{
    [RequireComponent(typeof(Rigidbody))]
    public class LucidTool : MonoBehaviour
    {
        public Transform targetTransform;
        public Transform itemPoses;
        public Transform PrimaryGrip, SecondaryGrip;
        public Transform ItemPosePrimary, ItemPoseSecondary;
        public GameObject DisableIfNotPriority;
        public bool switchGrabOrder = false;
        public bool disableDrop = false;
        public bool autoGrabL, autoGrabR = false;
        public bool queueForceGrabPrimary, queueForceGrabSecondary = false;
        public bool queueForceGrabAnimationPrimary, queueForceGrabAnimationSecondary = false;
        public bool queueUngrabPrimary, queueUngrabSecondary = false;
        [Header("Optional")]
        public Rigidbody animationTargetPrimary, animationTargetSecondary;
        private Vector3 posOffsetPrimary, posOffsetSecondary;
        private Quaternion rotOffsetPrimary, rotOffsetSecondary;
        private Rigidbody rb;

        [HideInInspector]
        public bool held, leftHanded, isPriority = false;

        public UnityEvent OnUse;
        public UnityEvent OnUseUp;
        public UnityEvent OnGrab;
        public UnityEvent OnDrop;

        public void OnEnable()
        {
            StartCoroutine(WaitForInit());
            posOffsetPrimary = transform.InverseTransformPoint(PrimaryGrip.position);
            posOffsetSecondary = transform.InverseTransformPoint(SecondaryGrip.position);
            rotOffsetPrimary = Quaternion.Inverse(transform.rotation) * PrimaryGrip.rotation;
            rotOffsetSecondary = Quaternion.Inverse(transform.rotation) * SecondaryGrip.rotation;
            rb = GetComponent<Rigidbody>();
        }

        public void OnDisable()
        {
            StopAllCoroutines();
        }

        public void FixedUpdate()
        {
            if (targetTransform != null)
            {

                ItemPosePrimary.position = targetTransform.TransformPoint(posOffsetPrimary);
                ItemPosePrimary.rotation = targetTransform.rotation * rotOffsetPrimary;

                ItemPoseSecondary.position = targetTransform.TransformPoint(posOffsetSecondary);
                ItemPoseSecondary.rotation = targetTransform.rotation * rotOffsetSecondary;
            }

            bool queueGrabActionPrimary = (queueForceGrabPrimary || queueForceGrabAnimationPrimary || queueUngrabPrimary);
            bool queueGrabActionSecondary = (queueForceGrabSecondary || queueForceGrabAnimationSecondary || queueUngrabSecondary);

            if (queueForceGrabPrimary)
                ForceGrab(true);
            else if (queueForceGrabAnimationPrimary)
                ForceGrabAnimationTransform(true);
            else if (queueUngrabPrimary)
                ForceUngrab(true);

            if (queueForceGrabSecondary)
                ForceGrab(false);
            else if (queueForceGrabAnimationSecondary)
                ForceGrabAnimationTransform(false);
            else if (queueUngrabSecondary)
                ForceUngrab(false);

            if (queueGrabActionPrimary)
            {
                queueForceGrabPrimary = false;
                queueForceGrabAnimationPrimary = false;
                queueUngrabPrimary = false;
            }
            if (queueGrabActionSecondary)
            {
                queueForceGrabSecondary = false;
                queueForceGrabAnimationSecondary = false;
                queueUngrabSecondary = false;
            }
        }

        public void UpdatePriority(bool priority)
        {
            isPriority = priority;
            DisableIfNotPriority.SetActive(priority);
        }

        IEnumerator WaitForInit()
        {
            while (!LucidPlayerInfo.animModelInitialized || LucidPlayerInfo.pelvis == null || !LucidArms.instance.initialized)
                yield return null;
            LucidArms la = LucidArms.instance;

            if (autoGrabL || autoGrabR)
            {
                la.ForceUngrab(true);
                la.ForceUngrab(false);
            }
            if (!switchGrabOrder)
            {
                if (autoGrabR)
                {
                    transform.position = LucidPlayerInfo.pelvis.position;
                    la.ForceGrab(rb, true);
                }
                if (autoGrabL)
                {
                    transform.position = LucidPlayerInfo.pelvis.position;
                    la.ForceGrab(rb, false);
                }
            }
            else
            {
                if (autoGrabL)
                {
                    transform.position = LucidPlayerInfo.pelvis.position;
                    la.ForceGrab(rb, false);
                }
                if (autoGrabR)
                {
                    transform.position = LucidPlayerInfo.pelvis.position;
                    la.ForceGrab(rb, true);
                }
            }
        }

        public void ForceUngrab(bool isPrimary) => LucidArms.instance.ForceUngrab(leftHanded ? !isPrimary : isPrimary);
        public void ForceGrab(bool isPrimary) => LucidArms.instance.ForceGrab(rb, leftHanded ? !isPrimary : isPrimary);
        public void ForceGrabAnimationTransform(bool isPrimary)
        {
            bool isRight = leftHanded ? !isPrimary : isPrimary;
            Rigidbody targetRigidbody = isPrimary ? animationTargetPrimary : animationTargetSecondary;
            LucidArms.instance.ForceGrab(targetRigidbody, isRight, true);
        }
    }
}