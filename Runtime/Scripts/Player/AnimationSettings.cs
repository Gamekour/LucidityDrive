using UnityEngine;

namespace LucidityDrive
{
    [CreateAssetMenu(fileName = "UntitledAnimationSettings", menuName = "LucidityDrive/AnimationSettings", order = 1)]
    public class AnimationSettings : ScriptableObject
    {
        [Tooltip("At this distance from the ground (scaled by leg length), consider the player to be grounded")] 
        public float groundDistanceThresholdScale = 0.1f;
        [Tooltip("At this Y value of the head's up direction, consider the camera upside down")] 
        public float camUpsideDownThreshold = -0.1f;
        [Tooltip("Smooth the animated velocity parameter by this value")] 
        public float velSmoothTime = 1;
        [Tooltip("Smooth the position of the feet by this value")] 
        public float footSmoothTime = 0.5f;
        [Tooltip("Smooth the animated motion-alignment parameter by this value")] 
        public float alignmentSmoothTime = 0.1f;
        [Tooltip("Smooth the animated hang (lateral climb offset) parameter by this value")]
        public float hangSmoothTime = 0.025f;
        [Tooltip("Smooth the animated climb (vertical climb offset) parameter by this value")]
        public float climbSmoothTime = 0.5f;
        [Tooltip("Smooth the lean offset by this value")]
        public float leanSmoothTime = 0.2f;
        [Tooltip("Smooth the flip offset by this value")]
        public float flipSmoothTime = 0.1f;
        [Tooltip("Smooth the stance height by this value")] 
        public float stanceHeightSmoothTime = 0.175f;
        [Tooltip("Thickness of the legs' ground check")] 
        public float legCastThickness = 0.1f;
        [Tooltip("Origin height offset of the legs' ground check")]
        public float legCastStartHeightOffset = 0.07f;
        [Tooltip("Offset the foot IK from the ground by this value")] 
        public float verticalFootAdjust = 0.1f;
        [Tooltip("Minimum distance for the legs' ground check - fallback IK if lesser than this")] 
        public float minCastDist = 0.3f;
        [Tooltip("Rate at which footsteps will occur; a.k.a animation speed per velocity")] 
        public float stepRate = 0.8f;
        [Tooltip("Multiply step rate by velocity, scaled by this value")]
        public float scaleStepRateByVelocity = 0.25f;
        [Tooltip("Speed of the passive animation wobble")] 
        public float wobbleScale = 2;
        [Tooltip("Required force to trigger a hard landing animation")] 
        public float hardLandingForce = 10;
        [Tooltip("Scale the lean offset by this value")] 
        public float leanScale = 50;
        [Tooltip("Maximum angle offset due to lean")] 
        public float maxLeanAngle = 45;
        [Tooltip("Below this Y-value of the current slope, consider the surface to be high (only IK if jumping)")] 
        public float highSlopeThreshold = 0.2f;
        [Tooltip("Fallback value (relative to actual Y-velocity) for normalized Y-velocity parameter if lateral velocity is too low.")] 
        public float velNFallback = 0.25f;
    }
}