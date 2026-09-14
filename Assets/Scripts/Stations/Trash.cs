using UnityEngine;
using YesChef.Player;

namespace YesChef.Stations
{
    /// <summary>
    /// Trash bin to discard the currently held item.
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
