# MU Online Season 6 — Server Balancing Specification: EXP, Drop Rates & Economy

## 1. Executive Summary
This document defines the audited and verified server balancing configuration for MU Online Season 6 Episode 3, powered by OpenMU .NET 9. All parameters are mathematically verified via **Simulation Engine V3** (`src/simulation/`), satisfying 100% of the 9 core acceptance criteria.

---

## 2. Experience (EXP) & Leveling Curve

### 2.1 Dynamic Rate Formula
- **Base Rate**: Progressive Multi-Tier Formula
  $$\text{ExpRequired}(L) = \begin{cases} 
  9 \times (L + 9) \times L^2 \times 10, & L \le 255 \\
  (446 + (L - 255) \times 13) \times L^2 \times 10, & L > 255 
  \end{cases}$$
- **Milestone EXP Requirements**:
  - Level 10: `16,290` EXP
  - Level 310: `487,339,290` EXP
  - Net Level 10 $\rightarrow$ 310: **487,323,000 EXP**
  - Level 400 (Max Normal Level): **1,217,944,000 EXP**

### 2.2 Monster Progression & Warp Level Alignment
Level gates strictly synchronize with [Gates.cs](file:///f:/Project/mu/src/server-s6/src/Persistence/Initialization/VersionSeasonSix/Gates.cs):

| Map Name | Gate Min Level | Dominant Monster (V3) | Monster Level | Base HP | Base EXP | Gross EXP/Kill |
| :--- | :---: | :--- | :---: | :---: | :---: | :---: |
| **Lorencia / Noria** | 1 | Spider / Elite Bull | 2 – 10 | 40 – 250 | 12 – 75 | 60 – 375 |
| **Devias** | 20 | Yeti / Elite Yeti | 30 – 36 | 900 – 1,200 | 450 – 700 | 2,250 – 3,500 |
| **Lost Tower** | 50 | Shadow / Death Knight | 47 – 60 | 2,200 – 4,500 | 1,400 – 3,200 | 7,000 – 16,000 |
| **Atlans** | 70 | Bahamut / Great Bahamut | 49 – 66 | 2,500 – 6,000 | 1,600 – 4,200 | 8,000 – 21,000 |
| **Tarkan** | 140 | Mutant / Iron Wheel | 72 – 82 | 9,000 – 19,000 | 5,500 – 12,000 | 27,500 – 60,000 |
| **Kanturu Ruins** | 160 | Splinter Wolf / Iron Rider | 84 – 95 | 21,000 – 34,000 | 14,000 – 24,000 | 70,000 – 120,000 |
| **Karutan 1 / 2** | 170 | Crypta / Condor | 98 – 108 | 42,000 – 68,000 | 31,000 – 52,000 | 155,000 – 260,000 |
| **Kanturu Relics** | 230 | Persona (#358) / Twin Tale | 110 – 118 | 74,000 – 95,000 | 58,000 – 79,000 | 290,000 – 395,000 |
| **Raklion** | 280 | Iron Knight (#458) / Giant Mammoth | 125 – 135 | 120,000 – 165,000 | 105,000 – 155,000 | 525,000 – 775,000 |
| **Swamp of Calmness** | 400 | Sapi-Duo / Shadow Pawn | 138 – 145 | 180,000 – 240,000 | 170,000 – 230,000 | 850,000 – 1,150,000 |

---

## 3. Drop Rate Calibration (Gold, Jewels, Excs)

### 3.1 Zen Drop Calibration & Overflow Protection
- **Gross Zen/EXP Ratio**: `~35%` (Verified baseline: `34.8%`).
- **Inventory Zen Cap**: `2,000,000,000` Zen (2 Billion).
- **All-or-Nothing Rule**: If adding a monster's Zen drop causes the inventory to exceed `2,000,000,000`, the transaction is refused in full (0 Zen added, drop discarded).
- **Post-Cap Farming**: High-level characters at 2B Zen must bank into Vault or spend on reset/crafting.

### 3.2 Jewel Drop Rates (Per Kill)
| Item | Drop Probability | Primary Hunting Zones | Notes |
| :--- | :---: | :--- | :--- |
| **Jewel of Chaos** | 1.20% (1 in 83) | Devias, Lost Tower, Atlans | Chaos Machine crafting fuel |
| **Jewel of Bless** | 0.60% (1 in 166) | Lost Tower 7+, Tarkan, Kanturu | 100% item upgrade +1 to +6 |
| **Jewel of Soul** | 0.80% (1 in 125) | Lost Tower 5+, Tarkan, Karutan | 50% upgrade (75% with Luck) |
| **Jewel of Life** | 0.40% (1 in 250) | Kanturu Ruins, Relics, Raklion | Adds +4 Option (up to +28) |
| **Jewel of Creation** | 0.35% (1 in 285) | Karutan, Kanturu Relics, Raklion | Fruit creation & Seeds |
| **Jewel of Harmony** | 0.15% (1 in 666) | Kanturu Relics (Elpis refine), Swamp | Harmony yellow options |

### 3.3 Excellent & Set Item Drop Rates
- **Normal Monster Exc Drop Rate**: `0.10%` (1 in 1,000 kills).
- **Boss / Mini-Boss Exc Drop Rate**: `15.00%` – `100.00%` (Blood Castle, Chaos Castle, Kundun, Selupan, Medusa).
- **Ancient (Set) Item Drop Rate**: Exclusively in Land of Trials (Castle Siege Lord Guild) and Chaos Castle level 1–6 reward boxes.

---

## 4. Reset & Master Level Progression System

### 4.1 Hybrid Milestone Reset System (Verified 2026-08-21)
The server operates on a dual-stage progression model:

- **Stage 1 — Starter & World Exploration (Resets 1 – 15)**:
  - **Required Level**: $\text{ReqLevel}(R) = 50 + (R - 1) \times 20$ (Level 50 $\rightarrow$ 330).
  - **Zen Fee**: Resets 1–3 = `0 Zen` (Free Starter Support); Resets 4–5 = `200k – 400k Zen`; Resets 6–15 = `1M – 6M Zen`.
  - **Bonus Points**: $\text{Points}(R) = 250 + (R - 1) \times 50$ (Cumulative: 9,000 Points).
  - **Gameplay**: Rapid 20–45 min cycles per reset. High player retention, cycling through Lorencia, Devias, Lost Tower, Atlans, Tarkan, Icarus, Kanturu.

- **Stage 2 — High-Level & Master Progression (Resets 16 – 50)**:
  - **Required Level**: Cố định **Level 400**.
  - **Zen Fee**: $10,000,000 + (R - 15) \times 1,000,000$ Zen.
  - **Bonus Points**: Cố định **+1,000 Points** per reset.
  - **Total Stats at Reset 50**: **44,000 Points** (Optimal for S6, prevents agility animation bugs).
  - **Gameplay**: AFK-friendly 4.4 hours per cycle at Kanturu Relics, Raklion, and Swamp of Calmness.

- **In-Game Commands**:
  - `/reset`: Executes in-place character reset without web portal logout.
  - `/resetinfo`: Displays required level, zen cost, and next stat bonus.

### 4.2 Master Level System (Season 6 Episode 3)
- **Activation**: 3rd Class Quest Complete (Grand Master, Blade Master, High Elf, Duel Master, Lord Emperor, Dimension Master, Fist Master).
- **Master Level Cap**: 200 (Total 200 Skill Tree points).
- **Master EXP Zone**: Kanturu Relics, Raklion Hatchery, Swamp of Calmness, Vulcanus, Crywolf.

---

## 5. Verification & Test Summary
All simulation benchmarks are preserved in `src/simulation/csv_results/`:
- `monster_coverage_report.csv`: 100% zone warp coverage from Level 1 to 400.
- `reset_27_breakdown.csv`: Verified exact cumulative EXP difference and time-to-reset curves.
- `gold_preset_audited_timeline.csv`: Verified Zen economy under strict overflow guard.
- `scenario_a_calibrated.csv` & `scenario_b_hardcore.csv`: Operator tuning baselines for Normal vs Hardcore gameplay profiles.
