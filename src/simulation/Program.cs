using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace OpenMU.Simulation
{
    public record MonsterProfile(int MonsterNumber, string MonsterName, int MonsterLevel, int HP, int MapNumber, string MapName, int BaseKillsPerHour);
    public record MapGateAccess(int MapNumber, string MapName, int RequiredLevel);

    public class Program
    {
        private static readonly long[] ExpTable = new long[402];

        // Map access gating based on EnterGate / Warp requirements from Gates.cs
        private static readonly List<MapGateAccess> AccessibleMaps = new()
        {
            new(0, "Lorencia", 1),
            new(3, "Noria", 1),
            new(1, "Dungeon", 10),
            new(2, "Devias", 15),
            new(4, "Lost Tower", 40),
            new(7, "Atlans", 60),
            new(8, "Tarkan", 130),
            new(37, "Kanturu Ruins", 150),
            new(10, "Icarus", 160),
            new(80, "Karutan 1", 160),
            new(81, "Karutan 2", 160),
            new(38, "Kanturu Relics", 220),
            new(57, "Raklion", 240),
            new(56, "Swamp of Calmness", 250)
        };

        // Calibrated monster progression database linked to monster HP from PostgreSQL / OpenMU maps
        // NOTE: BaseKillsPerHour is maintained temporarily pending empirical PlayerDps(level) curve measurements.
        private static readonly List<MonsterProfile> MonsterDatabase = new()
        {
            new(4, "Elite Bull Fighter", 12, 190, 0, "Lorencia", 900),
            new(20, "Elite Yeti", 36, 1200, 2, "Devias", 850),
            new(36, "Cursed Wizard", 56, 4500, 4, "Lost Tower", 800),
            new(48, "Lizard King", 70, 8500, 7, "Atlans", 750),
            new(58, "Tantallos", 83, 21000, 8, "Tarkan", 700),
            new(351, "Splinter Wolf", 85, 25000, 37, "Kanturu Ruins", 650),
            new(358, "Persona", 118, 68000, 38, "Kanturu Relics", 580),
            new(575, "Condra", 117, 90000, 81, "Karutan 2", 500),
            new(457, "Coolutin", 132, 88000, 57, "Raklion", 420),
            new(458, "Iron Knight", 142, 95000, 57, "Raklion", 380)
        };

        // Simulation House Rules (Operational Parameters)
        public const double PickupEfficiency = 0.70;            // House Rule: 70% Zen & items collected
        public const long RoutineCostPerHour = 850_000;          // House Rule: 850k Zen/h potion & repair sinks
        public const double BaseMoneyDrop = 7.0;                 // OpenMU GameLogic Constant
        public const double DropChanceMoney = 0.50;              // OpenMU GameConfiguration Constant
        public const double DropChanceJewel = 0.001;             // OpenMU GameConfiguration Constant (0.1%)
        public const double DropChanceExc = 0.0001;              // OpenMU GameConfiguration Constant (0.01%)
        public const long MaximumInventoryMoney = int.MaxValue;  // OpenMU 32-bit integer ceiling (2,147,483,647)

        public static void Main(string[] args)
        {
            InitializeExpTable();

            // Run Unit Tests first
            RunZenCapUnitTest();

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string outputDir = Path.Combine(baseDir, "results");
            string projectCsvDir = Path.Combine(baseDir, "..", "..", "..", "csv_results");

            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, true);
            }
            Directory.CreateDirectory(outputDir);

            Console.WriteLine("================================================================================");
            Console.WriteLine(" OPENMU S6E3 — UNIFIED POST-CALIBRATION SIMULATION (DYNAMIC MONSTER RANKING)");
            Console.WriteLine("================================================================================");

            RunAllExports(outputDir);

            try
            {
                if (Directory.Exists(projectCsvDir))
                {
                    RunAllExports(projectCsvDir);
                }
            }
            catch { }

            Console.WriteLine();
            Console.WriteLine("[SUCCESS] Unified simulation batch completed successfully.");
        }

        private static void RunAllExports(string dir)
        {
            Directory.CreateDirectory(dir);

            // 1. Export Exp Table (1..400)
            ExportExpTableVerification(dir);

            // 2. Export Monster Calibration Table
            ExportMonsterCalibration(dir);

            // 3. Export Monster Coverage Diagnostic Report
            ExportMonsterCoverageReport(dir);

            // 4. Export Reset 27 Step-by-Step Breakdown (Per-Level Integrated, Continuous Boundaries, Verifiable)
            ExportReset27Breakdown(dir);

            // 5. Scenario A: Baseline 1x
            RunScenarioA_Calibrated(dir);

            // 6. Scenario B Audited (with Zen waiting logic & All-or-Nothing Zen guard)
            RunScenarioB_Audited(dir);

            // 7. Scenario C2 & D2 (Capped ladders with 15% Exp cost)
            RunScenarioC2_D2_Audited(dir);

            // 8. Scenario E Invariance
            RunScenarioE_Invariance(dir);

            // 9. Scenario F Timeline (Day 1..90)
            RunScenarioF_Timeline(dir);

            // 10. Jewel Economy Model
            RunJewelEconomy(dir);

            // 11. Gold Preset Audited Run
            RunGoldPreset_Audited(dir);
        }

        /// <summary>
        /// Unit test verifying all-or-nothing Zen cap behavior matching OpenMU PlayerMoneyExtensions.TryAddMoney.
        /// </summary>
        public static void RunZenCapUnitTest()
        {
            long bal = MaximumInventoryMoney - 1000;
            long disc = 0;
            bool ok = TryAddMoneyAllOrNothing(ref bal, 5000, ref disc);

            if (ok)
            {
                throw new InvalidOperationException($"[FAIL] Zen Cap Unit Test: expected ok == false, got true");
            }
            if (bal != MaximumInventoryMoney - 1000)
            {
                throw new InvalidOperationException($"[FAIL] Zen Cap Unit Test: balance changed unexpectedly to {bal}");
            }
            if (disc != 5000)
            {
                throw new InvalidOperationException($"[FAIL] Zen Cap Unit Test: expected discardedZen == 5000 (entire amount), got {disc}");
            }

            Console.WriteLine("[PASS] Unit Test: Zen Cap All-or-Nothing overflow guard verified.");
        }

        /// <summary>
        /// Returns the required level to enter a given map according to AccessibleMaps.
        /// </summary>
        public static int GetRequiredLevelForMap(int mapNumber)
        {
            var map = AccessibleMaps.FirstOrDefault(m => m.MapNumber == mapNumber);
            return map?.RequiredLevel ?? 1;
        }

        /// <summary>
        /// Evaluates ranking score (EXP/hour) of a monster for a player at playerLevel.
        /// Isolated method to facilitate Part B empirical DPS curve integration.
        /// </summary>
        public static double ScoreMonster(MonsterProfile m, int playerLevel)
            => CalculateBaseExperience(m.MonsterLevel, playerLevel) * m.BaseKillsPerHour;

        /// <summary>
        /// Dynamic ranking monster selector: selects the accessible monster with highest EXP/hour at playerLevel.
        /// Replaces hardcoded ladder if statements.
        /// </summary>
        public static MonsterProfile GetBestMonsterForLevel(int playerLevel) =>
            MonsterDatabase
                .Where(m => playerLevel >= GetRequiredLevelForMap(m.MapNumber))
                .OrderByDescending(m => ScoreMonster(m, playerLevel))
                .First();

        /// <summary>
        /// Exact OpenMU Webzen Exp Formula from GameConfigurationInitializerBase.cs:CalculateNeededExperience
        /// </summary>
        public static void InitializeExpTable()
        {
            ExpTable[0] = 0;
            for (int level = 1; level <= 400; level++)
            {
                if (level < 256)
                {
                    ExpTable[level] = 10L * (level + 8) * (level - 1) * (level - 1);
                }
                else
                {
                    ExpTable[level] = (10L * (level + 8) * (level - 1) * (level - 1)) +
                                      (1000L * (level - 247) * (level - 256) * (level - 256));
                }
            }
        }

        private static void ExportExpTableVerification(string outputDir)
        {
            string file = Path.Combine(outputDir, "exp_table_verification.csv");
            using var writer = new StreamWriter(file);
            writer.WriteLine("level,expForThisLevel,cumulativeExp");

            for (int lvl = 1; lvl <= 400; lvl++)
            {
                long expThisLevel = lvl == 1 ? 0 : ExpTable[lvl] - ExpTable[lvl - 1];
                long cumulativeExp = ExpTable[lvl];
                writer.WriteLine($"{lvl},{expThisLevel},{cumulativeExp}");
            }
        }

        /// <summary>
        /// Exact OpenMU AttackableExtensions.cs:CalculateBaseExperience with 4.0 floating point division
        /// </summary>
        public static double CalculateBaseExperience(int targetLevel, float killerLevel)
        {
            var tempExperience = (targetLevel + 25) * targetLevel / 3.0;

            if (killerLevel > targetLevel + 10)
            {
                tempExperience *= (targetLevel + 10) / killerLevel;
            }

            if (targetLevel >= 65)
            {
                tempExperience += (targetLevel - 64) * (targetLevel / 4.0);
            }

            return Math.Max(tempExperience, 0) * 1.25;
        }

        private static void ExportMonsterCalibration(string outputDir)
        {
            string file = Path.Combine(outputDir, "monster_exp_calibration.csv");
            using var writer = new StreamWriter(file);
            writer.WriteLine("MinLevel,MaxLevel,ZoneName,MonsterLevel,MonsterHP,BaseExpUnpenalized,KillsPerHour");

            int[] starts = { 1, 31, 61, 101, 151, 201, 261, 321, 361 };
            int[] ends   = { 30, 60, 100, 150, 200, 260, 320, 360, 400 };

            for (int i = 0; i < starts.Length; i++)
            {
                var mob = GetBestMonsterForLevel(starts[i]);
                double unpenalizedExp = CalculateBaseExperience(mob.MonsterLevel, mob.MonsterLevel);
                string zoneLabel = $"{mob.MapName} ({mob.MonsterName} lv{mob.MonsterLevel})";
                writer.WriteLine(string.Format(CultureInfo.InvariantCulture,
                    "{0},{1},\"{2}\",{3},{4},{5:F2},{6}",
                    starts[i], ends[i], zoneLabel, mob.MonsterLevel, mob.HP, unpenalizedExp, mob.BaseKillsPerHour));
            }
        }

        /// <summary>
        /// Generates diagnostic coverage report identifying which entries are active vs dominated across levels 1..400.
        /// </summary>
        private static void ExportMonsterCoverageReport(string outputDir)
        {
            string file = Path.Combine(outputDir, "monster_coverage_report.csv");
            using var writer = new StreamWriter(file);
            writer.WriteLine("monsterNumber,monsterName,monsterLevel,mapNumber,gateLevel,everSelected,firstLevel,lastLevel,dominatedBy");

            var selectedLevels = new Dictionary<int, List<int>>();
            foreach (var m in MonsterDatabase)
            {
                selectedLevels[m.MonsterNumber] = new List<int>();
            }

            for (int lvl = 1; lvl <= 400; lvl++)
            {
                var best = GetBestMonsterForLevel(lvl);
                selectedLevels[best.MonsterNumber].Add(lvl);
            }

            foreach (var m in MonsterDatabase)
            {
                var levels = selectedLevels[m.MonsterNumber];
                bool everSelected = levels.Count > 0;
                string firstLvl = everSelected ? levels.Min().ToString() : "N/A";
                string lastLvl = everSelected ? levels.Max().ToString() : "N/A";
                string dominatedBy = "None";

                if (!everSelected)
                {
                    int gate = GetRequiredLevelForMap(m.MapNumber);
                    var candidates = MonsterDatabase
                        .Where(other => other.MonsterNumber != m.MonsterNumber && GetRequiredLevelForMap(other.MapNumber) <= gate)
                        .Select(other => new { Mob = other, Score = ScoreMonster(other, gate) })
                        .OrderByDescending(x => x.Score)
                        .ToList();

                    double myScore = ScoreMonster(m, gate);
                    var winner = candidates.FirstOrDefault(c => c.Score > myScore);
                    if (winner != null)
                    {
                        dominatedBy = $"{winner.Mob.MonsterName} #{winner.Mob.MonsterNumber} ({winner.Mob.MapName} - {winner.Score:F0} vs {myScore:F0} EXP/h @ L{gate})";
                    }
                    else
                    {
                        dominatedBy = "Outranked by higher tier accessible monsters";
                    }
                }

                int gateLevel = GetRequiredLevelForMap(m.MapNumber);
                writer.WriteLine($"{m.MonsterNumber},\"{m.MonsterName}\",{m.MonsterLevel},{m.MapNumber},{gateLevel},{everSelected},{firstLvl},{lastLvl},\"{dominatedBy}\"");
            }
        }

        /// <summary>
        /// Step-by-step breakdown of Reset 27 (Level 10 -> 310) with continuous boundaries and redundant verification metrics
        /// </summary>
        private static void ExportReset27Breakdown(string outputDir)
        {
            string file = Path.Combine(outputDir, "reset_27_breakdown.csv");
            using var writer = new StreamWriter(file);
            writer.WriteLine("BracketStart,BracketEnd,MapZone,MonsterLevel,MonsterHP,RateTier,BracketExp,TotalKills,LevelingHours,KillsPerHour_Verify,ExpPerKill_Verify,GrossZen,NetZen");

            int[] bracketStarts = { 10, 30, 60, 100, 150, 200, 260 };
            int[] bracketEnds   = { 30, 60, 100, 150, 200, 260, 310 };

            for (int i = 0; i < bracketStarts.Length; i++)
            {
                int s = bracketStarts[i];
                int e = bracketEnds[i];
                
                SimulateLevelRange(s, e, lvl => GetGoldPresetExpRate(lvl),
                    out double bHours, out long bExp, out long bGrossZen, out long bRoutineCosts,
                    out double bJewels, out double bExc, out double bKills);

                long bNetZen = bGrossZen - bRoutineCosts;
                var sampleMob = GetBestMonsterForLevel((s + e) / 2);
                
                // Calculate weighted average rate across the bracket to accurately reflect mixed tiers (e.g. 260-310)
                double totalExpWeighted = 0;
                for (int lvl = s; lvl < e; lvl++)
                {
                    long expNeeded = ExpTable[lvl + 1] - ExpTable[lvl];
                    totalExpWeighted += expNeeded * GetGoldPresetExpRate(lvl);
                }
                double rateTier = bExp > 0 ? totalExpWeighted / bExp : GetGoldPresetExpRate(s);

                string zoneLabel = $"{sampleMob.MapName} ({sampleMob.MonsterName} lv{sampleMob.MonsterLevel})";
                double killsPerHourCalc = bHours > 0 ? bKills / bHours : 0;
                double expPerKillCalc = bKills > 0 ? (double)bExp / bKills : 0;

                writer.WriteLine(string.Format(CultureInfo.InvariantCulture,
                    "{0},{1},\"{2}\",{3},{4},{5:F1},{6},{7:F0},{8:F4},{9:F1},{10:F1},{11},{12}",
                    s, e, zoneLabel, sampleMob.MonsterLevel, sampleMob.HP, rateTier, bExp, bKills, bHours, killsPerHourCalc, expPerKillCalc, bGrossZen, bNetZen));
            }
        }

        private static double GetDynamicExpRate(int level)
        {
            if (level <= 100) return 400.0;
            if (level <= 200) return 250.0;
            if (level <= 300) return 100.0;
            if (level <= 350) return 50.0;
            return 35.0;
        }

        public static double GetGoldPresetExpRate(int level)
        {
            if (level <= 100) return 50.0;
            if (level <= 200) return 30.0;
            if (level <= 300) return 15.0;
            if (level <= 350) return 8.0;
            return 5.0;
        }

        /// <summary>
        /// Pure level-by-level continuous integration of EXP, Zen, Kills and Time
        /// </summary>
        public static void SimulateLevelRange(int startLevel, int targetLevel, Func<int, double> getExpRateFunc,
            out double hours, out long totalExp, out long grossZen, out long routineCosts, out double jewels, out double excItems, out double totalKills)
        {
            hours = 0;
            totalExp = 0;
            grossZen = 0;
            jewels = 0;
            excItems = 0;
            totalKills = 0;

            for (int lvl = startLevel; lvl < targetLevel; lvl++)
            {
                long expNeeded = ExpTable[lvl + 1] - ExpTable[lvl];
                var mob = GetBestMonsterForLevel(lvl);
                double baseMonsterExp = CalculateBaseExperience(mob.MonsterLevel, lvl);
                double expRate = getExpRateFunc(lvl);
                double gainedExpPerKill = baseMonsterExp * expRate;

                double killsThisLevel = (double)expNeeded / gainedExpPerKill;
                double hoursThisLevel = killsThisLevel / mob.BaseKillsPerHour;

                hours += hoursThisLevel;
                totalExp += expNeeded;
                totalKills += killsThisLevel;

                // Drops: Money amount = gainedExpPerKill + BaseMoneyDrop (DefaultDropGenerator.cs:L517)
                double moneyPerKill = (gainedExpPerKill + BaseMoneyDrop);
                double moneyYield = killsThisLevel * DropChanceMoney * moneyPerKill * PickupEfficiency;
                grossZen += (long)moneyYield;

                jewels += killsThisLevel * DropChanceJewel * PickupEfficiency;
                excItems += killsThisLevel * DropChanceExc * PickupEfficiency;
            }

            routineCosts = (long)(hours * RoutineCostPerHour);
        }

        /// <summary>
        /// All-or-nothing Zen accumulation logic mimicking OpenMU Player.cs:TryAddMoney with DiscardedZen tracking.
        /// Discards Zen and returns false ONLY when inventory reaches MaximumInventoryMoney (2,147,483,647).
        /// </summary>
        public static bool TryAddMoneyAllOrNothing(ref long currentMoney, long amountToAdd, ref long discardedZen)
        {
            if (amountToAdd <= 0)
            {
                return true;
            }

            if (currentMoney + amountToAdd > MaximumInventoryMoney)
            {
                discardedZen += amountToAdd; // Zen dropped/lost due to 2.147.483.647 cap
                return false; // All-or-nothing: Zen is not picked up, balance unchanged
            }

            currentMoney += amountToAdd;
            return true;
        }

        /// <summary>
        /// Removes Zen from current balance mimicking OpenMU Player.cs:TryRemoveMoney semantics.
        /// Returns false and leaves balance unchanged if funds are insufficient.
        /// </summary>
        public static bool TryRemoveMoney(ref long currentMoney, long amountToRemove)
        {
            if (amountToRemove <= 0)
            {
                return true;
            }

            if (currentMoney < amountToRemove)
            {
                return false;
            }

            currentMoney -= amountToRemove;
            return true;
        }

        private static void RunScenarioA_Calibrated(string outputDir)
        {
            string file = Path.Combine(outputDir, "scenario_a_calibrated.csv");
            using var writer = new StreamWriter(file);
            writer.WriteLine("ResetCount,TargetLevel,LevelingHours,CumulativeHours,CycleExp,GrossZen,RoutineCosts,NetZenFromLeveling,ResetCost,ZenBalance,DiscardedZen,ResetPaid,CumulativeJewels,CumulativePoints");

            long zenBalance = 0;
            long discardedZen = 0;
            double cumHours = 0;
            double cumJewels = 0;
            int cumPoints = 0;

            for (int k = 1; k <= 36; k++)
            {
                int startLvl = 10;
                int targetLvl = 50 + (k - 1) * 10;
                long resetCost = 10_000_000L * k;
                int pointsGranted = 5 * targetLvl;

                SimulateLevelRange(startLvl, targetLvl, _ => 1.0, out double levelingHours, out long cycleExp, out long grossZen, out long routineCosts, out double jewels, out double excItems, out double kills);

                long netFromLeveling = grossZen - routineCosts;

                // 1. Add gross Zen earned from leveling
                TryAddMoneyAllOrNothing(ref zenBalance, grossZen, ref discardedZen);

                // 2. Pay routine costs
                TryRemoveMoney(ref zenBalance, routineCosts);

                // 3. Attempt to pay reset fee
                bool resetPaid = TryRemoveMoney(ref zenBalance, resetCost);
                if (resetPaid)
                {
                    cumPoints += pointsGranted;
                }

                cumHours += levelingHours;
                cumJewels += jewels;

                writer.WriteLine(string.Format(CultureInfo.InvariantCulture,
                    "{0},{1},{2:F2},{3:F2},{4},{5},{6},{7},{8},{9},{10},{11},{12:F2},{13}",
                    k, targetLvl, levelingHours, cumHours, cycleExp, grossZen, routineCosts, netFromLeveling, resetCost, zenBalance, discardedZen, resetPaid, cumJewels, cumPoints));
            }
        }

        private static void RunScenarioB_Audited(string outputDir)
        {
            string file = Path.Combine(outputDir, "scenario_b_dynamic_audited.csv");
            using var writer = new StreamWriter(file);
            writer.WriteLine("ResetCount,TargetLevel,LevelingHours,WaitHours,TotalHours,CumulativeHours,GrossZen,RoutineCosts,ExtraZenFarmed,ResetCost,ZenBalance,DiscardedZen,IsBlocked,CumulativeJewels");

            long zenBalance = 0;
            long discardedZen = 0;
            double cumHours = 0;
            double cumJewels = 0;

            for (int k = 1; k <= 36; k++)
            {
                int startLvl = 10;
                int targetLvl = 50 + (k - 1) * 10;
                long resetCost = 10_000_000L * k;

                SimulateLevelRange(startLvl, targetLvl, lvl => GetDynamicExpRate(lvl), out double levelingHours, out long cycleExp, out long grossZen, out long routineCosts, out double jewels, out double excItems, out double kills);

                // 1. Add gross Zen from leveling
                TryAddMoneyAllOrNothing(ref zenBalance, grossZen, ref discardedZen);

                // 2. Pay routine maintenance
                TryRemoveMoney(ref zenBalance, routineCosts);

                double waitHours = 0;
                long extraZen = 0;
                bool isBlocked = false;

                if (zenBalance < resetCost)
                {
                    long shortage = resetCost - zenBalance;
                    var mob = GetBestMonsterForLevel(targetLvl);
                    double baseExp = CalculateBaseExperience(mob.MonsterLevel, targetLvl);
                    double rate = GetDynamicExpRate(targetLvl);
                    double expPerKill = baseExp * rate;
                    double moneyPerKill = (expPerKill + BaseMoneyDrop);
                    double netZenRatePerHour = (mob.BaseKillsPerHour * DropChanceMoney * moneyPerKill * PickupEfficiency) - RoutineCostPerHour;

                    if (netZenRatePerHour > 0)
                    {
                        waitHours = (double)shortage / netZenRatePerHour;
                        extraZen = shortage;
                        TryAddMoneyAllOrNothing(ref zenBalance, extraZen, ref discardedZen);
                        jewels += waitHours * mob.BaseKillsPerHour * DropChanceJewel * PickupEfficiency;
                    }
                    else
                    {
                        isBlocked = true;
                    }
                }

                // 3. Deduct reset fee
                bool resetPaid = TryRemoveMoney(ref zenBalance, resetCost);
                if (!resetPaid)
                {
                    isBlocked = true;
                }

                double totalCycleHours = levelingHours + waitHours;
                cumHours += totalCycleHours;
                cumJewels += jewels;

                writer.WriteLine(string.Format(CultureInfo.InvariantCulture,
                    "{0},{1},{2:F2},{3:F2},{4:F2},{5:F2},{6},{7},{8},{9},{10},{11},{12},{13:F2}",
                    k, targetLvl, levelingHours, waitHours, totalCycleHours, cumHours, grossZen, routineCosts, extraZen, resetCost, zenBalance, discardedZen, isBlocked, cumJewels));
            }
        }

        private static void RunScenarioC2_D2_Audited(string outputDir)
        {
            string file = Path.Combine(outputDir, "scenario_c2_d2_audited.csv");
            using var writer = new StreamWriter(file);
            writer.WriteLine("ResetCount,TargetLevel,C2_LevelingHours,C2_CumHours,C2_ResetCost,C2_NetZen,C2_ZenBalance,C2_DiscardedZen,C2_ResetPaid,D2_LevelingHours,D2_CumHours,D2_ResetCost,D2_NetZen,D2_ZenBalance,D2_DiscardedZen,D2_ResetPaid");

            long zenBalanceC2 = 0;
            long discardedC2 = 0;
            double cumHoursC2 = 0;
            long zenBalanceD2 = 0;
            long discardedD2 = 0;
            double cumHoursD2 = 0;

            for (int k = 1; k <= 36; k++)
            {
                int targetLvl = 50 + (k - 1) * 10;

                SimulateLevelRange(10, targetLvl, _ => 1.0, out double hC2, out long expC2, out long grossC2, out long costC2, out double jC2, out double eC2, out double killsC2);
                long resetCostC2 = (long)(expC2 * 0.15);
                long netC2 = grossC2 - costC2 - resetCostC2;
                TryAddMoneyAllOrNothing(ref zenBalanceC2, grossC2, ref discardedC2);
                TryRemoveMoney(ref zenBalanceC2, costC2);
                bool paidC2 = TryRemoveMoney(ref zenBalanceC2, resetCostC2);
                cumHoursC2 += hC2;

                SimulateLevelRange(10, targetLvl, lvl => GetDynamicExpRate(lvl), out double hD2, out long expD2, out long grossD2, out long costD2, out double jD2, out double eD2, out double killsD2);
                long resetCostD2 = (long)(expD2 * 0.15);
                long netD2 = grossD2 - costD2 - resetCostD2;
                TryAddMoneyAllOrNothing(ref zenBalanceD2, grossD2, ref discardedD2);
                TryRemoveMoney(ref zenBalanceD2, costD2);
                bool paidD2 = TryRemoveMoney(ref zenBalanceD2, resetCostD2);
                cumHoursD2 += hD2;

                writer.WriteLine(string.Format(CultureInfo.InvariantCulture,
                    "{0},{1},{2:F2},{3:F2},{4},{5},{6},{7},{8},{9:F2},{10:F2},{11},{12},{13},{14},{15}",
                    k, targetLvl, hC2, cumHoursC2, resetCostC2, netC2, zenBalanceC2, discardedC2, paidC2, hD2, cumHoursD2, resetCostD2, netD2, zenBalanceD2, discardedD2, paidD2));
            }
        }

        private static void RunScenarioE_Invariance(string outputDir)
        {
            string file = Path.Combine(outputDir, "scenario_e_invariance.csv");
            using var writer = new StreamWriter(file);
            writer.WriteLine("ResetCount,TargetLevel,CostRatio_1x,CostRatio_10x,CostRatio_50x,CostRatio_Dynamic");

            double[] rates = { 1.0, 10.0, 50.0 };

            for (int k = 1; k <= 36; k++)
            {
                int targetLvl = 50 + (k - 1) * 10;
                double[] ratios = new double[4];

                for (int r = 0; r < 3; r++)
                {
                    double fixedRate = rates[r];
                    SimulateLevelRange(10, targetLvl, _ => fixedRate, out double hours, out long cycleExp, out long grossZen, out long routineCosts, out double j, out double e, out double kills);
                    long resetCost = (long)(cycleExp * 0.15);
                    ratios[r] = grossZen > 0 ? (double)(routineCosts + resetCost) / grossZen * 100.0 : 0;
                }

                SimulateLevelRange(10, targetLvl, lvl => GetDynamicExpRate(lvl), out double hDyn, out long expDyn, out long zenDyn, out long costDyn, out double jD, out double eD, out double killsDyn);
                long resetCostDyn = (long)(expDyn * 0.15);
                ratios[3] = zenDyn > 0 ? (double)(costDyn + resetCostDyn) / zenDyn * 100.0 : 0;

                writer.WriteLine(string.Format(CultureInfo.InvariantCulture,
                    "{0},{1},{2:F2},{3:F2},{4:F2},{5:F2}",
                    k, targetLvl, ratios[0], ratios[1], ratios[2], ratios[3]));
            }
        }

        private static void RunScenarioF_Timeline(string outputDir)
        {
            string file = Path.Combine(outputDir, "scenario_f_timeline.csv");
            using var writer = new StreamWriter(file);
            writer.WriteLine("Day,Casual_Resets,Casual_Points,Casual_Zen,Casual_Jewels,Hardcore_Resets,Hardcore_Points,Hardcore_Zen,Hardcore_Jewels,Nolife_Resets,Nolife_Points,Nolife_Zen,Nolife_Jewels");

            double[] cycleHours = new double[37];
            long[] cycleGrossZen = new long[37];
            long[] cycleRoutine = new long[37];
            long[] cycleResetFee = new long[37];
            double[] cycleJewels = new double[37];
            double[] cycleExc = new double[37];
            int[] cyclePoints = new int[37];

            for (int k = 1; k <= 36; k++)
            {
                int targetLvl = 50 + (k - 1) * 10;
                SimulateLevelRange(10, targetLvl, lvl => GetDynamicExpRate(lvl), out double h, out long exp, out long grossZ, out long routine, out double j, out double exc, out double kills);
                long resetCost = (long)(exp * 0.15);
                cycleHours[k] = h;
                cycleGrossZen[k] = grossZ;
                cycleRoutine[k] = routine;
                cycleResetFee[k] = resetCost;
                cycleJewels[k] = j;
                cycleExc[k] = exc;
                cyclePoints[k] = 5 * targetLvl;
            }

            var maxMob = GetBestMonsterForLevel(400);
            double maxBaseExp = CalculateBaseExperience(maxMob.MonsterLevel, 400);
            double maxExpRate = GetDynamicExpRate(400);
            double maxExpPerKill = maxBaseExp * maxExpRate;
            double maxZenPerHour = (maxMob.BaseKillsPerHour * DropChanceMoney * (maxExpPerKill + BaseMoneyDrop) * PickupEfficiency) - RoutineCostPerHour;
            double maxJewelsPerHour = maxMob.BaseKillsPerHour * DropChanceJewel * PickupEfficiency;

            var profiles = new (string Name, double HoursPerDay)[]
            {
                ("Casual", 2.0),
                ("Hardcore", 8.0),
                ("NoLife", 16.0)
            };

            for (int day = 1; day <= 90; day++)
            {
                var rowData = new List<string> { day.ToString() };

                foreach (var profile in profiles)
                {
                    double totalBudgetHours = day * profile.HoursPerDay;
                    double usedHours = 0;
                    int completedResets = 0;
                    int totalPoints = 0;
                    long totalZen = 0;
                    long dummyDiscard = 0;
                    double totalJewels = 0;

                    for (int k = 1; k <= 36; k++)
                    {
                        if (usedHours + cycleHours[k] <= totalBudgetHours)
                        {
                            TryAddMoneyAllOrNothing(ref totalZen, cycleGrossZen[k], ref dummyDiscard);
                            TryRemoveMoney(ref totalZen, cycleRoutine[k]);
                            bool paid = TryRemoveMoney(ref totalZen, cycleResetFee[k]);
                            if (paid)
                            {
                                usedHours += cycleHours[k];
                                completedResets = k;
                                totalPoints += cyclePoints[k];
                                totalJewels += cycleJewels[k];
                            }
                            else
                            {
                                // Blocked due to insufficient Zen for reset fee
                                break;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (completedResets == 36 && usedHours < totalBudgetHours)
                    {
                        double remainingHours = totalBudgetHours - usedHours;
                        if (maxZenPerHour > 0)
                        {
                            TryAddMoneyAllOrNothing(ref totalZen, (long)(remainingHours * maxZenPerHour), ref dummyDiscard);
                        }
                        totalJewels += remainingHours * maxJewelsPerHour;
                    }

                    rowData.Add(completedResets.ToString());
                    rowData.Add(totalPoints.ToString());
                    rowData.Add(totalZen.ToString());
                    rowData.Add(totalJewels.ToString("F1", CultureInfo.InvariantCulture));
                }

                writer.WriteLine(string.Join(",", rowData));
            }
        }

        private static void RunJewelEconomy(string outputDir)
        {
            string file = Path.Combine(outputDir, "jewel_economy_model.csv");
            using var writer = new StreamWriter(file);
            writer.WriteLine("ResetCount,TargetLevel,TotalKills,CumJewels_0.1pct,CumJewels_0.5pct,CumJewels_1.0pct,CumJewels_2.0pct");

            double cumJewel01 = 0;
            double cumJewel05 = 0;
            double cumJewel10 = 0;
            double cumJewel20 = 0;

            for (int k = 1; k <= 36; k++)
            {
                int targetLvl = 50 + (k - 1) * 10;

                SimulateLevelRange(10, targetLvl, lvl => GetDynamicExpRate(lvl),
                    out _, out _, out _, out _, out _, out _, out double totalKills);

                double j01 = totalKills * 0.001 * PickupEfficiency;
                double j05 = totalKills * 0.005 * PickupEfficiency;
                double j10 = totalKills * 0.010 * PickupEfficiency;
                double j20 = totalKills * 0.020 * PickupEfficiency;

                cumJewel01 += j01;
                cumJewel05 += j05;
                cumJewel10 += j10;
                cumJewel20 += j20;

                writer.WriteLine(string.Format(CultureInfo.InvariantCulture,
                    "{0},{1},{2:F0},{3:F2},{4:F2},{5:F2},{6:F2}",
                    k, targetLvl, totalKills, cumJewel01, cumJewel05, cumJewel10, cumJewel20));
            }
        }

        private static void RunGoldPreset_Audited(string outputDir)
        {
            string file = Path.Combine(outputDir, "gold_preset_audited_timeline.csv");
            using var writer = new StreamWriter(file);
            writer.WriteLine("ResetCount,TargetLevel,CycleHours,CumulativeHours,GrossZen,RoutineCosts,ResetFeeZen,ChaosRequired,CreationRequired,NetZen,ZenBalance,DiscardedZen,ResetPaid,CostRatioPercent,CumulativeJewels");

            long zenBalance = 0;
            long discardedZen = 0;
            double cumHours = 0;
            double cumJewels = 0;

            for (int k = 1; k <= 36; k++)
            {
                int targetLvl = 50 + (k - 1) * 10;
                SimulateLevelRange(10, targetLvl, lvl => GetGoldPresetExpRate(lvl), out double hours, out long cycleExp, out long grossZen, out long routineCosts, out double jewels, out double exc, out double kills);

                long resetFeeZen = 0;
                int chaosReq = 0;
                int creationReq = 0;

                // House Rule Reset Fee Table
                if (k <= 5) resetFeeZen = 100_000L * k;
                else if (k <= 15) resetFeeZen = 2_000_000L * (k - 5);
                else if (k <= 25) { resetFeeZen = 20_000_000L + (k - 15) * 3_000_000L; chaosReq = 1; }
                else { resetFeeZen = 100_000_000L + (k - 25) * 15_000_000L; chaosReq = 1; creationReq = 1; }

                cumHours += hours;
                cumJewels += jewels;

                long netZen = grossZen - routineCosts - resetFeeZen;
                TryAddMoneyAllOrNothing(ref zenBalance, grossZen, ref discardedZen);
                TryRemoveMoney(ref zenBalance, routineCosts);
                bool resetPaid = TryRemoveMoney(ref zenBalance, resetFeeZen);

                double ratio = grossZen > 0 ? (double)(routineCosts + resetFeeZen) / grossZen * 100.0 : 0;

                writer.WriteLine(string.Format(CultureInfo.InvariantCulture,
                    "{0},{1},{2:F2},{3:F2},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13:F2},{14:F2}",
                    k, targetLvl, hours, cumHours, grossZen, routineCosts, resetFeeZen, chaosReq, creationReq, netZen, zenBalance, discardedZen, resetPaid, ratio, cumJewels));
            }
        }
    }
}
