using UnityEngine;

namespace Skybound
{
    /// <summary>
    /// Large trigger volume under the level. Entering it ends the run.
    /// The <see cref="GameManager"/> also has a Y-position fallback.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class DeathZone : MonoBehaviour
    {
        private void Reset()
        {
            Collider volume = GetComponent<Collider>();
            volume.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            if (GameManager.Instance != null) GameManager.Instance.KillPlayer();
        }
    }
}
