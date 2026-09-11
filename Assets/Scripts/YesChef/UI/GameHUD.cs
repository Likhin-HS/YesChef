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
    /// Drives all screen-space HUD elements and binds them to
    /// <see cref="GameManager"/> events. UI hierarchy is built in the editor
    /// via Tools > YesChef > Build HUD Canvas; this script handles logic only.
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

        private void Start()
        {
            if (_manager == null) _manager = GameManager.Instance;
            if (_manager == null) return;

            // Wire button callbacks
            _vegButton?.onClick.AddListener(() => { _fridge?.Select(IngredientType.Vegetable); RefreshFridgeUI(); });
            _cheeseButton?.onClick.AddListener(() => { _fridge?.Select(IngredientType.Cheese); RefreshFridgeUI(); });
            _meatButton?.onClick.AddListener(() => { _fridge?.Select(IngredientType.Meat); RefreshFridgeUI(); });
            _pauseButton?.onClick.AddListener(() => _manager.TogglePause());
            _quitButton?.onClick.AddListener(() => _manager.QuitGame());

            // Find the Start / Resume / Play Again buttons inside modals and wire them
            WireModalButton(_startPanel, () => _manager.StartGame());
            WireModalButton(_pausePanel, () => _manager.TogglePause());
            WireModalButton(_gameOverPanel, () => _manager.RestartGame());

            _manager.StateChanged += OnState;
            _manager.TimerChanged += OnTimer;
            _manager.ScoreChanged += OnScore;
            _manager.OrderUpdated += OnOrderUpdated;
            _manager.OrderCompleted += OnOrderCompleted;

            OnState(_manager.State);
            OnScore(_manager.Score, _manager.HighScore);
            OnTimer(_manager.TimeRemaining);
            RefreshFridgeUI();
        }

        private void OnDestroy()
        {
            if (_manager == null) return;
            _manager.StateChanged -= OnState;
            _manager.TimerChanged -= OnTimer;
            _manager.ScoreChanged -= OnScore;
            _manager.OrderUpdated -= OnOrderUpdated;
            _manager.OrderCompleted -= OnOrderCompleted;
        }

        private void Update()
        {
            if (_manager == null) return;

            HandleKeyboardShortcuts();
            UpdatePromptAndHeld();
            UpdateOrderCards();
        }

        private void HandleKeyboardShortcuts()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (_manager.IsPlaying)
            {
                if (kb.digit1Key.wasPressedThisFrame) { _fridge?.Select(IngredientType.Vegetable); RefreshFridgeUI(); }
                else if (kb.digit2Key.wasPressedThisFrame) { _fridge?.Select(IngredientType.Cheese); RefreshFridgeUI(); }
                else if (kb.digit3Key.wasPressedThisFrame) { _fridge?.Select(IngredientType.Meat); RefreshFridgeUI(); }
                else if (kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame) _manager.TogglePause();

                var gp = Gamepad.current;
                if (gp != null && gp.startButton.wasPressedThisFrame) _manager.TogglePause();
            }
            else if (_manager.State == GameState.Paused)
            {
                if (kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame)
                    _manager.TogglePause();
            }
        }

        private void UpdatePromptAndHeld()
        {
            if (_interactor != null && _promptText != null)
                _promptText.text = _manager.IsPlaying ? _interactor.GetPrompt() : string.Empty;

            if (_player != null && _heldText != null)
                _heldText.text = _player.Held is { } held ? $"Held: {held.DisplayName}" : "Held: (empty)";
        }

        private void UpdateOrderCards()
        {
            for (int i = 0; i < GameConstants.MaxActiveOrders; i++)
            {
                if (_orderTimers[i] != null)
                {
                    if (_orders[i] != null && !_orders[i].IsComplete)
                    {
                        _orderTimers[i].text = $"Open {_orders[i].Elapsed:F1}s  \u2022  now {_orders[i].CalculateScore()}";
                        _orderTimers[i].color = _orders[i].Elapsed > 30f ? Color.red : Color.black;
                    }
                    else if (_waiting[i])
                    {
                        _respawnIn[i] = Mathf.Max(0f, _respawnIn[i] - Time.unscaledDeltaTime);
                        _orderTimers[i].text = $"Next in {Mathf.CeilToInt(_respawnIn[i])}s";
                        _orderTimers[i].color = Color.gray;
                    }
                }

                if (_orderPopups[i] != null)
                {
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
                _gameOverText.text = $"Time!  Score: {_manager.Score}  \u2022  Best: {_manager.HighScore}" +
                    (_manager.WasNewHighScore ? "\nNew High Score!" : string.Empty);
            }
        }

        private void OnTimer(float remaining)
        {
            if (_timerText == null) return;
            int total = Mathf.CeilToInt(remaining);
            _timerText.text = $"Time {total / 60}:{total % 60:D2}";
            _timerText.color = remaining <= 30f ? Color.red : Color.black;
        }

        private void OnScore(int score, int high)
        {
            if (_scoreText != null) _scoreText.text = $"Score {score}  \u2022  Best {high}";
        }

        private void OnOrderUpdated(int index, OrderData order)
        {
            if (index < 0 || index >= GameConstants.MaxActiveOrders) return;
            _orders[index] = order;
            _waiting[index] = false;
            if (_orderTitles[index] != null) _orderTitles[index].text = $"Window {index + 1}";
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

            if (_orderNeeds[index] != null)
            {
                _orderNeeds[index].text = "Complete!";
                _orderNeeds[index].color = new Color(0.1f, 0.6f, 0.1f);
            }
            if (_orderPopups[index] != null)
            {
                _orderPopups[index].text = awarded >= 0 ? $"+{awarded}" : $"{awarded}";
                var c = awarded >= 0 ? new Color(0.1f, 0.65f, 0.1f) : Color.red;
                c.a = 1f;
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
                if (_orderNeeds[index] != null) _orderNeeds[index].text = "\u2014";
            }
        }

        // ----- Helpers -----

        private void RefreshFridgeUI()
        {
            if (_fridge == null) return;
            if (_fridgeText != null) _fridgeText.text = $"Fridge: {_fridge.Selected}  (1/2/3)";
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
