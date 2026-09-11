using UnityEngine;
using YesChef.Core;
using YesChef.Data;
using YesChef.Player;

namespace YesChef.Stations
{
    /// <summary>
    /// Chops one raw vegetable over <see cref="GameConstants.TableChopDuration"/> seconds.
    /// Player may walk away; Q collects the chopped result.
    /// </summary>
    public sealed class ChoppingTable : Interactable
    {
        [SerializeField] private Renderer _itemVisual;
        [SerializeField] private Transform _progressBar;
        [SerializeField] private Transform _progressFill;

        private bool _busy;
        private bool _ready;
        private float _remaining;

        public bool IsBusy => _busy;
        public bool IsReady => _ready;
        public float Progress => _busy ? 1f - (_remaining / GameConstants.TableChopDuration) : (_ready ? 1f : 0f);

        private void Awake()
        {
            SetStationName("Table");
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
            }
            RefreshVisuals();
        }

        public override string GetPrompt(PlayerController player)
        {
            if (_ready) return "Q: pick up chopped vegetable";
            if (_busy) return $"Chopping... {Mathf.CeilToInt(_remaining)}s left";
            if (player.Held is { Type: IngredientType.Vegetable, State: IngredientState.Raw })
                return "E: place vegetable to chop";
            return "Bring a raw vegetable here (Fridge: press 1)";
        }

        public override void Interact(PlayerController player)
        {
            if (_busy || _ready) return;
            if (player.Held is { Type: IngredientType.Vegetable, State: IngredientState.Raw })
            {
                if (!player.TryTake(out _)) return;
                _busy = true;
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
            RefreshVisuals();
        }

        private void RefreshVisuals()
        {
            if (_itemVisual != null)
            {
                bool show = _busy || _ready;
                _itemVisual.gameObject.SetActive(show);
                if (show)
                    _itemVisual.material.color = _ready
                        ? new Color(0.2f, 0.8f, 0.25f)
                        : new Color(0.35f, 0.55f, 0.25f);
            }
            if (_progressBar != null)
                _progressBar.gameObject.SetActive(_busy);
            if (_progressFill != null && _busy)
            {
                float p = Mathf.Clamp01(Progress);
                _progressFill.localScale = new Vector3(Mathf.Max(p, 0.001f), 1f, 1f);
                _progressFill.localPosition = new Vector3(-0.5f * (1f - p), 0f, 0f);
            }
        }
    }
}
