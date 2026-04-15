using System.Collections.Generic;
using UnityEngine;

public class JellyfishSwimController : MonoBehaviour
{
    [System.Serializable]
    public class TentacleChain
    {
        public Transform rootBone;
        public Transform outwardReference;
        public bool invertOutward;

        [HideInInspector] public Transform[] bones;
        [HideInInspector] public Quaternion[] startRotations;
        [HideInInspector] public Vector3 outwardWorldDir;
    }

    public Transform bellBone;
    public Transform jellyfishCenter;

    public TentacleChain[] outerTentacles;
    public TentacleChain innerTentacle;

    public float swimCycleDuration = 2.5f;

    public float bellShrinkX = 0.05f;
    public float bellShrinkZ = 0.05f;
    public float bellStretchY = 0.025f;
    public float bellSmooth = 6f;

    public float rootOutwardAngle = 10f;
    public float midOutwardAngle = 8f;
    public float tipDragAngle = 4f;
    public float waveDelay = 0.25f;

    public float inwardPullAngle = 6f;
    public float inwardPullStart = 0.65f;

    public float idleSwayAngle = 1.2f;
    public float idleSwaySpeed = 0.7f;

    public float innerRootAngle = 3f;
    public float innerTipAngle = 6f;
    public float innerWaveDelay = 0.25f;
    public float innerSwayAngle = 1f;
    public float innerSwaySpeed = 0.55f;

    public float tentacleRotationSmooth = 6f;

    public Vector3 localTentacleDirection = Vector3.down;
    public Vector3 fallbackBendAxis = Vector3.right;

    private Vector3 bellStartScale;

    void Awake()
    {
        if (bellBone != null)
            bellStartScale = bellBone.localScale;

        BuildOuterTentacles();
        BuildInnerTentacle();
    }

    void Update()
    {
        if (swimCycleDuration <= 0.01f)
            return;

        float cycle = Mathf.Repeat(Time.time / swimCycleDuration, 1f);
        float bellPulse = GetSinglePulse(cycle);

        AnimateBell(bellPulse);
        AnimateOuterTentacles(cycle);
        AnimateInnerTentacle(cycle);
    }

    float GetSinglePulse(float cycle)
    {
        if (cycle < 0f || cycle > 1f)
            return 0f;

        return Mathf.Sin(cycle * Mathf.PI);
    }

    float GetDelayedSinglePulse(float cycle)
    {
        if (cycle < 0f || cycle > 1f)
            return 0f;

        return Mathf.Sin(cycle * Mathf.PI);
    }

    float GetEndInwardPulse(float cycle)
    {
        if (cycle < inwardPullStart)
            return 0f;

        float t = (cycle - inwardPullStart) / (1f - inwardPullStart);
        return Mathf.Sin(t * Mathf.PI);
    }

    void AnimateBell(float pulse)
    {
        if (bellBone == null)
            return;

        Vector3 targetScale = bellStartScale;
        targetScale.x *= 1f - bellShrinkX * pulse;
        targetScale.z *= 1f - bellShrinkZ * pulse;
        targetScale.y *= 1f + bellStretchY * pulse;

        bellBone.localScale = Vector3.Lerp(
            bellBone.localScale,
            targetScale,
            Time.deltaTime * bellSmooth
        );
    }

    void AnimateOuterTentacles(float cycle)
    {
        if (outerTentacles == null)
            return;

        for (int tIndex = 0; tIndex < outerTentacles.Length; tIndex++)
        {
            TentacleChain chain = outerTentacles[tIndex];
            if (!IsValid(chain))
                continue;

            Vector3 outwardDir = chain.outwardWorldDir;
            if (chain.invertOutward)
                outwardDir = -outwardDir;

            for (int i = 0; i < chain.bones.Length; i++)
            {
                Transform bone = chain.bones[i];
                if (bone == null)
                    continue;

                float bone01 = chain.bones.Length == 1 ? 1f : (float)i / (chain.bones.Length - 1);

                // Important: no Mathf.Repeat here.
                // This prevents a second outward pulse near the end of the cycle.
                float delayedCycle = cycle - bone01 * waveDelay;
                float delayedContraction = GetDelayedSinglePulse(delayedCycle);

                float rootWeight = 1f - bone01;
                float midWeight = 1f - Mathf.Abs(bone01 - 0.45f) / 0.45f;
                midWeight = Mathf.Clamp01(midWeight);

                float mainBendWeight = Mathf.Max(rootWeight * 0.85f, midWeight);
                float tipWeight = Mathf.SmoothStep(0.35f, 1f, bone01);

                float outwardAngle =
                    Mathf.Lerp(rootOutwardAngle, midOutwardAngle, bone01) *
                    delayedContraction *
                    mainBendWeight;

                float inwardPulse = GetEndInwardPulse(cycle);
                float inwardWeight = Mathf.Lerp(1f, 0.35f, bone01);

                outwardAngle -= inwardPullAngle * inwardPulse * inwardWeight;

                float dragAngle =
                    tipDragAngle *
                    delayedContraction *
                    tipWeight;

                float sway = Mathf.Sin(Time.time * idleSwaySpeed + tIndex * 0.8f + bone01 * 0.6f)
                             * idleSwayAngle * (0.75f - 0.35f * bone01);

                Quaternion startRot = chain.startRotations[i];

                Vector3 localOutward = bone.InverseTransformDirection(outwardDir).normalized;
                Vector3 bendAxis = Vector3.Cross(localTentacleDirection.normalized, localOutward).normalized;

                if (bendAxis.sqrMagnitude < 0.0001f)
                    bendAxis = fallbackBendAxis.normalized;

                Quaternion outwardRot = Quaternion.AngleAxis(outwardAngle + sway, bendAxis);
                Quaternion dragRot = Quaternion.AngleAxis(dragAngle, bendAxis);

                Quaternion targetRot = startRot * outwardRot * dragRot;

                bone.localRotation = Quaternion.Slerp(
                    bone.localRotation,
                    targetRot,
                    Time.deltaTime * tentacleRotationSmooth
                );
            }
        }
    }

