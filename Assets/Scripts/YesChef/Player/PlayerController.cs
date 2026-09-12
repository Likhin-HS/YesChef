using System;
using UnityEngine;
using UnityEngine.InputSystem;
using YesChef.Core;
using YesChef.Data;
using YesChef.Managers;

namespace YesChef.Player
{
    /// <summary>
    /// Player character movement and single-held item inventory controller.
    /// Clamped to the kitchen bounds. Enforces single-held item rule strictly.
    /// Uses MaterialPropertyBlock for the held visual to prevent runtime material leaks.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        private static readonly int s_ColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int s_LegacyColorId = Shader.PropertyToID("_Color");

        public event Action<IngredientItem?> HeldChanged;

        [SerializeField] private float _moveSpeed = GameConstants.PlayerMoveSpeed;
        [SerializeField] private Renderer _heldVisual;
        [SerializeField] private Transform _heldAnchor;

        private CharacterController _controller;
        private IngredientItem? _held;
        private MaterialPropertyBlock _propBlock;

        public IngredientItem? Held => _held;
        public bool HasHeld => _held.HasValue;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _propBlock = new MaterialPropertyBlock();
            if (_heldVisual != null)
            {
                _heldVisual.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
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

            // Also support Gamepad left stick if connected
            var gamepad = Gamepad.current;
            if (gamepad != null && input.sqrMagnitude < 0.01f)
            {
                input = gamepad.leftStick.ReadValue();
            }

            input = Vector2.ClampMagnitude(input, 1f);

            Vector3 move = new Vector3(input.x, 0f, input.y) * _moveSpeed * Time.deltaTime;
            move.y = -4f * Time.deltaTime; // slight grounding force
            _controller.Move(move);

            // Safety boundary clamp
            Vector3 pos = transform.position;
            float halfW = GameConstants.KitchenWidth / 2f - 0.8f;
            float halfD = GameConstants.KitchenDepth / 2f - 0.8f;
            if (pos.x < -halfW || pos.x > halfW || pos.z < -halfD || pos.z > halfD)
            {
                pos.x = Mathf.Clamp(pos.x, -halfW, halfW);
                pos.z = Mathf.Clamp(pos.z, -halfD, halfD);
                pos.y = 0f;
                transform.position = pos;
            }

            if (input.sqrMagnitude > 0.001f)
            {
                Vector3 face = new Vector3(input.x, 0f, input.y);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(face), 14f * Time.deltaTime);
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

        public void ClearHeld()
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
            Color itemColor = _held.Value.DisplayColor;
            _heldVisual.GetPropertyBlock(_propBlock);
            _propBlock.SetColor(s_ColorId, itemColor);
            _propBlock.SetColor(s_LegacyColorId, itemColor);
            _heldVisual.SetPropertyBlock(_propBlock);
        }
    }
}
