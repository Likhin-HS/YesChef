namespace YesChef.Core
{
    /// <summary>
    /// Central tuning constants for the Yes Chef! prototype.
    /// Keeping gameplay numbers in one place makes balancing trivial.
    /// </summary>
    public static class GameConstants
    {
        public const float GameDuration = 180f;
        public const int MaxActiveOrders = 4;
        public const float OrderRespawnDelay = 5f;

        public const float TableChopDuration = 2f;
        public const float StoveCookDuration = 6f;
        public const int StoveSlotCount = 2;

        public const float PlayerMoveSpeed = 6f;
        public const float InteractionRadius = 2.6f;

        public const float ScorePopupDuration = 2.5f;

        public const string HighScoreKey = "YesChef_HighScore";

        public const float KitchenWidth = 18f;
        public const float KitchenDepth = 11f;
        public const float WallHeight = 2.5f;
        public const float WallThickness = 0.5f;
    }
}
