// <copyright file="ChatRateLimiter.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.BotAI.Chat;

using System.Collections.Concurrent;

/// <summary>
/// Implements 4-layer rate-limiting / anti-flood protection for bot chat interactions.
/// </summary>
public class ChatRateLimiter
{
    private static readonly ConcurrentDictionary<string, DateTime> PlayerBotCooldowns = new();
    private static readonly ConcurrentDictionary<string, DateTime> BotGlobalCooldowns = new();
    private static readonly ConcurrentDictionary<string, int> MessageFanOutCounts = new();

    private static readonly object TokenLock = new();
    private static double Tokens = 30.0;
    private static DateTime LastRefillUtc = DateTime.UtcNow;
    private const double MaxCapacity = 30.0;
    private const double RefillRatePerSecond = 15.0;

    /// <summary>
    /// Checks if a bot can respond to a player message under 4-layer rate limits.
    /// </summary>
    /// <param name="botName">Name of the bot.</param>
    /// <param name="playerName">Name of the player sending chat.</param>
    /// <param name="messageId">Unique ID or hash of incoming chat message.</param>
    /// <returns><c>true</c> if request passes all 4 rate limit layers; otherwise <c>false</c>.</returns>
    public static bool TryAcquireChatSlot(string botName, string playerName, string messageId)
    {
        var now = DateTime.UtcNow;

        // Layer 1: Player-Bot Cooldown (4 seconds per pair)
        string pairKey = $"{playerName}_{botName}";
        if (PlayerBotCooldowns.TryGetValue(pairKey, out var lastPairChat) && (now - lastPairChat).TotalSeconds < 4.0)
        {
            return false;
        }

        // Layer 2: Bot Global Cooldown (2 seconds per bot)
        if (BotGlobalCooldowns.TryGetValue(botName, out var lastBotChat) && (now - lastBotChat).TotalSeconds < 2.0)
        {
            return false;
        }

        // Layer 3: Fan-out Cap (Max 2 bot responders per message)
        int currentResponders = MessageFanOutCounts.AddOrUpdate(messageId, 1, (_, count) => count + 1);
        if (currentResponders > 2)
        {
            return false;
        }

        // Layer 4: Global Token Bucket Rate Limiter (Capacity 30, Refill 15/s)
        lock (TokenLock)
        {
            RefillTokens(now);

            if (Tokens < 1.0)
            {
                return false; // Token bucket empty - drop response silently
            }

            Tokens -= 1.0;
        }

        // Update timestamps on success
        PlayerBotCooldowns[pairKey] = now;
        BotGlobalCooldowns[botName] = now;

        return true;
    }

    private static void RefillTokens(DateTime now)
    {
        double elapsedSeconds = (now - LastRefillUtc).TotalSeconds;
        if (elapsedSeconds > 0)
        {
            Tokens = Math.Min(MaxCapacity, Tokens + (elapsedSeconds * RefillRatePerSecond));
            LastRefillUtc = now;
        }
    }
}
