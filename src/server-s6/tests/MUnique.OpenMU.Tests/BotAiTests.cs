// <copyright file="BotAiTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.GameLogic.PlugIns.BotAI.Chat;
using MUnique.OpenMU.GameLogic.PlugIns.BotAI.Decision;
using MUnique.OpenMU.GameLogic.PlugIns.BotAI.Humanization;
using MUnique.OpenMU.GameLogic.PlugIns.BotAI.Party;
using MUnique.OpenMU.GameLogic.PlugIns.BotAI.Simulation;
using NUnit.Framework;

/// <summary>
/// Unit tests for Smart Bot AI Framework components.
/// </summary>
[TestFixture]
public class BotAiTests
{
    /// <summary>
    /// Verifies piecewise decay calculation for hibernate EXP catchup.
    /// </summary>
    [Test]
    public void TestHibernateExpPiecewiseDecay()
    {
        // 10 hours hibernate, 500 base EXP/sec, 0 danger (no death penalty for deterministic test)
        var result = HibernateSimulator.CalculateCatchupExp(TimeSpan.FromHours(10), 500, mapDangerLevel: 0.0);

        // Expected: 1h*70% + 2h*55% + 3h*40% + 4h*25% = 1.26M + 1.98M + 2.16M + 1.8M = 7.2M
        Assert.That(result.EarnedExperience, Is.EqualTo(7_200_000));
        Assert.That(result.DiedDuringHibernate, Is.False);
        Assert.That(result.PenaltyExperience, Is.Zero);
    }

    /// <summary>
    /// Verifies 24-hour cap on hibernate EXP catchup.
    /// </summary>
    [Test]
    public void TestHibernateExp24HourCap()
    {
        var result24 = HibernateSimulator.CalculateCatchupExp(TimeSpan.FromHours(24), 500, mapDangerLevel: 0.0);
        var result30 = HibernateSimulator.CalculateCatchupExp(TimeSpan.FromHours(30), 500, mapDangerLevel: 0.0);

        // 30 hours should equal 24 hours due to 24h hard cap
        Assert.That(result30.EarnedExperience, Is.EqualTo(result24.EarnedExperience));
    }

    /// <summary>
    /// Verifies Chat Intent Matcher regex classification.
    /// </summary>
    [Test]
    public void TestIntentMatcherClassification()
    {
        Assert.That(IntentMatcher.Classify("cho xin pt vói ông ơi"), Is.EqualTo(ChatIntent.RequestParty));
        Assert.That(IntentMatcher.Classify("xin do rác đi bro"), Is.EqualTo(ChatIntent.RequestItem));
        Assert.That(IntentMatcher.Classify("đang ở đâu đấy"), Is.EqualTo(ChatIntent.LocationQuery));
        Assert.That(IntentMatcher.Classify("alo hế lô"), Is.EqualTo(ChatIntent.Greeting));
        Assert.That(IntentMatcher.Classify("random text 123"), Is.EqualTo(ChatIntent.Unknown));
    }

    /// <summary>
    /// Verifies Template Chat Generator produces non-empty response.
    /// </summary>
    [Test]
    public void TestTemplateChatGenerator()
    {
        var personality = BotPersonality.CreateRandom();
        string response = TemplateChatGenerator.GenerateResponse(ChatIntent.RequestParty, isPartyFull: false, personality);

        Assert.That(response, Is.Not.Null.And.Not.Empty);
    }

    /// <summary>
    /// Verifies 4-layer Chat Rate Limiter throttling.
    /// </summary>
    [Test]
    public void TestChatRateLimiterPlayerBotCooldown()
    {
        string bot = "TestBot1";
        string player = "TestPlayer1";
        string msgId = "msg_1";

        bool firstAttempt = ChatRateLimiter.TryAcquireChatSlot(bot, player, msgId);
        bool immediateSecondAttempt = ChatRateLimiter.TryAcquireChatSlot(bot, player, msgId);

        Assert.That(firstAttempt, Is.True);
        Assert.That(immediateSecondAttempt, Is.False, "Second immediate attempt should be throttled by Player-Bot Cooldown");
    }

    /// <summary>
    /// Verifies Bot Auto-Party FSM state transitions.
    /// </summary>
    [Test]
    public void TestBotPartyFsmTransitions()
    {
        var fsm = new BotPartyFsm();
        Assert.That(fsm.State, Is.EqualTo(BotPartyState.Solo));

        // Update when no real players and 2 candidate bots in zone -> should transition to Scanning
        fsm.UpdateState(hasRealPlayerInFarmZone: false, realPlayerRequestedParty: false, botCountInZone: 2);
        Assert.That(fsm.State, Is.EqualTo(BotPartyState.Scanning));

        // Second update in Scanning with candidate bots -> should transition to Forming
        fsm.UpdateState(hasRealPlayerInFarmZone: false, realPlayerRequestedParty: false, botCountInZone: 2);
        Assert.That(fsm.State, Is.EqualTo(BotPartyState.Forming));

        // Third update -> Active
        fsm.UpdateState(hasRealPlayerInFarmZone: false, realPlayerRequestedParty: false, botCountInZone: 2);
        Assert.That(fsm.State, Is.EqualTo(BotPartyState.Active));

        // Player detected -> Yielding to real player
        fsm.UpdateState(hasRealPlayerInFarmZone: true, realPlayerRequestedParty: false, botCountInZone: 2);
        Assert.That(fsm.State, Is.EqualTo(BotPartyState.Yielding));
    }

    /// <summary>
    /// Verifies Utility Calculator action selection.
    /// </summary>
    [Test]
    public void TestUtilityCalculatorSelection()
    {
        var personality = new BotPersonality();

        // Max level reached context -> should select ResetCharacter
        var resetCtx = new BotStateContext(1.0, 1.0, false, true, false, 5, personality);
        Assert.That(UtilityCalculator.SelectBestAction(resetCtx), Is.EqualTo(BotHighLevelAction.ResetCharacter));

        // Critical low health context -> should select GoToTown
        var lowHpCtx = new BotStateContext(0.10, 1.0, false, false, false, 5, personality);
        Assert.That(UtilityCalculator.SelectBestAction(lowHpCtx), Is.EqualTo(BotHighLevelAction.GoToTown));

        // Normal farming context -> should select Farm
        var farmCtx = new BotStateContext(1.0, 1.0, false, false, false, 5, personality);
        Assert.That(UtilityCalculator.SelectBestAction(farmCtx), Is.EqualTo(BotHighLevelAction.Farm));
    }
}
