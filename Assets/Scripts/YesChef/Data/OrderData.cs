using System.Collections.Generic;
using UnityEngine;
using YesChef.Core;

namespace YesChef.Data
{
    /// <summary>
    /// Runtime model for a single customer order.
    /// Requirements are stored as per-type counts so duplicate ingredients
    /// (e.g. "Meat, Meat, Meat") fall out naturally with no bookkeeping.
    /// Scoring formula: sum(ingredient values) - floor(seconds open), per blueprint spec.
    /// Requirement text is cached to guarantee zero per-frame heap allocations during UI updates.
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
        /// Base value minus whole seconds open (floored per spec, can be negative).
        /// Example: 10 (Cheese) + 30 (Meat) - 14.99s = 40 - 14 = 26 points.
        /// </summary>
        public int CalculateScore() => _baseScore - Mathf.FloorToInt(Elapsed);

        public void MarkScored(int score)
        {
            CachedScore = score;
            IsComplete = true;
        }

        /// <summary>
        /// Returns cached requirement string. Zero heap allocation per call.
        /// </summary>
        public string GetRequirementText() => _cachedRequirementText;

        /// <summary>
        /// Populates a reusable list with remaining ingredient types for zero-allocation UI icon binding.
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
