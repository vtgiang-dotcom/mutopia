// <copyright file="BotChatHandler.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Bots;

using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using MUnique.OpenMU.GameLogic.Offline;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Plugin that enables active server-side AI bots to receive, understand, and reply to player chat.
/// Integrates with <see cref="BotPartyHandler"/> to perform real actions (no fake promises),
/// broadcasts replies to all nearby observers, and applies TTL-pruned rate limiting.
/// </summary>
[PlugIn]
[Display(Name = "Bot Chat Handler", Description = "Enables active server-side AI bots to receive, understand, and reply to player chat messages.")]
[Guid("A1B2C3D4-E5F6-7890-1234-56789ABCDEF0")]
public class BotChatHandler : IChatMessageReceivedPlugIn
{
    private static readonly ConcurrentDictionary<(string Sender, string Bot), DateTime> Cooldowns = new();
    private static readonly TimeSpan CooldownDuration = TimeSpan.FromSeconds(4);

    /// <summary>How often a bot may consider a proactive (unsolicited) message.</summary>
    private static readonly TimeSpan ProactiveInterval = TimeSpan.FromSeconds(45);

    /// <summary>Per-bot time until the next proactive chat check.</summary>
    private static readonly ConcurrentDictionary<string, DateTime> ProactiveCooldowns = new();

    /// <summary>Bots already greeted a player at least once (keyed by bot name + player name), so the first meeting stays special.</summary>
    private static readonly ConcurrentDictionary<(string Bot, string Player), byte> GreetedPlayers = new();

