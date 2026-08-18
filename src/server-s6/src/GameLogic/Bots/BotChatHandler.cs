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

        var nearbyPlayer = bot.Observers.OfType<Player>().FirstOrDefault(p => p.Account?.IsBot != true);
        if (nearbyPlayer is not null && GreetedPlayers.TryAdd((botName, nearbyPlayer.Name), 0))
        {
            var r = Rand.NextInt(0, 3);
            message = personality switch
            {
                BotPersonality.Greedy => r switch
                {
                    0 => $"Chào {nearbyPlayer.Name}, cần đồ hay ngọc thì nói tui nha!",
                    1 => $"Ê {nearbyPlayer.Name}, có ngọc rớt nhớ để tui nhặt nhé!",
                    _ => $"Hế lô {nearbyPlayer.Name}, chúc lượm được nhiều Bless nha!"
                },
                BotPersonality.Guardian => r switch
                {
                    0 => $"Chào {nearbyPlayer.Name}! Đi pt đỡ đòn thì gọi tui nhé.",
                    1 => $"Ông {nearbyPlayer.Name} cần buff hay tank thì hú một tiếng.",
                    _ => $"Yo {nearbyPlayer.Name}! Đứng gần tui cho an toàn."
                },
                BotPersonality.Warrior => r switch
                {
                    0 => $"Yo {nearbyPlayer.Name}, cày vui nha!",
                    1 => $"Xin chào {nearbyPlayer.Name}, ráng ks với tui nha!",
                    _ => $"Cùng cày mau lên cấp nhé {nearbyPlayer.Name}!"
                },
                _ => r switch
                {
                    0 => $"Chào {nearbyPlayer.Name}!",
                    1 => $"Xin chào {nearbyPlayer.Name}, dạo này khỏe không?",
                    _ => $"Cày chăm chỉ nha {nearbyPlayer.Name}!"
                }
            };
        }
        else if (inSafezone)
        {
            var r = Rand.NextInt(0, 3);
            message = personality switch
            {
                BotPersonality.Greedy => r switch
                {
                    0 => "Ai có Jewel of Bless bán rẻ không?",
                    1 => "Gom được mớ rác, để đi phi shop kiếm zen cái.",
                    _ => "Dạo này ngọc rớt ít quá, nghèo thật sự."
                },
                BotPersonality.Guardian => r switch
                {
                    0 => "Đang rảnh, ai cần pt thì hú nhé.",
                    1 => "Hôm nay server đông vui ghê.",
                    _ => "Ai đi Blood Castle không, lập team nào."
                },
                BotPersonality.Warrior => r switch
                {
                    0 => "Nghỉ chút lấy sức rồi cày tiếp.",
                    1 => "Vừa sửa đồ tốn zen quá...",
                    _ => "Cắm chuột mỏi tay ghê, nghỉ ngơi xíu."
                },
                _ => r switch
                {
                    0 => "Đứng đây nghỉ ngơi chút.",
                    1 => "Lâu lắm không về thành, thay đổi nhiều quá.",
                    _ => "Mọi người cày kéo chăm chỉ ghê ta."
                }
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
                var r = Rand.NextInt(0, 3);
                replyText = r switch
                {
                    0 => $"Tui thích đi solo hơn, cảm ơn {sender.Name} nhé!",
                    1 => $"Đang muốn cày một mình, sory {sender.Name}.",
                    _ => $"Thôi {sender.Name} cứ cày đi, tui tự chơi được."
                };
            }
            else
            {
                var scheduled = await BotPartyHandler.TryScheduleAcceptAsync(bot, sender).ConfigureAwait(false);
                var r = Rand.NextInt(0, 2);
                replyText = scheduled
                    ? (r == 0 ? $"Ok {sender.Name}, chờ tui chút nhận pt nha!" : $"Nhất trí {sender.Name}, pt đông cho vui.")
                    : (r == 0 ? $"Tui đang bận hoặc nhóm đầy rồi {sender.Name} ơi." : $"Để lúc khác nhé {sender.Name}, giờ chưa tiện.");
            }
        }
        else if (text.Contains("chao") || text.Contains("hello") || text.Contains("hi") || text.Contains("alo") || text.Contains("ê") || text.Contains("e!"))
        {
            var r = Rand.NextInt(0, 3);
            replyText = r switch
            {
                0 => $"Chào {sender.Name}! Chúc cày cuốc lượm nhiều ngọc nhé!",
                1 => $"Hello {sender.Name}, cày lv vui nha.",
                _ => $"Alo {sender.Name}, có chuyện gì không bồ tèo?"
            };
        }
        else if (text.Contains("level") || text.Contains("lv") || text.Contains("map") || text.Contains("o dau"))
        {
            var level = (short)(bot.Attributes?[Attributes.Stats.Level] ?? 1);
            var mapName = bot.CurrentMap?.Definition?.Name ?? "bản đồ";
            var r = Rand.NextInt(0, 2);
            replyText = r == 0
                ? $"Tui level {level} đang cày ở {mapName} nè."
                : $"Mới lv {level} thôi {sender.Name}, vẫn loanh quanh ở {mapName}.";
        }
        else if (text.Contains("zen") || text.Contains("xin ngoc") || text.Contains("do") || text.Contains("cho "))
        {
            var r = Rand.NextInt(0, 2);
            replyText = personality == BotPersonality.Greedy
                ? (r == 0 ? $"Tui cũng đang gom ngọc bán nè {sender.Name}, đâu ra mà cho!" : $"Đang nghèo kiết xác đây {sender.Name}, tự cày đi nha.")
                : (r == 0 ? $"Tui chưa có đồ thừa đâu {sender.Name} ơi." : $"Đồ trên người tui còn lởm đây này, thông cảm nha.");
        }
        else if (text.Contains("pk") || text.Contains("danh") || text.Contains("giêt") || text.Contains("solo"))
        {
            var r = Rand.NextInt(0, 3);
            replyText = r switch
            {
                0 => $"Thôi tui chỉ khoái cày cuốc thôi, tha cho tui {sender.Name} ơi.",
                1 => $"Muốn pk thì tìm người khác nha {sender.Name}, tui đang treo máy.",
                _ => $"Anh hùng hảo hán đừng bắt nạt tui, hihi."
            };
        }
        else if (text.Contains("lag") || text.Contains("giat") || text.Contains("dis"))
        {
            var r = Rand.NextInt(0, 2);
            replyText = r == 0
                ? $"Hơi lag xíu nhưng vẫn cắm chuột tốt {sender.Name} ạ."
                : $"Chắc do mạng ông chứ tui vẫn chơi mượt mà.";
        }
        else if (text.Contains("bot") || text.Contains("auto") || text.Contains("may") || text.Contains("npc"))
        {
            var r = Rand.NextInt(0, 3);
            replyText = r switch
            {
                0 => $"Tui là người chơi chân chính nha, auto hồi nào ba!",
                1 => $"Đang uống cà phê lướt web thôi, sao {sender.Name} kêu tui là bot?",
                _ => $"Cắm chuột MU xíu mà ai cũng kêu tui là bot, rầu ghê."
            };
        }
        else if (text.Contains("trade") || text.Contains("mua") || text.Contains("ban") || text.Contains("gia"))
        {
            var r = Rand.NextInt(0, 3);
            replyText = r switch
            {
                0 => $"Tui đang cày nên không trade đâu {sender.Name}, treo máy đi làm rồi.",
                1 => $"Không mua bán gì nhé {sender.Name}, toàn đồ cùi bắp thôi.",
                _ => $"Chưa nhặt được món nào có giá trị {sender.Name} ơi, bao giờ có tui gọi."
            };
        }
        else if (text.Contains("rs") || text.Contains("reset"))
        {
            var rsCount = bot.SelectedCharacter?.Attributes.FirstOrDefault(a => a.Definition == Attributes.Stats.Resets)?.Value ?? 0;
            var r = Rand.NextInt(0, 2);
            replyText = r == 0
                ? $"Tui mới reset {rsCount} lần à, còn yếu xìu."
                : $"Vẫn lẹt đẹt ở mốc rs {rsCount} {sender.Name} ạ, phải cày gấp.";
        }
        else if (text.Contains("g") || text.Contains("guild") || text.Contains("gui") || text.Contains("lm") || text.Contains("lien minh"))
        {
            var r = Rand.NextInt(0, 2);
            replyText = r == 0
                ? $"Giờ tui chưa vào guild nào, thích đi solo hơn."
                : $"Tui gà mờ chả ai thèm cho vào guild {sender.Name} ơi.";
        }
        else if (text.Contains("bc") || text.Contains("dv") || text.Contains("cc") || text.Contains("su kien") || text.Contains("event"))
        {
            var r = Rand.NextInt(0, 3);
            replyText = r switch
            {
                0 => $"Tui lười đi event lắm, treo bãi này cho lành.",
                1 => $"Mới bị ks văng vé Blood Castle xong, buồn ghê {sender.Name}.",
                _ => $"Đi Devil Square mà lag đơ máy luôn, thôi chừa rồi."
            };
        }
        else
        {
            var r = Rand.NextInt(0, 3);
            replyText = r switch
            {
                0 => $"Cùng cày MU với {sender.Name} cho xôm nhé!",
                1 => $"Haha, ừ {sender.Name} nói chí phải.",
                _ => $"Tui đang treo máy xíu, lát rep sau nha."
            };
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
