namespace YesChef.Core
{
    public enum GameState
    {
        NotStarted,
        Playing,
        Paused,
        GameOver
    }

    public enum IngredientType
    {
        Vegetable = 0,
        Cheese = 1,
        Meat = 2
    }

    public enum IngredientState
    {
        Raw = 0,
        Prepared = 1
    }
}
