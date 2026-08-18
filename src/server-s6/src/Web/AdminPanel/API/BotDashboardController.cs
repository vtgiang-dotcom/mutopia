// <copyright file="BotDashboardController.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.API;

using Microsoft.AspNetCore.Mvc;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.Bots;
using MUnique.OpenMU.GameServer;
using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.Web.AdminPanel.Services;

/// <summary>
/// API controller providing live monitoring and administration endpoints for Bot AI.
/// </summary>
[Route("api/bots")]
public class BotDashboardController : Controller
{
    private readonly IDictionary<int, IGameServer> _gameServers;

    /// <summary>
    /// Initializes a new instance of the <see cref="BotDashboardController"/> class.
    /// </summary>
    /// <param name="gameServers">The registered game servers.</param>
    public BotDashboardController(IDictionary<int, IGameServer> gameServers)
    {
        this._gameServers = gameServers;
    }

    /// <summary>
    /// Gets a list of all currently active bots in runtime with live metrics.
    /// </summary>
    /// <returns>A collection of <see cref="BotLiveDto"/>.</returns>
    [HttpGet]
    [Route("live")]
    public IActionResult GetLiveBots()
    {
        var botPlugin = this.GetBotPlugin();
        if (botPlugin is null)
        {
            return this.Ok(Array.Empty<BotLiveDto>());
        }

        var activeBots = botPlugin.GetAllActiveBots();
        var dtos = activeBots.Select(bot =>
        {
            var loginName = bot.Account?.LoginName ?? "N/A";
            var charName = bot.SelectedCharacter?.Name ?? bot.Name;
            var level = (int)(bot.Attributes?[Stats.Level] ?? 1);
            var resetCount = bot.SelectedCharacter is { } c ? (int)(c.Attributes.FirstOrDefault(a => a.Definition == Stats.Resets)?.Value ?? 0) : 0;
            var mapName = bot.CurrentMap?.Definition.Name ?? "Unknown";
            var partySize = bot.Party?.PartyList.Count ?? 0;

            var uptimeSeconds = Math.Max(1, (long)(DateTime.UtcNow - bot.StartedAt).TotalSeconds);
            var expPerHour = (long)(bot.ExperienceGainedSinceStart / (uptimeSeconds / 3600.0));

            string statusTag = "Normal";
            if (bot.AwaitsFaultRestart)
            {
                statusTag = "Faulted";
            }
            else if (bot.AwaitsMasterRestart)
            {
                statusTag = "MasterWait";
            }
            else if (expPerHour > 5_000_000)
            {
                statusTag = "Overpowered";
            }

            return new BotLiveDto
            {
                LoginName = loginName,
                CharacterName = charName,
                Level = level,
                ResetCount = resetCount,
                MapName = mapName,
                PartySize = partySize,
                ExperiencePerHour = expPerHour,
                TotalExperience = bot.ExperienceGainedSinceStart,
                UptimeSeconds = uptimeSeconds,
                IsFaulted = bot.AwaitsFaultRestart,
                AwaitsMasterRestart = bot.AwaitsMasterRestart,
                Personality = "Balanced",
                StatusTag = statusTag,
            };
        }).ToList();

        return this.Ok(dtos);
    }

