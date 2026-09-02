using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;

namespace BubbleTeaShop.Editor
{
    public class ShopSceneSetup : EditorWindow
    {
        [MenuItem("Tools/Bubble Tea Shop/Setup Full Game Scene")]
        public static void BuildSceneHierarchy()
        {
            // 1. Managers Root
            GameObject managersRoot = GameObject.Find("--- MANAGERS ---");
            if (managersRoot == null)
            {
                managersRoot = new GameObject("--- MANAGERS ---");
                Undo.RegisterCreatedObjectUndo(managersRoot, "Create Managers Root");
            }

            var gameMgr = GetOrAddComponent<GameManager>(managersRoot);
            var ecoMgr = GetOrAddComponent<EconomyManager>(managersRoot);
            var invMgr = GetOrAddComponent<InventoryManager>(managersRoot);
            var dayMgr = GetOrAddComponent<DayManager>(managersRoot);
            var custMgr = GetOrAddComponent<CustomerManager>(managersRoot);
            var upgMgr = GetOrAddComponent<UpgradeManager>(managersRoot);
            var mktMgr = GetOrAddComponent<MarketManager>(managersRoot);
            var forMgr = GetOrAddComponent<ForagingManager>(managersRoot);

            // 2. Event System
            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject es = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
                Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
            }

