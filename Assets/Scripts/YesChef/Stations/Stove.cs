using UnityEngine;
using YesChef.Core;
using YesChef.Data;
using YesChef.Player;

namespace YesChef.Stations
{
    /// <summary>
    /// Cooks up to two raw meats in parallel. Player may walk away; Q collects
    /// the first finished portion while other slots keep cooking.
    /// </summary>
    public sealed class Stove : Interactable
    {
        [SerializeField] private Renderer[] _slotVisuals = new Renderer[0];
        [SerializeField] private Transform[] _slotBars = new Transform[0];
        [SerializeField] private Transform[] _slotFills = new Transform[0];

        private readonly float[] _remaining = new float[GameConstants.StoveSlotCount];
        private readonly bool[] _active = new bool[GameConstants.StoveSlotCount];
        private readonly bool[] _ready = new bool[GameConstants.StoveSlotCount];

        private void Awake()
        {
            SetStationName("Stove");
        }

        private void Update()
        {
            bool dirty = false;
            for (int i = 0; i < GameConstants.StoveSlotCount; i++)
            {
                if (!_active[i] || _ready[i]) continue;
                _remaining[i] -= Time.deltaTime;
                if (_remaining[i] <= 0f)
                {
                    _ready[i] = true;
                    _active[i] = false;
                }
                dirty = true;
            }
            if (dirty) RefreshVisuals();
        }

        public override string GetPrompt(PlayerController player)
        {
            if (AnyReady && !player.HasHeld) return "Q: pick up cooked meat";
            if (player.Held is { Type: IngredientType.Meat, State: IngredientState.Raw })
                return HasFreeSlot ? "E: place raw meat on stove" : "Stove full — wait or collect with Q";
            int cooking = CookingCount;
            if (cooking > 0) return $"Cooking {cooking} meat... Q collects finished portions";
            return "Bring raw meat here (Fridge: press 3)";
        }

        public override void Interact(PlayerController player)
        {
            if (player.Held is not { Type: IngredientType.Meat, State: IngredientState.Raw }) return;
            int slot = FreeSlot();
            if (slot < 0) return;
            if (!player.TryTake(out _)) return;
            _active[slot] = true;
            _ready[slot] = false;
            _remaining[slot] = GameConstants.StoveCookDuration;
            RefreshVisuals();
        }

        public override void Alternate(PlayerController player)
        {
            if (player.HasHeld) return;
            for (int i = 0; i < GameConstants.StoveSlotCount; i++)
            {
                if (!_ready[i]) continue;
                _ready[i] = false;
                player.TryGive(new IngredientItem(IngredientType.Meat, IngredientState.Prepared));
                RefreshVisuals();
                return;
            }
        }

        public void ResetStation()
        {
            for (int i = 0; i < GameConstants.StoveSlotCount; i++)
            {
                _active[i] = false;
                _ready[i] = false;
                _remaining[i] = 0f;
            }
            RefreshVisuals();
        }

        private bool AnyReady
        {
            get
            {
                for (int i = 0; i < GameConstants.StoveSlotCount; i++)
                    if (_ready[i]) return true;
                return false;
            }
        }

        private int CookingCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < GameConstants.StoveSlotCount; i++)
                    if (_active[i] && !_ready[i]) count++;
                return count;
            }
        }

        private bool HasFreeSlot => FreeSlot() >= 0;

        private int FreeSlot()
        {
            for (int i = 0; i < GameConstants.StoveSlotCount; i++)
                if (!_active[i] && !_ready[i]) return i;
            return -1;
        }

        private void RefreshVisuals()
        {
            for (int i = 0; i < _slotVisuals.Length && i < GameConstants.StoveSlotCount; i++)
            {
                var visual = _slotVisuals[i];
                if (visual == null) continue;
                bool show = _active[i] || _ready[i];
                visual.gameObject.SetActive(show);
                if (show)
                    visual.material.color = _ready[i]
                        ? new Color(0.55f, 0.3f, 0.15f)
                        : new Color(0.9f, 0.4f, 0.4f);
            }
            for (int i = 0; i < _slotBars.Length && i < GameConstants.StoveSlotCount; i++)
            {
                var bar = _slotBars[i];
                if (bar == null) continue;
                bar.gameObject.SetActive(_active[i] && !_ready[i]);
            }
            for (int i = 0; i < _slotFills.Length && i < GameConstants.StoveSlotCount; i++)
            {
                var fill = _slotFills[i];
                if (fill == null) continue;
                float p = _active[i]
                    ? Mathf.Clamp01(1f - (_remaining[i] / GameConstants.StoveCookDuration))
                    : (_ready[i] ? 1f : 0f);
                fill.localScale = new Vector3(Mathf.Max(p, 0.001f), 1f, 1f);
                fill.localPosition = new Vector3(-0.5f * (1f - p), 0f, 0f);
            }
        }
    }
}
