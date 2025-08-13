using UnityEngine;

namespace QuantumConnect
{
    /// <summary>
    /// Gentle floating + rotational drift around a start position to simulate zero‑G.
    /// </summary>
    public class CameraZeroGravityFloat : MonoBehaviour
    {
        #region Fields
        [Header("Center")]
        [SerializeField] Vector3 startPosition = Vector3.zero;
        [SerializeField] float maxDriftDistance = 0.5f;

        [Header("Position Bobbing")]
        [SerializeField] float positionAmplitude = 0.1f;
        [SerializeField] float positionFrequency = 0.2f;

        [Header("Rotation Drift")]
        [SerializeField] float rotationAmplitude = 1.5f;
        [SerializeField] float rotationFrequency = 0.1f;

        Vector3 initialLocalOffset;
        Quaternion initialRotation;
        #endregion

        #region Unity Lifecycle
        void Start()
        {
            initialLocalOffset = transform.position - startPosition;
            initialRotation = transform.rotation;
        }

        void Update()
        {
            float t = Time.time;

            Vector3 posOffset = new(
                Mathf.Sin(t * positionFrequency) * positionAmplitude,
                Mathf.Cos(t * positionFrequency * 1.1f) * positionAmplitude,
                Mathf.Sin(t * positionFrequency * 0.9f) * positionAmplitude
            );

            Vector3 target = startPosition + initialLocalOffset + posOffset;
            Vector3 clamped = startPosition + Vector3.ClampMagnitude(target - startPosition, maxDriftDistance);
            transform.position = clamped;

            Quaternion rotOffset = Quaternion.Euler(
                Mathf.Sin(t * rotationFrequency) * rotationAmplitude,
                Mathf.Cos(t * rotationFrequency * 0.8f) * rotationAmplitude,
                Mathf.Sin(t * rotationFrequency * 1.2f) * rotationAmplitude
            );
            transform.rotation = initialRotation * rotOffset;
        }
        #endregion
    }
}