            // 3. UI Canvas
            GameObject canvasObj = GameObject.Find("GameCanvas");
            if (canvasObj == null)
            {
                canvasObj = new GameObject("GameCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                Undo.RegisterCreatedObjectUndo(canvasObj, "Create Canvas");
            }

            Canvas canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // Load Sprites if available
            Sprite frameSp = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Shopfront_Frame.png");
            Sprite streetSp = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Street_Background.png");
            Sprite shutterSp = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Shutter_Metal.png");
            Sprite bellSp = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Desk_Bell.png");
            Sprite workerSp = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Customer_OfficeWorker.png");
            Sprite studentSp = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Customer_Student.png");
            Sprite connoisseurSp = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Customer_Connoisseur.png");
            Sprite kidSp = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Customer_Kid.png");
            Sprite mysticSp = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Customer_Mystic.png");
            Sprite cupEmptySp = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Cup_Empty.png");
            Sprite liquidMaskSp = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Cup_LiquidMask.png");
            Sprite sealedLidSp = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Cup_SealedLid.png");
            Sprite bobaSp = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Topping_Boba.png");
            Sprite iceSp = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Ice_Cubes.png");
            Sprite bubbleSp = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/SpeechBubble.png");

            // 4. Shopfront Viewport & Street View
            GameObject shopfrontRoot = CreateUIElement("ShopfrontRoot", canvasObj.transform);
            SetFullStretch(shopfrontRoot.GetComponent<RectTransform>());

            // Street Background
            GameObject streetBg = CreateUIElement("StreetBackground", shopfrontRoot.transform);
            RectTransform streetRect = streetBg.GetComponent<RectTransform>();
            streetRect.anchorMin = new Vector2(0.5f, 0.5f);
            streetRect.anchorMax = new Vector2(0.5f, 0.5f);
            streetRect.sizeDelta = new Vector2(1200, 700);
            streetRect.anchoredPosition = new Vector2(0, 100);
            var streetImg = streetBg.AddComponent<Image>();
            streetImg.sprite = streetSp;

            // Customer Container (Outside the window)
            GameObject customerObj = CreateUIElement("Customer", streetBg.transform);
            RectTransform custRect = customerObj.GetComponent<RectTransform>();
            custRect.anchorMin = new Vector2(0.5f, 0.5f);
            custRect.anchorMax = new Vector2(0.5f, 0.5f);
            custRect.sizeDelta = new Vector2(512, 600);
            custRect.anchoredPosition = new Vector2(0, -50);
            var custImg = customerObj.AddComponent<Image>();
            custImg.sprite = workerSp;

            // Patience Bar
            GameObject patienceBarObj = CreateUIElement("PatienceBar", customerObj.transform);
            RectTransform patRect = patienceBarObj.GetComponent<RectTransform>();
            patRect.anchoredPosition = new Vector2(0, 310);
            patRect.sizeDelta = new Vector2(280, 20);
            var patBg = patienceBarObj.AddComponent<Image>();
            patBg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            GameObject patFillObj = CreateUIElement("Fill", patienceBarObj.transform);
            SetFullStretch(patFillObj.GetComponent<RectTransform>());
            var patFill = patFillObj.AddComponent<Image>();
            patFill.type = Image.Type.Filled;
            patFill.fillMethod = Image.FillMethod.Horizontal;
            patFill.color = Color.green;

            // Customer Speech Bubble
            GameObject bubbleObj = CreateUIElement("SpeechBubble", customerObj.transform);
            RectTransform bubbleRect = bubbleObj.GetComponent<RectTransform>();
            bubbleRect.anchoredPosition = new Vector2(260, 240);
            bubbleRect.sizeDelta = new Vector2(420, 220);
            var bubbleImg = bubbleObj.AddComponent<Image>();
            bubbleImg.sprite = bubbleSp;
            bubbleImg.type = Image.Type.Sliced;
            var bubbleGroup = bubbleObj.AddComponent<CanvasGroup>();
            var speechUI = bubbleObj.AddComponent<SpeechBubbleUI>();

            GameObject dialogTextObj = CreateUIElement("DialogueText", bubbleObj.transform);
            RectTransform dlgRect = dialogTextObj.GetComponent<RectTransform>();
            dlgRect.anchoredPosition = Vector2.zero;
            dlgRect.sizeDelta = new Vector2(370, 160);
            var dlgTmp = dialogTextObj.AddComponent<TextMeshProUGUI>();
            dlgTmp.fontSize = 19;
            dlgTmp.color = new Color(0.15f, 0.15f, 0.15f);
            dlgTmp.alignment = TextAlignmentOptions.Center;

            // Wire CustomerController
            var custController = customerObj.AddComponent<CustomerController>();
            SetSerializedProperty(custController, "customerImage", custImg);
            SetSerializedProperty(custController, "patienceFillImage", patFill);
            SetSerializedProperty(custController, "speechBubble", speechUI);
            SetSerializedProperty(custController, "adhdSprite", workerSp);
            SetSerializedProperty(custController, "autismSprite", studentSp);
            SetSerializedProperty(custController, "anxietySprite", connoisseurSp);
            SetSerializedProperty(custController, "tourettesSprite", kidSp);
            SetSerializedProperty(custController, "dyscalculiaSprite", mysticSp);
            SetSerializedProperty(custController, "dyslexiaSprite", mysticSp);

            SetSerializedProperty(speechUI, "canvasGroup", bubbleGroup);
            SetSerializedProperty(speechUI, "dialogueText", dlgTmp);
            SetSerializedProperty(custMgr, "customerController", custController);

            // Rent Choice Buttons
            GameObject rentChoicePanel = CreateUIElement("RentChoicePanel", shopfrontRoot.transform);
            RectTransform rentChoiceRect = rentChoicePanel.GetComponent<RectTransform>();
            rentChoiceRect.anchorMin = new Vector2(0.5f, 0.5f);
            rentChoiceRect.anchorMax = new Vector2(0.5f, 0.5f);
            rentChoiceRect.anchoredPosition = new Vector2(0, -60);
            rentChoiceRect.sizeDelta = new Vector2(440, 60);

            GameObject payBtnObj = CreateButton("PayRentButton", rentChoicePanel.transform, "Pay Rent ($150.00)", new Vector2(-110, 0), new Vector2(200, 50));
            GameObject skipBtnObj = CreateButton("SkipRentButton", rentChoicePanel.transform, "Ask for Extension", new Vector2(110, 0), new Vector2(200, 50));

            SetSerializedProperty(custController, "landlordSprite", connoisseurSp);
            SetSerializedProperty(custController, "rentChoicePanel", rentChoicePanel);
            SetSerializedProperty(custController, "payRentButton", payBtnObj.GetComponent<Button>());
            SetSerializedProperty(custController, "payRentButtonText", payBtnObj.GetComponentInChildren<TextMeshProUGUI>());
            SetSerializedProperty(custController, "skipRentButton", skipBtnObj.GetComponent<Button>());
            SetSerializedProperty(custController, "skipRentButtonText", skipBtnObj.GetComponentInChildren<TextMeshProUGUI>());

            // Shutter
            GameObject shutterObj = CreateUIElement("MetalShutter", streetBg.transform);
            RectTransform shutRect = shutterObj.GetComponent<RectTransform>();
            shutRect.sizeDelta = new Vector2(1200, 700);
            shutRect.anchoredPosition = Vector2.zero;
            var shutImg = shutterObj.AddComponent<Image>();
            shutImg.sprite = shutterSp;
            var shutterCtrl = shutterObj.AddComponent<ShutterController>();
            SetSerializedProperty(shutterCtrl, "shutterRect", shutRect);

            // Shopfront Wooden Frame & Counter
            GameObject frameObj = CreateUIElement("ShopfrontFrame", shopfrontRoot.transform);
            SetFullStretch(frameObj.GetComponent<RectTransform>());
            var frameImg = frameObj.AddComponent<Image>();
            frameImg.sprite = frameSp;
            frameImg.raycastTarget = false;

            // 5. Interactive Counter Stations
            // Shutter Lever & Bell Area (Left)
            GameObject leftStation = CreateUIElement("LeftControlsStation", shopfrontRoot.transform);
            RectTransform leftRect = leftStation.GetComponent<RectTransform>();
            leftRect.anchorMin = new Vector2(0, 0);
            leftRect.anchorMax = new Vector2(0.2f, 0.4f);
            leftRect.anchoredPosition = new Vector2(150, 200);

            // Shutter Button
            GameObject shutBtnObj = CreateButton("ShutterButton", leftStation.transform, "Open Shutter", new Vector2(0, 80), new Vector2(180, 50));
            SetSerializedProperty(shutterCtrl, "shutterToggleButton", shutBtnObj.GetComponent<Button>());
            SetSerializedProperty(shutterCtrl, "shutterButtonText", shutBtnObj.GetComponentInChildren<TextMeshProUGUI>());

            // Desk Bell
            GameObject bellObj = CreateUIElement("DeskBell", leftStation.transform);
            RectTransform bellRect = bellObj.GetComponent<RectTransform>();
            bellRect.anchoredPosition = new Vector2(0, -40);
            bellRect.sizeDelta = new Vector2(120, 120);
            var bellImg = bellObj.AddComponent<Image>();
            bellImg.sprite = bellSp;
            var bellBtn = bellObj.AddComponent<Button>();
            var deskBell = bellObj.AddComponent<DeskBell>();
            SetSerializedProperty(deskBell, "bellButton", bellBtn);
            SetSerializedProperty(deskBell, "bellTransform", bellRect);

            // Cup Station (Center Counter)
            GameObject cupStationObj = CreateUIElement("CupStation", shopfrontRoot.transform);
            RectTransform cupStRect = cupStationObj.GetComponent<RectTransform>();
            cupStRect.anchorMin = new Vector2(0.5f, 0);
            cupStRect.anchorMax = new Vector2(0.5f, 0);
            cupStRect.sizeDelta = new Vector2(300, 420);
            cupStRect.anchoredPosition = new Vector2(-60, 210);

            // Cup Visual Layers
            GameObject cupCont = CreateUIElement("CupContainer", cupStationObj.transform);
            SetFullStretch(cupCont.GetComponent<RectTransform>());

            GameObject teaLiquid = CreateUIElement("TeaLiquidLayer", cupCont.transform);
            SetFullStretch(teaLiquid.GetComponent<RectTransform>());
            var teaImg = teaLiquid.AddComponent<Image>();
            teaImg.sprite = liquidMaskSp;

            GameObject milkLayer = CreateUIElement("MilkLayer", cupCont.transform);
            SetFullStretch(milkLayer.GetComponent<RectTransform>());
            var milkImg = milkLayer.AddComponent<Image>();
            milkImg.sprite = liquidMaskSp;
            milkImg.color = new Color(1, 1, 1, 0.45f);

            GameObject toppingVisual = CreateUIElement("ToppingsVisual", cupCont.transform);
            SetFullStretch(toppingVisual.GetComponent<RectTransform>());
            var bobaImg = toppingVisual.AddComponent<Image>();
            bobaImg.sprite = bobaSp;

            GameObject iceVisual = CreateUIElement("IceVisual", cupCont.transform);
            SetFullStretch(iceVisual.GetComponent<RectTransform>());
            var iceImg = iceVisual.AddComponent<Image>();
            iceImg.sprite = iceSp;

            GameObject cupOutline = CreateUIElement("CupOutline", cupCont.transform);
            SetFullStretch(cupOutline.GetComponent<RectTransform>());
            var cupOutImg = cupOutline.AddComponent<Image>();
            cupOutImg.sprite = cupEmptySp;

            GameObject sealedLid = CreateUIElement("SealedLid", cupCont.transform);
            SetFullStretch(sealedLid.GetComponent<RectTransform>());
            var sealImg = sealedLid.AddComponent<Image>();
            sealImg.sprite = sealedLidSp;

            // Cup Action Buttons
            GameObject serveBtn = CreateButton("ServeButton", cupStationObj.transform, "SERVE DRINK", new Vector2(0, -60), new Vector2(200, 45));
            GameObject trashBtn = CreateButton("TrashButton", cupStationObj.transform, "Trash", new Vector2(-120, -60), new Vector2(90, 45));
            GameObject newCupBtn = CreateButton("NewCupButton", cupStationObj.transform, "New Cup", new Vector2(120, -60), new Vector2(90, 45));

            var cupStation = cupStationObj.AddComponent<CupStation>();
            SetSerializedProperty(cupStation, "cupContainer", cupCont);
            SetSerializedProperty(cupStation, "teaLiquidImage", teaImg);
            SetSerializedProperty(cupStation, "milkLayerImage", milkImg);
            SetSerializedProperty(cupStation, "toppingsVisualParent", toppingVisual);
            SetSerializedProperty(cupStation, "iceVisualParent", iceVisual);
            SetSerializedProperty(cupStation, "sealedLidObject", sealedLid);
            SetSerializedProperty(cupStation, "serveCupButton", serveBtn.GetComponent<Button>());
            SetSerializedProperty(cupStation, "trashCupButton", trashBtn.GetComponent<Button>());
            SetSerializedProperty(cupStation, "newCupButton", newCupBtn.GetComponent<Button>());

            // Cup Sealer Station
            GameObject sealerObj = CreateButton("SealerButton", shopfrontRoot.transform, "Seal Cup Lid", new Vector2(250, 160), new Vector2(160, 50));
            var sealer = sealerObj.AddComponent<CupSealer>();
            SetSerializedProperty(sealer, "sealButton", sealerObj.GetComponent<Button>());

            // Dispensers Area (Right Counter)
            GameObject rightStation = CreateUIElement("RightDispensersStation", shopfrontRoot.transform);
            RectTransform rightRect = rightStation.GetComponent<RectTransform>();
            rightRect.anchorMin = new Vector2(0.75f, 0);
            rightRect.anchorMax = new Vector2(1f, 0.45f);
            rightRect.anchoredPosition = new Vector2(-180, 200);

            // Tea Dispensers
            CreateTeaButton(rightStation.transform, "Black Tea", TeaBase.BlackTea, new Vector2(-80, 80));
            CreateTeaButton(rightStation.transform, "Green Tea", TeaBase.GreenTea, new Vector2(40, 80));
            CreateTeaButton(rightStation.transform, "Oolong", TeaBase.OolongTea, new Vector2(-80, 25));
            CreateTeaButton(rightStation.transform, "Thai Tea", TeaBase.ThaiTea, new Vector2(40, 25));
            CreateTeaButton(rightStation.transform, "Taro", TeaBase.TaroTea, new Vector2(-80, -30));

            // Topping Stations
            CreateToppingButton(rightStation.transform, "Boba", ToppingType.TapiocaPearls, new Vector2(-80, -90));
            CreateToppingButton(rightStation.transform, "Popping", ToppingType.PoppingBoba, new Vector2(40, -90));
            CreateToppingButton(rightStation.transform, "Jelly", ToppingType.GrassJelly, new Vector2(-80, -145));
            CreateToppingButton(rightStation.transform, "Pudding", ToppingType.EggPudding, new Vector2(40, -145));

            // 6. Top Bar HUD
            GameObject hudObj = CreateUIElement("HUD_TopBar", canvasObj.transform);
            RectTransform hudRect = hudObj.GetComponent<RectTransform>();
            hudRect.anchorMin = new Vector2(0, 1);
            hudRect.anchorMax = new Vector2(1, 1);
            hudRect.pivot = new Vector2(0.5f, 1);
            hudRect.sizeDelta = new Vector2(0, 70);
            hudRect.anchoredPosition = Vector2.zero;
            var hudImg = hudObj.AddComponent<Image>();
            hudImg.color = new Color(0.12f, 0.1f, 0.1f, 0.9f);

            var hudDay = CreateText("DayText", hudObj.transform, "Day 1", new Vector2(-700, 0), 24);
            var hudCash = CreateText("CashText", hudObj.transform, "$50.00", new Vector2(-400, 0), 26, Color.yellow);
            var hudRent = CreateText("RentText", hudObj.transform, "Rent in: 7d", new Vector2(0, 0), 22);
            var hudCust = CreateText("CustCountText", hudObj.transform, "Customers: 0/5", new Vector2(400, 0), 22);
            var hudHint = CreateText("HintText", hudObj.transform, "Open the shutter to start the day!", new Vector2(700, 0), 18, Color.cyan);

            // Payout Indicator Panel (parked under HUD / Canvas for clear hierarchy control)
            GameObject payoutPanel = CreateUIElement("PayoutIndicatorPanel", canvasObj.transform);
            RectTransform payoutRt = payoutPanel.GetComponent<RectTransform>();
            payoutRt.anchorMin = new Vector2(0.5f, 0f);
            payoutRt.anchorMax = new Vector2(0.5f, 0f);
            payoutRt.pivot = new Vector2(0.5f, 0f);
            payoutRt.anchoredPosition = new Vector2(0f, 15f);
            payoutRt.sizeDelta = new Vector2(560f, 36f);
            var payoutImg = payoutPanel.AddComponent<Image>();
            payoutImg.color = new Color(0.08f, 0.08f, 0.12f, 0.88f);
            payoutPanel.SetActive(false);

            var hudCtrl = hudObj.AddComponent<HUDController>();
            SetSerializedProperty(hudCtrl, "dayText", hudDay);
            SetSerializedProperty(hudCtrl, "cashText", hudCash);
            SetSerializedProperty(hudCtrl, "rentTimerText", hudRent);
            SetSerializedProperty(hudCtrl, "customerCountText", hudCust);
            SetSerializedProperty(hudCtrl, "statusHintText", hudHint);
            SetSerializedProperty(hudCtrl, "payoutIndicatorPanel", payoutPanel);

            // 7. Night Phase Panel
            GameObject nightRoot = CreateUIElement("NightPhaseCanvas", canvasObj.transform);
            SetFullStretch(nightRoot.GetComponent<RectTransform>());
            var nightBg = nightRoot.AddComponent<Image>();
            nightBg.color = new Color(0.08f, 0.08f, 0.12f, 0.96f);

            var nightMenu = CreateText("NightTitle", nightRoot.transform, "NIGHT MANAGEMENT", new Vector2(0, 440), 32, Color.white);
            
            // Tab buttons
            GameObject tabMkt = CreateButton("TabMarket", nightRoot.transform, "Wholesale Market", new Vector2(-400, 360), new Vector2(220, 50));
            GameObject tabFor = CreateButton("TabForage", nightRoot.transform, "Foraging Expedition", new Vector2(-130, 360), new Vector2(220, 50));
            GameObject tabUpg = CreateButton("TabUpgrades", nightRoot.transform, "Shop Upgrades", new Vector2(140, 360), new Vector2(220, 50));
            GameObject tabLed = CreateButton("TabLedger", nightRoot.transform, "Daily Ledger & Rent", new Vector2(410, 360), new Vector2(220, 50));
            GameObject sleepBtn = CreateButton("SleepButton", nightRoot.transform, "Sleep & Start Next Day", new Vector2(0, -420), new Vector2(300, 60));

            // Content Panels
            GameObject mktPanel = CreatePanel("MarketPanel", nightRoot.transform);
            GameObject forPanel = CreatePanel("ForagingPanel", nightRoot.transform);
            GameObject upgPanel = CreatePanel("UpgradesPanel", nightRoot.transform);
            GameObject ledPanel = CreatePanel("LedgerPanel", nightRoot.transform);

            // Foraging buttons inside Foraging Panel
            GameObject bmbBtn = CreateButton("ForageBambooBtn", forPanel.transform, "Whispering Bamboo Grove", new Vector2(-300, 60), new Vector2(260, 60));
            GameObject hnyBtn = CreateButton("ForageHoneyBtn", forPanel.transform, "Golden Honey Meadow", new Vector2(0, 60), new Vector2(260, 60));
            GameObject mntBtn = CreateButton("ForageMntBtn", forPanel.transform, "Mist Peak Mountain", new Vector2(300, 60), new Vector2(260, 60));
            var forLog = CreateText("ForageLogText", forPanel.transform, "Select a location to forage wild tea leaves & toppings.", new Vector2(0, -100), 20);

            // Ledger content
            var ledSummary = CreateText("LedgerSummary", ledPanel.transform, "Day Summary...", new Vector2(0, 80), 22);
            var rentStatus = CreateText("RentStatus", ledPanel.transform, "Rent: $150 (Due in 7 days)", new Vector2(0, -40), 24, Color.yellow);
            GameObject buyoutBtn = CreateButton("BuyoutButton", ledPanel.transform, "Buy Out Location ($1,500)", new Vector2(0, -140), new Vector2(320, 60));

            var nightMgr = nightRoot.AddComponent<NightPhaseManager>();
            SetSerializedProperty(nightMgr, "nightPanelRoot", nightRoot);
            SetSerializedProperty(nightMgr, "marketTabPanel", mktPanel);
            SetSerializedProperty(nightMgr, "foragingTabPanel", forPanel);
            SetSerializedProperty(nightMgr, "upgradesTabPanel", upgPanel);
            SetSerializedProperty(nightMgr, "ledgerTabPanel", ledPanel);
            SetSerializedProperty(nightMgr, "tabMarketButton", tabMkt.GetComponent<Button>());
            SetSerializedProperty(nightMgr, "tabForagingButton", tabFor.GetComponent<Button>());
            SetSerializedProperty(nightMgr, "tabUpgradesButton", tabUpg.GetComponent<Button>());
            SetSerializedProperty(nightMgr, "tabLedgerButton", tabLed.GetComponent<Button>());
            SetSerializedProperty(nightMgr, "sleepButton", sleepBtn.GetComponent<Button>());
            SetSerializedProperty(nightMgr, "forageBambooBtn", bmbBtn.GetComponent<Button>());
            SetSerializedProperty(nightMgr, "forageHoneyBtn", hnyBtn.GetComponent<Button>());
            SetSerializedProperty(nightMgr, "forageMountainBtn", mntBtn.GetComponent<Button>());
            SetSerializedProperty(nightMgr, "foragingLogText", forLog);
            SetSerializedProperty(nightMgr, "ledgerSummaryText", ledSummary);
            SetSerializedProperty(nightMgr, "rentStatusText", rentStatus);
            SetSerializedProperty(nightMgr, "buyoutShopButton", buyoutBtn.GetComponent<Button>());
            SetSerializedProperty(nightMgr, "buyoutButtonText", buyoutBtn.GetComponentInChildren<TextMeshProUGUI>());

            // 8. End Game Modal
            GameObject modalObj = CreateUIElement("EndGameModal", canvasObj.transform);
            SetFullStretch(modalObj.GetComponent<RectTransform>());
            var modBg = modalObj.AddComponent<Image>();
            modBg.color = new Color(0, 0, 0, 0.85f);

            var modTitle = CreateText("ModalTitle", modalObj.transform, "VICTORY!", new Vector2(0, 100), 40, Color.yellow);
            var modMsg = CreateText("ModalMessage", modalObj.transform, "You bought over the shop location!", new Vector2(0, 0), 22);
            GameObject restartBtn = CreateButton("RestartBtn", modalObj.transform, "Play Again", new Vector2(0, -120), new Vector2(200, 50));

            var modalCtrl = modalObj.AddComponent<EndGameModal>();
            SetSerializedProperty(modalCtrl, "modalRoot", modalObj);
            SetSerializedProperty(modalCtrl, "titleText", modTitle);
            SetSerializedProperty(modalCtrl, "messageText", modMsg);
            SetSerializedProperty(modalCtrl, "restartButton", restartBtn.GetComponent<Button>());

            // 9. Bamboo Grove View Controller
            GameObject bambooObj = CreateUIElement("BambooGroveViewController", canvasObj.transform);
            SetFullStretch(bambooObj.GetComponent<RectTransform>());
            bambooObj.AddComponent<BambooGroveViewController>();
            bambooObj.SetActive(false);

            // 10. Honey Meadow View Controller
            GameObject meadowObj = CreateUIElement("HoneyMeadowViewController", canvasObj.transform);
            SetFullStretch(meadowObj.GetComponent<RectTransform>());
            meadowObj.AddComponent<HoneyMeadowViewController>();
            meadowObj.SetActive(false);

            // 11. Mist Mountain View Controller
            GameObject mountainObj = CreateUIElement("MistMountainViewController", canvasObj.transform);
            SetFullStretch(mountainObj.GetComponent<RectTransform>());
            mountainObj.AddComponent<MistMountainViewController>();
            mountainObj.SetActive(false);

            // 12. Kitchen Prep Area View Controller
            GameObject prepObj = CreateUIElement("PrepAreaViewController", canvasObj.transform);
            SetFullStretch(prepObj.GetComponent<RectTransform>());
            prepObj.AddComponent<PrepAreaViewController>();
            prepObj.SetActive(false);

            // 13. Wholesale Supermarket View Controller
            GameObject mktCtrlObj = CreateUIElement("SupermarketViewController", canvasObj.transform);
            SetFullStretch(mktCtrlObj.GetComponent<RectTransform>());
            mktCtrlObj.AddComponent<SupermarketViewController>();
            mktCtrlObj.SetActive(false);

            EditorUtility.SetDirty(canvasObj);
            Debug.Log("[ShopSceneSetup] Complete Bubble Tea Game Scene setup successfully!");
        }

