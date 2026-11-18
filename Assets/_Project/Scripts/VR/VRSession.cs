using UnityEngine;
using UnityEngine.XR;

namespace Skybound
{
    /// <summary>
    /// Decides whether the current session runs in VR or on a flat screen.
    /// In a build the headset decides. In the editor we always start in a VR
    /// preview (mouse-look + keyboard) because no headset is attached, which
    /// lets the rig, world-space HUD and controls be tested without hardware.
    /// </summary>
    public static class VRSession
    {
        /// <summary>True when an XR device is present and running.</summary>
        public static bool HeadsetActive => XRSettings.isDeviceActive;

        /// <summary>Editor-only escape hatch so the flat camera can still be tested.</summary>
        public static bool EditorPreviewDisabled { get; set; }

        /// <summary>True when the VR rig should drive the camera.</summary>
        public static bool IsVR => HeadsetActive || (Application.isEditor && !EditorPreviewDisabled);

        /// <summary>True when a real tracked headset supplies the head pose.</summary>
        public static bool HeadTracking => HeadsetActive;

        /// <summary>The camera the HUD billboards towards.</summary>
        public static Camera Camera { get; private set; }

        /// <summary>The active rig, if any.</summary>
        public static VRRig Rig { get; private set; }

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
