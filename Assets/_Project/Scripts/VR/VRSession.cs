using System.Collections;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;

namespace Skybound
{
    /// <summary>
    /// Decides whether the current session runs in VR or on a flat screen.
    /// In a build the headset decides; the decision waits for XR Plug-in
    /// Management to finish starting the loader, because that happens
    /// asynchronously a frame or two after the scene loads.
    ///
    /// In the editor we always start in a VR preview (mouse-look + keyboard)
    /// because no headset is attached, which lets the rig, world-space HUD and
    /// controls be tested without hardware.
    /// </summary>
    public static class VRSession
    {
        private const float InitializationTimeout = 4f;

        /// <summary>True when an XR device is present and running.</summary>
        public static bool HeadsetActive => XRSettings.isDeviceActive;

        /// <summary>Editor-only escape hatch so the flat camera can still be tested.</summary>
        public static bool EditorPreviewDisabled { get; set; }

        /// <summary>True when the VR rig should drive the camera. Valid once <see cref="Ready"/> is true.</summary>
        public static bool IsVR { get; private set; }

        /// <summary>True once the VR/flat decision has been made.</summary>
        public static bool Ready { get; private set; }

        /// <summary>True when a real tracked headset supplies the head pose.</summary>
        public static bool HeadTracking => XRSettings.isDeviceActive;

        /// <summary>The camera the HUD billboards towards.</summary>
        public static Camera Camera { get; private set; }

        /// <summary>The active rig, if any.</summary>
        public static VRRig Rig { get; private set; }

        /// <summary>
        /// Waits for XR to settle, then decides the mode. Yields until
        /// <see cref="Ready"/> is set; safe to call from several components.
        /// </summary>
        public static IEnumerator Resolve()
        {
            if (Ready) yield break;

#if UNITY_EDITOR
            IsVR = !EditorPreviewDisabled;
            Ready = true;
            yield break;
#else
            float deadline = Time.realtimeSinceStartup + InitializationTimeout;

            while (!XRSettings.isDeviceActive && Time.realtimeSinceStartup < deadline)
            {
                XRManagerSettings manager = XRGeneralSettings.Instance != null
                    ? XRGeneralSettings.Instance.Manager
                    : null;

                // No loader configured for this build target (WebGL, flat
                // desktop): nothing will ever start, so stop waiting.
                if (manager == null) break;
                if (manager.isInitializationComplete) break;

                yield return null;
            }

            IsVR = XRSettings.isDeviceActive;
            Ready = true;
#endif
        }

        internal static void Register(VRRig rig, Camera camera)
        {
            Rig = rig;
            Camera = camera;
        }

        internal static void Unregister(VRRig rig)
        {
            if (Rig != rig) return;
            Rig = null;
            Camera = null;
        }
    }
}
