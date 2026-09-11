using System.Collections.Generic;
using YesChef.Core;

namespace YesChef.Data
{
    /// <summary>
    /// Runtime model for a single customer order.
    /// Score = sum(ingredient values) - floored seconds open (may be negative).
    /// </summary>
    public sealed class OrderData
    {
        public readonly List<IngredientType> Required;
        private readonly HashSet<int> _fulfilledIndices = new();

        public float Elapsed { get; private set; }
        public bool IsComplete { get; private set; }
        public int CachedScore { get; private set; }

        public OrderData(IEnumerable<IngredientType> required)
        {
            Required = new List<IngredientType>(required);
        }

        public OrderData(params IngredientType[] required) : this((IEnumerable<IngredientType>)required)
        {
        }

        public void Tick(float deltaTime)
        {
            if (IsComplete) return;
            Elapsed += deltaTime;
        }

        public bool Needs(IngredientType type)
        {
            for (int i = 0; i < Required.Count; i++)
            {
                if (!_fulfilledIndices.Contains(i) && Required[i] == type)
                    return true;
            }
            return false;
        }

        public bool TryFulfill(IngredientType type)
        {
            for (int i = 0; i < Required.Count; i++)
            {
                if (!_fulfilledIndices.Contains(i) && Required[i] == type)
                {
                    _fulfilledIndices.Add(i);
                    if (_fulfilledIndices.Count >= Required.Count)
                        IsComplete = true;
                    return true;
                }
            }
            return false;
        }

        public int CalculateScore()
        {
            int sum = 0;
            foreach (var ingredient in Required)
            {
                sum += ingredient switch
                {
                    IngredientType.Vegetable => 20,
                    IngredientType.Cheese => 10,
                    IngredientType.Meat => 30,
                    _ => 0
                };
            }
            return sum - (int)Elapsed;
        }

        public void MarkScored(int score)
        {
            CachedScore = score;
            IsComplete = true;
        }

        public int RemainingCount => Required.Count - _fulfilledIndices.Count;

        public string GetRequirementText()
        {
            var remaining = new List<string>();
            for (int i = 0; i < Required.Count; i++)
            {
                if (_fulfilledIndices.Contains(i)) continue;
                remaining.Add(Required[i] switch
                {
                    IngredientType.Vegetable => "Chopped Veg",
                    IngredientType.Cheese => "Cheese",
                    IngredientType.Meat => "Cooked Meat",
                    _ => "?"
                });
            }
            return remaining.Count == 0 ? "Done!" : string.Join(" + ", remaining);
        }
    }
}