        private static GameObject CreateUIElement(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static GameObject CreatePanel(string name, Transform parent)
        {
            GameObject panel = CreateUIElement(name, parent);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.1f, 0.15f);
            rect.anchorMax = new Vector2(0.9f, 0.75f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            return panel;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, string content, Vector2 pos, float size, Color? color = null)
        {
            GameObject go = CreateUIElement(name, parent);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(600, 80);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = content;
            tmp.fontSize = size;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color ?? Color.white;
            return tmp;
        }

        private static GameObject CreateButton(string name, Transform parent, string label, Vector2 pos, Vector2 size)
        {
            GameObject btnObj = CreateUIElement(name, parent);
            RectTransform rect = btnObj.GetComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            var img = btnObj.AddComponent<Image>();
            img.color = new Color(0.25f, 0.22f, 0.28f, 0.95f);
            var btn = btnObj.AddComponent<Button>();

            GameObject textObj = CreateUIElement("Text", btnObj.transform);
            SetFullStretch(textObj.GetComponent<RectTransform>());
            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 18;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return btnObj;
        }

        private static void CreateTeaButton(Transform parent, string label, TeaBase tea, Vector2 pos)
        {
            GameObject btn = CreateButton($"Tea_{tea}", parent, label, pos, new Vector2(110, 45));
            var disp = btn.AddComponent<TeaDispenser>();
            SetSerializedProperty(disp, "teaType", tea);
            SetSerializedProperty(disp, "dispenseButton", btn.GetComponent<Button>());
        }

        private static void CreateToppingButton(Transform parent, string label, ToppingType topping, Vector2 pos)
        {
            GameObject btn = CreateButton($"Topping_{topping}", parent, label, pos, new Vector2(110, 45));
            var station = btn.AddComponent<ToppingStation>();
            SetSerializedProperty(station, "toppingType", topping);
            SetSerializedProperty(station, "scoopButton", btn.GetComponent<Button>());
        }

        private static void SetFullStretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
        }

