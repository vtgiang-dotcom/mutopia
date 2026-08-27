// <copyright file="GuildWarEventChatCommandPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.PlugIns.ChatCommands.Arguments;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// A chat command plugin which controls guild war event state.
/// </summary>
[Guid("23C6E159-0D5E-44DE-8CF8-012A7278D42E")]
[PlugIn]
[ChatCommandHelp(Command, typeof(EmptyChatCommandArgs), CharacterStatus.GameMaster)]
public class GuildWarEventChatCommandPlugIn : ChatCommandPlugInBase<EmptyChatCommandArgs>
{
    private const string Command = "/warevent";

    /// <summary>
    /// Gets or sets a value indicating whether a global guild war is currently running.
    /// </summary>
    public static bool IsWarRunning { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether active combat has started.
    /// </summary>
    public static bool IsCombatStarted { get; set; }

    /// <summary>
    /// Checks whether the specified player is a defender bot.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns><c>true</c> if defender bot; otherwise, <c>false</c>.</returns>
    public static bool IsDefenderBot(Player player)
    {
        return player != null && player.GuildStatus?.GuildId == 1;
    }

    /// <summary>
    /// Checks whether two players belong to the same war faction.
    /// </summary>
    /// <param name="player1">The first player.</param>
    /// <param name="player2">The second player.</param>
    /// <returns><c>true</c> if in the same faction; otherwise, <c>false</c>.</returns>
    public static bool IsInSameFaction(Player player1, Player player2)
    {
        if (player1 == null || player2 == null)
        {
            return false;
        }

        if (ReferenceEquals(player1, player2))
        {
            return true;
        }

        if (player1.GuildStatus != null && player2.GuildStatus != null)
        {
            return player1.GuildStatus.GuildId == player2.GuildStatus.GuildId;
        }

        return false;
    }

    /// <inheritdoc />
    public override string Key => Command;

    /// <inheritdoc/>
    public override CharacterStatus MinCharacterStatusRequirement => CharacterStatus.GameMaster;

    /// <inheritdoc />
    protected override async ValueTask DoHandleCommandAsync(Player gameMaster, EmptyChatCommandArgs arguments)
    {
        IsWarRunning = !IsWarRunning;
        IsCombatStarted = IsWarRunning;
        string message = IsWarRunning ? "Guild War Event is now ACTIVE!" : "Guild War Event is now STOPPED.";
        await gameMaster.ShowBlueMessageAsync(message).ConfigureAwait(false);
    }
}
