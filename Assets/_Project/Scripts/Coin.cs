using UnityEngine;

namespace Skybound
{
    /// <summary>
    /// Spinning, bobbing collectible. Notifies the <see cref="GameManager"/> when the player touches it.
    /// </summary>
    public class Coin : MonoBehaviour
    {
        [Header("Motion")]
        [SerializeField] private float rotationSpeed = 150f;
        [SerializeField] private float bobHeight = 0.14f;
        [SerializeField] private float bobSpeed = 1.8f;

        [Header("Pickup")]
        [SerializeField] private GameObject collectEffect;

        private Transform _transform;
        private Vector3 _restPosition;
        private float _phase;
        private bool _collected;

        public bool Collected => _collected;

        private void Awake()
        {
            _transform = transform;
            _restPosition = _transform.position;
            _phase = Random.Range(0f, Mathf.PI * 2f);
        }

        private void Update()
        {
            if (_collected) return;

            _transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
            float offset = Mathf.Sin(Time.time * bobSpeed + _phase) * bobHeight;
            _transform.position = _restPosition + Vector3.up * offset;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_collected || !other.CompareTag("Player")) return;
            if (GameManager.Instance != null && !GameManager.Instance.IsRunning) return;
            Collect();
        }

        /// <summary>Collects the coin: spawns the burst, updates the score and hides the pickup.</summary>
        public void Collect()
        {
            if (_collected) return;
            _collected = true;

            if (collectEffect != null)
            {
                GameObject effect = Instantiate(collectEffect, _transform.position, Quaternion.identity);
                Destroy(effect, 1.5f);
            }

            if (GameManager.Instance != null) GameManager.Instance.CollectCoin(this);
            gameObject.SetActive(false);
        }

        /// <summary>Restores the coin for a new run.</summary>
        public void ResetCoin()
        {
            _collected = false;
            if (_transform == null) _transform = transform;
            _transform.position = _restPosition;
            gameObject.SetActive(true);
        }
    }
}
