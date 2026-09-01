# Yippee Tea - Changelog

All notable changes to the Yippee Tea project are documented in this file.

---

## [v1.4.0] - 2026-09-02

### Centralized Sprite Management & Architecture
* **Centralized `SpriteManager`**: Established a single source of truth for all project sprites (Milks, Sliced Toppings, Dispenser Objects including exact Ice Cube sprite `Store Objects_2`, Foraging Critters, and Raw Ingredients).
* **One-Click Asset Discovery**: Implemented `SpriteManagerEditor` allowing instant auto-population and asset resolution directly in the Unity Inspector.
* **Inspector & Controller Clean-Up**: Removed dozens of duplicate, redundant serialized sprite variables across `SupermarketViewController`, `CupStation`, `PrepAreaViewController`, `BambooGroveViewController`, `HoneyMeadowViewController`, and `MistMountainViewController`.

### Market Conditions Badge & Event System
* **Multi-Icon Badge Support**: Market event badges dynamically render single or dual icons (e.g., displaying both Milk and Ice Cube icons for Summer Heatwave).
* **Dynamic Day Counter Docking**: Implemented dynamic positional docking where the Day counter automatically shifts left (`-85px` for dual icons, `-65px` for single icons) and the badge aligns seamlessly alongside it based on text width.
* **Shutter-Synchronized Visibility**: Market event badges remain concealed behind closed shutters and cleanly appear once the storefront opens and morning briefing initiates.
* **Live Inspector Testing**: Added `MarketEventManagerEditor` with an event dropdown and preview buttons to trigger or clear market events on demand during testing.
* **2× Scaled Modal Typography**: Doubled font sizes (28pt–44pt) and enlarged the information card (900×600) for crystal-clear readability of event lore and financial impacts.

### Cup Visuals & Multi-Topping Stacking
* **Dynamic Multi-Topping Layering**: Cups instantiate distinct visual layers that stack with calibrated vertical offsets when multiple bottom toppings (Tapioca, Popping Boba, Grass Jelly, Coconut Jelly, Egg Pudding, Golden Honey Pearls) are selected.
* **Proportional Aspect Ratios**: Enforced aspect ratio preservation so pearls and toppings maintain crisp, circular shapes without vertical compression.
* **Calibrated Cheese Foam Layer**: Fine-tuned Cheese Foam positioning at the cup rim with custom scale, width, and froth opacity.

### Foraging & Minigame Calibration
* **Honey Meadow Soil Absorption Timing**: Increased fallen jelly block floor duration from 2.0s to 4.0s for a more relaxed and enjoyable harvesting rhythm.
* **Mist Mountain Bucket Dragging**: Refined pointer drag event handling and boundary clamping for smooth Golden Dew catching.
* **Self-Healing Subview Lifecycles**: Standardized activation and fallback hierarchy resolution across all night phase subviews.

### Inventory & UI Quality of Life
* **Owned-Items Inventory Filter**: Cash Register Inventory UI and Nightly Ledger now exclusively display items the player has previously unlocked or purchased.
* **Z-Sorting & Transparency**: Fixed render order so HUD hints remain interactable and legible over translucent inventory overlays.

---

## [v1.3.0] - 2026-09-01

### Cup Visuals and Topping Layering
* Dynamic Multi-Topping Stacking: Cups dynamically instantiate and stack separate visual layers when two or more toppings (e.g., Tapioca Pearls, Popping Boba, Grass Jelly, Coconut Jelly) are added to a single drink.
* Calibrated Cheese Foam Layer: Added support for Cheese Foam sitting across the top rim of the cup with fine-tuned width, positioning, and thickness.
* Aspect Ratio Preservation: Adjusted rendering so toppings and boba pearls retain circular proportions without vertical squishing.
* Week 4 Triple-Topping Orders: Customer orders in Week 4 can now request up to two bottom toppings plus a Cheese Foam cap (3 toppings total).

