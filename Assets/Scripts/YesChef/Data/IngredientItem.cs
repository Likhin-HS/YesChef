using UnityEngine;
using YesChef.Core;

namespace YesChef.Data
{
    /// <summary>
    /// Lightweight value type describing a single carried ingredient.
    /// Immutable so station logic cannot accidentally mutate shared state.
    /// </summary>
    public readonly struct IngredientItem
    {
        public IngredientType Type { get; }
        public IngredientState State { get; }

        public IngredientItem(IngredientType type, IngredientState state)
        {
            Type = type;
            State = state;
        }

        /// <summary>Cheese ships ready-to-serve; everything else needs a station.</summary>
        public bool IsPrepared => Type == IngredientType.Cheese || State == IngredientState.Prepared;

        public string DisplayName => Type switch
        {
            IngredientType.Cheese => "Cheese",
            IngredientType.Vegetable => IsPrepared ? "Chopped Veg" : "Raw Veg",
            IngredientType.Meat => IsPrepared ? "Cooked Meat" : "Raw Meat",
            _ => "Ingredient"
        };

        public Color DisplayColor => Type switch
        {
            IngredientType.Vegetable => IsPrepared ? GameConstants.VegPreparedColor : GameConstants.VegRawColor,
            IngredientType.Cheese => GameConstants.CheeseColor,
            IngredientType.Meat => IsPrepared ? GameConstants.MeatPreparedColor : GameConstants.MeatRawColor,
            _ => Color.white
        };
    }
}
