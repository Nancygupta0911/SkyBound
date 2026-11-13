using System;
using UnityEngine;

namespace Skybound
{
    /// <summary>
    /// Owns the run state: collected coins, the player reference and spawn point.
    /// Other systems listen to its events instead of polling.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Scene references")]
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private PlayerController player;

        private Coin[] _coins;
        private int _coinsCollected;

        public int Coins => _coinsCollected;
        public int TotalCoins => _coins != null ? _coins.Length : 0;
        public bool IsRunning { get; private set; } = true;
        public PlayerController Player => player;

        public event Action<int, int> CoinsChanged;

        public Vector3 SpawnPosition =>
            spawnPoint != null ? spawnPoint.position + Vector3.up * 0.05f : Vector3.up;

        public Quaternion SpawnRotation =>
            spawnPoint != null ? Quaternion.Euler(0f, spawnPoint.eulerAngles.y, 0f) : Quaternion.identity;

        private void Awake()
        {
            Instance = this;
            _coins = FindObjectsByType<Coin>();
            if (player == null) player = FindAnyObjectByType<PlayerController>();
        }

        private void Start()
        {
            CoinsChanged?.Invoke(_coinsCollected, TotalCoins);
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
            CoinsChanged?.Invoke(_coinsCollected, TotalCoins);
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
            CoinsChanged?.Invoke(_coinsCollected, TotalCoins);
        }
    }
}