        private static T GetOrAddComponent<T>(GameObject go) where T : Component
        {
            T comp = go.GetComponent<T>();
            if (comp == null) comp = go.AddComponent<T>();
            return comp;
        }

        private static void SetSerializedProperty(Object target, string propName, object value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(propName);
            if (prop != null)
            {
                if (value is Object unityObj) prop.objectReferenceValue = unityObj;
                else if (value is System.Enum enumVal) prop.enumValueIndex = System.Convert.ToInt32(enumVal);
                else if (value is int intVal) prop.intValue = intVal;
                else if (value is float floatVal) prop.floatValue = floatVal;
                else if (value is string strVal) prop.stringValue = strVal;
                else if (value is bool boolVal) prop.boolValue = boolVal;
                so.ApplyModifiedProperties();
            }
        }

        [MenuItem("Tools/Bubble Tea Shop/Auto-Wire Recovered Scene")]
        [MenuItem("Bubble Tea Shop/Auto-Wire Recovered Scene")]
        public static void AutoWireRecoveredScene()
        {
            var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            System.Func<string, GameObject> findGo = (name) =>
            {
                foreach (var go in allObjects)
                {
                    if (go != null && go.name == name && go.scene.isLoaded) return go;
                }
                return null;
            };

            System.Func<string, Sprite> loadSprite = (path) =>
            {
                var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sp == null)
                {
                    var all = AssetDatabase.LoadAllAssetsAtPath(path);
                    foreach (var a in all)
                    {
                        if (a is Sprite s) return s;
                    }
                }
                return sp;
            };

