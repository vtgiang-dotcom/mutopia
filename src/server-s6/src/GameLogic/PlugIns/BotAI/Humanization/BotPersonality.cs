// <copyright file="BotPersonality.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.BotAI.Humanization;

/// <summary>
/// Defines emotional state of a bot.
/// </summary>
public enum BotMood
{
    /// <summary>
    /// Friendly and happy.
    /// </summary>
    Happy,

    /// <summary>
    /// Neutral state.
    /// </summary>
    Neutral,

    /// <summary>
    /// Annoyed or frustrated (e.g. after being PK'd or KS'd).
    /// </summary>
    Annoyed,
}

/// <summary>
/// Defines individual personality traits for a Bot.
/// </summary>
public class BotPersonality
{
    /// <summary>
    /// Gets or sets sociability score (0 - 100). Higher = more likely to chat & party.
    /// </summary>
    public int Sociability { get; set; } = 70;

    /// <summary>
    /// Gets or sets aggression score (0 - 100). Higher = more likely to attack back.
    /// </summary>
    public int Aggression { get; set; } = 30;

    /// <summary>
    /// Gets or sets patience score (0 to 100). Higher = stays longer at crowded spots.
    /// </summary>
    public int Patience { get; set; } = 50;

    /// <summary>
    /// Gets or sets current emotional mood.
    /// </summary>
    public BotMood Mood { get; set; } = BotMood.Neutral;

    /// <summary>
    /// Generates a randomized personality profile for a new bot.
    /// </summary>
    /// <returns>A new <see cref="BotPersonality"/> instance.</returns>
    public static BotPersonality CreateRandom()
    {
        var rng = Random.Shared;
        return new BotPersonality
        {
            Sociability = rng.Next(20, 95),
            Aggression = rng.Next(10, 85),
            Patience = rng.Next(30, 90),
            Mood = (BotMood)rng.Next(0, 3),
        };
    }
}
