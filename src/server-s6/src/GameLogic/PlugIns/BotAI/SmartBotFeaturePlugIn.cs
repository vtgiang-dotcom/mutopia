// <copyright file="SmartBotFeaturePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.BotAI;

using System.Collections.Concurrent;
using System.ComponentModel;
using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameLogic.PlugIns.BotAI.Chat;
using MUnique.OpenMU.GameLogic.PlugIns.BotAI.Decision;
using MUnique.OpenMU.GameLogic.PlugIns.BotAI.Humanization;
using MUnique.OpenMU.GameLogic.PlugIns.BotAI.Party;
using MUnique.OpenMU.GameLogic.PlugIns.BotAI.Roles;
using MUnique.OpenMU.GameLogic.PlugIns.BotAI.Simulation;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Smart Bot AI PlugIn (Obsolete: Superseded by GameLogic/Bots/BotChatHandler.cs).
/// </summary>
[Guid("8e5c1a2b-3f4d-4e56-8a90-1b2c3d4e5f6a")]
[System.Obsolete("Superseded by GameLogic/Bots/BotChatHandler.cs")]
public class SmartBotFeaturePlugIn : IChatMessageReceivedPlugIn
{
    private static readonly ConcurrentDictionary<string, BotPersonality> BotPersonalities = new();
    private static readonly ConcurrentDictionary<string, BotPartyFsm> BotPartyFsms = new();

    /// <summary>
    /// Gets or creates a personality profile for specified bot name.
    /// </summary>
    /// <param name="botName">Name of the bot.</param>
    /// <returns><see cref="BotPersonality"/> instance.</returns>
    public static BotPersonality GetPersonality(string botName)
    {
        return BotPersonalities.GetOrAdd(botName, _ => BotPersonality.CreateRandom());
    }

    /// <summary>
    /// Gets or creates an Auto-Party FSM instance for specified bot name.
    /// </summary>
    /// <param name="botName">Name of the bot.</param>
    /// <returns><see cref="BotPartyFsm"/> instance.</returns>
    public static BotPartyFsm GetPartyFsm(string botName)
    {
        return BotPartyFsms.GetOrAdd(botName, _ => new BotPartyFsm());
    }

    /// <inheritdoc/>
    public void ChatMessageReceived(Player sender, string message, CancelEventArgs cancelEventArgs)
    {
        if (sender is null || string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        // Detect intent using Regex matcher
        var intent = IntentMatcher.Classify(message);
        if (intent == ChatIntent.Unknown)
        {
            return;
        }

        // Find nearby bot characters in sender's view/map
        var nearbyBots = sender.CurrentMap?.GetAttackablesInRange(sender.Position, 8)
            .OfType<Player>()
            .Where(p => p.Account?.IsBot ?? false)
            .ToList();

        if (nearbyBots is null || nearbyBots.Count == 0)
        {
            return;
        }

        // Pick one responding bot from nearby bots
        var respondingBot = nearbyBots[Random.Shared.Next(nearbyBots.Count)];
        var botName = respondingBot.SelectedCharacter?.Name ?? respondingBot.Name;
        var playerName = sender.SelectedCharacter?.Name ?? sender.Name;
        string messageId = $"{sender.Id}_{message.GetHashCode()}";

        // Apply 4-Layer Anti-Flood Chat Rate Limiting
        if (!ChatRateLimiter.TryAcquireChatSlot(botName, playerName, messageId))
        {
            return;
        }

        var personality = GetPersonality(botName);

        bool isPartyFull = respondingBot.Party?.PartyList.Count >= 5;
        string responseText = TemplateChatGenerator.GenerateResponse(intent, isPartyFull, personality);

        // Schedule delayed chat response to simulate typing speed (500ms - 1500ms)
        var delay = MarkovHumanizer.GetHumanizedDelay(HumanActionType.RandomJitterStep);
        _ = Task.Run(async () =>
        {
            await Task.Delay(delay > TimeSpan.Zero ? delay : TimeSpan.FromMilliseconds(800)).ConfigureAwait(false);
            try
            {
                await sender.InvokeViewPlugInAsync<IChatViewPlugIn>(p => p.ChatMessageAsync(responseText, botName, ChatMessageType.Normal)).ConfigureAwait(false);
            }
            catch
            {
                // Ignore chat delivery exceptions if player disconnected
            }
        });
    }

    /// <summary>
    /// Evaluates high-level decision for a bot during game tick.
    /// </summary>
    /// <param name="botPlayer">The bot player instance.</param>
    /// <returns>Selected <see cref="BotHighLevelAction"/>.</returns>
    public BotHighLevelAction EvaluateBotDecision(Player botPlayer)
    {
        var botName = botPlayer.SelectedCharacter?.Name ?? botPlayer.Name;
        var personality = GetPersonality(botName);

        var ctx = new BotStateContext(
            HealthPercentage: botPlayer.Attributes?[Stats.CurrentHealth] / Math.Max(1, botPlayer.Attributes?[Stats.MaximumHealth] ?? 1) ?? 1.0,
            ManaPercentage: botPlayer.Attributes?[Stats.CurrentMana] / Math.Max(1, botPlayer.Attributes?[Stats.MaximumMana] ?? 1) ?? 1.0,
            IsInventoryFull: botPlayer.Inventory?.ItemStorage.Items.Count() >= 60,
            IsMaxLevelReached: (botPlayer.Attributes?[Stats.Level] ?? 1) >= 400,
            IsUnderAttackByPlayer: false,
            NearbyMonsterCount: botPlayer.CurrentMap?.GetAttackablesInRange(botPlayer.Position, 5).Count() ?? 0,
            Personality: personality);

        var bestAction = UtilityCalculator.SelectBestAction(ctx);

        // Auto-Party FSM Update during Farm action
        if (bestAction == BotHighLevelAction.Farm && botPlayer.CurrentMap != null)
        {
            var nearbyPlayers = botPlayer.CurrentMap.GetAttackablesInRange(botPlayer.Position, 25)
                .OfType<Player>()
                .ToList();

            bool hasRealPlayerInZone = nearbyPlayers.Any(p => !(p.Account?.IsBot ?? false));
            int nearbyBotCount = nearbyPlayers.Count(p => p.Account?.IsBot ?? false);

            var fsm = GetPartyFsm(botName);
            fsm.UpdateState(hasRealPlayerInZone, false, nearbyBotCount);
        }

        return bestAction;
    }

    /// <summary>
    /// Simulates offline EXP catchup when bot wakes up from hibernate mode.
    /// </summary>
    /// <param name="hibernateDuration">Offline duration.</param>
    /// <param name="baseMapExpPerSec">Spot EXP rate.</param>
    /// <returns><see cref="HibernateCatchupResult"/>.</returns>
    public HibernateCatchupResult CalculateHibernateCatchup(TimeSpan hibernateDuration, long baseMapExpPerSec)
    {
        return HibernateSimulator.CalculateCatchupExp(hibernateDuration, baseMapExpPerSec);
    }
}
