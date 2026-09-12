using UnityEngine;
using YesChef.Core;
using YesChef.Data;
using YesChef.Managers;
using YesChef.Player;

namespace YesChef
{
    /// <summary>
    /// One of four serving windows. Generates 2-3 random ingredients,
    /// ticks its age while playing, accepts prepared items, displays world-space UI, and respawns after 5 seconds.
    /// Uses MaterialPropertyBlock for status lamp rendering to prevent material leaks.
    /// </summary>
    public sealed class CustomerWindow : Interactable
    {
        private static readonly int s_ColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int s_LegacyColorId = Shader.PropertyToID("_Color");

        [SerializeField] private int _windowIndex;
        [SerializeField] private Renderer _statusVisual;
        [SerializeField] private TextMesh _worldRequirementText;
        [SerializeField] private TextMesh _worldTimerText;
        [SerializeField] private TextMesh _scorePopup;

        private OrderData _order;
        private bool _waitingRespawn;
        private float _respawnRemaining;
        private float _popupRemaining;
        private Vector3 _popupInitialLocalPos;
        private MaterialPropertyBlock _propBlock;

        public int WindowIndex => _windowIndex;
        public OrderData Order => _order;
        public bool HasOrder => _order != null && !_order.IsComplete;
        public bool WaitingRespawn => _waitingRespawn;
        public float RespawnRemaining => _respawnRemaining;

        private static readonly string[] s_WindowNames = { "Window 1", "Window 2", "Window 3", "Window 4" };

        private void Awake()
        {
            int idx = Mathf.Clamp(_windowIndex, 0, 3);
            SetStationName(s_WindowNames[idx]);
            _propBlock = new MaterialPropertyBlock();

            if (_scorePopup != null)
            {
                _popupInitialLocalPos = _scorePopup.transform.localPosition;
                _scorePopup.gameObject.SetActive(false);
            }
        }

        public void Configure(int index)
        {
            _windowIndex = index;
            int idx = Mathf.Clamp(index, 0, 3);
            SetStationName(s_WindowNames[idx]);
        }

        private int _lastWorldTimerSec = -1;

