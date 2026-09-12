using UnityEngine;
using YesChef.Core;
using YesChef.Data;
using YesChef.Player;

namespace YesChef.Stations
{
    /// <summary>
    /// Chops one raw vegetable over <see cref="GameConstants.TableChopDuration"/> seconds.
    /// Player may walk away; E or Q collects the chopped result when hands are free.
    /// Uses MaterialPropertyBlock for zero-allocation, batch-friendly color swapping.
    /// </summary>
    public sealed class ChoppingTable : Interactable
    {
        private static readonly int s_ColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int s_LegacyColorId = Shader.PropertyToID("_Color");

        [SerializeField] private Renderer _itemVisual;
        [SerializeField] private Transform _progressBar;
        [SerializeField] private Transform _progressFill;
        [SerializeField] private TextMesh _worldTimerText;

        private MaterialPropertyBlock _propBlock;
        private bool _busy;
        private bool _ready;
        private float _remaining;

        public bool IsBusy => _busy;
        public bool IsReady => _ready;
        public float RemainingTime => _remaining;
        public float Progress => _busy ? 1f - (_remaining / GameConstants.TableChopDuration) : (_ready ? 1f : 0f);

        private void Awake()
        {
            SetStationName("Table");
            _propBlock = new MaterialPropertyBlock();
            if (_worldTimerText == null)
            {
                _worldTimerText = GetComponentInChildren<TextMesh>(true);
            }
            RefreshVisuals();
        }

        private void Update()
        {
            if (!_busy) return;

            _remaining -= Time.deltaTime;
            if (_remaining <= 0f)
            {
                _busy = false;
                _ready = true;
                _remaining = 0f;
            }
            RefreshVisuals();
        }

        public override string GetPrompt(PlayerController player)
        {
            if (_ready)
            {
                return player.HasHeld ? "Hands full — deliver or trash before taking veg" : "E or Q: pick up chopped vegetable";
            }
            if (_busy)
            {
                return $"Chopping... {_remaining:F1}s left";
            }
            if (player.Held is { Type: IngredientType.Vegetable, State: IngredientState.Raw })
            {
                return "E: place vegetable to chop";
            }
            return "Bring a raw vegetable here (Fridge: press 1)";
        }

        public override void Interact(PlayerController player)
        {
            if (_ready && !player.HasHeld)
            {
                Alternate(player);
                return;
            }

            if (_busy || _ready) return;

            if (player.Held is { Type: IngredientType.Vegetable, State: IngredientState.Raw })
            {
                if (!player.TryTake(out _)) return;
                _busy = true;
                _ready = false;
                _remaining = GameConstants.TableChopDuration;
                RefreshVisuals();
            }
        }

        public override void Alternate(PlayerController player)
        {
            if (!_ready || player.HasHeld) return;
            _ready = false;
            player.TryGive(new IngredientItem(IngredientType.Vegetable, IngredientState.Prepared));
            RefreshVisuals();
        }

        public void ResetStation()
        {
            _busy = false;
            _ready = false;
            _remaining = 0f;
            RefreshVisuals();
        }

        private void RefreshVisuals()
        {
            if (_itemVisual != null)
            {
                bool show = _busy || _ready;
                _itemVisual.gameObject.SetActive(show);
                if (show)
                {
                    Color color = _ready ? GameConstants.VegPreparedColor : GameConstants.VegRawColor;
                    _itemVisual.GetPropertyBlock(_propBlock);
                    _propBlock.SetColor(s_ColorId, color);
                    _propBlock.SetColor(s_LegacyColorId, color);
                    _itemVisual.SetPropertyBlock(_propBlock);
                }
            }

            if (_progressBar != null)
            {
                _progressBar.gameObject.SetActive(_busy);
            }

            if (_progressFill != null && _busy)
            {
                float p = Mathf.Clamp01(Progress);
                _progressFill.localScale = new Vector3(Mathf.Max(p, 0.001f), 1f, 1f);
                _progressFill.localPosition = new Vector3(-0.5f * (1f - p), 0f, 0f);
            }

            if (_worldTimerText != null)
            {
                if (_busy)
                {
                    _worldTimerText.gameObject.SetActive(true);
                    _worldTimerText.text = GameConstants.FormatSeconds(Mathf.CeilToInt(_remaining));
                }
                else if (_ready)
                {
                    _worldTimerText.gameObject.SetActive(true);
                    _worldTimerText.text = "READY";
                    _worldTimerText.color = GameConstants.VegPreparedColor;
                }
                else
                {
                    _worldTimerText.gameObject.SetActive(false);
                }
            }
        }
    }
}
