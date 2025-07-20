using UnityEngine;

namespace QuantumConnect
{
    /// <summary>
    /// Adds a floating and drifting effect to simulate a camera in zero-gravity,
    /// with clamped movement around a central position.
    /// </summary>
    public class CameraZeroGravityFloat : MonoBehaviour
    {
        [Header("Start Position")]
        [Tooltip("Center position the camera hovers around.")]
        public Vector3 startPosition = Vector3.zero;

        [Tooltip("Maximum drift distance from the center position.")]
        public float maxDriftDistance = 0.5f;

        [Header("Position Bobbing")]
        [Tooltip("Amplitude of position bobbing (in units).")]
        public float positionAmplitude = 0.1f;

        [Tooltip("Speed of position bobbing.")]
        public float positionFrequency = 0.2f;

        [Header("Rotation Drift")]
        [Tooltip("Amplitude of rotation drift (in degrees).")]
        public float rotationAmplitude = 1.5f;

        [Tooltip("Speed of rotation drift.")]
        public float rotationFrequency = 0.1f;

        Vector3 _initialLocalOffset;
        Quaternion _initialRotation;

        void Start()
        {
            _initialLocalOffset = transform.position - startPosition;
            _initialRotation = transform.rotation;
        }

        void Update()
        {
            float time = Time.time;

            Vector3 offset = new Vector3(
                Mathf.Sin(time * positionFrequency) * positionAmplitude,
                Mathf.Cos(time * positionFrequency * 1.1f) * positionAmplitude,
                Mathf.Sin(time * positionFrequency * 0.9f) * positionAmplitude
            );

            Vector3 targetPosition = startPosition + _initialLocalOffset + offset;

            Vector3 clampedPosition = startPosition + Vector3.ClampMagnitude(targetPosition - startPosition, maxDriftDistance);
            transform.position = clampedPosition;

            Quaternion rotOffset = Quaternion.Euler(
                Mathf.Sin(time * rotationFrequency) * rotationAmplitude,
                Mathf.Cos(time * rotationFrequency * 0.8f) * rotationAmplitude,
                Mathf.Sin(time * rotationFrequency * 1.2f) * rotationAmplitude
            );
            transform.rotation = _initialRotation * rotOffset;
        }
    }
}
