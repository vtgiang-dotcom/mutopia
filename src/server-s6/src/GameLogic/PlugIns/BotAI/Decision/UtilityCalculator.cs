// <copyright file="UtilityCalculator.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.BotAI.Decision;

using MUnique.OpenMU.GameLogic.PlugIns.BotAI.Humanization;

/// <summary>
/// Possible high-level action types evaluated by Utility AI.
/// </summary>
public enum BotHighLevelAction
{
    /// <summary>
    /// Farm monsters in current spot.
    /// </summary>
    Farm,

    /// <summary>
    /// Go to town to sell junk, buy potions, or repair items.
    /// </summary>
    GoToTown,

    /// <summary>
    /// Retaliate against PK attacker.
    /// </summary>
    RetaliatePK,

    /// <summary>
    /// Reset character when max level reached.
    /// </summary>
    ResetCharacter,

    /// <summary>
    /// Wander / Idle in safe zone.
    /// </summary>
    Idle,
}

/// <summary>
/// Snapshot of player state passed to Utility Calculator.
/// </summary>
public record BotStateContext(
    double HealthPercentage,
    double ManaPercentage,
    bool IsInventoryFull,
    bool IsMaxLevelReached,
    bool IsUnderAttackByPlayer,
    int NearbyMonsterCount,
    BotPersonality Personality);

/// <summary>
/// Calculates Utility scores (0.0 to 1.0) for Bot decisions.
/// </summary>
public class UtilityCalculator
{
    /// <summary>
    /// Evaluates utility scores and returns the highest priority action.
    /// </summary>
    /// <param name="ctx">Context snapshot of the bot.</param>
    /// <returns>Selected <see cref="BotHighLevelAction"/>.</returns>
    public static BotHighLevelAction SelectBestAction(BotStateContext ctx)
    {
        var scores = new Dictionary<BotHighLevelAction, double>
        {
            [BotHighLevelAction.ResetCharacter] = CalculateResetScore(ctx),
            [BotHighLevelAction.GoToTown] = CalculateGoToTownScore(ctx),
            [BotHighLevelAction.RetaliatePK] = CalculateRetaliateScore(ctx),
            [BotHighLevelAction.Farm] = CalculateFarmScore(ctx),
            [BotHighLevelAction.Idle] = 0.1, // Base fallback score
        };

        return scores.MaxBy(kv => kv.Value).Key;
    }

    private static double CalculateResetScore(BotStateContext ctx)
    {
        return ctx.IsMaxLevelReached ? 1.0 : 0.0;
    }

    private static double CalculateGoToTownScore(BotStateContext ctx)
    {
        if (ctx.HealthPercentage < 0.15)
        {
            return 0.95; // Critical low health with no pots
        }

        if (ctx.IsInventoryFull)
        {
            return 0.85;
        }

        return 0.0;
    }

    private static double CalculateRetaliateScore(BotStateContext ctx)
    {
        if (!ctx.IsUnderAttackByPlayer)
        {
            return 0.0;
        }

        // Aggressive bots have higher score to fight back
        double aggressionMultiplier = ctx.Personality.Aggression / 100.0;
        return 0.5 + (0.4 * aggressionMultiplier);
    }

    private static double CalculateFarmScore(BotStateContext ctx)
    {
        if (ctx.IsMaxLevelReached || ctx.HealthPercentage < 0.15)
        {
            return 0.0;
        }

        double score = 0.6;
        if (ctx.NearbyMonsterCount > 0)
        {
            score += 0.25;
        }

        return Math.Min(1.0, score);
    }
}
