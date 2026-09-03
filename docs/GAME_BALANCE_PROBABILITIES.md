# Yippee Tea - Game Balance & Probability Tables

This document details the exact mathematical probability distributions governing customer order generation, topping counts, topping type weights, and milk choices across all 4 weeks of gameplay (Days 1 to 28), including the impact of the **Artisanal Tea Menu** upgrade.

---

## 1. Probability of Number of Toppings Ordered

| Progression Stage | Menu Tier | 0 Toppings | 1 Topping | 2 Toppings | 3 Toppings (2 Bottom + Foam) | Max Allowed |
| :--- | :--- | :---: | :---: | :---: | :---: | :---: |
| **Day 1** (Tutorial) | Standard | **60.0%** | **40.0%** | 0.0% | 0.0% | 1 |
| **Day 2** (Intro Sliders) | Standard | **60.0%** | **40.0%** | 0.0% | 0.0% | 1 |
| **Week 1 (Days 3–7)** | Standard | **35.0%** | **65.0%** | 0.0% | 0.0% | 1 |
| | **+ Artisanal Menu** | **15.0%** | **85.0%** | 0.0% | 0.0% | 1 |
| **Week 2 (Days 8–14)** | Standard | **35.0%** | **32.5%** | **32.5%** | 0.0% | 2 |
| | **+ Artisanal Menu** | **15.0%** | **25.5%** | **59.5%** | 0.0% | 2 |
| **Week 3 (Days 15–21)** | Standard | **30.0%** | **35.0%** | **35.0%** | 0.0% | 2 |
| | **+ Artisanal Menu** | **15.0%** | **21.0%** | **64.0%** | 0.0% | 2 |
| **Week 4 (Days 22–28)** | Standard | **20.0%** | **16.0%** | **40.0%** | **24.0%** | **3** |
| | **+ Artisanal Menu** | **10.0%** | **8.1%** | **37.8%** | **44.1%** | **3** |

*Note on Week 4 Calculation:*
* When toppings are ordered (80% base, 90% with Artisanal):
  * Customers roll 1 or 2 bottom toppings (60% roll 2, 40% roll 1 without Artisanal; 70% roll 2, 30% roll 1 with Artisanal).
  * In addition, there is a dedicated roll for Cheese Foam on top (50% base, 70% with Artisanal).
  * 3 toppings = 2 bottom toppings + Cheese Foam.

---

## 2. Probability of Individual Topping Types (When Toppings Are Ordered)

*Note: With the Artisanal Menu, individual roll probabilities are strictly weighted proportional to ingredient unit price.*

| Topping Type | Unit Cost | Week 1 (Days 3–7) | Week 2 (Days 8–14) | Week 3 (Days 15–21) | Week 4 (Days 22–28) |
| :--- | :---: | :---: | :---: | :---: | :---: |
| **Tapioca Pearls** | $0.35 | **33.3%** *(Artisanal: 16.7%)* | **20.0%** *(Artisanal: 6.7%)* | **14.3%** *(Artisanal: 3.6%)* | **16.7%** *(Artisanal: 4.8%)* |
| **Popping Boba** | $0.55 | **33.3%** *(Artisanal: 33.3%)* | **20.0%** *(Artisanal: 13.3%)* | **14.3%** *(Artisanal: 7.1%)* | **16.7%** *(Artisanal: 9.5%)* |
| **Grass Jelly** | $0.70 | **33.3%** *(Artisanal: 50.0%)* | **20.0%** *(Artisanal: 20.0%)* | **14.3%** *(Artisanal: 10.7%)* | **16.7%** *(Artisanal: 14.3%)* |
| **Coconut Jelly** | $0.90 | — | **20.0%** *(Artisanal: 26.7%)* | **14.3%** *(Artisanal: 14.3%)* | **16.7%** *(Artisanal: 19.0%)* |
| **Egg Pudding** | $1.15 | — | **20.0%** *(Artisanal: 33.3%)* | **14.3%** *(Artisanal: 17.9%)* | **16.7%** *(Artisanal: 23.8%)* |
| **Golden Honey Pearls** | $1.85 | — | — | **14.3%** *(Artisanal: 25.0%)* | **16.7%** *(Artisanal: 28.6%)* |
| **Cheese Foam** | $1.45 | — | — | **14.3%** *(Artisanal: 21.4%)* | **50.0%** *(Artisanal: 70.0%)* |

---

## 3. Milk Type Distribution Across Progression Stages

| Milk Type | Serving Cost | Days 1–2 | Week 1 (Days 3–7) | Week 2 (Days 8–14) | Weeks 3–4 (Days 15–28) |
| :--- | :---: | :---: | :---: | :---: | :---: |
| **None (Clear Tea)** | $0.00 | **40.0%** | **35.0%** *(Artisanal: 15.0%)* | **35.0%** *(Artisanal: 15.0%)* | **35.0%** *(Artisanal: 15.0%)* |
| **Fresh Milk** | $0.50 | **60.0%** | **32.5%** *(Artisanal: 28.3%)* | **21.7%** *(Artisanal: 14.2%)* | **16.3%** *(Artisanal: 10.6%)* |
| **Oat Milk** | $0.75 | — | **32.5%** *(Artisanal: 56.7%)* | **21.7%** *(Artisanal: 28.3%)* | **16.3%** *(Artisanal: 21.3%)* |
| **Coconut Milk** | $0.85 | — | — | **21.7%** *(Artisanal: 42.5%)* | **16.3%** *(Artisanal: 31.9%)* |
| **Condensed Milk** | $0.75 | — | — | — | **16.3%** *(Artisanal: 21.3%)* |

---

## 4. Customer Archetypes & Patience Limits

