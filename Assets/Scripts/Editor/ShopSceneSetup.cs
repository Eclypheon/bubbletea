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

            var hudCtrl = hudObj.AddComponent<HUDController>();
            SetSerializedProperty(hudCtrl, "dayText", hudDay);
            SetSerializedProperty(hudCtrl, "cashText", hudCash);
            SetSerializedProperty(hudCtrl, "rentTimerText", hudRent);
            SetSerializedProperty(hudCtrl, "customerCountText", hudCust);
            SetSerializedProperty(hudCtrl, "statusHintText", hudHint);

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
    }
}