### Market Events and HUD Indicators
* Market Event HUD Indicator: Added an interactive event badge in the HUD right next to the Day counter displaying the event item icon, trend indicator (e.g., red shortage triangle, green discount indicator, harvest star), and remaining event duration.
* Market Event Details Modal: Clicking the HUD event badge opens an in-depth modal dialog displaying event lore, remaining days, wholesale cost impacts, and customer demand multipliers.

### Customer Dismissal Bell Safety
* Accidental Dismissal Confirmation: Ringing the counter bell while an unserved customer is waiting at the window now prompts for confirmation on the first ring ("Are you sure you want to dismiss this customer? Ring again to dismiss them.") and only skips the customer upon the second ring.
* Auto-Resetting Confirmation: Confirmation state automatically resets when a drink is served, trashed, or when a new cup is placed.
* Toggleable Setting: Added toggleable setting flag ConfirmDismissIfCustomerWaiting in CustomerManager for integration with the settings menu.

### Upgrades and Equipment
* Commercial Auto-Sealer Upgrade: Added a permanent shop upgrade in the Upgrades tab for $20.00 that automatically seals drinks when pressing the Serve Drink button, while still allowing manual sealing.
* Upgrades Store Price Rebalance: Rebalanced all shop upgrades and sorted the catalog in ascending price order.
* Artisanal Menu Rebalance: Updated the Artisanal Menu to skew customer orders toward higher-priced individual toppings and milks.

### Economy and Progression Calibration
* Lowered Buyout Target: Rebalanced the final shop buyout goal from $5,000 to $1,500 across the economy ledger, shop buyout button, and Mentor dialogue for a balanced 4-week story progression.
* Removed Legacy Assets: Completely removed non-existent Wild Mountain Tea references from enums, market pricing, inventory stock, and drink evaluation.

---

## [v1.2.0] - 2026-08-28

### Foraging Expeditions and Kitchen Prep
* Foraging Locations: Added playable foraging expeditions across Bamboo Grove, Honey Meadows, and Misty Mountains.
* Kitchen Prep Area: Added kitchen equipment stations including Blender and Sieve (Popping Boba), Chopping Board (Grass and Coconut Jellies), and High-Speed Centrifuge (Cheese Foam and Golden Honey Pearls).
* Expedition Visual Highlights: Added golden pulsing highlight on the Kitchen Prep button upon returning from expeditions.

### Upgrades and Quality of Life
* Shop Upgrades System: Added permanent shop upgrades including storefront beautification, advertisements, supply contracts, and lucky cat charm.
* Mentor Dialogue Skip: Added interactive skip buttons to accelerate through Mentor briefing dialogues.
* Economy Rebalance: Rebalanced ingredient purchase costs and drink sales payout markups across the menu.

---

## [v1.1.0] - 2026-08-20

### Wholesale Market and Inventory
* Wholesale Night Market: Introduced the Wholesale Market tab during the night phase to buy bulk cups, milks, and ingredients.
* Inventory Management System: Implemented full stock tracking with interactive Cash Register UI inspection on the front counter.
* Dynamic Market Events: Implemented daily market conditions and supply events affecting customer demand and pricing.

### Mentor and Order Tickets
* Mentor System: Added Mentor morning briefings and tutorial milestones.
* Order Ticket UI: Added physical clipped order tickets for customer preferences.

---

## [v1.0.0] - 2026-08-10

### Initial Release
* Core Tea Brewing: Interactive tea base dispensers, sweetness levels, ice sliders, milk layering, topping additions, and cup sealing lid station.
* Customer Archetypes: Neurodivergent customer personalities (ADHD, Autism, Anxiety, Tourettes, Dyscalculia, Dyslexia) with unique order behaviors, quirks, and patience mechanics.
* Daily Loop: Day service shift, rating evaluations, tips, weekly rent cycle, and shop closing ledger.

---
