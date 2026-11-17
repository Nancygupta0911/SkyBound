using System;
using UnityEngine;

namespace Skybound
{
    /// <summary>
    /// Owns the run state: collected coins, the player reference, spawn point and death.
    /// Other systems listen to its events instead of polling.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Scene references")]
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private PlayerController player;

        [Header("Fall detection")]
        [SerializeField] private float killHeight = -25f;

        private Coin[] _coins;
        private int _coinsCollected;

        public int Coins => _coinsCollected;
        public int TotalCoins => _coins != null ? _coins.Length : 0;
        public bool IsRunning { get; private set; }
        public PlayerController Player => player;
        public float KillHeight => killHeight;

        public event Action<int, int> CoinsChanged;
        public event Action PlayerDied;
        public event Action PlayerWon;
        public event Action RunReset;
        public event Action RunStarted;

        public Vector3 SpawnPosition =>
            spawnPoint != null ? spawnPoint.position + Vector3.up * 0.05f : Vector3.up;

        public Quaternion SpawnRotation =>
            spawnPoint != null ? Quaternion.Euler(0f, spawnPoint.eulerAngles.y, 0f) : Quaternion.identity;

        private void Awake()
        {
            Instance = this;
            _coins = FindObjectsByType<Coin>();
            if (player == null) player = FindAnyObjectByType<PlayerController>();

            // The run starts from the title screen.
            IsRunning = false;
            if (player != null) player.SetInputEnabled(false);
        }

        private void Start()
        {
            CoinsChanged?.Invoke(_coinsCollected, TotalCoins);
        }

        private void Update()
        {
            if (!IsRunning || player == null) return;

            // Safety net in case the player somehow slips past the death trigger.
            if (player.transform.position.y < killHeight) KillPlayer();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Called by a coin when the player touches it.</summary>
        public void CollectCoin(Coin coin)
        {
            if (!IsRunning) return;
            _coinsCollected++;
            AudioManager.Coin();
            CoinsChanged?.Invoke(_coinsCollected, TotalCoins);

            if (TotalCoins > 0 && _coinsCollected >= TotalCoins) WinRun();
        }

        /// <summary>Starts the run from the title screen.</summary>
        public void StartRun()
        {
            if (IsRunning) return;

            IsRunning = true;
            if (player != null) player.SetInputEnabled(true);
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StartMusic();
                AudioManager.Instance.DuckMusic(false);
            }
            RunStarted?.Invoke();
        }

        /// <summary>Ends the run: freezes the player and raises <see cref="PlayerDied"/>.</summary>
        public void KillPlayer()
        {
            if (!IsRunning) return;

            IsRunning = false;
            if (player != null) player.Freeze();
            AudioManager.Fall();
            if (AudioManager.Instance != null) AudioManager.Instance.DuckMusic(true);
            PlayerDied?.Invoke();
        }

        /// <summary>Ends the run on a high note once every coin has been collected.</summary>
        private void WinRun()
        {
            if (!IsRunning) return;

            IsRunning = false;
            if (player != null) player.SetInputEnabled(false);
            AudioManager.Win();
            if (AudioManager.Instance != null) AudioManager.Instance.DuckMusic(true);
            PlayerWon?.Invoke();
        }

        /// <summary>Restarts the run: resets coins, score and player position.</summary>
        public void ResetRun()
        {
            _coinsCollected = 0;
            IsRunning = true;

            if (_coins != null)
            {
                foreach (Coin coin in _coins)
                {
                    if (coin != null) coin.ResetCoin();
                }
            }

            if (player != null) player.Respawn(SpawnPosition, SpawnRotation);

            if (AudioManager.Instance != null) AudioManager.Instance.DuckMusic(false);

            CoinsChanged?.Invoke(_coinsCollected, TotalCoins);
            RunReset?.Invoke();
        }
    }
}
