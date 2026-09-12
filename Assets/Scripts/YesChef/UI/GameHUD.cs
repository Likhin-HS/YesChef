using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using YesChef.Core;
using YesChef.Data;
using YesChef.Managers;
using YesChef.Player;
using YesChef.Stations;

namespace YesChef.UI
{
    /// <summary>
    /// Drives all screen-space HUD elements and binds them to <see cref="GameManager"/> events.
    /// Engineered for zero per-frame heap allocations (0 B GC Alloc) and minimal Canvas vertex dirtying.
    /// </summary>
    public sealed class GameHUD : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private GameManager _manager;
        [SerializeField] private PlayerController _player;
        [SerializeField] private PlayerInteractor _interactor;
        [SerializeField] private Refrigerator _fridge;
        [SerializeField] private CustomerWindow[] _windows = Array.Empty<CustomerWindow>();

        [Header("Top Bar")]
        [SerializeField] private Text _scoreText;
        [SerializeField] private Text _timerText;
        [SerializeField] private Text _heldText;

        [Header("Bottom Bar")]
        [SerializeField] private Text _promptText;
        [SerializeField] private Text _fridgeText;
        [SerializeField] private Button _vegButton;
        [SerializeField] private Button _cheeseButton;
        [SerializeField] private Button _meatButton;
        [SerializeField] private Button _pauseButton;
        [SerializeField] private Button _quitButton;

        [Header("Panels")]
        [SerializeField] private GameObject _startPanel;
        [SerializeField] private GameObject _pausePanel;
        [SerializeField] private GameObject _gameOverPanel;
        [SerializeField] private Text _gameOverText;

        [Header("Order Cards")]
        [SerializeField] private Text[] _orderTitles = new Text[GameConstants.MaxActiveOrders];
        [SerializeField] private Text[] _orderNeeds = new Text[GameConstants.MaxActiveOrders];
        [SerializeField] private Text[] _orderTimers = new Text[GameConstants.MaxActiveOrders];
        [SerializeField] private Text[] _orderPopups = new Text[GameConstants.MaxActiveOrders];

        private readonly float[] _popupUntil = new float[GameConstants.MaxActiveOrders];
        private readonly OrderData[] _orders = new OrderData[GameConstants.MaxActiveOrders];
        private readonly bool[] _waiting = new bool[GameConstants.MaxActiveOrders];
        private readonly float[] _respawnIn = new float[GameConstants.MaxActiveOrders];
        private readonly int[] _lastCardSeconds = new int[GameConstants.MaxActiveOrders];

        private string _lastPrompt = string.Empty;
        private string _lastHeldDisplayName = string.Empty;
        private int _lastTimerSecond = -1;

        private void Start()
        {
            if (_manager == null) _manager = GameManager.Instance;
            if (_player == null) _player = FindAnyObjectByType<PlayerController>();
            if (_interactor == null) _interactor = FindAnyObjectByType<PlayerInteractor>();
            if (_fridge == null) _fridge = FindAnyObjectByType<Refrigerator>();
            if (_manager == null) return;

            // Wire button callbacks
            _vegButton?.onClick.AddListener(() => SelectIngredient(IngredientType.Vegetable));
            _cheeseButton?.onClick.AddListener(() => SelectIngredient(IngredientType.Cheese));
            _meatButton?.onClick.AddListener(() => SelectIngredient(IngredientType.Meat));
            _pauseButton?.onClick.AddListener(() => _manager.TogglePause());
            _quitButton?.onClick.AddListener(() => _manager.QuitGame());

            // Wire modals
            WireModalButton(_startPanel, () => _manager.StartGame());
            WireModalButton(_pausePanel, () => _manager.TogglePause());
            WireModalButton(_gameOverPanel, () => _manager.RestartGame());

            // Subscribe to events
            _manager.StateChanged += OnState;
            _manager.TimerChanged += OnTimer;
            _manager.ScoreChanged += OnScore;
            _manager.OrderUpdated += OnOrderUpdated;
            _manager.OrderCompleted += OnOrderCompleted;

            if (_player != null)
            {
                _player.HeldChanged += OnHeldChanged;
            }

            for (int i = 0; i < GameConstants.MaxActiveOrders; i++)
            {
                _lastCardSeconds[i] = -1;
            }

            OnState(_manager.State);
            OnScore(_manager.Score, _manager.HighScore);
            OnTimer(_manager.TimeRemaining);
            RefreshFridgeUI();
            RefreshHeldUI(_player != null ? _player.Held : null);

            StartCoroutine(OrderCardTickRoutine());
        }

