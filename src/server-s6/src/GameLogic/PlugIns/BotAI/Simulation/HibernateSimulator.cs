// <copyright file="HibernateSimulator.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.BotAI.Simulation;

/// <summary>
/// Result of offline hibernate catchup calculation.
/// </summary>
/// <param name="EarnedExperience">Total net EXP earned after decay and penalty.</param>
/// <param name="DiedDuringHibernate">Whether death risk event was triggered during offline time.</param>
/// <param name="PenaltyExperience">EXP deducted due to death penalty if triggered.</param>
public record HibernateCatchupResult(long EarnedExperience, bool DiedDuringHibernate, long PenaltyExperience);

/// <summary>
/// Simulates offline EXP progression using discrete tier decay schedules and death risk rolls.
/// </summary>
public class HibernateSimulator
{
    private static readonly (double MaxHours, double Ratio)[] TierTable =
    [
        (1.0, 0.70),  // 0 - 1h: 70%
        (3.0, 0.55),  // 1 - 3h: 55%
        (6.0, 0.40),  // 3 - 6h: 40%
        (12.0, 0.25), // 6 - 12h: 25%
        (24.0, 0.10), // 12 - 24h: 10%
    ];

    /// <summary>
    /// Calculates piecewise net EXP earned after applying tier decay schedule and death risk roll.
    /// </summary>
    /// <param name="hibernateDuration">Total time elapsed in hibernate state.</param>
    /// <param name="baseMapExpPerSec">Base EXP per second earned at current farm spot.</param>
    /// <param name="mapDangerLevel">Map danger multiplier (0.1 to 1.0).</param>
    /// <returns><see cref="HibernateCatchupResult"/>.</returns>
    public static HibernateCatchupResult CalculateCatchupExp(TimeSpan hibernateDuration, long baseMapExpPerSec, double mapDangerLevel = 0.5)
    {
        double totalHours = Math.Min(24.0, hibernateDuration.TotalHours);
        if (totalHours <= 0 || baseMapExpPerSec <= 0)
        {
            return new HibernateCatchupResult(0, false, 0);
        }

        double earnedExpDouble = 0.0;
        double previousHours = 0.0;

        foreach (var (maxHours, ratio) in TierTable)
        {
            if (totalHours <= previousHours)
            {
                break;
            }

            double hoursInTier = Math.Min(totalHours, maxHours) - previousHours;
            double secondsInTier = hoursInTier * 3600.0;
            earnedExpDouble += baseMapExpPerSec * ratio * secondsInTier;

            previousHours = maxHours;
        }

        long grossExp = (long)Math.Max(0, earnedExpDouble);

        // Death Risk Roll: DeathProbability = min(0.5, mapDangerLevel * t / 24)
        double deathProbability = Math.Min(0.5, mapDangerLevel * totalHours / 24.0);
        bool died = Random.Shared.NextDouble() < deathProbability;

        long penaltyExp = 0;
        if (died && grossExp > 0)
        {
            double penaltyPercent = Random.Shared.Next(20, 41) / 100.0; // 20% - 40%
            penaltyExp = (long)(grossExp * penaltyPercent);
        }

        long netExp = Math.Max(0, grossExp - penaltyExp);
        return new HibernateCatchupResult(netExp, died, penaltyExp);
    }
}
