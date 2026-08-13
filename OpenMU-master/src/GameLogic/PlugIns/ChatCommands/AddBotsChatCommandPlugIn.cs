// <copyright file="AddBotsChatCommandPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;

using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using MUnique.OpenMU.GameLogic.Bots;
using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Chat command to generate and spawn server-side AI bots into the game world.
/// Usage: /addbots 5
/// </summary>
[Guid("F98E1A2B-3C4D-5E6F-7A8B-9C0D1E2F3A4B")]
[PlugIn]
[Display(Name = "Add Bots", Description = "Spawns server-side AI bots into the game world.")]
[ChatCommandHelp(CommandKey, null, CharacterStatus.GameMaster)]
public class AddBotsChatCommandPlugIn : IChatCommandPlugIn
{
    private const string CommandKey = "/addbots";

    /// <inheritdoc />
    public string Key => CommandKey;

    /// <inheritdoc />
    public CharacterStatus MinCharacterStatusRequirement => CharacterStatus.GameMaster;

    /// <inheritdoc />
    public async ValueTask HandleCommandAsync(Player player, string command)
    {
        var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var count = 5;

        if (parts.Length > 1 && int.TryParse(parts[1], out var parsedCount) && parsedCount > 0)
        {
            count = Math.Min(parsedCount, 50);
        }

        await player.ShowBlueMessageAsync($"[BotManager] Generating {count} AI bots...").ConfigureAwait(false);

        try
        {
            var generator = new BotGenerator(player.GameContext, player.Logger);
            var created = await generator.EnsureBotsAsync(count, 1).ConfigureAwait(false);

            await player.ShowBlueMessageAsync($"[BotManager] Spawned AI bots (Created {created} new accounts).").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            player.Logger.LogError(ex, "Failed to spawn bots via chat command.");
            await player.ShowBlueMessageAsync("[BotManager] Error spawning bots: " + ex.Message).ConfigureAwait(false);
        }
    }
}
