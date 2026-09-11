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

        public bool IsPrepared => Type == IngredientType.Cheese || State == IngredientState.Prepared;

        public int ScoreValue => Type switch
        {
            IngredientType.Vegetable => 20,
            IngredientType.Cheese => 10,
            IngredientType.Meat => 30,
            _ => 0
        };

        public string DisplayName
        {
            get
            {
                string baseName = Type switch
                {
                    IngredientType.Vegetable => "Vegetable",
                    IngredientType.Cheese => "Cheese",
                    IngredientType.Meat => "Meat",
                    _ => "Ingredient"
                };
                if (Type == IngredientType.Cheese)
                    return baseName;
                return State == IngredientState.Prepared
                    ? Type == IngredientType.Vegetable ? "Chopped Veg" : "Cooked Meat"
                    : Type == IngredientType.Vegetable ? "Raw Veg" : "Raw Meat";
            }
        }

        public Color DisplayColor => Type switch
        {
            IngredientType.Vegetable => State == IngredientState.Prepared
                ? new Color(0.2f, 0.8f, 0.25f)
                : new Color(0.35f, 0.55f, 0.25f),
            IngredientType.Cheese => new Color(1f, 0.85f, 0.2f),
            IngredientType.Meat => State == IngredientState.Prepared
                ? new Color(0.55f, 0.3f, 0.15f)
                : new Color(0.9f, 0.4f, 0.4f),
            _ => Color.white
        };
    }
}