            // 1. Managers
            var mgrRoot = findGo("---Managers---") ?? findGo("--- MANAGERS ---");
            if (mgrRoot != null)
            {
                GetOrAddComponent<GameManager>(mgrRoot);
                GetOrAddComponent<EconomyManager>(mgrRoot);
                GetOrAddComponent<InventoryManager>(mgrRoot);
                GetOrAddComponent<DayManager>(mgrRoot);
                GetOrAddComponent<CustomerManager>(mgrRoot);
                GetOrAddComponent<UpgradeManager>(mgrRoot);
                GetOrAddComponent<MarketManager>(mgrRoot);
                GetOrAddComponent<ForagingManager>(mgrRoot);
                GetOrAddComponent<MarketPriceManager>(mgrRoot);
                GetOrAddComponent<MarketEventManager>(mgrRoot);
                GetOrAddComponent<MentorController>(mgrRoot);
            }

            // 2. HUD Controller
            var hudGo = findGo("HUD_TopBar") ?? findGo("HUD_Top_Panel");
            if (hudGo != null)
            {
                var hud = GetOrAddComponent<HUDController>(hudGo);
                var day = findGo("DayText")?.GetComponent<TextMeshProUGUI>();
                var cash = findGo("CashText")?.GetComponent<TextMeshProUGUI>();
                var rent = findGo("RentTimerText")?.GetComponent<TextMeshProUGUI>() ?? findGo("RentText")?.GetComponent<TextMeshProUGUI>();
                var cust = findGo("CustomerCountText")?.GetComponent<TextMeshProUGUI>() ?? findGo("CustCountText")?.GetComponent<TextMeshProUGUI>();
                var hint = findGo("StatusHintText")?.GetComponent<TextMeshProUGUI>() ?? findGo("HintText")?.GetComponent<TextMeshProUGUI>();
                if (day != null) SetSerializedProperty(hud, "dayText", day);
                if (cash != null) SetSerializedProperty(hud, "cashText", cash);
                if (rent != null) SetSerializedProperty(hud, "rentTimerText", rent);
                if (cust != null) SetSerializedProperty(hud, "customerCountText", cust);
                if (hint != null) SetSerializedProperty(hud, "statusHintText", hint);
                EditorUtility.SetDirty(hud);
            }

            // 3. NightPhaseManager
            var nightRoot = findGo("Night Phase Canvas") ?? findGo("NightPhaseCanvas");
            if (nightRoot != null)
            {
                var npm = GetOrAddComponent<NightPhaseManager>(nightRoot);
                SetSerializedProperty(npm, "nightPanelRoot", nightRoot);
                var mktPanel = findGo("MarketPanel");
                var forPanel = findGo("ForagingPanel");
                var upgPanel = findGo("UpgradesPanel");
                var ledPanel = findGo("LedgerPanel");
                if (mktPanel != null) SetSerializedProperty(npm, "marketTabPanel", mktPanel);
                if (forPanel != null) SetSerializedProperty(npm, "foragingTabPanel", forPanel);
                if (upgPanel != null) SetSerializedProperty(npm, "upgradesTabPanel", upgPanel);
                if (ledPanel != null) SetSerializedProperty(npm, "ledgerTabPanel", ledPanel);

                var tabMkt = findGo("TabMarket")?.GetComponent<Button>();
                var tabFor = findGo("TabForage")?.GetComponent<Button>();
                var tabUpg = findGo("TabUpgrades")?.GetComponent<Button>();
                var tabLed = findGo("TabLedger")?.GetComponent<Button>();
                var prepBtn = findGo("PrepAreaButton")?.GetComponent<Button>();
                var sleepBtn = findGo("SleepButton")?.GetComponent<Button>();
                var bmbBtn = findGo("ForageBambooBtn")?.GetComponent<Button>();
                var hnyBtn = findGo("ForageHoneyBtn")?.GetComponent<Button>();
                var mntBtn = findGo("ForageMountainBtn")?.GetComponent<Button>();
                var forLog = findGo("ForageLogText")?.GetComponent<TextMeshProUGUI>();
                var ledSum = findGo("LedgerSummary")?.GetComponent<TextMeshProUGUI>();
                var rentStat = findGo("RentStatus")?.GetComponent<TextMeshProUGUI>();
                var buyoutBtn = findGo("BuyoutButton")?.GetComponent<Button>();

                if (tabMkt != null) SetSerializedProperty(npm, "tabMarketButton", tabMkt);
                if (tabFor != null) SetSerializedProperty(npm, "tabForagingButton", tabFor);
                if (tabUpg != null) SetSerializedProperty(npm, "tabUpgradesButton", tabUpg);
                if (tabLed != null) SetSerializedProperty(npm, "tabLedgerButton", tabLed);
                if (prepBtn != null) SetSerializedProperty(npm, "prepAreaButton", prepBtn);
                if (sleepBtn != null) SetSerializedProperty(npm, "sleepButton", sleepBtn);
                if (bmbBtn != null) SetSerializedProperty(npm, "forageBambooBtn", bmbBtn);
                if (hnyBtn != null) SetSerializedProperty(npm, "forageHoneyBtn", hnyBtn);
                if (mntBtn != null) SetSerializedProperty(npm, "forageMountainBtn", mntBtn);
                if (forLog != null) SetSerializedProperty(npm, "foragingLogText", forLog);
                if (ledSum != null) SetSerializedProperty(npm, "ledgerSummaryText", ledSum);
                if (rentStat != null) SetSerializedProperty(npm, "rentStatusText", rentStat);
                if (buyoutBtn != null)
                {
                    SetSerializedProperty(npm, "buyoutShopButton", buyoutBtn);
                    var bTxt = buyoutBtn.GetComponentInChildren<TextMeshProUGUI>();
                    if (bTxt != null) SetSerializedProperty(npm, "buyoutButtonText", bTxt);
                }
                EditorUtility.SetDirty(npm);
            }