        private void Update()
        {
            UpdateScorePopupAnimation();

            var manager = GameManager.Instance;
            if (manager == null || !manager.IsPlaying) return;

            if (_waitingRespawn)
            {
                _respawnRemaining -= Time.deltaTime;
                if (_respawnRemaining <= 0f)
                {
                    _waitingRespawn = false;
                    _respawnRemaining = 0f;
                    SpawnOrder();
                }
                else
                {
                    int sec = Mathf.CeilToInt(_respawnRemaining);
                    if (sec != _lastWorldTimerSec && _worldRequirementText != null)
                    {
                        _lastWorldTimerSec = sec;
                        _worldRequirementText.text = GameConstants.FormatNextIn(sec);
                    }
                }
                return;
            }

            if (_order != null && !_order.IsComplete)
            {
                _order.Tick(Time.deltaTime);
                int sec = (int)_order.Elapsed;
                if (sec != _lastWorldTimerSec && _worldTimerText != null)
                {
                    _lastWorldTimerSec = sec;
                    _worldTimerText.text = GameConstants.FormatSeconds(sec);
                    _worldTimerText.color = _order.Elapsed > 30f ? Color.red : Color.white;
                }
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
            _order = null;
            _waitingRespawn = false;
            _respawnRemaining = 0f;
            _lastWorldTimerSec = -1;
            RefreshVisual(null);
            RefreshWorldRequirements();
            if (_worldTimerText != null) _worldTimerText.gameObject.SetActive(false);
        }

        public override string GetPrompt(PlayerController player)
        {
            if (_waitingRespawn)
            {
                return GameConstants.FormatNextIn(Mathf.CeilToInt(_respawnRemaining));
            }
            if (!HasOrder)
            {
                return "Waiting for customer...";
            }
            if (player.Held is not { } held)
            {
                return _order.GetRequirementText();
            }
            if (!held.IsPrepared)
            {
                return "Ingredient is not prepared yet";
            }
            return _order.Needs(held.Type)
                ? "E: serve ingredient"
                : "Ingredient not needed here";
        }

        public override void Interact(PlayerController player)
        {
            if (!HasOrder) return;
            if (player.Held is not { } held || !held.IsPrepared) return;
            if (!_order.TryFulfill(held.Type)) return;

            player.TryTake(out _);

            var manager = GameManager.Instance;
            if (_order.IsComplete)
            {
                manager?.NotifyOrderCompleted(this, _windowIndex, _order);
                RefreshVisual(null);
            }
            else
            {
                manager?.NotifyOrderProgress(_windowIndex, _order);
                RefreshVisual(_order);
            }
            RefreshWorldRequirements();
        }

        public void BeginRespawn()
        {
            _order = null;
            _waitingRespawn = true;
            _respawnRemaining = GameConstants.OrderRespawnDelay;
            _lastWorldTimerSec = -1;
            RefreshVisual(null);
            RefreshWorldRequirements();
            if (_worldTimerText != null) _worldTimerText.gameObject.SetActive(false);
        }

        private void SpawnOrder()
        {
            int size = Random.value < 0.5f ? 2 : 3;
            var picks = new IngredientType[size];
            for (int i = 0; i < size; i++)
            {
                picks[i] = (IngredientType)Random.Range(0, GameConstants.IngredientTypeCount);
            }
            _order = new OrderData(picks);
            _lastWorldTimerSec = -1;
            RefreshVisual(_order);
            RefreshWorldRequirements();
            GameManager.Instance?.NotifyOrderProgress(_windowIndex, _order);
        }

        /// <summary>
        /// Displays floating score popup (+17 / -6) near the window, animating upwards and fading.
        /// </summary>
        public void ShowScorePopup(int awarded)
        {
            EnsureScorePopupCreated();
            if (_scorePopup == null) return;

            _scorePopup.text = GameConstants.FormatScorePopup(awarded);
            _scorePopup.color = awarded >= 0 ? GameConstants.PopupGoodColor : GameConstants.PopupBadColor;
            _scorePopup.transform.localPosition = _popupInitialLocalPos;
            _scorePopup.gameObject.SetActive(true);
            _popupRemaining = GameConstants.ScorePopupDuration;
        }

        private void EnsureScorePopupCreated()
        {
            if (_scorePopup != null) return;

            var popupGo = new GameObject("ScorePopup", typeof(TextMesh));
            _scorePopup = popupGo.GetComponent<TextMesh>();
            _scorePopup.transform.SetParent(transform, false);
            _popupInitialLocalPos = new Vector3(0f, 2.2f, -0.6f);
            _scorePopup.transform.localPosition = _popupInitialLocalPos;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font != null) _scorePopup.font = font;

            _scorePopup.fontSize = 48;
            _scorePopup.characterSize = 0.12f;
            _scorePopup.anchor = TextAnchor.MiddleCenter;
            _scorePopup.alignment = TextAlignment.Center;
        }

        private void UpdateScorePopupAnimation()
        {
            if (_popupRemaining <= 0f) return;

            _popupRemaining -= Time.unscaledDeltaTime;
            if (_scorePopup == null) return;

            if (_popupRemaining <= 0f)
            {
                _scorePopup.gameObject.SetActive(false);
                return;
            }

            // Smooth float upwards
            float progress = 1f - (_popupRemaining / GameConstants.ScorePopupDuration);
            _scorePopup.transform.localPosition = _popupInitialLocalPos + Vector3.up * (progress * 0.8f);

            // Smooth fade out
            var c = _scorePopup.color;
            c.a = Mathf.Clamp01(_popupRemaining / GameConstants.ScorePopupDuration);
            _scorePopup.color = c;
        }

        private void RefreshVisual(OrderData order)
        {
            if (_statusVisual == null) return;

            Color targetColor;
            if (order == null)
            {
                targetColor = GameConstants.WindowIdleColor;
            }
            else
            {
                targetColor = order.IsComplete ? GameConstants.WindowDoneColor : GameConstants.WindowActiveColor;
            }

            _statusVisual.GetPropertyBlock(_propBlock);
            _propBlock.SetColor(s_ColorId, targetColor);
            _propBlock.SetColor(s_LegacyColorId, targetColor);
            _statusVisual.SetPropertyBlock(_propBlock);
        }

        private void RefreshWorldRequirements()
        {
            if (_worldRequirementText == null) return;

            if (_waitingRespawn)
            {
                _worldRequirementText.text = GameConstants.FormatNextIn(Mathf.CeilToInt(_respawnRemaining));
                _worldRequirementText.color = Color.gray;
            }
            else if (_order != null)
            {
                _worldRequirementText.text = _order.IsComplete ? "Done!" : _order.GetRequirementText();
                _worldRequirementText.color = _order.IsComplete ? GameConstants.WindowDoneColor : Color.white;
            }
            else
            {
                _worldRequirementText.text = "Waiting...";
                _worldRequirementText.color = Color.gray;
            }
        }
    }
}
