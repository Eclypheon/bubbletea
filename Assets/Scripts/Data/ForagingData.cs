using System.Collections.Generic;
using UnityEngine;

namespace BubbleTeaShop
{
    [System.Serializable]
    public class ForagingLocation
    {
        public string locationName;
        public string description;
        public float staminaCost = 1f;
        public List<string> possibleDiscoveries = new List<string>();
    }

    [System.Serializable]
    public class ForagingReward
    {
        public string title;
        public string description;
        public TeaBase teaReward = TeaBase.None;
        public ToppingType toppingReward = ToppingType.TapiocaPearls;
        public int quantity = 1;
        public float bonusCash = 0f;
    }
}
