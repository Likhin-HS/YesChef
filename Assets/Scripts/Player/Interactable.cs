using System.Collections.Generic;
using UnityEngine;

namespace YesChef.Player
{
    /// <summary>
    /// Base class for all interactive kitchen stations.
    /// </summary>
    public abstract class Interactable : MonoBehaviour
    {
        private static readonly List<Interactable> s_All = new List<Interactable>();
        public static IReadOnlyList<Interactable> All => s_All;

        [SerializeField] private string _stationName = "Station";

        public string StationName => _stationName;

        protected void SetStationName(string value) => _stationName = value;

        protected virtual void OnEnable()
        {
            if (!s_All.Contains(this))
            {
                s_All.Add(this);
            }
        }

        protected virtual void OnDisable()
        {
            s_All.Remove(this);
        }

        public abstract string GetPrompt(PlayerController player);
        public abstract void Interact(PlayerController player);

        public virtual void Alternate(PlayerController player) { }
    }
}
