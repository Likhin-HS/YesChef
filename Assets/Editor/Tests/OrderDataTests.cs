using NUnit.Framework;
using UnityEngine;
using YesChef.Core;
using YesChef.Data;

namespace YesChef.Tests
{
    [TestFixture]
    public class OrderDataTests
    {
        [Test]
        public void SpecExample_CheeseAndMeat_DeliveredIn14Point99Seconds_Scores26Points()
        {
            // Blueprint specification example:
            // "An order was cheese and meat and it took 14 seconds to complete it, so the player
            // receives 10 + 30 - 14 = 26 points for that order. Time to deliver is rounded down,
            // i.e. 14.99 seconds to deliver an order only docks 14 points for that order."
            var order = new OrderData(IngredientType.Cheese, IngredientType.Meat);
            Assert.AreEqual(40, order.BaseScore);

            order.Tick(14.99f);

            int score = order.CalculateScore();
            Assert.AreEqual(26, score, "14.99 seconds must floor to 14 docked points (40 - 14 = 26).");
        }

        [Test]
        public void Scoring_WholeSecondsFlooredCorrectly()
        {
            var order = new OrderData(IngredientType.Vegetable); // 20 pts
            Assert.AreEqual(20, order.CalculateScore());

            order.Tick(0.99f);
            Assert.AreEqual(20, order.CalculateScore(), "Sub-second elapsed time docks 0 points.");

            order.Tick(0.02f); // total 1.01s
            Assert.AreEqual(19, order.CalculateScore(), "1.01s docks 1 point.");

            order.Tick(0.98f); // total 1.99s
            Assert.AreEqual(19, order.CalculateScore(), "1.99s still docks only 1 point.");

            order.Tick(0.01f); // total 2.00s
            Assert.AreEqual(18, order.CalculateScore(), "2.00s docks 2 points.");
        }

        [Test]
        public void Scoring_CanBeNegativeIfOrderTakesTooLong()
        {
            // Blueprint specification:
            // "Orders can have negative score value if it takes too long to complete!"
            var order = new OrderData(IngredientType.Cheese); // 10 pts
            order.Tick(15.5f); // 10 - 15 = -5

            Assert.AreEqual(-5, order.CalculateScore());
        }

        [Test]
        public void DuplicateIngredients_HandledCorrectly()
        {
            // Blueprint specification:
            // "meaning you can have any combination of ingredients, including duplicate ingredients
            // (meat, meat, meat is a possible combination)."
            var order = new OrderData(IngredientType.Meat, IngredientType.Meat, IngredientType.Meat);

            Assert.AreEqual(90, order.BaseScore);
            Assert.AreEqual(3, order.RemainingCount);
            Assert.IsFalse(order.IsComplete);
            Assert.IsTrue(order.Needs(IngredientType.Meat));
            Assert.IsFalse(order.Needs(IngredientType.Vegetable));
            Assert.IsFalse(order.Needs(IngredientType.Cheese));

            // Fulfill Meat 1
            Assert.IsTrue(order.TryFulfill(IngredientType.Meat));
            Assert.AreEqual(2, order.RemainingCount);
            Assert.IsFalse(order.IsComplete);
            Assert.IsTrue(order.Needs(IngredientType.Meat));

            // Fulfill Meat 2
            Assert.IsTrue(order.TryFulfill(IngredientType.Meat));
            Assert.AreEqual(1, order.RemainingCount);
            Assert.IsFalse(order.IsComplete);
            Assert.IsTrue(order.Needs(IngredientType.Meat));

            // Fulfill Meat 3
            Assert.IsTrue(order.TryFulfill(IngredientType.Meat));
            Assert.AreEqual(0, order.RemainingCount);
            Assert.IsTrue(order.IsComplete);
            Assert.IsFalse(order.Needs(IngredientType.Meat));

            // Attempting to fulfill extra Meat returns false and does not corrupt state
            Assert.IsFalse(order.TryFulfill(IngredientType.Meat));
            Assert.AreEqual(0, order.RemainingCount);
        }

        [Test]
        public void WrongIngredient_Rejected()
        {
            var order = new OrderData(IngredientType.Vegetable, IngredientType.Cheese);

            Assert.IsFalse(order.Needs(IngredientType.Meat));
            Assert.IsFalse(order.TryFulfill(IngredientType.Meat));
            Assert.AreEqual(2, order.RemainingCount);
        }

        [Test]
        public void IngredientItem_CheeseIsAlwaysPrepared()
        {
            // Blueprint specification:
            // "Cheese: Does not need to be prepared. Can be added directly to an order."
            var cheeseRaw = new IngredientItem(IngredientType.Cheese, IngredientState.Raw);
            var cheesePrepared = new IngredientItem(IngredientType.Cheese, IngredientState.Prepared);

            Assert.IsTrue(cheeseRaw.IsPrepared, "Cheese must report IsPrepared even when state is Raw.");
            Assert.IsTrue(cheesePrepared.IsPrepared);
        }

        [Test]
        public void IngredientItem_VegAndMeatRequirePreparation()
        {
            var rawVeg = new IngredientItem(IngredientType.Vegetable, IngredientState.Raw);
            var prepVeg = new IngredientItem(IngredientType.Vegetable, IngredientState.Prepared);
            var rawMeat = new IngredientItem(IngredientType.Meat, IngredientState.Raw);
            var prepMeat = new IngredientItem(IngredientType.Meat, IngredientState.Prepared);

            Assert.IsFalse(rawVeg.IsPrepared);
            Assert.IsTrue(prepVeg.IsPrepared);
            Assert.IsFalse(rawMeat.IsPrepared);
            Assert.IsTrue(prepMeat.IsPrepared);
        }

        [Test]
        public void CachedRequirementText_UpdatesAsItemsAreFulfilled()
        {
            var order = new OrderData(IngredientType.Vegetable, IngredientType.Cheese);
            string initial = order.GetRequirementText();
            Assert.AreEqual("Chopped Veg + Cheese", initial);

            order.TryFulfill(IngredientType.Vegetable);
            string afterVeg = order.GetRequirementText();
            Assert.AreEqual("Cheese", afterVeg);

            order.TryFulfill(IngredientType.Cheese);
            string afterCheese = order.GetRequirementText();
            Assert.AreEqual("Done!", afterCheese);
        }
    }
}
