using UnityEngine;

namespace QuantumConnect
{
    /// <summary>
    /// Adds a subtle floating and rotation effect to UI elements to simulate zero gravity.
    /// </summary>
    public class UIHover : MonoBehaviour
    {
        [Header("Hover Settings")]
        [Tooltip("Amplitude of position offset (in local space).")]
        public float positionAmplitude = 5f;

        [Tooltip("Speed of positional oscillation.")]
        public float positionSpeed = 1f;

        [Header("Rotation Settings")]
        [Tooltip("Amplitude of rotation offset (degrees).")]
        public float rotationAmplitude = 5f;

        [Tooltip("Speed of rotational oscillation.")]
        public float rotationSpeed = 1f;

        RectTransform _rectTransform;
        Vector3 _initialPosition;
        Quaternion _initialRotation;
        float _timeOffset;

        void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _initialPosition = _rectTransform.anchoredPosition;
            _initialRotation = _rectTransform.localRotation;
            _timeOffset = Random.Range(0f, 100f);
        }

        void Update()
        {
            float t = Time.time + _timeOffset;

            Vector3 posOffset = new Vector3(
                Mathf.Sin(t * positionSpeed) * positionAmplitude,
                Mathf.Cos(t * positionSpeed) * positionAmplitude,
                0f
            );
            _rectTransform.anchoredPosition = _initialPosition + posOffset;

            Vector3 rotOffset = new Vector3(
                Mathf.Sin(t * rotationSpeed) * rotationAmplitude,
                Mathf.Cos(t * rotationSpeed) * rotationAmplitude,
                Mathf.Sin(t * rotationSpeed * 0.5f) * rotationAmplitude
            );
            _rectTransform.localRotation = _initialRotation * Quaternion.Euler(rotOffset);
        }
    }
}
