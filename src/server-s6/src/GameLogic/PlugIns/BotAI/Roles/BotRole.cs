// <copyright file="BotRole.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.BotAI.Roles;

/// <summary>
/// Specialized roles for OpenMU AI Bots.
/// </summary>
public enum BotRole
{
    /// <summary>
    /// Farmer bot (focuses on farming monsters and looting).
    /// </summary>
    Farmer,

    /// <summary>
    /// Buff Elf bot (focuses on casting Greater Defense/Damage/Heal on nearby players).
    /// </summary>
    BuffElf,

    /// <summary>
    /// PK Guard bot (focuses on retaliating and defending against hostile PK players).
    /// </summary>
    PkGuard,

    /// <summary>
    /// Trader bot (focuses on automated jewel trading).
    /// </summary>
    Trader,
}
