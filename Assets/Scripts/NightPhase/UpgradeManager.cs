using System;
using System.Collections.Generic;
using UnityEngine;

namespace BubbleTeaShop
{
    public class UpgradeManager : MonoBehaviour
    {
        public static UpgradeManager Instance { get; private set; }

        [SerializeField] private List<ShopUpgrade> availableUpgrades = new List<ShopUpgrade>();

        public List<ShopUpgrade> Upgrades => availableUpgrades;

        public event Action<UpgradeType> OnUpgradePurchased;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InitializeUpgrades();
        }

        private void InitializeUpgrades()
        {
            availableUpgrades.Clear();
            
            availableUpgrades.Add(new ShopUpgrade(
                UpgradeType.Advertisements,
                "Advertisements",
                "Launch targeted flyers and local social media buzz across the district.",
                "Increases maximum daily customers by +1.",
                65f
            ));

            availableUpgrades.Add(new ShopUpgrade(
                UpgradeType.StorefrontBeautification,
                "Storefront Beautification",
                "Install lush flower planters, polished glass, and warm lantern lighting.",
                "Increases minimum daily customers by +1.",
                75f
            ));

            availableUpgrades.Add(new ShopUpgrade(
                UpgradeType.YippeePheromones,
                "Yippee Pheromones",
                "Brew an irresistible botanical scent that lures critters out of the brush.",
                "Increases min and max Baby Yippees spawned when foraging by +1.",
                80f
            ));

            availableUpgrades.Add(new ShopUpgrade(
                UpgradeType.SwitchSupplyContract,
                "Switch Supply Contract",
                "Negotiate wholesale bulk distributor rates for cups, straws, tea, and ice.",
                "Reduces daily restock deductions from $10 to $3 per day.",
                90f
            ));

            availableUpgrades.Add(new ShopUpgrade(
                UpgradeType.LuckyCat,
                "Lucky Cat (Maneki-neko)",
                "A polished golden waving cat statue placed right on the front counter.",
                "Increases customer tips by +30% on well-made drinks.",
                85f
            ));

            availableUpgrades.Add(new ShopUpgrade(
                UpgradeType.BambooGroveTrailMap,
                "Bamboo Grove Trail Map",
                "An annotated map marking swift shortcuts through the Bamboo Grove.",
                "Removes -1 customer late opening penalty when foraging in Bamboo Grove.",
                55f
            ));

            availableUpgrades.Add(new ShopUpgrade(
                UpgradeType.HoneyMeadowsTrailMap,
                "Honey Meadows Trail Map",
                "A surveyor's chart revealing rapid routes across the Honey Meadows.",
                "Removes -1 customer late opening penalty when foraging in Honey Meadows.",
                70f
            ));

            availableUpgrades.Add(new ShopUpgrade(
                UpgradeType.MistyMountainsTrailMap,
                "Misty Mountains Trail Map",
                "A mountaineer's trail guide traversing the misty mountain peaks.",
                "Removes -1 customer late opening penalty when foraging in Misty Mountains.",
                85f
            ));

            availableUpgrades.Add(new ShopUpgrade(
                UpgradeType.ImproveStoreAmbience,
                "Improve Store Ambience",
                "Aromatherapy diffusers, plush barstools, and soothing lofi beats.",
                "REMOVES customer patience timer completely! Customers wait indefinitely.",
                140f
            ));

            availableUpgrades.Add(new ShopUpgrade(
                UpgradeType.ArtisanalTeaMenu,
                "Artisanal Tea Menu",
                "A chic chalkboard menu tempting patrons with gourmet creations.",
                "Significantly increases chances customer orders involve rare ingredients.",
                95f
            ));

            availableUpgrades.Add(new ShopUpgrade(
                UpgradeType.LuckyPoppingBobaBracelet,
                "Lucky Popping Boba Bracelet",
                "A hand-woven bead charm said to bless your kitchen blending.",
                "Increases chances of obtaining Popping Boba when blending.",
                60f
            ));

            availableUpgrades.Add(new ShopUpgrade(
                UpgradeType.ChefsHoningSteel,
                "Chef's Honing Steel",
                "A ceramic sharpening rod keeping your kitchen knives razor sharp.",
                "25% chance to yield DOUBLE toppings when chopping Jelly Blocks.",
                75f
            ));

            availableUpgrades.Add(new ShopUpgrade(
                UpgradeType.DowsingRods,
                "Dowsing Rods",
                "Attuned copper rods that resonate with dense Golden Dew currents.",
                "Increases chances of refining Golden Honey Pearls from the centrifuge.",
                90f
            ));

            availableUpgrades.Add(new ShopUpgrade(
                UpgradeType.NightChauffeur,
                "Night Chauffeur",
                "A private evening courier to rush you back from wholesale markets.",
                "Removes -1 customer late opening penalty when visiting the Wholesale Market.",
                80f
            ));

            availableUpgrades.Add(new ShopUpgrade(
                UpgradeType.MarketingIntern,
                "Marketing Intern",
                "Studied market trends and realized some drinks were under-priced!",
                "All drink prices involving Grass Jelly or Coconut Jelly increased by 10%.",
                85f
            ));
        }

        public bool HasUpgrade(UpgradeType type)
        {
            var u = availableUpgrades.Find(x => x.type == type);
            return u != null && u.isPurchased;
        }

        public bool TryPurchaseUpgrade(UpgradeType type)
        {
            var u = availableUpgrades.Find(x => x.type == type);
            if (u == null || u.isPurchased) return false;

            if (EconomyManager.Instance.SpendCash(u.cost, $"Purchased Upgrade: {u.title}"))
            {
                u.isPurchased = true;
                u.currentLevel = 1;
                OnUpgradePurchased?.Invoke(type);
                return true;
            }

            return false;
        }
    }
}
