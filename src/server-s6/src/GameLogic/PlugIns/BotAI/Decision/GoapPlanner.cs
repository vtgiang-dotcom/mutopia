// <copyright file="GoapPlanner.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.BotAI.Decision;

/// <summary>
/// Goal state for GOAP planner.
/// </summary>
public enum GoapGoal
{
    /// <summary>
    /// Reach max level to perform reset.
    /// </summary>
    AchieveReset,

    /// <summary>
    /// Restock inventory (potions, sell loot).
    /// </summary>
    RestockAndSell,

    /// <summary>
    /// Find and maintain a good monster spawn spot.
    /// </summary>
    MaintainFarmSpot,
}

/// <summary>
/// Atomic step in a GOAP plan.
/// </summary>
public enum GoapStep
{
    /// <summary>
    /// Walk to NPC merchant in town.
    /// </summary>
    WalkToMerchant,

    /// <summary>
    /// Sell junk items in inventory.
    /// </summary>
    SellJunkItems,

    /// <summary>
    /// Buy health and mana potions.
    /// </summary>
    BuyPotions,

    /// <summary>
    /// Walk to target farm map spot.
    /// </summary>
    WalkToSpot,

    /// <summary>
    /// Combat loop (cast skills, attack target).
    /// </summary>
    ExecuteCombatLoop,

    /// <summary>
    /// Perform character reset at NPC or command.
    /// </summary>
    PerformReset,
}

/// <summary>
/// Simple lightweight GOAP planner for sequence generation.
/// </summary>
public class GoapPlanner
{
    /// <summary>
    /// Generates a sequence of execution steps to achieve specified GOAP goal.
    /// </summary>
    /// <param name="goal">Target goal.</param>
    /// <param name="ctx">Current state context.</param>
    /// <returns>Queue of steps to execute.</returns>
    public static Queue<GoapStep> BuildPlan(GoapGoal goal, BotStateContext ctx)
    {
        var plan = new Queue<GoapStep>();

        switch (goal)
        {
            case GoapGoal.AchieveReset:
                if (ctx.IsMaxLevelReached)
                {
                    plan.Enqueue(GoapStep.WalkToMerchant);
                    plan.Enqueue(GoapStep.PerformReset);
                }
                else
                {
                    plan.Enqueue(GoapStep.WalkToSpot);
                    plan.Enqueue(GoapStep.ExecuteCombatLoop);
                }

                break;

            case GoapGoal.RestockAndSell:
                plan.Enqueue(GoapStep.WalkToMerchant);
                plan.Enqueue(GoapStep.SellJunkItems);
                plan.Enqueue(GoapStep.BuyPotions);
                plan.Enqueue(GoapStep.WalkToSpot);
                break;

            case GoapGoal.MaintainFarmSpot:
            default:
                plan.Enqueue(GoapStep.WalkToSpot);
                plan.Enqueue(GoapStep.ExecuteCombatLoop);
                break;
        }

        return plan;
    }
}