            // 4. CupStation & Sealer
            var cupStGo = findGo("CupStation");
            if (cupStGo != null)
            {
                var cs = GetOrAddComponent<CupStation>(cupStGo);
                var serve = findGo("ServeButton")?.GetComponent<Button>();
                var trash = findGo("TrashButton")?.GetComponent<Button>();
                var seal = findGo("SealerButton")?.GetComponent<Button>();
                var emptyCup = findGo("CupContainer")?.GetComponent<Image>() ?? findGo("CupOutline")?.GetComponent<Image>();
                var sealedLid = findGo("SealedLid")?.GetComponent<Image>();
                var teaLiquid = findGo("TeaLiquidLayer")?.GetComponent<Image>();
                var milkLiquid = findGo("MilkLayer")?.GetComponent<Image>();
                var iceVis = findGo("IceVisual")?.transform;
                var topVis = findGo("ToppingsVisual")?.transform;

                if (serve != null) SetSerializedProperty(cs, "serveButton", serve);
                if (trash != null) SetSerializedProperty(cs, "trashButton", trash);
                if (seal != null) SetSerializedProperty(cs, "sealerButton", seal);
                if (emptyCup != null) SetSerializedProperty(cs, "emptyCupImage", emptyCup);
                if (sealedLid != null) SetSerializedProperty(cs, "sealedLidImage", sealedLid);
                if (teaLiquid != null) SetSerializedProperty(cs, "teaLiquidImage", teaLiquid);
                if (milkLiquid != null) SetSerializedProperty(cs, "milkLiquidImage", milkLiquid);
                if (iceVis != null) SetSerializedProperty(cs, "iceVisualContainer", iceVis);
                if (topVis != null) SetSerializedProperty(cs, "toppingsContainer", topVis);
                EditorUtility.SetDirty(cs);
            }

            var sealerGo = findGo("SealerButton");
            if (sealerGo != null)
            {
                var sealer = GetOrAddComponent<CupSealer>(sealerGo);
                SetSerializedProperty(sealer, "sealerButton", sealerGo.GetComponent<Button>());
                EditorUtility.SetDirty(sealer);
            }

            // 5. Deskbell
            var bellGo = findGo("Deskbell") ?? findGo("Desk_Bell");
            if (bellGo != null)
            {
                var bell = GetOrAddComponent<DeskBell>(bellGo);
                SetSerializedProperty(bell, "bellButton", bellGo.GetComponent<Button>());
                EditorUtility.SetDirty(bell);
            }

            // 6. ShutterController
            var shutterGo = findGo("MetalShutter");
            var leverGo = findGo("ShutterLever");
            if (leverGo != null)
            {
                var sc = GetOrAddComponent<ShutterController>(leverGo);
                if (shutterGo != null) SetSerializedProperty(sc, "shutterTransform", shutterGo.GetComponent<RectTransform>());
                SetSerializedProperty(sc, "leverButton", leverGo.GetComponent<Button>());
                EditorUtility.SetDirty(sc);
            }

            // 7. Customer & Dialogue
            var custGo = findGo("Customer");
            if (custGo != null)
            {
                var cc = GetOrAddComponent<CustomerController>(custGo);
                var cImg = custGo.GetComponent<Image>();
                var sb = findGo("Speech Bubble")?.GetComponent<SpeechBubbleUI>() ?? findGo("SpeechBubble")?.GetComponent<SpeechBubbleUI>();
                var pBar = findGo("Fill")?.GetComponent<Image>() ?? findGo("PatienceBar")?.GetComponentInChildren<Image>();
                if (cImg != null) SetSerializedProperty(cc, "customerImage", cImg);
                if (sb != null) SetSerializedProperty(cc, "speechBubble", sb);
                if (pBar != null) SetSerializedProperty(cc, "patienceFillImage", pBar);
                EditorUtility.SetDirty(cc);
            }

            // 8. OrderTicketUI
            var ticketGo = findGo("OrderTicket");
            if (ticketGo != null)
            {
                var ot = GetOrAddComponent<OrderTicketUI>(ticketGo);
                SetSerializedProperty(ot, "ticketRoot", ticketGo);
                var tTitle = findGo("TicketTitle")?.GetComponent<TextMeshProUGUI>();
                var tDetails = findGo("TicketDetails")?.GetComponent<TextMeshProUGUI>();
                if (tTitle != null) SetSerializedProperty(ot, "titleText", tTitle);
                if (tDetails != null) SetSerializedProperty(ot, "detailsText", tDetails);
                EditorUtility.SetDirty(ot);
            }

            // 9. CashRegisterInventoryUI
            var crGo = findGo("CashRegisterButton");
            if (crGo != null)
            {
                var cr = GetOrAddComponent<CashRegisterInventoryUI>(crGo);
                SetSerializedProperty(cr, "inventoryButton", crGo.GetComponent<Button>());
                var invModal = findGo("InventoryModal");
                var closeBtn = findGo("CloseButton")?.GetComponent<Button>();
                var cardCont = findGo("InventoryCardContainer")?.transform;
                if (invModal != null) SetSerializedProperty(cr, "inventoryModalRoot", invModal);
                if (closeBtn != null) SetSerializedProperty(cr, "closeModalButton", closeBtn);
                if (cardCont != null) SetSerializedProperty(cr, "cardContainer", cardCont);
                EditorUtility.SetDirty(cr);
            }

            // 10. Foraging Screens
            var bmbScreen = findGo("BambooGroveScreen");
            if (bmbScreen != null)
            {
                var bgc = GetOrAddComponent<BambooGroveViewController>(bmbScreen);
                SetSerializedProperty(bgc, "bambooGrovePanelRoot", bmbScreen);
                var bg = findGo("BambooGroveBG")?.GetComponent<Image>();
                var ret = findGo("ReturnShopButton")?.GetComponent<Button>();
                var cnt = findGo("HarvestCounter")?.GetComponent<TextMeshProUGUI>();
                var sp = findGo("Signpost")?.GetComponent<Signpost>();
                if (bg != null) SetSerializedProperty(bgc, "backgroundImage", bg);
                if (ret != null) SetSerializedProperty(bgc, "returnToNightHubButton", ret);
                if (cnt != null) SetSerializedProperty(bgc, "harvestCounterText", cnt);
                if (sp != null) SetSerializedProperty(bgc, "signpost", sp);
                EditorUtility.SetDirty(bgc);
            }

