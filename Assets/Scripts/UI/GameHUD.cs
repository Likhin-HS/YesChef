using System;
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

        [Header("Top Bar")]
        [SerializeField] private Text _scoreText;
        [SerializeField] private Text _highScoreText;
        [SerializeField] private Text _timerText;

        [Header("Bottom Bar & Controls")]
        [SerializeField] private Text _controlsTitle;
        [SerializeField] private Text _promptText;
        [SerializeField] private Button _pauseButton;
        [SerializeField] private Button _quitButton;

        [Header("Panels")]
        [SerializeField] private GameObject _startPanel;
        [SerializeField] private GameObject _pausePanel;
        [SerializeField] private Button _pauseResumeButton;
        [SerializeField] private Button _pauseQuitButton;
        [SerializeField] private GameObject _gameOverPanel;
        [SerializeField] private Text _gameOverText;

        private const string DefaultControlsTitle = "Controls";
        private const string DefaultControlsBody = "<b>W  A  S  D</b>   - Move\n<b>E</b>               - Interact / Pick up / Place\n<b>Q</b>               - Collect / Cycle\n<b>Trash</b>       - Discard item";

        private string _lastPrompt = null;
        private int _lastTimerSecond = -1;

        private void Start()
        {
            if (_manager == null) _manager = GameManager.Instance;
            if (_player == null) _player = FindAnyObjectByType<PlayerController>();
            if (_interactor == null) _interactor = FindAnyObjectByType<PlayerInteractor>();
            if (_fridge == null) _fridge = FindAnyObjectByType<Refrigerator>();
            if (_manager == null) return;

            // Wire button callbacks
            _pauseButton?.onClick.AddListener(() => _manager.TogglePause());
            _quitButton?.onClick.AddListener(() => _manager.QuitGame());

            // Wire modals
            WireModalButton(_startPanel, () => _manager.StartGame());
            WirePauseModal();
            WireModalButton(_gameOverPanel, () => _manager.RestartGame());

            // Subscribe to events
            _manager.StateChanged += OnState;
            _manager.TimerChanged += OnTimer;
            _manager.ScoreChanged += OnScore;

            OnState(_manager.State);
            OnScore(_manager.Score, _manager.HighScore);
            OnTimer(_manager.TimeRemaining);
        }

        private void OnDestroy()
        {
            if (_manager != null)
            {
                _manager.StateChanged -= OnState;
                _manager.TimerChanged -= OnTimer;
                _manager.ScoreChanged -= OnScore;
            }
        }

        private void Update()
        {
            if (_manager == null) return;

            HandleKeyboardShortcuts();
            UpdatePrompt();
        }

        private void HandleKeyboardShortcuts()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (_manager.IsPlaying)
            {
                if (kb.digit1Key.wasPressedThisFrame) _fridge?.Select(IngredientType.Vegetable);
                else if (kb.digit2Key.wasPressedThisFrame) _fridge?.Select(IngredientType.Cheese);
                else if (kb.digit3Key.wasPressedThisFrame) _fridge?.Select(IngredientType.Meat);
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
                if (string.IsNullOrEmpty(currentPrompt))
                {
                    if (_controlsTitle != null) _controlsTitle.text = DefaultControlsTitle;
                    _promptText.text = DefaultControlsBody;
                }
                else
                {
                    if (_controlsTitle != null) _controlsTitle.text = "<color=#F1C40F>★ CURRENT ACTION</color>";
                    string promptColor = currentPrompt.Contains("!") ? "#F39C12" : "#2ECC71";
                    _promptText.text = $"<color={promptColor}><b>▶ {currentPrompt}</b></color>\n<size=19><color=#8892B0><b>W A S D</b> - Move  •  <b>E</b> - Interact  •  <b>Q</b> - Collect/Cycle</color></size>";
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

            if (_highScoreText != null)
            {
                _timerText.text = $"{total / 60:D2}:{total % 60:D2}";
            }
            else
            {
                _timerText.text = $"Time {total / 60}:{total % 60:D2}";
            }
            _timerText.color = remaining <= 30f ? new Color(1f, 0.35f, 0.35f) : Color.white;
        }

        private void OnScore(int score, int high)
        {
            if (_scoreText != null)
            {
                if (_highScoreText != null)
                {
                    _scoreText.text = score.ToString();
                    _highScoreText.text = high.ToString();
                }
                else
                {
                    _scoreText.text = $"Score {score}  •  Best {high}";
                }
            }
        }

        // ----- Helpers -----

        private static void WireModalButton(GameObject modal, UnityEngine.Events.UnityAction action)
        {
            if (modal == null) return;
            var btn = modal.GetComponentInChildren<Button>(true);
            if (btn != null) btn.onClick.AddListener(action);
        }

        private void WirePauseModal()
        {
            if (_pausePanel == null) return;

            // Wire explicit serialized references if assigned
            if (_pauseResumeButton != null)
            {
                _pauseResumeButton.onClick.AddListener(() => _manager.TogglePause());
            }
            if (_pauseQuitButton != null)
            {
                _pauseQuitButton.onClick.AddListener(() => _manager.QuitGame());
            }

            // Inspect all buttons under _pausePanel to ensure any Resume/Quit button is wired
            var buttons = _pausePanel.GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons)
            {
                if (btn == null) continue;
                string btnName = btn.gameObject.name.ToLowerInvariant();
                var label = btn.GetComponentInChildren<Text>(true);
                string text = label != null ? label.text.ToLowerInvariant() : string.Empty;

                if (btnName.Contains("quit") || text.Contains("quit"))
                {
                    if (btn != _pauseQuitButton)
                    {
                        btn.onClick.AddListener(() => _manager.QuitGame());
                    }
                }
                else if (btnName.Contains("resume") || text.Contains("resume") || text.Contains("continue") || btnName.Contains("play"))
                {
                    if (btn != _pauseResumeButton)
                    {
                        btn.onClick.AddListener(() => _manager.TogglePause());
                    }
                }
            }
        }
    }
}
