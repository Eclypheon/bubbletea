using UnityEngine;

namespace BubbleTeaShop
{
    [System.Serializable]
    public class ShopUpgrade
    {
        public UpgradeType type;
        public string title;
        public string description;
        public float cost;
        public bool isPurchased;
        public int currentLevel;
        public int maxLevel = 1;

        public ShopUpgrade(UpgradeType type, string title, string description, float cost)
        {
            this.type = type;
            this.title = title;
            this.description = description;
            this.cost = cost;
            this.isPurchased = false;
            this.currentLevel = 0;
            this.maxLevel = 1;
        }
    }
}