            var hnyScreen = findGo("HoneyMeadowScreen");
            if (hnyScreen != null)
            {
                var hmc = GetOrAddComponent<HoneyMeadowViewController>(hnyScreen);
                SetSerializedProperty(hmc, "honeyMeadowPanelRoot", hnyScreen);
                var bg = findGo("HoneyMeadowBG")?.GetComponent<Image>();
                var ret = findGo("ReturnShopButton (1)")?.GetComponent<Button>();
                var cnt = findGo("HarvestCounter (1)")?.GetComponent<TextMeshProUGUI>();
                var tree = findGo("JellyTree")?.GetComponent<Button>();
                if (bg != null) SetSerializedProperty(hmc, "backgroundImage", bg);
                if (ret != null) SetSerializedProperty(hmc, "returnToNightHubButton", ret);
                if (cnt != null) SetSerializedProperty(hmc, "harvestCounterText", cnt);
                if (tree != null) SetSerializedProperty(hmc, "jellyTreeButton", tree);
                EditorUtility.SetDirty(hmc);
            }

            var mntScreen = findGo("MistMountainScreen");
            if (mntScreen != null)
            {
                var mmc = GetOrAddComponent<MistMountainViewController>(mntScreen);
                SetSerializedProperty(mmc, "mistMountainPanelRoot", mntScreen);
                var bg = findGo("MistMountainBG")?.GetComponent<Image>();
                var ret = findGo("ReturnShopButton (3)")?.GetComponent<Button>();
                var cnt = findGo("HarvestCounter (2)")?.GetComponent<TextMeshProUGUI>();
                var shelf = findGo("RockShelf")?.GetComponent<Button>();
                var wall = findGo("Rockwall");
                var bkt = findGo("Bucket")?.GetComponent<Image>();
                if (bg != null) SetSerializedProperty(mmc, "panoramaBackground", bg);
                if (ret != null) SetSerializedProperty(mmc, "returnToNightHubButton", ret);
                if (cnt != null) SetSerializedProperty(mmc, "harvestCounterText", cnt);
                if (shelf != null) SetSerializedProperty(mmc, "rockShelfButton", shelf);
                if (wall != null) SetSerializedProperty(mmc, "rockWallObject", wall);
                if (bkt != null) SetSerializedProperty(mmc, "bucketImage", bkt);
                EditorUtility.SetDirty(mmc);
            }

            var prepScreen = findGo("PrepAreaScreen");
            if (prepScreen != null)
            {
                var pvc = GetOrAddComponent<PrepAreaViewController>(prepScreen);
                SetSerializedProperty(pvc, "prepAreaPanelRoot", prepScreen);
                var ret = findGo("ReturnShopButton (2)")?.GetComponent<Button>();
                var bld = findGo("Blender")?.GetComponent<Button>();
                var chp = findGo("Chopping")?.GetComponent<Button>();
                var cen = findGo("Centrifuge")?.GetComponent<Button>();
                var sieve = findGo("Sieve")?.GetComponent<Image>();
                var knife = findGo("Knife")?.GetComponent<Image>();
                var topRaw = findGo("TopRawPanel")?.transform;
                if (ret != null) SetSerializedProperty(pvc, "returnToNightHubButton", ret);
                if (bld != null) SetSerializedProperty(pvc, "stationBlenderButton", bld);
                if (chp != null) SetSerializedProperty(pvc, "stationChoppingButton", chp);
                if (cen != null) SetSerializedProperty(pvc, "stationCentrifugeButton", cen);
                if (sieve != null) SetSerializedProperty(pvc, "stationSieveImage", sieve);
                if (knife != null) SetSerializedProperty(pvc, "stationKnifeImage", knife);
                if (topRaw != null) SetSerializedProperty(pvc, "topRawCardsContainer", topRaw);
                EditorUtility.SetDirty(pvc);
            }

            var mktScreen = findGo("SupermarketScreen");
            if (mktScreen != null)
            {
                var svc = GetOrAddComponent<SupermarketViewController>(mktScreen);
                SetSerializedProperty(svc, "supermarketPanelRoot", mktScreen);
                var bg = findGo("SupermarketBg")?.GetComponent<Image>();
                var ret = findGo("ReturnShopButton")?.GetComponent<Button>();
                var cash = findGo("CashBalanceText (1)")?.GetComponent<TextMeshProUGUI>();
                var cat = findGo("MarketCatalogContainer")?.transform;
                if (bg != null) SetSerializedProperty(svc, "supermarketBackgroundImage", bg);
                if (ret != null) SetSerializedProperty(svc, "returnToNightHubButton", ret);
                if (cash != null) SetSerializedProperty(svc, "cashBalanceText", cash);
                if (cat != null) SetSerializedProperty(svc, "marketCatalogContainer", cat);
                EditorUtility.SetDirty(svc);
            }

            // 11. Tea Dispensers, Milk Dispensers, Topping Stations
            System.Action<string, TeaBase> wireTea = (name, tb) =>
            {
                var go = findGo(name);
                if (go != null)
                {
                    var d = GetOrAddComponent<TeaDispenser>(go);
                    SetSerializedProperty(d, "teaType", tb);
                    SetSerializedProperty(d, "dispenseButton", go.GetComponent<Button>());
                    EditorUtility.SetDirty(d);
                }
            };
            wireTea("Tea_Black", TeaBase.BlackTea);
            wireTea("Tea_Green", TeaBase.GreenTea);
            wireTea("Tea_Oolong", TeaBase.OolongTea);
            wireTea("Tea_Thai", TeaBase.ThaiTea);
            wireTea("Tea_Taro", TeaBase.TaroTea);

            System.Action<string, MilkType> wireMilk = (name, mt) =>
            {
                var go = findGo(name);
                if (go != null)
                {
                    var d = GetOrAddComponent<MilkDispenser>(go);
                    SetSerializedProperty(d, "milkType", mt);
                    SetSerializedProperty(d, "dispenseButton", go.GetComponent<Button>());
                    EditorUtility.SetDirty(d);
                }
            };
            wireMilk("Milk_Fresh", MilkType.FreshMilk);
            wireMilk("Milk_Oat", MilkType.OatMilk);
            wireMilk("Milk_Coconut", MilkType.CoconutMilk);
            wireMilk("Milk_Condensed", MilkType.CondensedMilk);

            System.Action<string, ToppingType> wireTop = (name, tt) =>
            {
                var go = findGo(name);
                if (go != null)
                {
                    var d = GetOrAddComponent<ToppingStation>(go);
                    SetSerializedProperty(d, "toppingType", tt);
                    SetSerializedProperty(d, "scoopButton", go.GetComponent<Button>());
                    EditorUtility.SetDirty(d);
                }
            };
            wireTop("Topping_Boba", ToppingType.TapiocaPearls);
            wireTop("Topping_Popping", ToppingType.PoppingBoba);
            wireTop("Topping_GrassJelly", ToppingType.GrassJelly);
            wireTop("Topping_Pudding", ToppingType.EggPudding);
            wireTop("Topping_CoconutJelly", ToppingType.CoconutJelly);
            wireTop("Topping_CheeseFoam", ToppingType.CheeseFoam);
            wireTop("Topping_GoldenPearl", ToppingType.GoldenHoneyPearls);

            var iceGo = findGo("IceButton");
            if (iceGo != null)
            {
                var id = GetOrAddComponent<IceDispenser>(iceGo);
                SetSerializedProperty(id, "dispenseButton", iceGo.GetComponent<Button>());
                EditorUtility.SetDirty(id);
            }

