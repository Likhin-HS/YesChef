using UnityEngine;

namespace YesChef.Core
{
    /// <summary>
    /// Central tuning constants and shared styling for the Yes Chef! prototype.
    /// Gameplay balance numbers, station timers, colors, and cached string tables are kept here.
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

        public const int IngredientTypeCount = 3;

        public const float KitchenWidth = 18f;
        public const float KitchenDepth = 11f;
        public const float WallHeight = 2.5f;
        public const float WallThickness = 0.5f;

        /// <summary>Spec values: Vegetable 20 / Cheese 10 / Meat 30.</summary>
        public static int IngredientValue(IngredientType type) => type switch
        {
            IngredientType.Vegetable => 20,
            IngredientType.Cheese => 10,
            IngredientType.Meat => 30,
            _ => 0
        };

        // Shared raw vs prepared colors, reused by held items and station visuals.
        public static readonly Color VegRawColor = new Color(0.35f, 0.55f, 0.25f);
        public static readonly Color VegPreparedColor = new Color(0.2f, 0.8f, 0.25f);
        public static readonly Color CheeseColor = new Color(1f, 0.85f, 0.2f);
        public static readonly Color MeatRawColor = new Color(0.9f, 0.4f, 0.4f);
        public static readonly Color MeatPreparedColor = new Color(0.55f, 0.3f, 0.15f);

        // Window status lamp + score popup colors, shared by 3D and HUD.
        public static readonly Color WindowIdleColor = new Color(0.3f, 0.3f, 0.3f);
        public static readonly Color WindowActiveColor = new Color(1f, 0.75f, 0.2f);
        public static readonly Color WindowDoneColor = new Color(0.3f, 0.9f, 0.3f);
        public static readonly Color PopupGoodColor = new Color(0.1f, 0.65f, 0.1f);
        public static readonly Color PopupBadColor = Color.red;

        // Pre-cached string tables to avoid string allocations during frequent HUD updates
        private static readonly string[] s_SecondsStrings = new string[301];
        private static readonly string[] s_TimerClockStrings = new string[301];
        private static readonly string[] s_NextInStrings = new string[301];
        private static readonly string[] s_GoodScoreStrings = new string[101];
        private static readonly string[] s_BadScoreStrings = new string[101];

        static GameConstants()
        {
            for (int i = 0; i <= 300; i++)
            {
                s_SecondsStrings[i] = i + "s";
                s_TimerClockStrings[i] = string.Format("Time {0}:{1:D2}", i / 60, i % 60);
                s_NextInStrings[i] = "Next in " + i + "s";
            }
            for (int i = 0; i <= 100; i++)
            {
                s_GoodScoreStrings[i] = "+" + i;
                s_BadScoreStrings[i] = "-" + i;
            }
        }

        public static string FormatScorePopup(int score)
        {
            if (score >= 0)
            {
                int s = Mathf.Clamp(score, 0, 100);
                return s_GoodScoreStrings[s];
            }
            else
            {
                int s = Mathf.Clamp(-score, 0, 100);
                return s_BadScoreStrings[s];
            }
        }

        public static string FormatSeconds(int seconds)
        {
            if (seconds < 0) seconds = 0;
            if (seconds > 300) seconds = 300;
            return s_SecondsStrings[seconds];
        }

        public static string FormatTimerClock(int seconds)
        {
            if (seconds < 0) seconds = 0;
            if (seconds > 300) seconds = 300;
            return s_TimerClockStrings[seconds];
        }

        public static string FormatNextIn(int seconds)
        {
            if (seconds < 0) seconds = 0;
            if (seconds > 300) seconds = 300;
            return s_NextInStrings[seconds];
        }
    }
}
