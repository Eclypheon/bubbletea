using System;
using System.Collections.Generic;
using UnityEngine;

namespace BubbleTeaShop
{
    /// <summary>
    /// Centralized Single Source of Truth for all Game Sprites and Icons.
    /// Eliminates duplicate sprite fields across UI, Stations, and Subviews.
    /// </summary>
    public class SpriteManager : MonoBehaviour
    {
        private static SpriteManager instance;
        public static SpriteManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<SpriteManager>(FindObjectsInactive.Include);
                    if (instance == null)
                    {
                        var go = new GameObject("SpriteManager");
                        instance = go.AddComponent<SpriteManager>();
                    }
                }
                return instance;
            }
            private set => instance = value;
        }

        [Header("Milks & Liquids")]
        [SerializeField] private Sprite freshMilkSprite;
        [SerializeField] private Sprite oatMilkSprite;
        [SerializeField] private Sprite coconutMilkSprite;
        [SerializeField] private Sprite condensedMilkSprite;

        [Header("Toppings")]
        [SerializeField] private Sprite tapiocaSprite;
        [SerializeField] private Sprite poppingBobaSprite;
        [SerializeField] private Sprite grassJellySprite;
        [SerializeField] private Sprite coconutJellySprite;
        [SerializeField] private Sprite eggPuddingSprite;
        [SerializeField] private Sprite cheeseFoamSprite;
        [SerializeField] private Sprite goldenHoneyPearlsSprite;

        [Header("Store & Prep Objects")]
        [SerializeField] private Sprite iceCubeSprite;
        [SerializeField] private Sprite deskBellSprite;
        [SerializeField] private Sprite shutterMetalSprite;
        [SerializeField] private Sprite cupEmptySprite;
        [SerializeField] private Sprite cupLiquidMaskSprite;
        [SerializeField] private Sprite cupSealedLidSprite;

        [Header("Foraging & Raw Ingredients")]
        [SerializeField] private Sprite babyYippeeSprite;
        [SerializeField] private Sprite rawJellyBlocksSprite;
        [SerializeField] private Sprite rawGoldenDewSprite;
        [SerializeField] private Sprite grassPileSprite;
        [SerializeField] private Sprite jellyTreeSprite;
        [SerializeField] private Sprite rockShelfSprite;
        [SerializeField] private Sprite rockWallSprite;

        // Public Getters
        public Sprite FreshMilkSprite => freshMilkSprite;
        public Sprite OatMilkSprite => oatMilkSprite;
        public Sprite CoconutMilkSprite => coconutMilkSprite;
        public Sprite CondensedMilkSprite => condensedMilkSprite;

        public Sprite TapiocaSprite => tapiocaSprite;
        public Sprite PoppingBobaSprite => poppingBobaSprite;
        public Sprite GrassJellySprite => grassJellySprite;
        public Sprite CoconutJellySprite => coconutJellySprite;
        public Sprite EggPuddingSprite => eggPuddingSprite;
        public Sprite CheeseFoamSprite => cheeseFoamSprite;
        public Sprite GoldenHoneyPearlsSprite => goldenHoneyPearlsSprite;

        public Sprite IceCubeSprite => iceCubeSprite;
        public Sprite DeskBellSprite => deskBellSprite;
        public Sprite ShutterMetalSprite => shutterMetalSprite;
        public Sprite CupEmptySprite => cupEmptySprite;
        public Sprite CupLiquidMaskSprite => cupLiquidMaskSprite;
        public Sprite CupSealedLidSprite => cupSealedLidSprite;

        public Sprite BabyYippeeSprite => babyYippeeSprite;
        public Sprite RawJellyBlocksSprite => rawJellyBlocksSprite;
        public Sprite RawGoldenDewSprite => rawGoldenDewSprite;
        public Sprite GrassPileSprite => grassPileSprite;
        public Sprite JellyTreeSprite => jellyTreeSprite;
        public Sprite RockShelfSprite => rockShelfSprite;
        public Sprite RockWallSprite => rockWallSprite;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            EnsureAssetsLoaded();
        }

        private void OnValidate()
        {
            EnsureAssetsLoaded();
        }

        public void EnsureAssetsLoaded()
        {
#if UNITY_EDITOR
            // 1. Milks from milkicons.png
            if (freshMilkSprite == null || oatMilkSprite == null || coconutMilkSprite == null || condensedMilkSprite == null)
            {
                var milks = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/Sprites2/Sprites 3/milkicons.png");
                foreach (var obj in milks)
                {
                    if (obj is Sprite s)
                    {
                        if (s.name.ToLower().Contains("fresh") && freshMilkSprite == null) freshMilkSprite = s;
                        else if (s.name.ToLower().Contains("oat") && oatMilkSprite == null) oatMilkSprite = s;
                        else if (s.name.ToLower().Contains("coconut") && coconutMilkSprite == null) coconutMilkSprite = s;
                        else if (s.name.ToLower().Contains("condensed") && condensedMilkSprite == null) condensedMilkSprite = s;
                    }
                }
                // Fallback by slice index if naming is numeric
                if (milks.Length > 1)
                {
                    List<Sprite> list = new List<Sprite>();
                    foreach (var o in milks) if (o is Sprite sp) list.Add(sp);
                    if (list.Count >= 4)
                    {
                        if (freshMilkSprite == null) freshMilkSprite = list[0];
                        if (oatMilkSprite == null) oatMilkSprite = list[1];
                        if (coconutMilkSprite == null) coconutMilkSprite = list[2];
                        if (condensedMilkSprite == null) condensedMilkSprite = list[3];
                    }
                }
            }

            // 2. Toppings from ingredients.png
            if (tapiocaSprite == null || poppingBobaSprite == null || grassJellySprite == null ||
                coconutJellySprite == null || eggPuddingSprite == null || goldenHoneyPearlsSprite == null || cheeseFoamSprite == null)
            {
                var tops = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/Sprites2/Sprites 3/ingredients.png");
                List<Sprite> list = new List<Sprite>();
                foreach (var o in tops) if (o is Sprite sp) list.Add(sp);
                for (int i = 0; i < list.Count; i++)
                {
                    var s = list[i];
                    string n = s.name.ToLower();
                    if (n.Contains("tapioca") || n.Contains("pearl") || i == 0) { if (tapiocaSprite == null) tapiocaSprite = s; }
                    if (n.Contains("popping") || i == 5) { if (poppingBobaSprite == null) poppingBobaSprite = s; }
                    if (n.Contains("grass") || i == 4) { if (grassJellySprite == null) grassJellySprite = s; }
                    if (n.Contains("coconut") || i == 2) { if (coconutJellySprite == null) coconutJellySprite = s; }
                    if (n.Contains("pudding") || n.Contains("egg") || i == 1) { if (eggPuddingSprite == null) eggPuddingSprite = s; }
                    if (n.Contains("honey") || n.Contains("golden") || i == 3) { if (goldenHoneyPearlsSprite == null) goldenHoneyPearlsSprite = s; }
                    if (n.Contains("cheese") || n.Contains("foam") || i == 6) { if (cheeseFoamSprite == null) cheeseFoamSprite = s; }
                }
            }

            // 3. Store Objects (Ice Cube, Bell, etc.) from Store Objects.png
            if (iceCubeSprite == null)
            {
                var storeObjs = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/Sprites2/Sprites 3/Store Objects.png");
                foreach (var o in storeObjs)
                {
                    if (o is Sprite s)
                    {
                        // Store Objects_2 is the single ice cube sprite
                        if (s.name == "Store Objects_2" || s.name.ToLower().Contains("ice"))
                        {
                            iceCubeSprite = s;
                            break;
                        }
                    }
                }
            }

            // 4. Baby Yippee from bbyalienrun.png
            if (babyYippeeSprite == null)
            {
                var yippees = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/Sprites2/Sprites 3/Forage and prep/Bamboo/bbyalienrun.png");
                foreach (var o in yippees)
                {
                    if (o is Sprite s)
                    {
                        babyYippeeSprite = s;
                        break;
                    }
                }
            }

            // 5. Raw Ingredients & Landscapes
            if (rawJellyBlocksSprite == null)
            {
                rawJellyBlocksSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Sprites2/Sprites 3/Forage and prep/Meadows/jellytree.png");
            }
            if (rawGoldenDewSprite == null)
            {
                rawGoldenDewSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Sprites2/Sprites 3/Forage and prep/mountains/Rockshelf.png");
            }
            if (grassPileSprite == null)
            {
                grassPileSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Sprites2/Sprites 3/Forage and prep/Bamboo/grasspile.png");
            }
            if (jellyTreeSprite == null)
            {
                jellyTreeSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Sprites2/Sprites 3/Forage and prep/Meadows/jellytree.png");
            }
            if (rockShelfSprite == null)
            {
                rockShelfSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Sprites2/Sprites 3/Forage and prep/mountains/Rockshelf.png");
            }
            if (rockWallSprite == null)
            {
                rockWallSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Sprites2/Sprites 3/Forage and prep/mountains/RockWall.jpg");
            }

            // 6. Common Shop Visuals
            if (cupEmptySprite == null) cupEmptySprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Cup_Empty.png");
            if (cupLiquidMaskSprite == null) cupLiquidMaskSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Cup_LiquidMask.png");
            if (cupSealedLidSprite == null) cupSealedLidSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Cup_SealedLid.png");
            if (deskBellSprite == null) deskBellSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Desk_Bell.png");
            if (shutterMetalSprite == null) shutterMetalSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Shutter_Metal.png");
#endif
        }

        public Sprite GetMilkSprite(MilkType milkType)
        {
            return milkType switch
            {
                MilkType.FreshMilk => freshMilkSprite,
                MilkType.OatMilk => oatMilkSprite,
                MilkType.CoconutMilk => coconutMilkSprite,
                MilkType.CondensedMilk => condensedMilkSprite,
                _ => null
            };
        }

        public Sprite GetToppingSprite(ToppingType toppingType)
        {
            return toppingType switch
            {
                ToppingType.TapiocaPearls => tapiocaSprite,
                ToppingType.PoppingBoba => poppingBobaSprite,
                ToppingType.GrassJelly => grassJellySprite,
                ToppingType.CoconutJelly => coconutJellySprite,
                ToppingType.EggPudding => eggPuddingSprite,
                ToppingType.CheeseFoam => cheeseFoamSprite,
                ToppingType.GoldenHoneyPearls => goldenHoneyPearlsSprite,
                _ => null
            };
        }

        public Sprite GetSprite(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;

            string normalized = key.Replace(" ", "").Replace("_", "").ToLower();

            // 1. Ice
            if (normalized.Contains("ice")) return iceCubeSprite;

            // 2. Baby Yippee
            if (normalized.Contains("yippee") || normalized.Contains("alien") || normalized.Contains("foraging")) return babyYippeeSprite;

            // 3. Milks
            if (normalized.Contains("freshmilk")) return freshMilkSprite;
            if (normalized.Contains("oatmilk")) return oatMilkSprite;
            if (normalized.Contains("coconutmilk")) return coconutMilkSprite;
            if (normalized.Contains("condensedmilk")) return condensedMilkSprite;

            // 4. Toppings
            if (normalized.Contains("tapioca") || normalized.Contains("boba") && !normalized.Contains("popping")) return tapiocaSprite;
            if (normalized.Contains("popping")) return poppingBobaSprite;
            if (normalized.Contains("grassjelly")) return grassJellySprite;
            if (normalized.Contains("coconutjelly")) return coconutJellySprite;
            if (normalized.Contains("eggpudding") || normalized.Contains("custard")) return eggPuddingSprite;
            if (normalized.Contains("cheesefoam")) return cheeseFoamSprite;
            if (normalized.Contains("goldenhoney") || normalized.Contains("honeymerald")) return goldenHoneyPearlsSprite;

            // 5. Raw materials & Landscapes
            if (normalized.Contains("rawjelly") || normalized.Contains("jellytree")) return rawJellyBlocksSprite;
            if (normalized.Contains("goldendew") || normalized.Contains("dew")) return rawGoldenDewSprite;
            if (normalized.Contains("grasspile")) return grassPileSprite;
            if (normalized.Contains("rockshelf")) return rockShelfSprite;
            if (normalized.Contains("rockwall")) return rockWallSprite;

            // 6. Shop objects
            if (normalized.Contains("bell")) return deskBellSprite;
            if (normalized.Contains("shutter")) return shutterMetalSprite;
            if (normalized.Contains("cupempty")) return cupEmptySprite;
            if (normalized.Contains("liquidmask")) return cupLiquidMaskSprite;
            if (normalized.Contains("lid") || normalized.Contains("sealed")) return cupSealedLidSprite;

            return null;
        }
    }
}
