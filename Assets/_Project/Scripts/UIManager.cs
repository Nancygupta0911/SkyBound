using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Skybound
{
    /// <summary>
    /// Drives the title screen, HUD and Game Over screen from <see cref="GameManager"/> events.
    /// All animation uses unscaled time so it still plays while the game is ended.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("HUD")]
        [SerializeField] private CanvasGroup hudGroup;
        [SerializeField] private TMP_Text coinLabel;
        [SerializeField] private RectTransform coinPulseTarget;
        [SerializeField] private Button muteButton;
        [SerializeField] private TMP_Text muteLabel;
        [SerializeField] private CanvasGroup muteGroup;

        [Header("Start Screen")]
        [SerializeField] private GameObject startScreen;
        [SerializeField] private CanvasGroup startScreenGroup;
        [SerializeField] private RectTransform startScreenPanel;
        [SerializeField] private RectTransform startTitle;
        [SerializeField] private Button playButton;

        [Header("Game Over")]
        [SerializeField] private GameObject gameOverScreen;
        [SerializeField] private CanvasGroup gameOverGroup;
        [SerializeField] private RectTransform gameOverPanel;
        [SerializeField] private TMP_Text gameOverTitle;
        [SerializeField] private TMP_Text gameOverSubtitle;
        [SerializeField] private TMP_Text gameOverScore;
        [SerializeField] private Button restartButton;

        private Coroutine _pulse;
        private Coroutine _screenFade;
        private Coroutine _startFade;
        private Coroutine _hudFade;
        private Vector2 _titleBasePosition;

        private static readonly Color WonScoreColor = new Color(1f, 0.82f, 0.35f, 1f);
        private static readonly Color LostScoreColor = new Color(1f, 0.94f, 0.79f, 1f);

        private void Awake()
        {
            if (gameOverScreen != null) gameOverScreen.SetActive(false);
            if (startScreen != null) startScreen.SetActive(true);
            if (hudGroup != null) hudGroup.alpha = 0f;
            if (muteGroup != null) muteGroup.alpha = 0f;
            if (startTitle != null) _titleBasePosition = startTitle.anchoredPosition;

            if (restartButton != null) restartButton.onClick.AddListener(HandleRestartClicked);
            if (playButton != null) playButton.onClick.AddListener(HandlePlayClicked);
            if (muteButton != null) muteButton.onClick.AddListener(HandleMuteClicked);
        }

        private void Start()
        {
            GameManager manager = GameManager.Instance;
            if (manager == null) return;

            manager.CoinsChanged += HandleCoinsChanged;
            manager.PlayerDied += HandlePlayerDied;
            manager.PlayerWon += HandlePlayerWon;
            manager.RunReset += HandleRunReset;
            HandleCoinsChanged(manager.Coins, manager.TotalCoins);
            UpdateMuteLabel();

            if (AudioManager.Instance != null) AudioManager.Instance.MuteChanged += HandleMuteChanged;

            if (startScreen != null && startScreen.activeSelf)
            {
                if (_startFade != null) StopCoroutine(_startFade);
                _startFade = StartCoroutine(AnimateScreenIn(startScreenGroup, startScreenPanel, 0.55f, 0.94f));
            }
        }

        private void Update()
        {
            if (startTitle == null || startScreen == null || !startScreen.activeSelf) return;

            float bob = Mathf.Sin(Time.unscaledTime * 1.5f) * 4f;
            startTitle.anchoredPosition = _titleBasePosition + new Vector2(0f, bob);
        }

        private void OnDestroy()
        {
            if (restartButton != null) restartButton.onClick.RemoveListener(HandleRestartClicked);
            if (playButton != null) playButton.onClick.RemoveListener(HandlePlayClicked);
            if (muteButton != null) muteButton.onClick.RemoveListener(HandleMuteClicked);

            GameManager manager = GameManager.Instance;
            if (manager == null) return;

            manager.CoinsChanged -= HandleCoinsChanged;
            manager.PlayerDied -= HandlePlayerDied;
            manager.PlayerWon -= HandlePlayerWon;
            manager.RunReset -= HandleRunReset;

            if (AudioManager.Instance != null) AudioManager.Instance.MuteChanged -= HandleMuteChanged;
        }

        private void HandleMuteChanged(bool muted)
        {
            UpdateMuteLabel();
        }

        private void HandlePlayClicked()
        {
            AudioManager.Click();
            if (GameManager.Instance != null) GameManager.Instance.StartRun();

            if (startScreen != null) startScreen.SetActive(false);
            if (_startFade != null)
            {
                StopCoroutine(_startFade);
                _startFade = null;
            }

            if (hudGroup != null)
            {
                if (_hudFade != null) StopCoroutine(_hudFade);
                _hudFade = StartCoroutine(FadeHud(1f, 0.35f));
            }
        }

        private void HandleRestartClicked()
        {
            AudioManager.Click();
            if (GameManager.Instance != null) GameManager.Instance.ResetRun();
        }

        private void HandleMuteClicked()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.ToggleMute();
            UpdateMuteLabel();
            AudioManager.Click();
        }

        private void UpdateMuteLabel()
        {
            if (muteLabel == null) return;
            bool muted = AudioManager.Instance != null && AudioManager.Instance.Muted;
            muteLabel.text = muted ? "SOUND OFF" : "SOUND ON";
        }

        private void HandleCoinsChanged(int coins, int total)
        {
            if (coinLabel != null) coinLabel.text = "COINS: " + coins + " / " + total;

            if (coinPulseTarget == null || !isActiveAndEnabled) return;
            if (_pulse != null) StopCoroutine(_pulse);
            _pulse = StartCoroutine(Pulse());
        }

        private void HandlePlayerDied()
        {
            ShowResult("YOU FELL!", "FINAL SCORE", false);
        }

        private void HandlePlayerWon()
        {
            GameManager manager = GameManager.Instance;
            string subtitle = manager != null ? "ALL " + manager.TotalCoins + " COINS COLLECTED" : "ALL COINS COLLECTED";
            ShowResult("YOU WIN!", subtitle, true);
        }

        private void ShowResult(string title, string subtitle, bool won)
        {
            if (gameOverScreen == null) return;

            GameManager manager = GameManager.Instance;
            if (gameOverTitle != null) gameOverTitle.text = title;
            if (gameOverSubtitle != null) gameOverSubtitle.text = subtitle;

            if (gameOverScore != null && manager != null)
                gameOverScore.text = "COINS: " + manager.Coins + " / " + manager.TotalCoins;

            if (gameOverScore != null)
                gameOverScore.color = won ? WonScoreColor : LostScoreColor;

            gameOverScreen.SetActive(true);

            if (_screenFade != null) StopCoroutine(_screenFade);
            _screenFade = StartCoroutine(AnimateScreenIn(gameOverGroup, gameOverPanel, 0.35f, 0.92f));
        }

        private void HandleRunReset()
        {
            if (_screenFade != null)
            {
                StopCoroutine(_screenFade);
                _screenFade = null;
            }

            if (gameOverScreen != null) gameOverScreen.SetActive(false);
            if (hudGroup != null) hudGroup.alpha = 1f;
            if (muteGroup != null) muteGroup.alpha = 1f;
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

        private IEnumerator FadeHud(float target, float duration)
        {
            if (hudGroup == null) yield break;

            float start = hudGroup.alpha;
            float muteStart = muteGroup != null ? muteGroup.alpha : target;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                hudGroup.alpha = Mathf.Lerp(start, target, t);
                if (muteGroup != null) muteGroup.alpha = Mathf.Lerp(muteStart, target, t);
                yield return null;
            }

            hudGroup.alpha = target;
            if (muteGroup != null) muteGroup.alpha = target;
            _hudFade = null;
        }

        private IEnumerator AnimateScreenIn(CanvasGroup group, RectTransform panel, float duration, float fromScale)
        {
            if (group != null) group.alpha = 0f;
            if (panel != null) panel.localScale = Vector3.one * fromScale;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);

                if (group != null) group.alpha = eased;
                if (panel != null)
                    panel.localScale = Vector3.one * Mathf.LerpUnclamped(fromScale, 1f, EaseOutBack(t));

                yield return null;
            }

            if (group != null) group.alpha = 1f;
            if (panel != null) panel.localScale = Vector3.one;

            _screenFade = null;
            _startFade = null;
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
