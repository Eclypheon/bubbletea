using System;

namespace BubbleTeaShop
{
    public enum GameState
    {
        MorningPrep,
        ShopOpen,
        CustomerWaiting,
        DrinkBrewing,
        CustomerReacting,
        ShopClosing,
        NightPhase,
        GameOver,
        GameWon
    }

    public enum TeaBase
    {
        None = 0,
        BlackTea,
        GreenTea,
        OolongTea,
        ThaiTea,
        TaroTea,
        WildMountainTea
    }

    public enum MilkType
    {
        None = 0,
        FreshMilk,
        OatMilk,
        CoconutMilk,
        CondensedMilk
    }

    public enum SweetnessLevel
    {
        Zero = 0,
        Quarter = 25,
        Half = 50,
        ThreeQuarter = 75,
        Full = 100
    }

    public enum IceLevel
    {
        None = 0,
        Light = 30,
        Regular = 50,
        Extra = 100
    }

    public enum ToppingType
    {
        TapiocaPearls,
        PoppingBoba,
        GrassJelly,
        CoconutJelly,
        EggPudding,
        CheeseFoam,
        GoldenHoneyPearls
    }

    public enum CustomerArchetype
    {
        Adhd,
        Autism,
        Anxiety,
        Tourettes,
        Dyscalculia,
        Dyslexia
    }

    public enum UpgradeType
    {
        PearlFarm,
        DigitalSugarMeter,
        AutoSealer,
        FastChiller,
        CozyDecor,
        StorefrontSign,
        Advertisements,
        StorefrontBeautification,
        YippeePheromones,
        SwitchSupplyContract,
        LuckyCat,
        BambooGroveTrailMap,
        HoneyMeadowsTrailMap,
        MistyMountainsTrailMap,
        ImproveStoreAmbience,
        ArtisanalTeaMenu,
        LuckyPoppingBobaBracelet,
        ChefsHoningSteel,
        DowsingRods,
        NightChauffeur,
        MarketingIntern
    }
}
