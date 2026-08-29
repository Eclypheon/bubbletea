using System.Collections.Generic;
using UnityEngine;

namespace BubbleTeaShop
{
    [System.Serializable]
    public class CustomerArchetypeProfile
    {
        public CustomerArchetype archetype;
        public string typeName;
        public string description;
        public float basePatienceSeconds = 45f;
        public float tipBonusMultiplier = 1.0f;
        public List<TeaBase> preferredTeas = new List<TeaBase>();
        public List<ToppingType> preferredToppings = new List<ToppingType>();
        
        public List<string> orderDialogues = new List<string>();
        public List<string> happyDialogues = new List<string>();
        public List<string> neutralDialogues = new List<string>();
        public List<string> angryDialogues = new List<string>();
        public List<string> mysteryDialogues = new List<string>();
    }
}
