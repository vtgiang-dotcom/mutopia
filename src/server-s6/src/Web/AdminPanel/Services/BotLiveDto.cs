// <copyright file="BotLiveDto.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Services;

/// <summary>
/// DTO representing live state of an active bot player.
/// </summary>
public record BotLiveDto
{
    /// <summary>Gets the account login name.</summary>
    public string LoginName { get; init; } = string.Empty;

    /// <summary>Gets the account id.</summary>
    public Guid AccountId { get; init; }

    /// <summary>Gets the character name.</summary>
    public string CharacterName { get; init; } = string.Empty;

    /// <summary>Gets the character id.</summary>
    public Guid CharacterId { get; init; }

    /// <summary>Gets the character level.</summary>
    public int Level { get; init; }

    /// <summary>Gets the character reset count.</summary>
    public int ResetCount { get; init; }

    /// <summary>Gets the current map name.</summary>
    public string MapName { get; init; } = string.Empty;

    /// <summary>Gets the current party size (0 if solo).</summary>
    public int PartySize { get; init; }

    /// <summary>Gets estimated experience gained per hour.</summary>
    public long ExperiencePerHour { get; init; }

    /// <summary>Gets total experience gained since spawn.</summary>
    public long TotalExperience { get; init; }

    /// <summary>Gets uptime in seconds.</summary>
    public long UptimeSeconds { get; init; }

    /// <summary>Gets a value indicating whether bot AI is faulted.</summary>
    public bool IsFaulted { get; init; }

    /// <summary>Gets a value indicating whether bot awaits master restart.</summary>
    public bool AwaitsMasterRestart { get; init; }

    /// <summary>Gets personality description.</summary>
    public string Personality { get; init; } = string.Empty;

    /// <summary>Gets status tag (Normal, Faulted, MasterWait, Overpowered).</summary>
    public string StatusTag { get; init; } = "Normal";

    /// <summary>Gets the current map X coordinate.</summary>
    public byte PositionX { get; init; }

    /// <summary>Gets the current map Y coordinate.</summary>
    public byte PositionY { get; init; }

    /// <summary>Gets current health as a percentage of maximum (0-100).</summary>
    public int HealthPercent { get; init; }

    /// <summary>Gets current mana as a percentage of maximum (0-100).</summary>
    public int ManaPercent { get; init; }

    /// <summary>Gets a value indicating whether the bot is currently walking.</summary>
    public bool IsWalking { get; init; }
}

/// <summary>
/// DTO representing overall stats for bot system.
/// </summary>
public record BotDashboardStats
{
    /// <summary>Gets total active bots in runtime.</summary>
    public int TotalActive { get; init; }

    /// <summary>Gets total bot accounts in database.</summary>
    public int TotalInDatabase { get; init; }

    /// <summary>Gets faulted bot count.</summary>
    public int FaultedCount { get; init; }

    /// <summary>Gets count of bots currently in a party.</summary>
    public int InPartyCount { get; init; }

    /// <summary>Gets average level of active bots.</summary>
    public double AverageLevel { get; init; }

    /// <summary>Gets average EXP/h across all active bots.</summary>
    public long AverageExperiencePerHour { get; init; }

    /// <summary>Gets name of highest EXP/h farmer bot.</summary>
    public string TopFarmerName { get; init; } = string.Empty;

    /// <summary>Gets EXP/h rate of highest farmer bot.</summary>
    public long TopFarmerExpPerHour { get; init; }

    /// <summary>Gets total zen earned by all active bots.</summary>
    public long TotalZenEarned { get; init; }

    /// <summary>Gets total zen spent by all active bots.</summary>
    public long TotalZenSpent { get; init; }

    /// <summary>Gets total items picked up by all active bots.</summary>
    public long TotalItemsPickedUp { get; init; }

    /// <summary>Gets total items consumed by all active bots.</summary>
    public long TotalItemsConsumed { get; init; }

    /// <summary>Gets total items dropped by all active bots.</summary>
    public long TotalItemsDropped { get; init; }

    /// <summary>Gets total jewels picked up by all active bots.</summary>
    public long TotalJewelsPickedUp { get; init; }

    /// <summary>Gets total experience gained by all active bots since they started.</summary>
    public long TotalExperienceGained { get; init; }

    /// <summary>Gets breakdown of active bots per personality archetype.</summary>
    public Dictionary<string, int> PersonalityCounts { get; init; } = new();

    /// <summary>Gets breakdown of active bots per map.</summary>
    public Dictionary<string, int> MapCounts { get; init; } = new();

    /// <summary>Gets the target online count for the current player-local hour (presence curve).</summary>
    public int TargetOnline { get; init; }

    /// <summary>Gets the current hour in the player base's local time (UTC+7, 0-23).</summary>
    public int CurrentPlayerHour { get; init; }

    /// <summary>Gets the 24 target-online values (index 0 = player-local midnight) for the curve chart.</summary>
    public int[] TargetByHour { get; init; } = new int[24];
}
