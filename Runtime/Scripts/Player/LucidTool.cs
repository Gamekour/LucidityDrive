using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace LucidityDrive
{
    public class LucidTool : MonoBehaviour
    {
        public Transform itemPosesR;
        public Transform itemPosesL;
        public Transform PrimaryGripR;
        public Transform SecondaryGripR;
        public Transform targetTransform;
        public Transform ItemPosePrimaryR;
        public Transform ItemPoseSecondaryR;
        public bool GrabLockPrimary = true;
        public bool GrabLockSecondary = false;
        public bool autoGrabL = false;
        public bool autoGrabR = false;
        public bool switchGrabOrder = false;
        public bool disableDrop = false;
        private Vector3 posOffsetPrimaryR;
        private Vector3 posOffsetSecondaryR;
        private Quaternion rotOffsetPrimaryR;
        private Quaternion rotOffsetSecondaryR;

        [HideInInspector]
        public bool held = false;

        public UnityEvent OnUse;
        public UnityEvent OnUseUp;
        public UnityEvent OnGrab;
        public UnityEvent OnDrop;

        public void OnEnable()
        {
            StartCoroutine(WaitForInit());
            posOffsetPrimaryR = transform.InverseTransformPoint(PrimaryGripR.position);
            posOffsetSecondaryR = transform.InverseTransformPoint(SecondaryGripR.position);
            rotOffsetPrimaryR = Quaternion.Inverse(transform.rotation) * PrimaryGripR.rotation;
            rotOffsetSecondaryR = Quaternion.Inverse(transform.rotation) * SecondaryGripR.rotation;
        }

        public void OnDisable()
        {
            StopAllCoroutines();
        }

        public void FixedUpdate()
        {
            if (targetTransform != null)
            {

                ItemPosePrimaryR.position = targetTransform.TransformPoint(posOffsetPrimaryR);
                ItemPosePrimaryR.rotation = targetTransform.rotation * rotOffsetPrimaryR;

                ItemPoseSecondaryR.position = targetTransform.TransformPoint(posOffsetSecondaryR);
                ItemPoseSecondaryR.rotation = targetTransform.rotation * rotOffsetSecondaryR;
            }
        }

        IEnumerator WaitForInit()
        {
            LucidArms la = FindObjectOfType<LucidArms>();
            while (!LucidPlayerInfo.animModelInitialized || LucidPlayerInfo.pelvis == null || !LucidArms.instance.initialized)
                yield return null;

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
                    la.ForceGrab(this, true);
                }
                if (autoGrabL)
                {
                    transform.position = LucidPlayerInfo.pelvis.position;
                    la.ForceGrab(this, false);
                }
            }
            else
            {
                if (autoGrabL)
                {
                    transform.position = LucidPlayerInfo.pelvis.position;
                    la.ForceGrab(this, false);
                }
                if (autoGrabR)
                {
                    transform.position = LucidPlayerInfo.pelvis.position;
                    la.ForceGrab(this, true);
                }
            }
        }
    }
}