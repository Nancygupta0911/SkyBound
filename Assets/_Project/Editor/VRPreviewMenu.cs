using UnityEditor;
using UnityEngine;

namespace Skybound.EditorTools
{
    /// <summary>
    /// Editor-only helpers for working on the VR rig without a headset.
    /// The game always starts in VR preview in the editor; this menu flips it
    /// back to the flat camera so both paths can be compared.
    /// </summary>
    public static class VRPreviewMenu
    {
        private const string MenuPath = "Skybound/VR/Use Flat Camera In Editor";

        [MenuItem(MenuPath, false, 20)]
        private static void ToggleFlatPreview()
        {
            VRSession.EditorPreviewDisabled = !VRSession.EditorPreviewDisabled;
            Menu.SetChecked(MenuPath, VRSession.EditorPreviewDisabled);
            Debug.Log("[Skybound] Editor preview mode: " +
                      (VRSession.EditorPreviewDisabled ? "flat camera" : "VR rig") +
                      ". Enter play mode to apply.");
        }

        [MenuItem(MenuPath, true)]
        private static bool ToggleFlatPreviewValidate()
        {
            Menu.SetChecked(MenuPath, VRSession.EditorPreviewDisabled);
            return !EditorApplication.isPlaying;
        }
    }
}