    void AnimateInnerTentacle(float cycle)
    {
        if (!IsValid(innerTentacle))
            return;

        for (int i = 0; i < innerTentacle.bones.Length; i++)
        {
            Transform bone = innerTentacle.bones[i];
            if (bone == null)
                continue;

            float bone01 = innerTentacle.bones.Length == 1 ? 1f : (float)i / (innerTentacle.bones.Length - 1);

            float delayedCycle = cycle - bone01 * innerWaveDelay;
            float pulse = GetDelayedSinglePulse(delayedCycle);

            float bend = Mathf.Lerp(innerRootAngle, innerTipAngle, bone01) * pulse;

            float sway = Mathf.Sin(Time.time * innerSwaySpeed + bone01 * 1.1f)
                         * innerSwayAngle * (0.8f - 0.3f * bone01);

            Quaternion startRot = innerTentacle.startRotations[i];
            Quaternion bendRot = Quaternion.AngleAxis(bend + sway, fallbackBendAxis.normalized);

            Quaternion targetRot = startRot * bendRot;

            bone.localRotation = Quaternion.Slerp(
                bone.localRotation,
                targetRot,
                Time.deltaTime * tentacleRotationSmooth
            );
        }
    }

    void BuildOuterTentacles()
    {
        if (outerTentacles == null)
            return;

        for (int i = 0; i < outerTentacles.Length; i++)
        {
            TentacleChain chain = outerTentacles[i];
            if (chain == null || chain.rootBone == null)
                continue;

            chain.bones = GetBoneChain(chain.rootBone);
            chain.startRotations = CaptureStartRotations(chain.bones);
            chain.outwardWorldDir = CalculateOutwardDirection(chain);
        }
    }

    void BuildInnerTentacle()
    {
        if (innerTentacle == null || innerTentacle.rootBone == null)
            return;

        innerTentacle.bones = GetBoneChain(innerTentacle.rootBone);
        innerTentacle.startRotations = CaptureStartRotations(innerTentacle.bones);
    }

    Quaternion[] CaptureStartRotations(Transform[] bones)
    {
        Quaternion[] result = new Quaternion[bones.Length];

        for (int i = 0; i < bones.Length; i++)
        {
            if (bones[i] != null)
                result[i] = bones[i].localRotation;
        }

        return result;
    }

    Vector3 CalculateOutwardDirection(TentacleChain chain)
    {
        if (chain.outwardReference != null && chain.rootBone != null)
        {
            Vector3 dir = (chain.outwardReference.position - chain.rootBone.position).normalized;
            if (dir.sqrMagnitude > 0.0001f)
                return dir;
        }

        if (jellyfishCenter != null && chain.rootBone != null)
        {
            Vector3 dir = (chain.rootBone.position - jellyfishCenter.position).normalized;
            if (dir.sqrMagnitude > 0.0001f)
                return dir;
        }

        return transform.right;
    }

    Transform[] GetBoneChain(Transform root)
    {
        List<Transform> chain = new List<Transform>();
        Transform current = root;

        while (current != null)
        {
            chain.Add(current);

            if (current.childCount == 1)
                current = current.GetChild(0);
            else
                break;
        }

        return chain.ToArray();
    }

    bool IsValid(TentacleChain chain)
    {
        return chain != null &&
               chain.bones != null &&
               chain.startRotations != null &&
               chain.bones.Length > 0 &&
               chain.bones.Length == chain.startRotations.Length;
    }
}