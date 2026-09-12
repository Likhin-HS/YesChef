using System;
using UnityEngine;
using YesChef.Core;
using YesChef.Data;
using YesChef.Player;
using YesChef.Stations;

namespace YesChef.Managers
{
    /// <summary>
    /// Owns game state, 3-minute timer, score tally, session high-score persistence, and order management.
    /// Stations and windows report to this manager; UI observes it through decoupled events.
    /// </summary>
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public event Action<GameState> StateChanged;
        public event Action<float> TimerChanged;
        public event Action<int, int> ScoreChanged;
        public event Action<int, OrderData> OrderUpdated;
        public event Action<int, int> OrderCompleted;

        [SerializeField] private CustomerWindow[] _customerWindows = Array.Empty<CustomerWindow>();
        [SerializeField] private ChoppingTable[] _tables = Array.Empty<ChoppingTable>();
        [SerializeField] private Stove[] _stoves = Array.Empty<Stove>();
        [SerializeField] private PlayerController _player;

        private GameState _state = GameState.NotStarted;
        private float _timeRemaining = GameConstants.GameDuration;
        private int _score;
        private int _highScore;
        private bool _wasNewHighScore;

        public GameState State => _state;
        public float TimeRemaining => _timeRemaining;
        public int Score => _score;
        public int HighScore => _highScore;
        public bool IsPlaying => _state == GameState.Playing;
        public bool WasNewHighScore => _wasNewHighScore;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _highScore = PlayerPrefs.GetInt(GameConstants.HighScoreKey, 0);
        }

        private void Start()
        {
            if (_customerWindows.Length == 0)
                _customerWindows = FindObjectsByType<CustomerWindow>();
            if (_tables.Length == 0)
                _tables = FindObjectsByType<ChoppingTable>();
            if (_stoves.Length == 0)
                _stoves = FindObjectsByType<Stove>();
            if (_player == null)
                _player = FindAnyObjectByType<PlayerController>();

            Array.Sort(_customerWindows, (a, b) => a.WindowIndex.CompareTo(b.WindowIndex));
            SetState(GameState.NotStarted);
            TimerChanged?.Invoke(_timeRemaining);
            ScoreChanged?.Invoke(_score, _highScore);
        }

        private void Update()
        {
            if (_state != GameState.Playing) return;

            _timeRemaining = Mathf.Max(0f, _timeRemaining - Time.deltaTime);
            TimerChanged?.Invoke(_timeRemaining);
            if (_timeRemaining <= 0f)
            {
                EndGame();
            }
        }

        public void StartGame()
        {
            ResetStations();
            _score = 0;
            _wasNewHighScore = false;
            _timeRemaining = GameConstants.GameDuration;
            ScoreChanged?.Invoke(_score, _highScore);
            TimerChanged?.Invoke(_timeRemaining);
            SetState(GameState.Playing);

            for (int i = 0; i < _customerWindows.Length; i++)
            {
                _customerWindows[i].SpawnInitialOrder(i);
            }
        }

        public void RestartGame()
        {
            foreach (var window in _customerWindows)
            {
                window.ResetWindow();
            }
            StartGame();
        }

        public void TogglePause()
        {
            if (_state == GameState.Playing)
            {
                SetState(GameState.Paused);
            }
            else if (_state == GameState.Paused)
            {
                SetState(GameState.Playing);
            }
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void NotifyOrderProgress(int windowIndex, OrderData order)
        {
            OrderUpdated?.Invoke(windowIndex, order);
        }

        public void NotifyOrderCompleted(CustomerWindow window, int windowIndex, OrderData order)
        {
            int awarded = order.CalculateScore();
            order.MarkScored(awarded);
            _score += awarded;

            ScoreChanged?.Invoke(_score, _highScore);
            OrderUpdated?.Invoke(windowIndex, order);
            OrderCompleted?.Invoke(windowIndex, awarded);
            window.ShowScorePopup(awarded);
            window.BeginRespawn();
        }

        private void ResetStations()
        {
            foreach (var table in _tables)
            {
                table.ResetStation();
            }
            foreach (var stove in _stoves)
            {
                stove.ResetStation();
            }
            _player?.ClearHeld();
        }

        private void EndGame()
        {
            // Blueprint spec: Evaluate high score at end of 3-minute session
            if (_score > _highScore)
            {
                _highScore = _score;
                _wasNewHighScore = true;
                PlayerPrefs.SetInt(GameConstants.HighScoreKey, _highScore);
                PlayerPrefs.Save();
            }
            else
            {
                _wasNewHighScore = false;
            }

            ScoreChanged?.Invoke(_score, _highScore);
            SetState(GameState.GameOver);
        }

        private void SetState(GameState next)
        {
            _state = next;
            Time.timeScale = next == GameState.Paused ? 0f : 1f;
            StateChanged?.Invoke(next);
        }
    }
}
