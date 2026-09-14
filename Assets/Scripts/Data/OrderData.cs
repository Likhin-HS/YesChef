using System.Collections.Generic;
using UnityEngine;
using YesChef.Core;

namespace YesChef.Data
{
    /// <summary>
    /// Tracks customer order requirements, fulfilled items, and score.
    /// </summary>
    public sealed class OrderData
    {
        private static readonly int TypeCount = GameConstants.IngredientTypeCount;

        private readonly int[] _required = new int[TypeCount];
        private readonly int[] _fulfilled = new int[TypeCount];
        private int _baseScore;
        private int _remaining;
        private string _cachedRequirementText;

        public float Elapsed { get; private set; }
        public bool IsComplete { get; private set; }
        public int CachedScore { get; private set; }
        public int RemainingCount => _remaining;
        public int BaseScore => _baseScore;

        public OrderData(IEnumerable<IngredientType> required)
        {
            foreach (IngredientType type in required)
            {
                int index = IndexOf(type);
                if (index < 0) continue;
                _required[index]++;
                _baseScore += GameConstants.IngredientValue(type);
                _remaining++;
            }
            RefreshCachedRequirementText();
        }

        public OrderData(params IngredientType[] required) : this((IEnumerable<IngredientType>)required) { }

        public void Tick(float deltaTime)
        {
            if (!IsComplete)
            {
                Elapsed += deltaTime;
            }
        }

        public bool Needs(IngredientType type)
        {
            int index = IndexOf(type);
            return index >= 0 && _fulfilled[index] < _required[index];
        }

        public int GetRequiredCount(IngredientType type)
        {
            int index = IndexOf(type);
            return index >= 0 ? _required[index] : 0;
        }

        public int GetFulfilledCount(IngredientType type)
        {
            int index = IndexOf(type);
            return index >= 0 ? _fulfilled[index] : 0;
        }

        public int GetRemainingCount(IngredientType type)
        {
            int index = IndexOf(type);
            return index >= 0 ? Mathf.Max(0, _required[index] - _fulfilled[index]) : 0;
        }

        public bool TryFulfill(IngredientType type)
        {
            int index = IndexOf(type);
            if (index < 0 || _fulfilled[index] >= _required[index]) return false;

            _fulfilled[index]++;
            _remaining--;
            if (_remaining == 0)
            {
                IsComplete = true;
            }

            RefreshCachedRequirementText();
            return true;
        }

        /// <summary>
        /// Sum of ingredient values minus whole seconds elapsed.
        /// </summary>
        public int CalculateScore() => _baseScore - Mathf.FloorToInt(Elapsed);

        public void MarkScored(int score)
        {
            CachedScore = score;
            IsComplete = true;
        }

        /// <summary>
        /// Returns cached requirement string.
        /// </summary>
        public string GetRequirementText() => _cachedRequirementText;

        /// <summary>
        /// Fills a list with remaining ingredients needed for UI icons.
        /// </summary>
        public void FillRemainingIngredients(IList<IngredientType> destination)
        {
            destination.Clear();
            for (int i = 0; i < TypeCount; i++)
            {
                int remainingForType = _required[i] - _fulfilled[i];
                for (int count = 0; count < remainingForType; count++)
                {
                    destination.Add((IngredientType)i);
                }
            }
        }

        private void RefreshCachedRequirementText()
        {
            if (_remaining == 0)
            {
                _cachedRequirementText = "Done!";
                return;
            }

            var parts = new List<string>();
            for (int i = 0; i < TypeCount; i++)
            {
                int remainingForType = _required[i] - _fulfilled[i];
                for (int count = 0; count < remainingForType; count++)
                {
                    parts.Add(RequirementLabel((IngredientType)i));
                }
            }
            _cachedRequirementText = string.Join(" + ", parts);
        }

        private static int IndexOf(IngredientType type)
        {
            int index = (int)type;
            return index >= 0 && index < TypeCount ? index : -1;
        }

        private static string RequirementLabel(IngredientType type) => type switch
        {
            IngredientType.Vegetable => "Chopped Veg",
            IngredientType.Cheese => "Cheese",
            IngredientType.Meat => "Cooked Meat",
            _ => "?"
        };
    }
}
