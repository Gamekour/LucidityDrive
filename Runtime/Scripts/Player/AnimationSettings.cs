using UnityEngine;

namespace LucidityDrive
{
    [CreateAssetMenu(fileName = "UntitledAnimationSettings", menuName = "LucidityDrive/AnimationSettings", order = 1)]
    public class AnimationSettings : ScriptableObject
    {
        [Tooltip("At this distance from the ground (scaled by leg length), consider the player to be grounded")] 
        public float groundDistanceThresholdScale = 0.1f;
        [Tooltip("At this Y value of the head's up direction, consider the camera upside down")] 
        public float camUpsideDownThreshold;
        [Tooltip("Smooth the animated velocity parameter by this value")] 
        public float velSmoothTime;
        [Tooltip("")] 
        public float footSmoothTime;
        [Tooltip("")] 
        public float willSmoothTime;
        [Tooltip("")]
        public float alignmentSmoothTime;
        [Tooltip("")]
        public float hangSmoothTime;
        [Tooltip("")]
        public float climbSmoothTime;
        [Tooltip("")] 
        public float stanceHeightSmoothTime;
        [Tooltip("")] 
        public float castThickness;
        [Tooltip("")]
        public float castHeight;
        [Tooltip("")] 
        public float lerp;
        [Tooltip("")] 
        public float landTime;
        [Tooltip("")] 
        public float footSlideThreshold;
        [Tooltip("")] 
        public float footSlideVelThreshold;
        [Tooltip("")]
        public float leanSmoothTime;
        [Tooltip("")] 
        public float unrotateFeetBySpeed;
        [Tooltip("")] 
        public float maxFootAngle;
        [Tooltip("")] 
        public float verticalFootAdjust;
        [Tooltip("")]
        public float crouchTime;
        [Tooltip("")] 
        public float minCastDist;
        [Tooltip("")] 
        public float stepRate;
        [Tooltip("")]
        public float scaleStepRateByVelocity;
        [Tooltip("")]
        public float minStepRate;
        [Tooltip("")] 
        public float dampAnimPhaseByAirtime;
        [Tooltip("")] 
        public float wobbleScale;
        [Tooltip("")] 
        public float rollForceThreshold;
        [Tooltip("")] 
        public float leanScale;
        [Tooltip("")] 
        public float maxLeanAngle;
        [Tooltip("")] 
        public float flipSmoothTime;
        [Tooltip("")] 
        public float highSlopeThreshold;
        [Tooltip("")] 
        public float airVelNFix;
    }
}