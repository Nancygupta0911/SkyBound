using UnityEngine;
using UnityEngine.InputSystem;

namespace Skybound
{
    /// <summary>
    /// Smooth third-person orbit camera with sphere-cast collision against the world.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class CameraFollow : MonoBehaviour
    {
        [Header("Target")]
        public Transform target;
        [SerializeField] private float targetHeight = 1.55f;

        [Header("Orbit")]
        [SerializeField] private float distance = 7.5f;
        [SerializeField] private float mouseSensitivity = 0.12f;
        [SerializeField] private float minPitch = -12f;
        [SerializeField] private float maxPitch = 68f;
        [SerializeField] private float startPitch = 18f;

        [Header("Smoothing")]
        [SerializeField] private float pivotSharpness = 16f;
        [SerializeField] private float positionSharpness = 14f;
        [SerializeField] private float distanceSharpness = 10f;

        [Header("Collision")]
        [SerializeField] private float collisionRadius = 0.32f;
        [SerializeField] private float minDistance = 1.4f;
        [SerializeField] private LayerMask collisionMask = ~0;

        private float _yaw;
        private float _pitch;
        private float _currentDistance;
        private Vector3 _pivot;

        public bool ControlsEnabled { get; set; } = true;

        private void Start()
        {
            if (target == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) target = player.transform;
            }

            _pitch = startPitch;
            _yaw = target != null ? target.eulerAngles.y : 0f;
            _currentDistance = distance;
            _pivot = GetPivot();
            transform.position = _pivot - Quaternion.Euler(_pitch, _yaw, 0f) * Vector3.forward * distance;
            transform.rotation = Quaternion.LookRotation((_pivot - transform.position).normalized, Vector3.up);
        }

        private Vector3 GetPivot()
        {
            return target != null ? target.position + Vector3.up * targetHeight : transform.position;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            if (ControlsEnabled)
            {
                Mouse mouse = Mouse.current;
                if (mouse != null)
                {
                    Vector2 delta = mouse.delta.ReadValue();
                    _yaw += delta.x * mouseSensitivity;
                    _pitch = Mathf.Clamp(_pitch - delta.y * mouseSensitivity, minPitch, maxPitch);
                }
            }

            _pivot = Vector3.Lerp(_pivot, GetPivot(), 1f - Mathf.Exp(-pivotSharpness * dt));

            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 desiredOffset = rotation * Vector3.back * distance;
            Vector3 desiredPosition = _pivot + desiredOffset;

            Vector3 direction = desiredPosition - _pivot;
            float desiredDistance = direction.magnitude;
            if (desiredDistance > 0.01f)
            {
                Vector3 normalized = direction / desiredDistance;
                if (Physics.SphereCast(_pivot, collisionRadius, normalized, out RaycastHit hit, desiredDistance,
                        collisionMask, QueryTriggerInteraction.Ignore))
                {
                    desiredDistance = Mathf.Max(minDistance, hit.distance - 0.08f);
                }
            }

            _currentDistance = Mathf.Lerp(_currentDistance, desiredDistance, 1f - Mathf.Exp(-distanceSharpness * dt));

            Vector3 finalPosition = _pivot + (rotation * Vector3.back) * _currentDistance;
            transform.position = Vector3.Lerp(transform.position, finalPosition, 1f - Mathf.Exp(-positionSharpness * dt));
            transform.rotation = Quaternion.LookRotation((_pivot - transform.position).normalized, Vector3.up);
        }

        /// <summary>Sets which layers the camera collides with.</summary>
        public void SetCollisionMask(LayerMask mask)
        {
            collisionMask = mask;
        }

        /// <summary>Snaps the camera behind the target without smoothing.</summary>
        public void SnapBehind(float yaw)
        {
            _yaw = yaw;
            _pitch = startPitch;
            _pivot = GetPivot();
            _currentDistance = distance;
            transform.position = _pivot - Quaternion.Euler(_pitch, _yaw, 0f) * Vector3.forward * distance;
            transform.rotation = Quaternion.LookRotation((_pivot - transform.position).normalized, Vector3.up);
        }
    }
}
