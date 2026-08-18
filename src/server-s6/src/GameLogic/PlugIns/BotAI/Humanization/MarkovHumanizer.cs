// <copyright file="MarkovHumanizer.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.BotAI.Humanization;

/// <summary>
/// Possible humanized micro-actions for a bot.
/// </summary>
public enum HumanActionType
{
    /// <summary>
    /// Continue regular farming/combat action.
    /// </summary>
    ContinueFarm,

    /// <summary>
    /// Simulate human AFK pause (3 - 8 seconds).
    /// </summary>
    SimulateAfkPause,

    /// <summary>
    /// Small random movement jitter (rotate/step around).
    /// </summary>
    RandomJitterStep,

    /// <summary>
    /// Open inventory simulation pause.
    /// </summary>
    CheckInventoryPause,
}

/// <summary>
/// Humanizes bot behavior using Markov probability transitions.
/// </summary>
public class MarkovHumanizer
{
    /// <summary>
    /// Decides next micro-action type based on Markov probability transition.
    /// </summary>
    /// <param name="personality">Personality of the bot.</param>
    /// <returns>Selected <see cref="HumanActionType"/>.</returns>
    public static HumanActionType EvaluateNextAction(BotPersonality personality)
    {
        var roll = Random.Shared.Next(0, 100);

        // More patient bots are slightly more likely to pause/AFK
        int afkThreshold = 70 + (personality.Patience / 10); // e.g. 73 - 79
        int jitterThreshold = afkThreshold + 12; // e.g. 85 - 91
        int inventoryThreshold = jitterThreshold + 5; // e.g. 90 - 96

        if (roll < afkThreshold)
        {
            return HumanActionType.ContinueFarm;
        }

        if (roll < jitterThreshold)
        {
            return HumanActionType.SimulateAfkPause;
        }

        if (roll < inventoryThreshold)
        {
            return HumanActionType.RandomJitterStep;
        }

        return HumanActionType.CheckInventoryPause;
    }

    /// <summary>
    /// Gets random delay in milliseconds for simulated human pause.
    /// </summary>
    /// <param name="action">Action type.</param>
    /// <returns>Delay in ms.</returns>
    public static TimeSpan GetHumanizedDelay(HumanActionType action)
    {
        var rng = Random.Shared;
        return action switch
        {
            HumanActionType.SimulateAfkPause => TimeSpan.FromMilliseconds(rng.Next(2500, 6000)),
            HumanActionType.RandomJitterStep => TimeSpan.FromMilliseconds(rng.Next(800, 2000)),
            HumanActionType.CheckInventoryPause => TimeSpan.FromMilliseconds(rng.Next(1500, 3500)),
            _ => TimeSpan.Zero,
        };
    }
}
