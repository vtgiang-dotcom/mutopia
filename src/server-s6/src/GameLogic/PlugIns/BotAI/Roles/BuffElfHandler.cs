// <copyright file="BuffElfHandler.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.BotAI.Roles;

using MUnique.OpenMU.GameLogic;

/// <summary>
/// Handles buff actions for Buff Elf Bots when requested by nearby players.
/// </summary>
public class BuffElfHandler
{
    /// <summary>
    /// Checks if specified player is eligible for Elf Buffs.
    /// </summary>
    /// <param name="elfBot">The elf bot player.</param>
    /// <param name="targetPlayer">The target player to receive buff.</param>
    /// <returns><c>true</c> if buff can be cast; otherwise <c>false</c>.</returns>
    public static bool CanCastBuff(Player elfBot, Player targetPlayer)
    {
        if (elfBot is null || targetPlayer is null)
        {
            return false;
        }

        if (Equals(elfBot, targetPlayer))
        {
            return false;
        }

        // Distance Check: Must be within 8 tiles
        return elfBot.IsInRange(targetPlayer.Position, 8);
    }
}
