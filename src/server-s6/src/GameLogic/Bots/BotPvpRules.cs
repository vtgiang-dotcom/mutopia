// <copyright file="BotPvpRules.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Bots;

using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;

/// <summary>
/// Helper class defining legal PvP targeting rules for bot players.
/// Ensures bots never attack unprovoked or become outlaws unless in a designated PvP zone/event.
/// </summary>
public static class BotPvpRules
{
    /// <summary>
    /// Checks whether the target player is a legal PvP target for the bot.
    /// </summary>
    /// <param name="bot">The bot player.</param>
    /// <param name="target">The target player.</param>
    /// <returns><c>true</c> if the bot can legally attack the target; otherwise, <c>false</c>.</returns>
    public static bool IsLegalPvpTarget(Player bot, Player target)
    {
        if (bot == null || target == null || ReferenceEquals(bot, target) || !bot.IsAlive || !target.IsAlive)
        {
            return false;
        }

        // Safezone check
        if (bot.IsAtSafezone() || target.IsAtSafezone())
        {
            return false;
        }

        // Guild War check
        if (GuildWarEventChatCommandPlugIn.IsWarRunning)
        {
            return !GuildWarEventChatCommandPlugIn.IsInSameFaction(bot, target);
        }

        // Duel / Self defense / Outlaw check
        if (target.PlayerState.CurrentState == PlayerState.EnteredWorld)
        {
            // Outlaw / PK stage
            if (target.SelectedCharacter?.State is HeroState.PlayerKillWarning or HeroState.PlayerKiller1stStage or HeroState.PlayerKiller2ndStage)
            {
                return true;
            }

            // Duel target
            if (bot.DuelRoom != null && bot.DuelRoom == target.DuelRoom)
            {
                return true;
            }
        }

        return false;
    }
}