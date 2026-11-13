using UnityEngine;
using UnityEngine.InputSystem;

namespace Skybound
{
    /// <summary>
    /// Camera-relative third-person character controller with coyote time,
    /// jump buffering, variable jump height and animation blending.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 7f;
        [SerializeField] private float acceleration = 42f;
        [SerializeField] private float deceleration = 58f;
        [SerializeField, Range(0f, 1f)] private float airControl = 0.65f;
        [SerializeField] private float rotationSpeed = 16f;

        [Header("Jump")]
        [SerializeField] private float jumpHeight = 2.35f;
        [SerializeField] private float gravity = 26f;
        [SerializeField] private float fallMultiplier = 1.35f;
        [SerializeField] private float coyoteTime = 0.12f;
        [SerializeField] private float jumpBufferTime = 0.15f;
        [SerializeField, Range(0f, 1f)] private float jumpCutMultiplier = 0.5f;
        [SerializeField] private float maxFallSpeed = 35f;

        [Header("Ground")]
        [SerializeField] private float groundStick = -2f;

        private CharacterController _controller;
        private Animator _animator;
        private Transform _cameraTransform;

        private InputAction _moveAction;
        private InputAction _jumpAction;

        private Vector3 _horizontalVelocity;
        private float _verticalVelocity;
        private float _lastGroundedTime = -99f;
        private float _lastJumpPressedTime = -99f;
        private bool _wasGrounded;
        private bool _inputEnabled = true;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int GroundedHash = Animator.StringToHash("Grounded");
        private static readonly int VerticalSpeedHash = Animator.StringToHash("VerticalSpeed");

        public bool IsGrounded => _controller != null && _controller.isGrounded;
        public float VerticalVelocity => _verticalVelocity;
        public bool InputEnabled => _inputEnabled;

        public event System.Action Jumped;
        public event System.Action Landed;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _animator = GetComponentInChildren<Animator>();
            BuildActions();
        }

        private void BuildActions()
        {
            _moveAction = new InputAction("Move", InputActionType.Value);
            _moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            _moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");

            _jumpAction = new InputAction("Jump", InputActionType.Button, "<Keyboard>/space");
        }

        private void OnEnable()
        {
            _moveAction?.Enable();
            _jumpAction?.Enable();
        }

        private void OnDisable()
        {
            _moveAction?.Disable();
            _jumpAction?.Disable();
        }

        private void OnDestroy()
        {
            _moveAction?.Dispose();
            _jumpAction?.Dispose();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            if (_controller.isGrounded) _lastGroundedTime = Time.time;

            Vector2 input = _inputEnabled ? _moveAction.ReadValue<Vector2>() : Vector2.zero;

            if (_inputEnabled && _jumpAction.WasPressedThisFrame())
                _lastJumpPressedTime = Time.time;

            if (_inputEnabled && _jumpAction.WasReleasedThisFrame() && _verticalVelocity > 0f)
                _verticalVelocity *= jumpCutMultiplier;

            Vector3 wishDirection = CameraRelativeDirection(input);
            Vector3 targetVelocity = wishDirection * (wishDirection.magnitude * moveSpeed);

            bool grounded = _controller.isGrounded;
            float rate = grounded
                ? (targetVelocity.sqrMagnitude > 0.01f ? acceleration : deceleration)
                : acceleration * airControl;

            _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, targetVelocity, rate * dt);

            TryJump();

            if (grounded && _verticalVelocity < 0f)
                _verticalVelocity = groundStick;

            float gravityScale = _verticalVelocity < 0f ? fallMultiplier : 1f;
            _verticalVelocity = Mathf.Max(_verticalVelocity - gravity * gravityScale * dt, -maxFallSpeed);

            Vector3 motion = _horizontalVelocity;
            motion.y = _verticalVelocity;
            _controller.Move(motion * dt);

            if (wishDirection.sqrMagnitude > 0.0025f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(wishDirection, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 1f - Mathf.Exp(-rotationSpeed * dt));
            }

            bool nowGrounded = _controller.isGrounded;
            if (nowGrounded && !_wasGrounded && _verticalVelocity <= 0f) Landed?.Invoke();
            _wasGrounded = nowGrounded;

            UpdateAnimator(nowGrounded, dt);
        }

        private void TryJump()
        {
            bool hasBufferedJump = Time.time - _lastJumpPressedTime <= jumpBufferTime;
            bool withinCoyote = Time.time - _lastGroundedTime <= coyoteTime;

            if (!hasBufferedJump || !withinCoyote) return;

            _verticalVelocity = Mathf.Sqrt(2f * gravity * jumpHeight);
            _lastJumpPressedTime = -99f;
            _lastGroundedTime = -99f;
            Jumped?.Invoke();
        }

        private Vector3 CameraRelativeDirection(Vector2 input)
        {
            if (input.sqrMagnitude < 0.0001f) return Vector3.zero;

            if (_cameraTransform == null)
            {
                Camera main = Camera.main;
                if (main != null) _cameraTransform = main.transform;
            }

            Vector3 forward = Vector3.forward;
            Vector3 right = Vector3.right;

            if (_cameraTransform != null)
            {
                forward = _cameraTransform.forward;
                forward.y = 0f;
                forward.Normalize();
                right = _cameraTransform.right;
                right.y = 0f;
                right.Normalize();
            }

            Vector3 direction = forward * input.y + right * input.x;
            if (direction.sqrMagnitude > 1f) direction.Normalize();
            return direction;
        }

        private void UpdateAnimator(bool grounded, float dt)
        {
            if (_animator == null) return;

            Vector3 flat = new Vector3(_horizontalVelocity.x, 0f, _horizontalVelocity.z);
            float speed01 = Mathf.Clamp01(flat.magnitude / Mathf.Max(0.01f, moveSpeed));

            _animator.SetFloat(SpeedHash, speed01, 0.08f, dt);
            _animator.SetBool(GroundedHash, grounded);
            _animator.SetFloat(VerticalSpeedHash, _verticalVelocity);
        }

        /// <summary>Enables or disables player control without disabling physics.</summary>
        public void SetInputEnabled(bool enabled)
        {
            _inputEnabled = enabled;
            if (!enabled)
            {
                _horizontalVelocity = Vector3.zero;
            }
        }

        /// <summary>Moves the player instantly (used by respawn).</summary>
        public void Teleport(Vector3 position, Quaternion rotation)
        {
            bool wasEnabled = _controller.enabled;
            _controller.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            _horizontalVelocity = Vector3.zero;
            _verticalVelocity = 0f;
            _controller.enabled = wasEnabled;
            _lastGroundedTime = -99f;
            _lastJumpPressedTime = -99f;
            _wasGrounded = false;
        }

        /// <summary>Restores the player to a spawn point and re-enables input.</summary>
        public void Respawn(Vector3 position, Quaternion rotation)
        {
            Teleport(position, rotation);
            SetInputEnabled(true);
        }
    }
}
