using System.Collections;
using TMPro;
using UnityEngine;

namespace Skybound
{
    /// <summary>
    /// Updates the HUD from <see cref="GameManager"/> events. Screens are toggled by the same component.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("HUD")]
        [SerializeField] private TMP_Text coinLabel;
        [SerializeField] private RectTransform coinPulseTarget;

        private Coroutine _pulse;

        private void Start()
        {
            if (GameManager.Instance == null) return;
            GameManager.Instance.CoinsChanged += HandleCoinsChanged;
            HandleCoinsChanged(GameManager.Instance.Coins, GameManager.Instance.TotalCoins);
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null) GameManager.Instance.CoinsChanged -= HandleCoinsChanged;
        }

        private void HandleCoinsChanged(int coins, int total)
        {
            if (coinLabel != null) coinLabel.text = "COINS: " + coins;

            if (coinPulseTarget == null || !isActiveAndEnabled) return;
            if (_pulse != null) StopCoroutine(_pulse);
            _pulse = StartCoroutine(Pulse());
        }

        private IEnumerator Pulse()
        {
            const float duration = 0.24f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float scale = 1f + 0.16f * Mathf.Sin(progress * Mathf.PI);
                coinPulseTarget.localScale = Vector3.one * scale;
                yield return null;
            }

            coinPulseTarget.localScale = Vector3.one;
            _pulse = null;
        }
    }
}
