using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Skybound
{
    /// <summary>
    /// Turns the existing screen-space UI into a world-space panel that floats
    /// in front of the VR camera. The canvas is billboarded, follows the head
    /// with a little smoothing, and is pulled closer if level geometry gets in
    /// the way. In flat mode the component simply switches itself off.
    /// </summary>
    [DefaultExecutionOrder(200)]
    public class VRWorldCanvas : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private float distance = 1.7f;
        [SerializeField] private float minDistance = 0.6f;
        [SerializeField] private float canvasScale = 0.0008f;
        [SerializeField] private float verticalOffset = -0.05f;

        [Header("Follow")]
        [SerializeField] private float followSharpness = 9f;
        [SerializeField] private float occlusionRadius = 0.12f;
        [SerializeField] private LayerMask occlusionMask = ~0;

        private RectTransform _rect;
        private Camera _camera;

        private void Awake()
        {
            if (canvas == null) canvas = GetComponent<Canvas>();
            _rect = transform as RectTransform;
        }

        private IEnumerator Start()
        {
            if (canvas == null)
            {
                enabled = false;
                yield break;
            }

            // The VR/flat decision is asynchronous (XR starts after the scene
            // loads), so wait for it instead of assuming flat.
            float deadline = Time.realtimeSinceStartup + 5f;
            while (!VRSession.Ready && Time.realtimeSinceStartup < deadline)
                yield return null;

            if (!VRSession.IsVR)
            {
                enabled = false;
                yield break;
            }

            // The rig registers its camera after it has resolved the mode.
            float cameraDeadline = Time.realtimeSinceStartup + 2f;
            while (VRSession.Camera == null && Time.realtimeSinceStartup < cameraDeadline)
                yield return null;

            _camera = VRSession.Camera;
            if (_camera == null)
            {
                enabled = false;
                yield break;
            }

            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = _camera;

            CanvasScaler scaler = GetComponent<CanvasScaler>();
            if (scaler != null) scaler.enabled = false;

            _rect.localScale = Vector3.one * canvasScale;

            UIManager ui = FindAnyObjectByType<UIManager>();
            if (ui != null) ui.ApplyVRHudLayout();

            Place(true);
        }

        private void LateUpdate()
        {
            if (_camera == null) return;
            Place(false);
        }

        private void Place(bool snap)
        {
            Transform cam = _camera.transform;

            float placeDistance = distance;
            if (Physics.SphereCast(cam.position, occlusionRadius, cam.forward, out RaycastHit hit,
                    distance, occlusionMask, QueryTriggerInteraction.Ignore))
            {
                placeDistance = Mathf.Max(minDistance, hit.distance - 0.15f);
            }

            Vector3 targetPosition = cam.position + cam.forward * placeDistance + cam.up * verticalOffset;
            Quaternion targetRotation = Quaternion.LookRotation(targetPosition - cam.position, cam.up);

            float dt = Time.deltaTime;
            if (snap || dt <= 0f)
            {
                transform.SetPositionAndRotation(targetPosition, targetRotation);
                return;
            }

            float t = 1f - Mathf.Exp(-followSharpness * dt);
            transform.position = Vector3.Lerp(transform.position, targetPosition, t);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, t);
        }
    }
}