            var sugGo = findGo("SugarButton");
            if (sugGo != null)
            {
                var sd = GetOrAddComponent<SugarDispenser>(sugGo);
                SetSerializedProperty(sd, "dispenseButton", sugGo.GetComponent<Button>());
                EditorUtility.SetDirty(sd);
            }

            // 12. Auto-Restore Images & Sprites
            System.Action<string, string, Color?> setImg = (goName, spritePath, col) =>
            {
                var go = findGo(goName);
                if (go != null)
                {
                    var img = go.GetComponent<Image>();
                    if (img != null)
                    {
                        if (!string.IsNullOrEmpty(spritePath))
                        {
                            var sp = loadSprite(spritePath);
                            if (sp != null) img.sprite = sp;
                        }
                        if (col.HasValue)
                        {
                            img.color = col.Value;
                        }
                        else if (img.sprite != null && (img.color == Color.clear || (img.color.a == 0 && goName != "TeaLiquidLayer" && goName != "MilkLayer")))
                        {
                            img.color = Color.white;
                        }
                        EditorUtility.SetDirty(img);
                    }
                }
            };

            setImg("StreetBackground", "Assets/Sprites/Sprites2/street_background.png", Color.white);
            if (findGo("StreetBackground")?.GetComponent<Image>()?.sprite == null)
                setImg("StreetBackground", "Assets/Sprites/Street_Background.png", Color.white);

            setImg("MetalShutter", "Assets/Sprites/Sprites2/shutters.png", Color.white);
            if (findGo("MetalShutter")?.GetComponent<Image>()?.sprite == null)
                setImg("MetalShutter", "Assets/Sprites/Shutter_Metal.png", Color.white);

            setImg("ShopfrontFrame", "Assets/Sprites/Sprites2/Shopfront.png", Color.white);
            if (findGo("ShopfrontFrame")?.GetComponent<Image>()?.sprite == null)
                setImg("ShopfrontFrame", "Assets/Sprites/Shopfront_Frame.png", Color.white);

            setImg("Deskbell", "Assets/Sprites/Sprites2/deskbell.png", Color.white);
            if (findGo("Deskbell")?.GetComponent<Image>()?.sprite == null)
                setImg("Deskbell", "Assets/Sprites/Desk_Bell.png", Color.white);

            setImg("Customer", "Assets/Sprites/Customer_Student.png", Color.white);
            setImg("Speech Bubble", "Assets/Sprites/SpeechBubble.png", Color.white);
            setImg("CupContainer", "Assets/Sprites/Cup_Empty.png", Color.white);
            setImg("CupOutline", "Assets/Sprites/Cup_Empty.png", Color.white);
            setImg("SealedLid", "Assets/Sprites/Cup_SealedLid.png", Color.white);
            setImg("TeaLiquidLayer", "Assets/Sprites/Cup_LiquidMask.png", new Color(0.85f, 0.5f, 0.2f, 0f));
            setImg("MilkLayer", "Assets/Sprites/Cup_LiquidMask.png", new Color(1f, 0.98f, 0.92f, 0f));
            setImg("IceVisual", "Assets/Sprites/Ice_Cubes.png", Color.white);
            setImg("Topping_Boba", "Assets/Sprites/Topping_Boba.png", Color.white);
            setImg("Fill", null, new Color(0.2f, 0.8f, 0.3f, 1f));
            setImg("OrderTicket", null, new Color(0.98f, 0.94f, 0.84f, 1f));
            setImg("BambooGroveBG", "Assets/Sprites/Sprites2/Sprites 3/Forage and prep/Bamboo/bamboogrove.jpg", Color.white);
            setImg("HoneyMeadowBG", "Assets/Sprites/Sprites2/Sprites 3/Forage and prep/Meadows/meadows.jpg", Color.white);
            setImg("MistMountainBG", "Assets/Sprites/Sprites2/Sprites 3/Forage and prep/mountains/mistmountain.jpg", Color.white);
            setImg("RockShelf", "Assets/Sprites/Sprites2/Sprites 3/Forage and prep/mountains/Rockshelf.png", Color.white);
            setImg("Rockwall", "Assets/Sprites/Sprites2/Sprites 3/Forage and prep/mountains/RockWall.jpg", Color.white);
            setImg("Bucket", "Assets/Sprites/Sprites2/Sprites 3/Forage and prep/Preparea/prepEquipment.png", Color.white);
            setImg("Signpost", "Assets/Sprites/Sprites2/Sprites 3/Forage and prep/Bamboo/isitworthsignboard.png", Color.white);
            setImg("JellyTree", "Assets/Sprites/Sprites2/Sprites 3/Forage and prep/Meadows/jellytree.png", Color.white);
            setImg("SupermarketBg", "Assets/Sprites/Sprites2/Sprites 3/market.png", Color.white);
            setImg("TopRawPanel", "Assets/Sprites/Sprites2/Sprites 3/Forage and prep/raw ingre.png", Color.white);
            setImg("Blender", "Assets/Sprites/Sprites2/Sprites 3/Forage and prep/Preparea/prepEquipment.png", Color.white);
            setImg("Chopping", "Assets/Sprites/Sprites2/Sprites 3/Forage and prep/Preparea/prepEquipment.png", Color.white);
            setImg("Centrifuge", "Assets/Sprites/Sprites2/Sprites 3/Forage and prep/Preparea/prepEquipment.png", Color.white);
            setImg("Sieve", "Assets/Sprites/Sprites2/Sprites 3/Forage and prep/Preparea/prepEquipment.png", Color.white);
            setImg("Knife", "Assets/Sprites/Sprites2/Sprites 3/Forage and prep/Preparea/prepEquipment.png", Color.white);

            // 13. Auto-Restore TextMeshPro Fonts and Opacities
            TMP_FontAsset defaultFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset")
                ?? TMP_Settings.defaultFontAsset;
            var allTexts = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();
            foreach (var txt in allTexts)
            {
                if (txt != null && txt.gameObject.scene.isLoaded)
                {
                    if (defaultFont != null && (txt.font == null || txt.font.name == ""))
                    {
                        txt.font = defaultFont;
                    }
                    if (txt.color.a <= 0.05f)
                    {
                        txt.color = Color.white;
                    }
                    // Speech bubble dialogue text is dark
                    if (txt.transform.IsChildOf(findGo("Speech Bubble")?.transform ?? findGo("Customer")?.transform))
                    {
                        txt.color = new Color(0.15f, 0.15f, 0.15f, 1f);
                    }
                    // Ticket text is dark charcoal
                    if (txt.transform.IsChildOf(findGo("OrderTicket")?.transform ?? hudGo?.transform))
                    {
                        if (txt.name == "TicketTitle" || txt.name == "TicketDetails")
                            txt.color = new Color(0.18f, 0.12f, 0.08f, 1f);
                    }
                    EditorUtility.SetDirty(txt);
                }
            }

            // Save Scene
            UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            Debug.Log("[ShopSceneSetup] Auto-wired all recovered scene components and restored sprites, fonts, and colors successfully!");
        }
    }
}
