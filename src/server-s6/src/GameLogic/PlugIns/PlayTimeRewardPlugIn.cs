// <copyright file="PlayTimeRewardPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Awards Goblin Points to real players (non-bot, non-template accounts) on a fixed interval
/// while they are actively in the game world (<see cref="PlayerState.EnteredWorld"/>).
/// </summary>
[PlugIn]
[Display(Name = "Play Time Reward", Description = "Awards Goblin Points to real online players at a fixed interval. Bot accounts are excluded.")]
[Guid("422792AB-E206-4274-9CA8-6BE6CCA91DFB")]
public sealed class PlayTimeRewardPlugIn : IPeriodicTaskPlugIn, ISupportCustomConfiguration<PlayTimeRewardConfiguration>, ISupportDefaultCustomConfiguration
{
    private DateTime _nextRunUtc = DateTime.UtcNow;

    /// <inheritdoc />
    public PlayTimeRewardConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new PlayTimeRewardConfiguration();

    /// <inheritdoc />
    public void ForceStart()
    {
        this._nextRunUtc = DateTime.UtcNow;
    }

    /// <inheritdoc />
    public async ValueTask ExecuteTaskAsync(GameContext gameContext)
    {
        if (DateTime.UtcNow < this._nextRunUtc)
        {
            return;
        }

        var configuration = this.Configuration ??= new PlayTimeRewardConfiguration();
        this._nextRunUtc = DateTime.UtcNow + configuration.Interval;

        var logger = gameContext.LoggerFactory.CreateLogger(nameof(PlayTimeRewardPlugIn));
        try
        {
            var players = await gameContext.GetPlayersAsync().ConfigureAwait(false);
            int rewarded = 0;
            foreach (var player in players)
            {
                if (player.Account is null)
                {
                    continue;
                }

                if (player.Account.IsBot || player.Account.IsTemplate)
                {
                    continue;
                }

                if (player.PlayerState.CurrentState != PlayerState.EnteredWorld)
                {
                    continue;
                }

                player.Account.GoblinPoints += configuration.PointsPerInterval;
                rewarded++;
            }

            if (rewarded > 0)
            {
                logger.LogInformation("PlayTimeReward: awarded {Points} Goblin Points to {Count} players.", configuration.PointsPerInterval, rewarded);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error in PlayTimeRewardPlugIn.");
        }
    }
}
