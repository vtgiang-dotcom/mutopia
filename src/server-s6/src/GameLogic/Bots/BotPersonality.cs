// <copyright file="BotPersonality.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Bots;

using System;

/// <summary>
/// Defines the distinct personality archetypes for AI bots.
/// Each personality influences combat thresholds, shopping frequency, party acceptance, item pickup, and map navigation.
/// </summary>
public enum BotPersonality
{
    /// <summary>Balanced behavior using standard default settings.</summary>
    Balanced = 0,

    /// <summary>Greedy: Picks up all items, visits merchants 2x more frequently.</summary>
    Greedy = 1,

    /// <summary>Warrior: Aggressive, hunts stronger monsters, lower safety thresholds.</summary>
    Warrior = 2,

    /// <summary>Loner: Refuses party invitations from players, prefers solo hunting.</summary>
    Loner = 3,

    /// <summary>Guardian: Supports party members, high healing thresholds, stays in safe areas.</summary>
    Guardian = 4,

    /// <summary>Reckless: Extremely low heal/potion thresholds, hunts far above level.</summary>
    Reckless = 5,
}
