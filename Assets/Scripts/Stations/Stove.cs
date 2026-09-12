using UnityEngine;
using YesChef.Core;
using YesChef.Data;
using YesChef.Player;

namespace YesChef.Stations
{
    /// <summary>
    /// Cooks up to two raw meats in parallel over <see cref="GameConstants.StoveCookDuration"/> seconds.
    /// Player may walk away; E or Q collects the first finished portion when hands are free.
    /// Uses MaterialPropertyBlock to update slot visuals without creating material instances.
    /// </summary>
    public sealed class Stove : Interactable
    {
        private static readonly int s_ColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int s_LegacyColorId = Shader.PropertyToID("_Color");

        [SerializeField] private Renderer[] _slotVisuals = new Renderer[0];
        [SerializeField] private Transform[] _slotBars = new Transform[0];
        [SerializeField] private Transform[] _slotFills = new Transform[0];
        [SerializeField] private TextMesh[] _slotTimerTexts = new TextMesh[0];
        [SerializeField] private GameObject[] _slotSteams = new GameObject[0];
        [SerializeField] private Renderer[] _slotLeds = new Renderer[0];

        private readonly float[] _remaining = new float[GameConstants.StoveSlotCount];
        private readonly bool[] _active = new bool[GameConstants.StoveSlotCount];
        private readonly bool[] _ready = new bool[GameConstants.StoveSlotCount];
        private MaterialPropertyBlock _propBlock;

        public float GetSlotRemaining(int slot) => (slot >= 0 && slot < GameConstants.StoveSlotCount) ? _remaining[slot] : 0f;
        public bool IsSlotActive(int slot) => (slot >= 0 && slot < GameConstants.StoveSlotCount) && _active[slot];
        public bool IsSlotReady(int slot) => (slot >= 0 && slot < GameConstants.StoveSlotCount) && _ready[slot];

        private void Awake()
        {
            SetStationName("Stove");
            _propBlock = new MaterialPropertyBlock();
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
                    _remaining[i] = 0f;
                }
                dirty = true;
            }
            if (dirty) RefreshVisuals();
        }

        public override string GetPrompt(PlayerController player)
        {
            if (AnyReady && !player.HasHeld)
            {
                return "E or Q: pick up cooked meat";
            }
            if (player.Held is { Type: IngredientType.Meat, State: IngredientState.Raw })
            {
                return HasFreeSlot ? "E: place raw meat on stove" : "Stove full — wait or collect with E/Q";
            }
            int cooking = CookingCount;
            if (cooking > 0)
            {
                return cooking == 1
                    ? "Cooking 1 meat... E/Q collects finished portions"
                    : "Cooking 2 meat... E/Q collects finished portions";
            }
            return "Bring raw meat here (Fridge: press 3)";
        }

        public override void Interact(PlayerController player)
        {
            // Contextual pickup: If cooked meat is ready and hands are free, pick it up
            if (AnyReady && !player.HasHeld)
            {
                Alternate(player);
                return;
            }

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

        public bool AnyReady
        {
            get
            {
                for (int i = 0; i < GameConstants.StoveSlotCount; i++)
                {
                    if (_ready[i]) return true;
                }
                return false;
            }
        }

        public int CookingCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < GameConstants.StoveSlotCount; i++)
                {
                    if (_active[i] && !_ready[i]) count++;
                }
                return count;
            }
        }

        public bool HasFreeSlot => FreeSlot() >= 0;

        public int FreeSlot()
        {
            for (int i = 0; i < GameConstants.StoveSlotCount; i++)
            {
                if (!_active[i] && !_ready[i]) return i;
            }
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
                {
                    Color color = _ready[i] ? GameConstants.MeatPreparedColor : GameConstants.MeatRawColor;
                    visual.GetPropertyBlock(_propBlock);
                    _propBlock.SetColor(s_ColorId, color);
                    _propBlock.SetColor(s_LegacyColorId, color);
                    visual.SetPropertyBlock(_propBlock);
                }
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

            for (int i = 0; i < _slotTimerTexts.Length && i < GameConstants.StoveSlotCount; i++)
            {
                var timerText = _slotTimerTexts[i];
                if (timerText == null) continue;
                if (_active[i] && !_ready[i])
                {
                    timerText.gameObject.SetActive(true);
                    timerText.text = GameConstants.FormatSeconds(Mathf.CeilToInt(_remaining[i]));
                }
                else if (_ready[i])
                {
                    timerText.gameObject.SetActive(true);
                    timerText.text = "READY";
                    timerText.color = GameConstants.MeatPreparedColor;
                }
                else
                {
                    timerText.gameObject.SetActive(false);
                }
            }

            for (int i = 0; i < _slotSteams.Length && i < GameConstants.StoveSlotCount; i++)
            {
                var steam = _slotSteams[i];
                if (steam != null) steam.SetActive(_active[i] || _ready[i]);
            }

            for (int i = 0; i < _slotLeds.Length && i < GameConstants.StoveSlotCount; i++)
            {
                var led = _slotLeds[i];
                if (led != null)
                {
                    bool lit = _active[i] || _ready[i];
                    Color ledColor = lit ? new Color(1.0f, 0.65f, 0.1f) : new Color(0.1f, 0.12f, 0.15f);
                    led.GetPropertyBlock(_propBlock);
                    _propBlock.SetColor(s_ColorId, ledColor);
                    _propBlock.SetColor(s_LegacyColorId, ledColor);
                    led.SetPropertyBlock(_propBlock);
                }
            }
        }
    }
}