    /// <inheritdoc/>
    public void ChatMessageReceived(Player sender, string message, CancelEventArgs eventArgs)
    {
        if (sender is null || sender is OfflinePlayer || string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        if (sender.CurrentMap is not { } map)
        {
            return;
        }

        var normalizedMessage = message.Trim();

        // Find active bots on the same map within 10 tiles, or mentioned by name
        var nearbyBots = map.GetAttackablesInRange(sender.Position, 10)
            .OfType<OfflinePlayer>()
            .Where(b => b.Account?.IsBot == true && b.IsAlive)
            .ToList();

        if (nearbyBots.Count == 0)
        {
            return;
        }

        // Target a specific bot if mentioned by name, or pick a random nearby bot
        var targetBot = nearbyBots.FirstOrDefault(b => normalizedMessage.Contains(b.Name, StringComparison.OrdinalIgnoreCase))
                        ?? nearbyBots[Rand.NextInt(0, nearbyBots.Count)];

        if (!TryAcquireSlot(sender.Name, targetBot.Name))
        {
            return;
        }

        _ = HandleBotResponseAsync(sender, targetBot, normalizedMessage);
    }

    /// <summary>
    /// Broadcasts a proactive or event-triggered chat message from a bot to all nearby players.
    /// </summary>
    /// <param name="bot">The bot player.</param>
    /// <param name="message">The chat message content.</param>
    public static async ValueTask BroadcastBotMessageAsync(Player bot, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        var senderName = bot.SelectedCharacter?.Name ?? bot.Name;
        await bot.ForEachWorldObserverAsync<IChatViewPlugIn>(
            p => p.ChatMessageAsync(message, senderName, ChatMessageType.Normal), true).ConfigureAwait(false);
    }

    /// <summary>
    /// Lets a bot spontaneously speak once in a while - greeting a player it meets for the first time, or
    /// making small talk while idling in a safezone - so the world does not stay silent around players.
    /// Called from the bot's navigation tick (see <see cref="BotNavigator"/>), rate-limited and personality-
    /// aware. A Loner stays quiet, a Greedy bot pitches its shopping, others greet.
    /// </summary>
    /// <param name="bot">The bot player.</param>
    /// <param name="inSafezone">Whether the bot currently stands in a safezone.</param>
    public static void ConsiderProactiveChat(Player bot, bool inSafezone)
    {
        if (bot is not OfflinePlayer || bot.Account?.IsBot != true || !bot.IsAlive || bot.CurrentMap is null)
        {
            return;
        }

        var botName = bot.SelectedCharacter?.Name ?? bot.Name;
        var personality = (bot.MuHelperSettings as BotPersonalitySettings)?.Personality
                          ?? BotPersonalityResolver.Resolve(bot.Name);

        // The Loner speaks only when spoken to - that is exactly its archetype.
        if (personality == BotPersonality.Loner)
        {
            return;
        }

        var now = DateTime.UtcNow;
        if (ProactiveCooldowns.TryGetValue(botName, out var next) && now < next)
        {
            return;
        }

        if (ProactiveCooldowns.Count > 2000)
        {
            foreach (var kvp in ProactiveCooldowns)
            {
                if (kvp.Value <= now)
                {
                    ProactiveCooldowns.TryRemove(kvp.Key, out _);
                }
            }
        }

        // Only a modest chance per check, so speech stays occasional rather than a constant murmur.
        if (!Rand.NextRandomBool(12))
        {
            return;
        }

        string? message = null;

        // Greet a nearby player the bot has never met; otherwise idle chatter while in town.
        var nearbyPlayer = bot.CurrentMap.GetAttackablesInRange(bot.Position, 8)
            .OfType<Player>()
            .FirstOrDefault(p => p is not OfflinePlayer && p.IsAlive);
        if (nearbyPlayer is not null && GreetedPlayers.TryAdd((botName, nearbyPlayer.Name), 0))
        {
            message = personality switch
            {
                BotPersonality.Greedy => $"Chào {nearbyPlayer.Name}, cần đồ hay ngọc thì nói tui nha!",
                BotPersonality.Guardian => $"Chào {nearbyPlayer.Name}! Đi pt đỡ đòn thì gọi tui nhé.",
                BotPersonality.Warrior => $"Yo {nearbyPlayer.Name}, cày vui nha!",
                _ => $"Chào {nearbyPlayer.Name}!",
            };
        }
        else if (inSafezone)
        {
            message = personality switch
            {
                BotPersonality.Greedy => "Ai có Jewel of Bless bán rẻ không?",
                BotPersonality.Guardian => "Đang rảnh, ai cần pt thì hú nhé.",
                BotPersonality.Warrior => "Nghỉ chút lấy sức rồi cày tiếp.",
                _ => "Đứng đây nghỉ ngơi chút.",
            };
        }

        if (message is null)
        {
            return;
        }

        ProactiveCooldowns[botName] = now + ProactiveInterval + TimeSpan.FromSeconds(Rand.NextInt(0, 30));
        _ = BroadcastBotMessageAsync(bot, message);
    }

    private static async Task HandleBotResponseAsync(Player sender, OfflinePlayer bot, string message)
    {
        // Humanized typing delay (500ms - 1200ms)
        await Task.Delay(Rand.NextInt(500, 1200)).ConfigureAwait(false);

        if (!bot.IsAlive || bot.CurrentMap is null)
        {
            return;
        }

        var text = message.ToLowerInvariant();
        var personality = (bot.MuHelperSettings as BotPersonalitySettings)?.Personality
                          ?? BotPersonalityResolver.Resolve(bot.Name);

        string replyText;

        // Intent matching & real action execution
        if (text.Contains("pt") || text.Contains("party") || text.Contains("nhom") || text.Contains("vong"))
        {
            if (personality == BotPersonality.Loner)
            {
                replyText = $"Tui thích đi solo hơn, cảm ơn {sender.Name} nhé!";
            }
            else
            {
                var scheduled = await BotPartyHandler.TryScheduleAcceptAsync(bot, sender).ConfigureAwait(false);
                replyText = scheduled
                    ? $"Ok {sender.Name}, chờ tui chút nhận pt nha!"
                    : $"Tui đang bận hoặc nhóm đầy rồi {sender.Name} ơi.";
            }
        }
        else if (text.Contains("chao") || text.Contains("hello") || text.Contains("hi") || text.Contains("alo"))
        {
            replyText = $"Chào {sender.Name}! Chúc cày cuốc lượm nhiều ngọc nhé!";
        }
        else if (text.Contains("level") || text.Contains("lv") || text.Contains("map") || text.Contains("o dau"))
        {
            var level = (short)(bot.Attributes?[Attributes.Stats.Level] ?? 1);
            var mapName = bot.CurrentMap?.Definition?.Name ?? "bản đồ";
            replyText = $"Tui level {level} đang cày ở {mapName} nè.";
        }
        else if (text.Contains("zen") || text.Contains("xin ngoc") || text.Contains("do"))
        {
            replyText = personality == BotPersonality.Greedy
                ? $"Tui cũng đang gom ngọc bán nè {sender.Name}, đâu ra mà cho!"
                : $"Tui chưa có đồ thừa đâu {sender.Name} ơi.";
        }
        else
        {
            replyText = $"Cùng cày MU với {sender.Name} cho xôm nhé!";
        }

        await BroadcastBotMessageAsync(bot, replyText).ConfigureAwait(false);
    }

    private static bool TryAcquireSlot(string senderName, string botName)
    {
        PruneExpiredCooldowns();

        var key = (senderName, botName);
        var now = DateTime.UtcNow;

        if (Cooldowns.TryGetValue(key, out var expiry) && now < expiry)
        {
            return false;
        }

        Cooldowns[key] = now + CooldownDuration;
        return true;
    }

    private static void PruneExpiredCooldowns()
    {
        if (Cooldowns.Count < 200)
        {
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var kvp in Cooldowns)
        {
            if (kvp.Value <= now)
            {
                Cooldowns.TryRemove(kvp.Key, out _);
            }
        }
    }
}
