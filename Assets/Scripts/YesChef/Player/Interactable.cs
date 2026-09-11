using YesChef.Core;

namespace YesChef.Player
{
    using UnityEngine;

    /// <summary>
    /// Base class for anything the chef can use with E.
    /// Proximity + prompt keeps input consistent across stations.
    /// </summary>
    public abstract class Interactable : MonoBehaviour
    {
        [SerializeField] private string _stationName = "Station";

        public string StationName => _stationName;

        protected void SetStationName(string value) => _stationName = value;

        public abstract string GetPrompt(PlayerController player);
        public abstract void Interact(PlayerController player);

        public virtual void Alternate(PlayerController player) { }
    }
}
