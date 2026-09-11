using System;
using UnityEngine;
using UnityEngine.InputSystem;
using YesChef.Core;
using YesChef.Data;

namespace YesChef.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        public event Action<IngredientItem?> HeldChanged;

        [SerializeField] private float _moveSpeed = GameConstants.PlayerMoveSpeed;
        [SerializeField] private Renderer _heldVisual;
        [SerializeField] private Transform _heldAnchor;

        private CharacterController _controller;
        private IngredientItem? _held;

        public IngredientItem? Held => _held;
        public bool HasHeld => _held.HasValue;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (_heldVisual != null)
                _heldVisual.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (Managers.GameManager.Instance != null && !Managers.GameManager.Instance.IsPlaying)
                return;

            Vector2 input = Vector2.zero;
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) input.y += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) input.y -= 1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input.x -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input.x += 1f;
            }
            var gamepad = Gamepad.current;
            if (gamepad != null)
                input += Vector2.ClampMagnitude(gamepad.leftStick.ReadValue() + gamepad.dpad.ReadValue(), 1f);
            input = Vector2.ClampMagnitude(input, 1f);

            Vector3 move = new Vector3(input.x, 0f, input.y) * _moveSpeed * Time.deltaTime;
            move.y = -2f * Time.deltaTime;
            _controller.Move(move);

            Vector3 clamped = transform.position;
            clamped.x = Mathf.Clamp(clamped.x, -GameConstants.KitchenWidth / 2f + 0.8f, GameConstants.KitchenWidth / 2f - 0.8f);
            clamped.z = Mathf.Clamp(clamped.z, -GameConstants.KitchenDepth / 2f + 0.8f, GameConstants.KitchenDepth / 2f - 0.8f);
            clamped.y = 0f;
            transform.position = clamped;

            if (move.sqrMagnitude > 0.0001f)
            {
                Vector3 face = new Vector3(move.x, 0f, move.z);
                if (face.sqrMagnitude > 0.0001f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(face), 12f * Time.deltaTime);
            }
        }

        public bool TryGive(IngredientItem item)
        {
            if (_held.HasValue) return false;
            _held = item;
            RefreshHeldVisual();
            return true;
        }

        public bool TryTake(out IngredientItem item)
        {
            if (!_held.HasValue)
            {
                item = default;
                return false;
            }
            item = _held.Value;
            _held = null;
            RefreshHeldVisual();
            return true;
        }

        public void ClearHeldVisual()
        {
            _held = null;
            RefreshHeldVisual();
        }

        private void RefreshHeldVisual()
        {
            HeldChanged?.Invoke(_held);
            if (_heldVisual == null) return;
            if (!_held.HasValue)
            {
                _heldVisual.gameObject.SetActive(false);
                return;
            }
            _heldVisual.gameObject.SetActive(true);
            _heldVisual.material.color = _held.Value.DisplayColor;
        }
    }
}