        private void OnDestroy()
        {
            if (_manager != null)
            {
                _manager.StateChanged -= OnState;
                _manager.TimerChanged -= OnTimer;
                _manager.ScoreChanged -= OnScore;
                _manager.OrderUpdated -= OnOrderUpdated;
                _manager.OrderCompleted -= OnOrderCompleted;
            }
            if (_player != null)
            {
                _player.HeldChanged -= OnHeldChanged;
            }
        }

        private void Update()
        {
            if (_manager == null) return;

            HandleKeyboardShortcuts();
            UpdatePrompt();
            UpdatePopupFade();
        }

        private IEnumerator OrderCardTickRoutine()
        {
            var wait = new WaitForSeconds(0.2f);
            while (true)
            {
                yield return wait;
                if (_manager != null && _manager.IsPlaying)
                {
                    TickOrderCardTimers();
                }
            }
        }

        private void HandleKeyboardShortcuts()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (_manager.IsPlaying)
            {
                if (kb.digit1Key.wasPressedThisFrame) SelectIngredient(IngredientType.Vegetable);
                else if (kb.digit2Key.wasPressedThisFrame) SelectIngredient(IngredientType.Cheese);
                else if (kb.digit3Key.wasPressedThisFrame) SelectIngredient(IngredientType.Meat);
                else if (kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame) _manager.TogglePause();
            }
            else if (_manager.State == GameState.Paused)
            {
                if (kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame)
                {
                    _manager.TogglePause();
                }
            }
        }

        private void UpdatePrompt()
        {
            if (_interactor == null || _promptText == null) return;

            string currentPrompt = _manager.IsPlaying ? _interactor.GetPrompt() : string.Empty;
            if (currentPrompt != _lastPrompt)
            {
                _lastPrompt = currentPrompt;
                _promptText.text = currentPrompt;
            }
        }

        private void OnHeldChanged(IngredientItem? item)
        {
            RefreshHeldUI(item);
        }

        private void RefreshHeldUI(IngredientItem? item)
        {
            if (_heldText == null) return;

            string name = item.HasValue ? item.Value.DisplayName : "(empty)";
            if (name != _lastHeldDisplayName)
            {
                _lastHeldDisplayName = name;
                _heldText.text = "Held: " + name;
            }
        }

        private void TickOrderCardTimers()
        {
            for (int i = 0; i < GameConstants.MaxActiveOrders; i++)
            {
                if (_waiting[i])
                {
                    _respawnIn[i] = Mathf.Max(0f, _respawnIn[i] - 0.2f);
                    int remainingSec = Mathf.CeilToInt(_respawnIn[i]);
                    if (remainingSec != _lastCardSeconds[i] && _orderTimers[i] != null)
                    {
                        _lastCardSeconds[i] = remainingSec;
                        _orderTimers[i].text = GameConstants.FormatNextIn(remainingSec);
                        _orderTimers[i].color = Color.gray;
                    }
                }
                else if (_orders[i] is { IsComplete: false } order && _orderTimers[i] != null)
                {
                    int elapsedSec = (int)order.Elapsed;
                    if (elapsedSec != _lastCardSeconds[i])
                    {
                        _lastCardSeconds[i] = elapsedSec;
                        _orderTimers[i].text = "Open " + GameConstants.FormatSeconds(elapsedSec) + "  •  Score " + order.CalculateScore();
                        _orderTimers[i].color = order.Elapsed > 30f ? Color.red : Color.black;
                    }
                }
            }
        }

        private void UpdatePopupFade()
        {
            for (int i = 0; i < GameConstants.MaxActiveOrders; i++)
            {
                if (_orderPopups[i] == null) continue;

                if (Time.unscaledTime < _popupUntil[i])
                {
                    float t = (_popupUntil[i] - Time.unscaledTime) / GameConstants.ScorePopupDuration;
                    var c = _orderPopups[i].color;
                    c.a = Mathf.Clamp01(t);
                    _orderPopups[i].color = c;
                }
                else if (_orderPopups[i].color.a > 0f)
                {
                    var c = _orderPopups[i].color;
                    c.a = 0f;
                    _orderPopups[i].color = c;
                }
            }
        }

        // ----- Event handlers -----

        private void OnState(GameState state)
        {
            if (_startPanel != null) _startPanel.SetActive(state == GameState.NotStarted);
            if (_pausePanel != null) _pausePanel.SetActive(state == GameState.Paused);
            if (_gameOverPanel != null) _gameOverPanel.SetActive(state == GameState.GameOver);
            if (_pauseButton != null) _pauseButton.gameObject.SetActive(state == GameState.Playing || state == GameState.Paused);

            if (state == GameState.GameOver && _gameOverText != null)
            {
                _gameOverText.text = $"Time!  Score: {_manager.Score}  •  Best: {_manager.HighScore}" +
                    (_manager.WasNewHighScore ? "\nNew High Score!" : string.Empty);
            }
        }

        private void OnTimer(float remaining)
        {
            if (_timerText == null) return;

            int total = Mathf.CeilToInt(remaining);
            if (total == _lastTimerSecond) return;
            _lastTimerSecond = total;

            _timerText.text = $"Time {total / 60}:{total % 60:D2}";
            _timerText.color = remaining <= 30f ? Color.red : Color.black;
        }

        private void OnScore(int score, int high)
        {
            if (_scoreText != null)
            {
                _scoreText.text = $"Score {score}  •  Best {high}";
            }
        }

        private void OnOrderUpdated(int index, OrderData order)
        {
            if (index < 0 || index >= GameConstants.MaxActiveOrders) return;

            _orders[index] = order;
            _waiting[index] = false;
            _lastCardSeconds[index] = -1; // force card timer refresh

            if (_orderTitles[index] != null)
            {
                _orderTitles[index].text = $"Window {index + 1}";
            }
            if (_orderNeeds[index] != null)
            {
                _orderNeeds[index].text = order.IsComplete ? "Complete!" : order.GetRequirementText();
                _orderNeeds[index].color = order.IsComplete ? new Color(0.1f, 0.6f, 0.1f) : Color.black;
            }
        }

        private void OnOrderCompleted(int index, int awarded)
        {
            if (index < 0 || index >= GameConstants.MaxActiveOrders) return;

            _orders[index] = null;
            _waiting[index] = true;
            _respawnIn[index] = GameConstants.OrderRespawnDelay;
            _lastCardSeconds[index] = -1;

            if (_orderNeeds[index] != null)
            {
                _orderNeeds[index].text = "Complete!";
                _orderNeeds[index].color = new Color(0.1f, 0.6f, 0.1f);
            }
            if (_orderPopups[index] != null)
            {
                _orderPopups[index].text = awarded >= 0 ? $"+{awarded}" : $"{awarded}";
                var c = awarded >= 0 ? GameConstants.PopupGoodColor : GameConstants.PopupBadColor;
                _orderPopups[index].color = c;
                _popupUntil[index] = Time.unscaledTime + GameConstants.ScorePopupDuration;
            }
            StartCoroutine(ClearWaitingFlag(index));
        }

        private IEnumerator ClearWaitingFlag(int index)
        {
            yield return new WaitForSeconds(GameConstants.OrderRespawnDelay + 0.1f);
            if (_orders[index] == null)
            {
                _waiting[index] = false;
                if (_orderTimers[index] != null) _orderTimers[index].text = string.Empty;
                if (_orderNeeds[index] != null) _orderNeeds[index].text = "—";
            }
        }

        // ----- Helpers -----

        private void SelectIngredient(IngredientType type)
        {
            _fridge?.Select(type);
            RefreshFridgeUI();
        }

        private void RefreshFridgeUI()
        {
            if (_fridge == null) return;
            if (_fridgeText != null) _fridgeText.text = $"Fridge: {_fridge.Selected} (1/2/3)";
            HighlightButton(_vegButton, _fridge.Selected == IngredientType.Vegetable);
            HighlightButton(_cheeseButton, _fridge.Selected == IngredientType.Cheese);
            HighlightButton(_meatButton, _fridge.Selected == IngredientType.Meat);
        }

        private static void HighlightButton(Button button, bool on)
        {
            if (button == null) return;
            var colors = button.colors;
            colors.normalColor = on ? new Color(0.6f, 1f, 0.6f) : Color.white;
            button.colors = colors;
        }

        private static void WireModalButton(GameObject modal, UnityEngine.Events.UnityAction action)
        {
            if (modal == null) return;
            var btn = modal.GetComponentInChildren<Button>(true);
            if (btn != null) btn.onClick.AddListener(action);
        }
    }
}
