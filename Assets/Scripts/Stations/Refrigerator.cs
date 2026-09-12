using UnityEngine;
using YesChef.Core;
using YesChef.Data;
using YesChef.Player;

namespace YesChef.Stations
{
    /// <summary>
    /// Infinite source of raw ingredients. E takes the currently selected
    /// ingredient (1/2/3 to choose); selection UI lives in the HUD.
    /// </summary>
    public sealed class Refrigerator : Interactable
    {
        public IngredientType Selected { get; private set; } = IngredientType.Vegetable;

        private void Awake() => SetStationName("Refrigerator");

        public void Select(IngredientType type) => Selected = type;

        public override string GetPrompt(PlayerController player)
        {
            if (player.HasHeld) return "Hands full — deliver or trash it";
            return $"E: take {Selected}  •  Q: cycle  •  1/2/3: select";
        }

        public override void Interact(PlayerController player)
        {
            if (player.HasHeld) return;
            var state = Selected == IngredientType.Cheese ? IngredientState.Prepared : IngredientState.Raw;
            player.TryGive(new IngredientItem(Selected, state));
        }

        public override void Alternate(PlayerController player)
        {
            // Cycle ingredient with Q
            Selected = (IngredientType)(((int)Selected + 1) % GameConstants.IngredientTypeCount);
        }
    }
}