    /// <summary>
    /// Gets overall system statistics for bot system.
    /// </summary>
    [HttpGet]
    [Route("stats")]
    public IActionResult GetStats()
    {
        var botPlugin = this.GetBotPlugin();
        if (botPlugin is null)
        {
            return this.Ok(new BotDashboardStats());
        }

        var activeBots = botPlugin.GetAllActiveBots();

        long topExp = 0;
        string topFarmer = "N/A";
        long totalExpPerHour = 0;
        
        long totalZenEarned = 0;
        long totalZenSpent = 0;
        long totalItemsPickedUp = 0;
        long totalItemsConsumed = 0;
        long totalItemsDropped = 0;
        long totalJewelsPickedUp = 0;
        long totalExperienceGained = 0;

        foreach (var bot in activeBots)
        {
            var uptimeHours = Math.Max(0.001, (DateTime.UtcNow - bot.StartedAt).TotalHours);
            var expPerHour = (long)(bot.ExperienceGainedSinceStart / uptimeHours);
            totalExpPerHour += expPerHour;
            
            totalZenEarned += bot.ZenEarnedSinceStart;
            totalZenSpent += bot.ZenSpentSinceStart;
            totalItemsPickedUp += bot.ItemsPickedUpSinceStart;
            totalItemsConsumed += bot.ItemsConsumedSinceStart;
            totalItemsDropped += bot.ItemsDroppedSinceStart;
            totalJewelsPickedUp += bot.JewelsPickedUpSinceStart;
            totalExperienceGained += bot.ExperienceGainedSinceStart;

            if (expPerHour > topExp)
            {
                topExp = expPerHour;
                topFarmer = bot.SelectedCharacter?.Name ?? bot.Name;
            }
        }

        var stats = new BotDashboardStats
        {
            TotalActive = activeBots.Count,
            FaultedCount = activeBots.Count(b => b.AwaitsFaultRestart),
            InPartyCount = activeBots.Count(b => b.Party is not null),
            AverageLevel = activeBots.Count > 0 ? activeBots.Average(b => b.Attributes?[Stats.Level] ?? 1) : 0,
            AverageExperiencePerHour = activeBots.Count > 0 ? totalExpPerHour / activeBots.Count : 0,
            TopFarmerName = topFarmer,
            TopFarmerExpPerHour = topExp,
            TotalZenEarned = totalZenEarned,
            TotalZenSpent = totalZenSpent,
            TotalItemsPickedUp = totalItemsPickedUp,
            TotalItemsConsumed = totalItemsConsumed,
            TotalItemsDropped = totalItemsDropped,
            TotalJewelsPickedUp = totalJewelsPickedUp,
            TotalExperienceGained = totalExperienceGained,
        };

        return this.Ok(stats);
    }

    /// <summary>
    /// Stops a specific bot by login name.
    /// </summary>
    /// <param name="loginName">The login name of the bot account.</param>
    [HttpDelete]
    [Route("{loginName}")]
    public async Task<IActionResult> KillBotAsync(string loginName)
    {
        var botPlugin = this.GetBotPlugin();
        if (botPlugin is null)
        {
            return this.BadRequest("Bot plugin not active");
        }

        var stopped = await botPlugin.KillBotAsync(loginName).ConfigureAwait(false);
        if (stopped)
        {
            return this.Ok(new { message = $"Bot '{loginName}' successfully killed." });
        }

        return this.NotFound(new { message = $"Bot '{loginName}' not found among active bots." });
    }

    /// <summary>
    /// Kills all bots that are currently in faulted state.
    /// </summary>
    [HttpPost]
    [Route("kill-all-faulted")]
    public async Task<IActionResult> KillAllFaultedAsync()
    {
        var botPlugin = this.GetBotPlugin();
        if (botPlugin is null)
        {
            return this.BadRequest("Bot plugin not active");
        }

        var activeBots = botPlugin.GetAllActiveBots();
        var faultedBots = activeBots.Where(b => b.AwaitsFaultRestart).ToList();

        int count = 0;
        foreach (var bot in faultedBots)
        {
            var login = bot.Account?.LoginName;
            if (login is not null && await botPlugin.KillBotAsync(login).ConfigureAwait(false))
            {
                count++;
            }
        }

        return this.Ok(new { message = $"Killed {count} faulted bot(s)." });
    }

    /// <summary>
    /// Kills all bots whose EXP/h exceeds specified threshold (default 5,000,000 EXP/h).
    /// </summary>
    [HttpPost]
    [Route("kill-overpowered")]
    public async Task<IActionResult> KillOverpoweredAsync([FromQuery] long threshold = 5_000_000)
    {
        var botPlugin = this.GetBotPlugin();
        if (botPlugin is null)
        {
            return this.BadRequest("Bot plugin not active");
        }

        var activeBots = botPlugin.GetAllActiveBots();
        int count = 0;

        foreach (var bot in activeBots)
        {
            var uptimeHours = Math.Max(0.001, (DateTime.UtcNow - bot.StartedAt).TotalHours);
            var expPerHour = (long)(bot.ExperienceGainedSinceStart / uptimeHours);

            if (expPerHour > threshold)
            {
                var login = bot.Account?.LoginName;
                if (login is not null && await botPlugin.KillBotAsync(login).ConfigureAwait(false))
                {
                    count++;
                }
            }
        }

        return this.Ok(new { message = $"Killed {count} overpowered bot(s) exceeding {threshold:N0} EXP/h." });
    }

    private BotFeaturePlugIn? GetBotPlugin()
    {
        var server = this._gameServers.Values.OfType<GameServer>().FirstOrDefault();
        return server?.Context.FeaturePlugIns.GetPlugIn<BotFeaturePlugIn>();
    }
}
