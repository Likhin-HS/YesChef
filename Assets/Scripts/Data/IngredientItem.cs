using UnityEngine;
using YesChef.Core;

namespace YesChef.Data
{
    /// <summary>
    /// Represents a held ingredient and its preparation state.
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

        /// <summary>Cheese is ready immediately; veg and meat need prep.</summary>
        public bool IsPrepared => Type == IngredientType.Cheese || State == IngredientState.Prepared;

        public string DisplayName => Type switch
        {
            IngredientType.Cheese => "Cheese",
            IngredientType.Vegetable => IsPrepared ? "Chopped Vegetable" : "Raw Vegetable (Cabbage)",
            IngredientType.Meat => IsPrepared ? "Cooked Meat" : "Raw Meat (Steak)",
            _ => "Ingredient"
        };

        public string ActionHint => IsPrepared
            ? "Ready to Deliver"
            : (Type == IngredientType.Vegetable ? "Chop on Table" : "Cook on Stove");

        public Color DisplayColor => Type switch
        {
            IngredientType.Vegetable => IsPrepared ? GameConstants.VegPreparedColor : GameConstants.VegRawColor,
            IngredientType.Cheese => GameConstants.CheeseColor,
            IngredientType.Meat => IsPrepared ? GameConstants.MeatPreparedColor : GameConstants.MeatRawColor,
            _ => Color.white
        };
    }
}
