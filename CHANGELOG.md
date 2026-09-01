# 📜 Yippee Tea - Changelog

All notable changes to the *Yippee Tea* project starting from version 1.2 onwards will be documented in this file.

---

## [v1.2.0] - 2026-09-01

### 🧋 Cup Visuals & Topping Layering
* **Dynamic Multi-Topping Stacking**: Cups now dynamically instantiate and stack separate visual layers when 2 or more toppings (e.g. Tapioca Pearls, Popping Boba, Grass Jelly, Coconut Jelly) are added to a single drink.
* **Calibrated Cheese Foam Layer**: Added support for Cheese Foam sitting across the top rim of the cup with fine-tuned width, positioning, and thickness.
* **Aspect Ratio Preservation**: Fixed vertical compression / squishing so toppings and boba pearls retain circular proportions.

### 🛎️ Quality of Life & Safety
* **Accidental Dismissal Confirmation**: Ringing the counter bell while an unserved customer is waiting at the window now prompts for confirmation on the 1st ring (*"Are you sure you want to dismiss this customer? Ring again to dismiss them."*) and only skips the customer upon the 2nd ring.
* **Auto-Resetting Confirmation**: Confirmation state automatically resets when a drink is served, trashed, or when a new cup is placed.
* **Toggleable Setting**: Exposed `ConfirmDismissIfCustomerWaiting` in `CustomerManager` for integration with the upcoming Settings Menu.

### 💰 Economy & Progression Calibration
* **4-Week Buyout Target**: Rebalanced the final shop buyout goal from $5,000 to **$1,500** across the economy ledger, shop buyout button, and Day 1 Mentor dialogue for a smooth 4-week story arc.
* **Removed Legacy Tea**: Completely purged non-existent `WildMountainTea` references from enums, market pricing, inventory stock, and drink evaluation.

### 🏔️ Misty Mountains Foraging & Kitchen Centrifuge
* **2-Stage Foraging Minigame**: Implemented panoramic shelf approach, screen-shaking rock wall impact, and interactive bucket catching minigame.
* **Kitchen Prep Centrifuge**: Added High-Speed Centrifuge station to refine Raw Golden Dew into Cheese Foam and Golden Honey Pearls.
* **Dynamic Hub Button Glow**: Added golden-amber pulsing highlight on the Kitchen Prep button upon returning from foraging expeditions.

---
