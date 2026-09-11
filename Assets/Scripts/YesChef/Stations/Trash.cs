using YesChef.Player;

namespace YesChef.Stations
{
    using UnityEngine;

    /// <summary>
    /// Discards whatever the player is holding. Safety valve for wrong picks.
    /// </summary>
    public sealed class Trash : Interactable
    {
        private void Awake() => SetStationName("Trash");

        public override string GetPrompt(PlayerController player)
            => player.HasHeld ? "E: throw away held ingredient" : "Trash: nothing to discard";

        public override void Interact(PlayerController player)
        {
            if (player.HasHeld) player.TryTake(out _);
        }
    }
}
