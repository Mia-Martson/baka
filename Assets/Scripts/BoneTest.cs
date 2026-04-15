using UnityEngine;

public class JellyfishController : MonoBehaviour
{
    [Header("References")]
    public Transform bell;
    public Transform[] tentacleChains; // assign ROOT bone of each tentacle
    public Rigidbody rb;

    [Header("Pulse Settings")]
    public float pulseSpeed = 2f;
    public float pulseScaleAmount = 0.15f;
    public float forwardForce = 2f;

    [Header("Tentacle Settings")]
    public float swayAmplitude = 20f;
    public float swaySpeed = 2f;
    public float waveOffset = 0.4f;
    public float dragStrength = 2f;

    [Header("Body Movement")]
    public float tiltAmount = 10f;
    public float bobAmount = 0.2f;
    public float bobSpeed = 1.5f;

    private Vector3 bellStartScale;
    private Vector3 startPos;

    private class Tentacle
    {
        public Transform[] bones;
        public Quaternion[] startRotations;
    }

    private Tentacle[] tentacles;

    void Start()
    {
        bellStartScale = bell.localScale;
        startPos = transform.position;

        // Cache tentacle chains
        tentacles = new Tentacle[tentacleChains.Length];

        for (int i = 0; i < tentacleChains.Length; i++)
        {
            tentacles[i] = new Tentacle();
            tentacles[i].bones = GetBoneChain(tentacleChains[i]);

            tentacles[i].startRotations = new Quaternion[tentacles[i].bones.Length];
            for (int j = 0; j < tentacles[i].bones.Length; j++)
                tentacles[i].startRotations[j] = tentacles[i].bones[j].localRotation;
        }
    }

    void Update()
    {
        float t = Time.time;

        HandlePulse(t);
        HandleBodyTilt();
        HandleTentacles(t);
        HandleBobbing(t);
    }

    // ------------------------
    // Bell pulse + movement
    // ------------------------
    void HandlePulse(float t)
    {
        float pulse = Mathf.Sin(t * pulseSpeed) * 0.5f + 0.5f;

        float scale = 1f + pulse * pulseScaleAmount;
        bell.localScale = bellStartScale * scale;

        // Push forward on pulse peak
        if (pulse > 0.95f)
        {
            rb.AddForce(transform.forward * forwardForce, ForceMode.Acceleration);
        }
    }

    // ------------------------
    // Tilt based on movement
    // ------------------------
    void HandleBodyTilt()
    {
        Vector3 vel = rb.linearVelocity;
        Vector3 localVel = transform.InverseTransformDirection(vel);

        float tiltX = -localVel.z * tiltAmount * 0.1f;
        float tiltZ = localVel.x * tiltAmount * 0.1f;

        transform.localRotation = Quaternion.Euler(tiltX, transform.localEulerAngles.y, tiltZ);
    }

    // ------------------------
    // Tentacle animation
    // ------------------------
    void HandleTentacles(float t)
    {
        Vector3 vel = rb.linearVelocity;
        Vector3 localVel = transform.InverseTransformDirection(vel);
        float speed = vel.magnitude;

        for (int i = 0; i < tentacles.Length; i++)
        {
            var tentacle = tentacles[i];

            for (int j = 0; j < tentacle.bones.Length; j++)
            {
                float wave = Mathf.Sin(t * swaySpeed - j * waveOffset)
                             * swayAmplitude * (0.3f + speed * 0.1f);

                float drag = -localVel.x * dragStrength * (j + 1) * 0.1f;

                tentacle.bones[j].localRotation =
                    tentacle.startRotations[j] *
                    Quaternion.Euler(wave, drag, 0f);
            }
        }
    }

    // ------------------------
    // Floating motion
    // ------------------------
    void HandleBobbing(float t)
    {
        float bob = Mathf.Sin(t * bobSpeed) * bobAmount;
        transform.position = new Vector3(
            transform.position.x,
            startPos.y + bob,
            transform.position.z
        );
    }

    // ------------------------
    // Utility: get bone chain
    // ------------------------
    Transform[] GetBoneChain(Transform root)
    {
        System.Collections.Generic.List<Transform> chain = new();

        Transform current = root;
        while (current != null)
        {
            chain.Add(current);

            if (current.childCount > 0)
                current = current.GetChild(0);
            else
                break;
        }

        return chain.ToArray();
    }
}