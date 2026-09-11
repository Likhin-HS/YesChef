using UnityEngine;
using UnityEngine.InputSystem;
using YesChef.Core;

namespace YesChef.Player
{
    /// <summary>
    /// Finds the nearest interactable inside a radius and routes E / Q to it.
    /// Exposes a prompt string so the HUD always tells the player what E does.
    /// The candidate set is cached (all stations are built upfront) and matched
    /// with squared distances to avoid per-frame allocations.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private float _radius = GameConstants.InteractionRadius;

        private PlayerController _player;
        private Interactable[] _all = System.Array.Empty<Interactable>();
        private Interactable _current;
        private float _radiusSqr;

        public Interactable Current => _current;

        private void Awake()
        {
            _player = GetComponent<PlayerController>();
            _radiusSqr = _radius * _radius;
        }

        private void Start()
        {
            RefreshCache();
        }

        private void OnValidate()
        {
            _radiusSqr = _radius * _radius;
        }

        private void Update()
        {
            RefreshCurrent();

            var keyboard = Keyboard.current;
            var gamepad = Gamepad.current;
            bool interactPressed = (keyboard != null && keyboard.eKey.wasPressedThisFrame)
                || (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame);
            bool alternatePressed = (keyboard != null && keyboard.qKey.wasPressedThisFrame)
                || (gamepad != null && gamepad.buttonWest.wasPressedThisFrame);

            if (_current == null) return;
            var manager = Managers.GameManager.Instance;
            if (manager != null && !manager.IsPlaying) return;

            if (interactPressed) _current.Interact(_player);
            else if (alternatePressed) _current.Alternate(_player);
        }

        public string GetPrompt()
        {
            if (_current == null)
                return "WASD move  •  E interact  •  Q pick cooked/chopped";
            return _current.GetPrompt(_player);
        }

        private void RefreshCache()
        {
            _all = FindObjectsByType<Interactable>(FindObjectsSortMode.None);
        }

        private void RefreshCurrent()
        {
            if (_all == null || _all.Length == 0)
                RefreshCache();
            float best = float.MaxValue;
            Interactable winner = null;
            Vector3 self = transform.position;

            foreach (var item in _all)
            {
                if (item == null || !item.isActiveAndEnabled) continue;
                float d = (self - item.transform.position).sqrMagnitude;
                if (d <= _radiusSqr && d < best)
                {
                    best = d;
                    winner = item;
                }
            }
            _current = winner;
        }
    }
}
