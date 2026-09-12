using System;
using UnityEngine;
using UnityEngine.InputSystem;
using YesChef.Core;

namespace YesChef.Player
{
    /// <summary>
    /// Finds the nearest interactable inside interaction radius and routes E / Q to it.
    /// Uses Interactable.All static registry for zero scene-traversal allocations.
    /// Exposes a prompt string so the HUD always informs the player of available actions.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public sealed class PlayerInteractor : MonoBehaviour
    {
        public event Action<Interactable> CurrentChanged;

        [SerializeField] private float _radius = GameConstants.InteractionRadius;

        private PlayerController _player;
        private Interactable _current;
        private float _radiusSqr;
        private string _cachedPrompt = string.Empty;

        public Interactable Current => _current;

        private void Awake()
        {
            _player = GetComponent<PlayerController>();
            _radiusSqr = _radius * _radius;
        }

        private void OnValidate()
        {
            _radiusSqr = _radius * _radius;
        }

        private void Update()
        {
            RefreshCurrent();

            var keyboard = Keyboard.current;
            bool interactPressed = keyboard != null && keyboard.eKey.wasPressedThisFrame;
            bool alternatePressed = keyboard != null && keyboard.qKey.wasPressedThisFrame;

            if (_current == null) return;

            var manager = Managers.GameManager.Instance;
            if (manager != null && !manager.IsPlaying) return;

            if (interactPressed)
            {
                _current.Interact(_player);
            }
            else if (alternatePressed)
            {
                _current.Alternate(_player);
            }
        }

        public string GetPrompt()
        {
            if (_current == null)
            {
                return "WASD: Move  •  E: Interact  •  1/2/3: Select Fridge Item";
            }
            return _current.GetPrompt(_player);
        }

        private void RefreshCurrent()
        {
            var candidates = Interactable.All;
            float bestDistanceSqr = float.MaxValue;
            Interactable winner = null;
            Vector3 selfPosition = transform.position;

            int count = candidates.Count;
            for (int i = 0; i < count; i++)
            {
                var item = candidates[i];
                if (item == null || !item.isActiveAndEnabled) continue;

                float distSqr = (selfPosition - item.transform.position).sqrMagnitude;
                if (distSqr <= _radiusSqr && distSqr < bestDistanceSqr)
                {
                    bestDistanceSqr = distSqr;
                    winner = item;
                }
            }

            if (_current != winner)
            {
                _current = winner;
                CurrentChanged?.Invoke(_current);
            }
        }
    }
}
