# Yippee Tea - Changelog

All notable changes to the Yippee Tea project are documented in this file.

---

## [v1.3.0] - 2026-09-01

### Cup Visuals and Topping Layering
* Dynamic Multi-Topping Stacking: Cups dynamically instantiate and stack separate visual layers when two or more toppings (e.g., Tapioca Pearls, Popping Boba, Grass Jelly, Coconut Jelly) are added to a single drink.
* Calibrated Cheese Foam Layer: Added support for Cheese Foam sitting across the top rim of the cup with fine-tuned width, positioning, and thickness.
* Aspect Ratio Preservation: Adjusted rendering so toppings and boba pearls retain circular proportions without vertical squishing.

### Customer Dismissal Bell Safety
* Accidental Dismissal Confirmation: Ringing the counter bell while an unserved customer is waiting at the window now prompts for confirmation on the first ring ("Are you sure you want to dismiss this customer? Ring again to dismiss them.") and only skips the customer upon the second ring.
* Auto-Resetting Confirmation: Confirmation state automatically resets when a drink is served, trashed, or when a new cup is placed.
* Toggleable Setting: Added toggleable setting flag ConfirmDismissIfCustomerWaiting in CustomerManager for integration with the settings menu.

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
* Wholesale Night Market: Introduced the Wholesale Market tab during the night phase to buy bulk cups, ingredients, milks, and toppings.
* Inventory Management System: Implemented full stock tracking with interactive Cash Register UI inspection on the front counter.
* Dynamic Market Events: Implemented daily market conditions and supply events affecting customer demand and pricing.

### Mentor and Order Tickets
* Mentor System: Added Mentor morning briefings and dialogue tutorials introducing core mechanics across early days.
* Order Ticket UI: Added dynamic physical order tickets clipped to the counter displaying customer preferences, sensory notes, and drink specs.

---

## [v1.0.0] - 2026-08-10

### Initial Release
* Core Tea Brewing: Interactive tea base dispensers, sweetness levels, ice sliders, milk layering, topping additions, and cup sealing lid station.
* Customer Archetypes: Neurodivergent customer personalities (ADHD, Autism, Anxiety, Tourettes, Dyscalculia, Dyslexia) with unique order behaviors, quirks, and patience mechanics.
* Daily Loop: Day service shift, rating evaluations, tips, weekly rent cycle, and shop closing ledger.

---
