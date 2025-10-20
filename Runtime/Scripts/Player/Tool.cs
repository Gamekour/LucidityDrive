using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace LucidityDrive
{
    [RequireComponent(typeof(Rigidbody))]
    public class Tool : MonoBehaviour
    {
        [Tooltip("Transform the tool will try to match")]
        public Transform targetTransform;
        [Tooltip("Root transform for item poses")]
        public Transform itemPoses;
        [Tooltip("Grab point for the primary holding hand, relative to the tool transform")]
        public Transform PrimaryGrip;
        [Tooltip("Grab point for the secondary holding hand, relative to the tool transform")]
        public Transform SecondaryGrip;
        [Tooltip("Target Pose for primary holding hand")]
        public Transform ItemPosePrimary;
        [Tooltip("Target Pose for secondary holding hand")]
        public Transform ItemPoseSecondary;
        [Tooltip("Objects to disable if this tool is not priority")]
        public GameObject DisableIfNotPriority;
        [Tooltip("Enable this to switch the grab order when grabbing with both hands")]
        public bool switchGrabOrder = false;
        [Tooltip("Enable this to prevent the player from dropping this tool")]
        public bool disableDrop = false;
        [Tooltip("Automatically grab the tool with the left hand when enabled")]
        public bool autoGrabL = false;
        [Tooltip("Automatically grab the tool with the right hand when enabled")]
        public bool autoGrabR = false;
        [Tooltip("Queue a forced grab, useful for animations")]
        public bool queueForceGrabPrimary, queueForceGrabSecondary = false;
        [Tooltip("Queue a forced animation grab, useful for animations involving sub-parts like ammo magazines")]
        public bool queueForceGrabAnimationPrimary, queueForceGrabAnimationSecondary = false;
        [Tooltip("Queue a forced ungrab, useful for animations")]
        public bool queueUngrabPrimary, queueUngrabSecondary = false;
        [Tooltip("(Optional) Rigidbody to follow when doing a Grab Animation")]
        public Rigidbody animationTargetPrimary, animationTargetSecondary;

        [Tooltip("Triggered when the grab input is invoked on the same hand holding this tool")]
        public UnityEvent OnUse;
        [Tooltip("Triggered when the grab input is cancelled on the same hand holding this tool")]
        public UnityEvent OnUseUp;
        [Tooltip("Triggered when this tool is grabbed")]
        public UnityEvent OnGrab;
        [Tooltip("Triggered when this tool is dropped")]
        public UnityEvent OnDrop;

        [HideInInspector]
        public bool held, leftHanded, isPriority = false;

        private Vector3 posOffsetPrimary, posOffsetSecondary;
        private Quaternion rotOffsetPrimary, rotOffsetSecondary;
        private Rigidbody rb;

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
            while (!PlayerInfo.animModelInitialized || PlayerInfo.pelvis == null || !Arms.instance.initialized)
                yield return null;
            Arms la = Arms.instance;

            if (autoGrabL || autoGrabR)
            {
                la.ForceUngrab(true);
                la.ForceUngrab(false);
            }
            if (!switchGrabOrder)
            {
                if (autoGrabR)
                {
                    transform.position = PlayerInfo.pelvis.position;
                    la.ForceGrab(rb, true);
                }
                if (autoGrabL)
                {
                    transform.position = PlayerInfo.pelvis.position;
                    la.ForceGrab(rb, false);
                }
            }
            else
            {
                if (autoGrabL)
                {
                    transform.position = PlayerInfo.pelvis.position;
                    la.ForceGrab(rb, false);
                }
                if (autoGrabR)
                {
                    transform.position = PlayerInfo.pelvis.position;
                    la.ForceGrab(rb, true);
                }
            }
        }

        public void ForceUngrab(bool isPrimary) => Arms.instance.ForceUngrab(leftHanded ? !isPrimary : isPrimary);
        public void ForceGrab(bool isPrimary) => Arms.instance.ForceGrab(rb, leftHanded ? !isPrimary : isPrimary);
        public void ForceGrabAnimationTransform(bool isPrimary)
        {
            bool isRight = leftHanded ? !isPrimary : isPrimary;
            Rigidbody targetRigidbody = isPrimary ? animationTargetPrimary : animationTargetSecondary;
            Arms.instance.ForceGrab(targetRigidbody, isRight, true);
        }
    }
}