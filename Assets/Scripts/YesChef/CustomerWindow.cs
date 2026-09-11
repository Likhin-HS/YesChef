using System.Collections;
using UnityEngine;
using YesChef.Core;
using YesChef.Data;
using YesChef.Managers;
using YesChef.Player;

namespace YesChef
{
    /// <summary>
    /// One of four serving windows. Generates 2-3 random ingredients,
    /// ticks its age while playing, accepts prepared items, and respawns.
    /// </summary>
    public sealed class CustomerWindow : Interactable
    {
        [SerializeField] private int _windowIndex;
        [SerializeField] private Renderer _statusVisual;

        private OrderData _order;
        private bool _waitingRespawn;
        private float _respawnRemaining;

        public int WindowIndex => _windowIndex;
        public OrderData Order => _order;
        public bool HasOrder => _order != null && !_order.IsComplete;
        public bool WaitingRespawn => _waitingRespawn;
        public float RespawnRemaining => _respawnRemaining;

        private void Awake()
        {
            SetStationName($"Window {_windowIndex + 1}");
        }

        public void Configure(int index)
        {
            _windowIndex = index;
            SetStationName($"Window {index + 1}");
        }

        private void Update()
        {
            var manager = GameManager.Instance;
            if (manager == null || !manager.IsPlaying) return;

            if (_waitingRespawn)
            {
                _respawnRemaining -= Time.deltaTime;
                if (_respawnRemaining <= 0f)
                {
                    _waitingRespawn = false;
                    SpawnOrder();
                }
                return;
            }
            if (_order != null && !_order.IsComplete)
            {
                _order.Tick(Time.deltaTime);
                manager.NotifyOrderProgress(_windowIndex, _order);
            }
        }

        public void SpawnInitialOrder(int index)
        {
            Configure(index);
            _waitingRespawn = false;
            SpawnOrder();
        }

        public void ResetWindow()
        {
            StopAllCoroutines();
            _order = null;
            _waitingRespawn = false;
            RefreshVisual(null);
        }

        public override string GetPrompt(PlayerController player)
        {
            if (_waitingRespawn) return $"Next customer in {Mathf.CeilToInt(_respawnRemaining)}s";
            if (!HasOrder) return "Waiting for customer...";
            if (player.Held is not { } held) return $"Need: {_order.GetRequirementText()}";
            if (!held.IsPrepared) return $"{held.DisplayName} is not prepared yet";
            return _order.Needs(held.Type)
                ? $"E: serve {held.DisplayName}"
                : $"{held.DisplayName} not needed here ({_order.GetRequirementText()})";
        }

        public override void Interact(PlayerController player)
        {
            if (!HasOrder) return;
            if (player.Held is not { } held || !held.IsPrepared) return;
            if (!_order.TryFulfill(held.Type)) return;
            player.TryTake(out _);

            var manager = GameManager.Instance;
            if (manager == null) return;
            if (_order.IsComplete)
            {
                manager.NotifyOrderCompleted(this, _windowIndex, _order);
                RefreshVisual(null);
            }
            else
            {
                manager.NotifyOrderProgress(_windowIndex, _order);
                RefreshVisual(_order);
            }
        }

        public IEnumerator RespawnRoutine()
        {
            _order = null;
            _waitingRespawn = true;
            _respawnRemaining = GameConstants.OrderRespawnDelay;
            RefreshVisual(null);
            yield break;
        }

        private void SpawnOrder()
        {
            int size = Random.value < 0.5f ? 2 : 3;
            var picks = new IngredientType[size];
            for (int i = 0; i < size; i++)
                picks[i] = (IngredientType)Random.Range(0, 3);
            _order = new OrderData(picks);
            RefreshVisual(_order);
            GameManager.Instance?.NotifyOrderProgress(_windowIndex, _order);
        }

        private void RefreshVisual(OrderData order)
        {
            if (_statusVisual == null) return;
            if (order == null)
            {
                _statusVisual.material.color = new Color(0.3f, 0.3f, 0.3f);
                return;
            }
            _statusVisual.material.color = order.IsComplete
                ? new Color(0.3f, 0.9f, 0.3f)
                : new Color(1f, 0.75f, 0.2f);
        }
    }
}
