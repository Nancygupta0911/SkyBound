using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Skybound
{
    /// <summary>
    /// Drives the HUD and the Game Over screen from <see cref="GameManager"/> events.
    /// All animation uses unscaled time so it still plays while the game is paused or ended.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("HUD")]
        [SerializeField] private TMP_Text coinLabel;
        [SerializeField] private RectTransform coinPulseTarget;

        [Header("Game Over")]
        [SerializeField] private GameObject gameOverScreen;
        [SerializeField] private CanvasGroup gameOverGroup;
        [SerializeField] private RectTransform gameOverPanel;
        [SerializeField] private TMP_Text gameOverScore;
        [SerializeField] private Button restartButton;

        private Coroutine _pulse;
        private Coroutine _screenFade;

        private void Awake()
        {
            if (gameOverScreen != null) gameOverScreen.SetActive(false);
            if (restartButton != null) restartButton.onClick.AddListener(HandleRestartClicked);
        }

        private void Start()
        {
            GameManager manager = GameManager.Instance;
            if (manager == null) return;

            manager.CoinsChanged += HandleCoinsChanged;
            manager.PlayerDied += HandlePlayerDied;
            manager.RunReset += HandleRunReset;
            HandleCoinsChanged(manager.Coins, manager.TotalCoins);
        }

        private void OnDestroy()
        {
            if (restartButton != null) restartButton.onClick.RemoveListener(HandleRestartClicked);

            GameManager manager = GameManager.Instance;
            if (manager == null) return;

            manager.CoinsChanged -= HandleCoinsChanged;
            manager.PlayerDied -= HandlePlayerDied;
            manager.RunReset -= HandleRunReset;
        }

        private void HandleRestartClicked()
        {
            if (GameManager.Instance != null) GameManager.Instance.ResetRun();
        }

        private void HandleCoinsChanged(int coins, int total)
        {
            if (coinLabel != null) coinLabel.text = "COINS: " + coins;

            if (coinPulseTarget == null || !isActiveAndEnabled) return;
            if (_pulse != null) StopCoroutine(_pulse);
            _pulse = StartCoroutine(Pulse());
        }

        private void HandlePlayerDied()
        {
            if (gameOverScreen == null) return;

            GameManager manager = GameManager.Instance;
            if (gameOverScore != null && manager != null)
                gameOverScore.text = "COINS: " + manager.Coins + " / " + manager.TotalCoins;

            gameOverScreen.SetActive(true);

            if (_screenFade != null) StopCoroutine(_screenFade);
            _screenFade = StartCoroutine(FadeIn());
        }

        private void HandleRunReset()
        {
            if (_screenFade != null)
            {
                StopCoroutine(_screenFade);
                _screenFade = null;
            }

            if (gameOverScreen != null) gameOverScreen.SetActive(false);
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

        private IEnumerator FadeIn()
        {
            const float duration = 0.35f;

            if (gameOverGroup != null) gameOverGroup.alpha = 0f;
            if (gameOverPanel != null) gameOverPanel.localScale = Vector3.one * 0.92f;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);

                if (gameOverGroup != null) gameOverGroup.alpha = eased;
                if (gameOverPanel != null)
                    gameOverPanel.localScale = Vector3.one * Mathf.LerpUnclamped(0.92f, 1f, EaseOutBack(t));

                yield return null;
            }

            if (gameOverGroup != null) gameOverGroup.alpha = 1f;
            if (gameOverPanel != null) gameOverPanel.localScale = Vector3.one;
            _screenFade = null;
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float p = t - 1f;
            return 1f + c3 * p * p * p + c1 * p * p;
        }
    }
}
