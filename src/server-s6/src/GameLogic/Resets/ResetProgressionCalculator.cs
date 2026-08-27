// <copyright file="ResetProgressionCalculator.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Resets;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Calculates costs and rewards for the character reset using the verified Hybrid Milestone Model.
/// </summary>
public static class ResetProgressionCalculator
{
    /// <summary>
    /// Calculates the required level for the next character reset.
    /// Stage 1 (Resets 1-15): Progressive ladder Level 50 -> 330 (+20 levels/reset).
    /// Stage 2 (Resets 16-50): Fixed Level 400.
    /// </summary>
    /// <param name="configuration">The reset configuration.</param>
    /// <param name="currentResetCount">The current reset count.</param>
    /// <returns>The required level.</returns>
    public static int GetRequiredLevel(ResetConfiguration configuration, int currentResetCount)
    {
        if (currentResetCount < 15)
        {
            var ladderLevel = 50 + (currentResetCount * 20);
            return Math.Min(400, Math.Max(50, ladderLevel));
        }

        return 400;
    }

    /// <summary>
    /// Calculates the reset progression for the next reset.
    /// </summary>
    /// <param name="currentResetCount">The current reset count.</param>
    /// <param name="pointsPerResetOverride">The player-specific points per reset override (0 means not configured).</param>
    /// <param name="configuration">The reset configuration.</param>
    /// <returns>The calculated progression.</returns>
    public static ResetProgression Calculate(int currentResetCount, int pointsPerResetOverride, ResetConfiguration configuration)
    {
        var nextResetCount = currentResetCount + 1;
        long requiredZen;
        if (nextResetCount <= 3)
        {
            requiredZen = 0; // Free for starter resets (Resets 1-3)
        }
        else if (nextResetCount <= 5)
        {
            requiredZen = 200_000L * (nextResetCount - 3);
        }
        else if (nextResetCount <= 15)
        {
            requiredZen = 1_000_000L + ((nextResetCount - 5) * 500_000L);
        }
        else
        {
            requiredZen = 10_000_000L + ((nextResetCount - 15) * 1_000_000L);
        }

        var pointsForReset = GetPointsForReset(configuration, pointsPerResetOverride, nextResetCount);
        var totalPointsAfterReset = GetTotalPointsAfterReset(configuration, pointsPerResetOverride, nextResetCount, pointsForReset);
        var requiredItemAmount = GetRequiredItemAmount(configuration, nextResetCount);

        return new ResetProgression(nextResetCount, (int)Math.Min(int.MaxValue, requiredZen), requiredItemAmount, pointsForReset, totalPointsAfterReset);
    }

    private static int GetPointsForReset(ResetConfiguration configuration, int pointsPerResetOverride, int nextResetCount)
    {
        if (GetMatchingTier(configuration.PointsTiers, nextResetCount, tier => tier.MinimumResetCount) is { } tier)
        {
            return Math.Max(0, tier.PointsGranted);
        }

        if (pointsPerResetOverride > 0)
        {
            return pointsPerResetOverride;
        }

        if (nextResetCount <= 15)
        {
            // Resets 1-15: +250, +300, +350 ... +950 Points
            return 250 + ((nextResetCount - 1) * 50);
        }

        // Resets 16-50: +1,000 Points per reset
        return 1000;
    }

    private static int GetRequiredItemAmount(ResetConfiguration configuration, int nextResetCount)
    {
        if (configuration.RequiredResetItem is null)
        {
            return 0;
        }

        if (GetMatchingTier(configuration.ItemCostTiers, nextResetCount, tier => tier.MinimumResetCount) is not { } tier)
        {
            return 0;
        }

        return Math.Max(0, tier.RequiredItemAmount);
    }

    private static int GetTotalPointsAfterReset(ResetConfiguration configuration, int pointsPerResetOverride, int nextResetCount, int pointsForReset)
    {
        long total = 0;
        for (var resetCount = 1; resetCount <= nextResetCount; resetCount++)
        {
            total += GetPointsForReset(configuration, pointsPerResetOverride, resetCount);
            if (total >= int.MaxValue)
            {
                return int.MaxValue;
            }
        }

        return (int)total;
    }

    private static TTier? GetMatchingTier<TTier>(IEnumerable<TTier> tiers, int resetCount, Func<TTier, int> getMinimumResetCount)
        where TTier : class
    {
        return tiers
            .OrderByDescending(getMinimumResetCount)
            .FirstOrDefault(tier => getMinimumResetCount(tier) <= resetCount);
    }
}