| Archetype | Patience Duration | Base Personality / Phrasing |
| :--- | :---: | :--- |
| **ADHD** | **30s** | Frantic, rapid-fire phrasing (*"Quick, quick! Can I get a..."*) |
| **Tourettes** | **35s** | High energy, capitalized exclamation tics (*"GIVE ME A..."*) |
| **Autism** | **45s** | Structured, polite, exact phrasing (*"Exactly 50% sweetness..."*) |
| **Dyslexia** | **50s** | Relieved phrasing (*"Hi! I finally read the menu!..."*) |
| **Anxiety** | **55s** | Soft-spoken, shy phrasing (*"U-um... hello! Could I please have..."*) |
| **Dyscalculia** | **60s** | Enthusiastic coin-counting dialogue (*"I counted my coins!..."*) |

*(Note: Purchasing the **Improve Store Ambience** upgrade for $30 removes patience timers completely, allowing all archetypes to wait indefinitely).*

---

## 5. Market Event Trigger Probability & Progressive Event Pool

Market events last for **3 full days** and alter ingredient wholesale prices, customer order preferences, and foraging yields. Events only trigger from pools containing ingredients and mechanics currently unlocked by the player.

### Event Occurrence Rates
* **Days 1–3**: **0.0%** (Tutorial and basic shop operations).
* **Day 4**: **100.0%** (Guaranteed first random event to introduce market dynamics; 3-day duration extends into Days 5 & 6 when Foraging unlocks on Day 5).
* **Day 5+**: **75.0%** daily chance to roll a new 3-day event whenever no event is currently active (with a minimum 1-day cooldown after an event concludes).

### Event Selection Probabilities (When an Event Triggers)

| Market Event | Affected Mechanics & Ingredients | Week 1 (Days 4–7) | Week 2 (Days 8–14) | Weeks 3–4 & Endless (Days 15+) |
| :--- | :--- | :---: | :---: | :---: |
| **Tapioca Pearl Shortage** | Tapioca Pearls wholesale +40%, Pearl customer demand +50% | **16.67%** (1/6) | **12.50%** (1/8) | **10.00%** (1/10) |
| **Local Dairy Surplus** | Fresh Milk & Oat Milk wholesale -30%, Milk customer demand **-35%** (Surplus Saturation) | **16.67%** (1/6) | **12.50%** (1/8) | **10.00%** (1/10) |
| **Summer Heatwave** | 100% Full Ice demand +70%, Fruity Popping Boba demand; **Weather Secret:** Must give 100% Ice for full tips | **16.67%** (1/6) | **12.50%** (1/8) | **10.00%** (1/10) |
| **Herbal Wellness Trend** | Grass Jelly wholesale -15%, Grass Jelly demand +50%, Low/Zero sugar preference | **16.67%** (1/6) | **12.50%** (1/8) | **10.00%** (1/10) |
| **Bountiful Foraging Season** | Foraging expeditions yield 2.0x double harvests across all unlocked zones | **16.67%** (1/6) | **12.50%** (1/8) | **10.00%** (1/10) |
| **Wholesale Stock Clearance** | **ALL** Wholesale Market goods, milks & toppings are **70% cheaper (-70% flash sale)** | **16.67%** (1/6) | **12.50%** (1/8) | **10.00%** (1/10) |
| **Tropical Coconut Harvest** | Coconut Milk & Coconut Jelly wholesale -35%, Coconut customer demand **-40%** (Harvest Saturation) | — | **12.50%** (1/8) | **10.00%** (1/10) |
| **Plant-Based Milk Craze** | Barista Oat Milk & Organic Coconut Milk demand +60% | — | **12.50%** (1/8) | **10.00%** (1/10) |
| **Chilly Monsoon Rain** | 0% Ice preference, Condensed Milk demand +40%; **Weather Secret:** Must give 0% Ice (No Ice) for full tips | — | — | **10.00%** (1/10) |
| **Gourmet Cream Shortage** | Egg Pudding & Cheese Foam wholesale +30%, Customer tips on cream drinks **+25%** | — | — | **10.00%** (1/10) |
| **Total Active Pool Size** | | **6 Events** | **8 Events** | **10 Events** |

---

### Weather Secret Ice Mechanics
1. **Summer Heatwave (Scorching Weather)**:
   - Regardless of what ice level the customer specified on their order ticket (e.g. 0% or 50%), customers secretly crave **100% Full Ice** to cool down.
   - If the player prepares **100% Full Ice**:
     - Ice is evaluated as 100% correct (0 mistakes), unlocking 5-star rating and full tip bonus.
     - Customer delivers archetype-specific praise (*"Ah, I really needed a cool drink!"*).
   - If the player provides **less than 100% Ice** (e.g. following 0% or 50% on the ticket):
     - Incurs a 1-mistake penalty, reducing max star rating and tip.
     - Customer delivers archetype-specific penalty dialogue (*"Actually, it's so hot now... I would have preferred more ice!"*).

2. **Chilly Monsoon Rain (Freezing Cold Front)**:
   - Regardless of what ice level the customer specified on their order ticket (e.g. 50% or 100%), customers secretly crave **0% No Ice** to stay warm.
   - If the player prepares **0% No Ice**:
     - Ice is evaluated as 100% correct (0 mistakes), unlocking 5-star rating and full tip bonus.
     - Customer delivers archetype-specific warm praise (*"Ah, no ice is so comforting in this chilly weather!"*).
   - If the player provides **more than 0% Ice** (e.g. following 50% or 100% on the ticket):
     - Incurs a 1-mistake penalty, reducing max star rating and tip.
     - Customer delivers archetype-specific penalty dialogue (*"Actually, I'm pretty cold... I should have ordered less ice!"*).


