using System;
using UnityEngine;
using UnityEngine.InputSystem;
using YesChef.Core;
using YesChef.Data;
using YesChef.Managers;

namespace YesChef.Player
{
    /// <summary>
    /// Handles player movement, kitchen boundary clamping, and held items.
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

        [Header("Held Models")]
        [SerializeField] private GameObject _heldRawVeg;
        [SerializeField] private GameObject _heldChoppedVeg;
        [SerializeField] private GameObject _heldRawMeat;
        [SerializeField] private GameObject _heldCookedMeat;
        [SerializeField] private GameObject _heldCheese;

        [Header("Overhead Floating Badge")]
        [SerializeField] private GameObject _overheadBadge;
        [SerializeField] private TextMesh _overheadText;

        private CharacterController _controller;
        private IngredientItem? _held;
        private MaterialPropertyBlock _propBlock;
        private Vector3 _spawnPosition;
        private Quaternion _spawnRotation;

        public IngredientItem? Held => _held;
        public bool HasHeld => _held.HasValue;
        public bool IsMoving { get; private set; }
        public Vector2 MoveInput { get; private set; }
        public float MoveSpeed => _moveSpeed;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _propBlock = new MaterialPropertyBlock();
            _spawnPosition = transform.position;
            _spawnRotation = transform.rotation;
            if (_heldVisual != null) _heldVisual.gameObject.SetActive(false);
            if (_heldRawVeg != null) _heldRawVeg.SetActive(false);
            if (_heldChoppedVeg != null) _heldChoppedVeg.SetActive(false);
            if (_heldRawMeat != null) _heldRawMeat.SetActive(false);
            if (_heldCookedMeat != null) _heldCookedMeat.SetActive(false);
            if (_heldCheese != null) _heldCheese.SetActive(false);
            if (_overheadBadge != null) _overheadBadge.SetActive(false);
        }

        private void Update()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
            {
                MoveInput = Vector2.zero;
                IsMoving = false;
                return;
            }

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
            MoveInput = input;
            IsMoving = input.sqrMagnitude > 0.001f;

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

        private void LateUpdate()
        {
            if (_overheadBadge != null && _overheadBadge.activeSelf)
            {
                _overheadBadge.transform.rotation = Quaternion.Euler(62f, 0f, 0f);
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

        public void ResetPosition()
        {
            if (_controller != null)
            {
                _controller.enabled = false;
                transform.position = _spawnPosition;
                transform.rotation = _spawnRotation;
                _controller.enabled = true;
            }
            else
            {
                transform.position = _spawnPosition;
                transform.rotation = _spawnRotation;
            }
            MoveInput = Vector2.zero;
            IsMoving = false;
        }

        private void RefreshHeldVisual()
        {
            HeldChanged?.Invoke(_held);

            bool hasItem = _held.HasValue;

            if (_heldRawVeg != null)
                _heldRawVeg.SetActive(hasItem && _held.Value.Type == IngredientType.Vegetable && !_held.Value.IsPrepared);

            if (_heldChoppedVeg != null)
                _heldChoppedVeg.SetActive(hasItem && _held.Value.Type == IngredientType.Vegetable && _held.Value.IsPrepared);

            if (_heldRawMeat != null)
                _heldRawMeat.SetActive(hasItem && _held.Value.Type == IngredientType.Meat && !_held.Value.IsPrepared);

            if (_heldCookedMeat != null)
                _heldCookedMeat.SetActive(hasItem && _held.Value.Type == IngredientType.Meat && _held.Value.IsPrepared);

            if (_heldCheese != null)
                _heldCheese.SetActive(hasItem && _held.Value.Type == IngredientType.Cheese);

            bool hasSpecific = (_heldRawVeg != null && _heldRawVeg.activeSelf) ||
                               (_heldChoppedVeg != null && _heldChoppedVeg.activeSelf) ||
                               (_heldRawMeat != null && _heldRawMeat.activeSelf) ||
                               (_heldCookedMeat != null && _heldCookedMeat.activeSelf) ||
                               (_heldCheese != null && _heldCheese.activeSelf);

            if (_heldVisual != null)
            {
                if (!hasItem || hasSpecific)
                {
                    _heldVisual.gameObject.SetActive(false);
                }
                else
                {
                    _heldVisual.gameObject.SetActive(true);
                    Color itemColor = _held.Value.DisplayColor;
                    _heldVisual.GetPropertyBlock(_propBlock);
                    _propBlock.SetColor(s_ColorId, itemColor);
                    _propBlock.SetColor(s_LegacyColorId, itemColor);
                    _heldVisual.SetPropertyBlock(_propBlock);
                }
            }

            if (_overheadBadge != null)
            {
                _overheadBadge.SetActive(hasItem);
                if (hasItem && _overheadText != null)
                {
                    var item = _held.Value;
                    if (item.Type == IngredientType.Vegetable)
                    {
                        _overheadText.text = item.IsPrepared
                            ? "Chopped Veg\n<color=#2ecc71>READY TO SERVE</color>"
                            : "Raw Veg (Cabbage)\n<color=#f39c12>CHOP ON TABLE</color>";
                    }
                    else if (item.Type == IngredientType.Meat)
                    {
                        _overheadText.text = item.IsPrepared
                            ? "Cooked Meat\n<color=#2ecc71>READY TO SERVE</color>"
                            : "Raw Meat (Steak)\n<color=#e74c3c>COOK ON STOVE</color>";
                    }
                    else if (item.Type == IngredientType.Cheese)
                    {
                        _overheadText.text = "Cheese\n<color=#2ecc71>READY TO SERVE</color>";
                    }
                }
            }
        }
    }
}
