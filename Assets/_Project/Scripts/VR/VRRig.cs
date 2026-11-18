using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace Skybound
{
    /// <summary>
    /// Third-person VR rig. It keeps the flat game's camera framing (the rig
    /// orbits the character at a fixed distance and pitch) and adds the
    /// headset's head rotation on top, so the player can look around the level
    /// while the character stays centred.
    ///
    /// Input: left stick moves, right stick snaps the view in 30 degree steps,
    /// A/X jumps (and starts or restarts the run from a menu), B/Y mutes.
    /// Without a headset the editor falls back to mouse-look and Q/E snapping.
    /// </summary>
    [DefaultExecutionOrder(150)]
    public class VRRig : MonoBehaviour
    {
        [Header("Scene references")]
        [SerializeField] private Transform player;
        [SerializeField] private Transform orbit;
        [SerializeField] private Camera vrCamera;
        [SerializeField] private Camera flatCamera;
        [SerializeField] private VRComfortVignette vignette;
        [SerializeField] private UIManager ui;

        [Header("Orbit")]
        [SerializeField] private float pivotHeight = 1.7f;
        [SerializeField] private float orbitDistance = 5.5f;
        [SerializeField] private float orbitPitch = 15f;
        [SerializeField] private float pivotSharpness = 14f;

        [Header("Turning")]
        [SerializeField] private float snapTurnAngle = 30f;
        [SerializeField] private float turnDeadzone = 0.6f;
        [SerializeField] private float turnReleaseThreshold = 0.25f;

        [Header("Editor preview")]
        [SerializeField] private float mouseSensitivity = 0.12f;
        [SerializeField] private float previewMinPitch = -18f;
        [SerializeField] private float previewMaxPitch = 62f;

        private InputAction _turnAction;
        private InputAction _jumpAction;
        private InputAction _muteAction;

        private PlayerController _playerController;
        private float _yaw;
        private float _pitch;
        private bool _turnLatched;
        private float _suppressJumpUntil;
        private Vector3 _pivot;

        public bool IsActive => VRSession.IsVR;

        private IEnumerator Start()
        {
            if (player == null)
            {
                GameObject found = GameObject.FindGameObjectWithTag("Player");
                if (found != null) player = found.transform;
            }

            if (player != null)
            {
                _playerController = player.GetComponent<PlayerController>();
            }

            BuildActions();

            // XR Plug-in Management starts the loader asynchronously, so the
            // headset only reports as active a frame or two after the scene
            // loads. Wait for that before choosing the camera path.
            yield return VRSession.Resolve();

            if (!VRSession.IsVR)
            {
                if (vrCamera != null) vrCamera.gameObject.SetActive(false);
                enabled = false;
                yield break;
            }

            if (flatCamera != null)
            {
                flatCamera.gameObject.SetActive(false);
                flatCamera.gameObject.tag = "Untagged";
            }

            if (vrCamera != null)
            {
                vrCamera.gameObject.SetActive(true);
                vrCamera.gameObject.tag = "MainCamera";
                vrCamera.nearClipPlane = 0.05f;
            }

            if (_playerController != null && vrCamera != null)
                _playerController.SetCameraTransform(vrCamera.transform);

            if (ui == null) ui = FindAnyObjectByType<UIManager>();

            if (vignette == null && vrCamera != null)
            {
                var go = new GameObject("Comfort Vignette");
                go.transform.SetParent(vrCamera.transform, false);
                vignette = go.AddComponent<VRComfortVignette>();
            }

            _yaw = player != null ? player.eulerAngles.y : 0f;
            _pitch = orbitPitch;
            _pivot = GetPivot();
            transform.SetPositionAndRotation(_pivot, Quaternion.identity);

            ApplyOrbit();
            VRSession.Register(this, vrCamera);

            // After a restart the character teleports back to the spawn point;
            // snap the view behind it instead of flying across the level.
            if (GameManager.Instance != null) GameManager.Instance.RunReset += Recentre;
        }

        private void OnEnable()
        {
            _turnAction?.Enable();
            _jumpAction?.Enable();
            _muteAction?.Enable();
        }

        private void OnDisable()
        {
            _turnAction?.Disable();
            _jumpAction?.Disable();
            _muteAction?.Disable();
        }

        private void OnDestroy()
        {
            _turnAction?.Dispose();
            _jumpAction?.Dispose();
            _muteAction?.Dispose();

            if (GameManager.Instance != null) GameManager.Instance.RunReset -= Recentre;

            VRSession.Unregister(this);
        }

        private void BuildActions()
        {
            _turnAction = new InputAction("VRTurn", InputActionType.Value);
            _turnAction.AddBinding("<XRController>{RightHand}/primary2DAxis");

            _jumpAction = new InputAction("VRJump", InputActionType.Button);
            _jumpAction.AddBinding("<XRController>{RightHand}/primaryButton");
            _jumpAction.AddBinding("<XRController>{LeftHand}/primaryButton");
            _jumpAction.AddBinding("<Keyboard>/space");

            _muteAction = new InputAction("VRMute", InputActionType.Button);
            _muteAction.AddBinding("<XRController>{RightHand}/secondaryButton");
            _muteAction.AddBinding("<XRController>{LeftHand}/secondaryButton");
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            HandleSnapTurn();
            if (!VRSession.HeadTracking) ApplyPreviewLook();
            HandleMenuInput();

            _pivot = Vector3.Lerp(_pivot, GetPivot(), 1f - Mathf.Exp(-pivotSharpness * dt));
            transform.position = _pivot;

            ApplyOrbit();
            ApplyHeadPose();
            UpdateVignette();
        }

        private Vector3 GetPivot()
        {
            return player != null ? player.position + Vector3.up * pivotHeight : transform.position;
        }

        private void ApplyOrbit()
        {
            if (orbit != null) orbit.localRotation = Quaternion.Euler(_pitch, _yaw, 0f);

            if (vrCamera != null)
                vrCamera.transform.localPosition = new Vector3(0f, 0f, -orbitDistance);
        }

        private void ApplyHeadPose()
        {
            if (!VRSession.HeadTracking || vrCamera == null) return;

            UnityEngine.XR.InputDevice head = InputDevices.GetDeviceAtXRNode(XRNode.CenterEye);
            if (!head.isValid) return;

            if (head.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceRotation, out Quaternion rotation))
                vrCamera.transform.localRotation = rotation;
        }

        private void ApplyPreviewLook()
        {
            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                Vector2 delta = mouse.delta.ReadValue();
                _yaw += delta.x * mouseSensitivity;
                _pitch = Mathf.Clamp(_pitch - delta.y * mouseSensitivity, previewMinPitch, previewMaxPitch);
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.qKey.wasPressedThisFrame) SnapTurn(-1f);
            if (keyboard.eKey.wasPressedThisFrame) SnapTurn(1f);
        }

        private void HandleSnapTurn()
        {
            Vector2 stick = _turnAction.ReadValue<Vector2>();

            if (_turnLatched)
            {
                if (Mathf.Abs(stick.x) < turnReleaseThreshold) _turnLatched = false;
                return;
            }

            if (Mathf.Abs(stick.x) < turnDeadzone) return;

            SnapTurn(Mathf.Sign(stick.x));
            _turnLatched = true;
        }

        private void SnapTurn(float direction)
        {
            _yaw += direction * snapTurnAngle;
        }

        private void HandleMenuInput()
        {
            if (_muteAction.WasPressedThisFrame() && ui != null) ui.ToggleMute();

            if (Time.unscaledTime < _suppressJumpUntil) return;
            if (!_jumpAction.WasPressedThisFrame() || ui == null) return;

            if (ui.IsResultScreenVisible)
            {
                ui.Restart();
                _suppressJumpUntil = Time.unscaledTime + 0.3f;
            }
            else if (ui.IsStartScreenVisible)
            {
                ui.Play();
                _suppressJumpUntil = Time.unscaledTime + 0.3f;
            }
        }

        private void UpdateVignette()
        {
            if (vignette == null) return;

            float speed = _playerController != null ? _playerController.PlanarSpeed : 0f;
            bool running = GameManager.Instance != null && GameManager.Instance.IsRunning;
            vignette.Intensity = running ? Mathf.InverseLerp(1.2f, 7f, speed) : 0f;
        }

        /// <summary>Re-centres the orbit behind the character (used on respawn).</summary>
        public void Recentre()
        {
            _yaw = player != null ? player.eulerAngles.y : 0f;
            _pitch = orbitPitch;
            _pivot = GetPivot();
            transform.position = _pivot;
            ApplyOrbit();
        }
    }
}
