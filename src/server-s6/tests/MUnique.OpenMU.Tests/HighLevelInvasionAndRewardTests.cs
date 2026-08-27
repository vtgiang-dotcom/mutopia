// <copyright file="HighLevelInvasionAndRewardTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using System;
using System.Linq;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameLogic.PlugIns.InvasionEvents;
using NUnit.Framework;

/// <summary>
/// Unit tests for High-Level Golden Invasion and PlayTimeReward / Goblin Points plugins.
/// </summary>
[TestFixture]
public class HighLevelInvasionAndRewardTests
{
    /// <summary>
    /// Verifies that GoldenHighLevel configuration default is correctly set up with expected maps and monsters.
    /// </summary>
    [Test]
    public void TestGoldenHighLevelInvasionConfiguration()
    {
        var config = InvasionConfigurationDefaults.GoldenHighLevel;

        Assert.That(config, Is.Not.Null);
        Assert.That(config.TaskDuration, Is.EqualTo(TimeSpan.FromMinutes(30)));
        Assert.That(config.Mobs, Has.Count.EqualTo(3));

        // Kanturu Relics mob
        var kanturuMob = config.Mobs.FirstOrDefault(m => m.MapIds.Contains(InvasionMaps.KanturuRelics));
        Assert.That(kanturuMob, Is.Not.Null);
        Assert.That(kanturuMob!.MonsterId, Is.EqualTo(InvasionMonsters.Persona));
        Assert.That(kanturuMob.Count, Is.EqualTo(10));

        // Raklion mob
        var raklionMob = config.Mobs.FirstOrDefault(m => m.MapIds.Contains(InvasionMaps.Raklion));
        Assert.That(raklionMob, Is.Not.Null);
        Assert.That(raklionMob!.MonsterId, Is.EqualTo(InvasionMonsters.IronKnight));
        Assert.That(raklionMob.Count, Is.EqualTo(8));

        // Swamp of Calmness mob
        var swampMob = config.Mobs.FirstOrDefault(m => m.MapIds.Contains(InvasionMaps.SwampOfCalmness));
        Assert.That(swampMob, Is.Not.Null);
        Assert.That(swampMob!.MonsterId, Is.EqualTo(InvasionMonsters.SapiDuo));
        Assert.That(swampMob.Count, Is.EqualTo(10));
    }

    /// <summary>
    /// Verifies that GoldenHighLevelInvasionPlugIn can be instantiated and exposes default configuration.
    /// </summary>
    [Test]
    public void TestGoldenHighLevelInvasionPlugInInstance()
    {
        var plugIn = new GoldenHighLevelInvasionPlugIn();
        var defaultConfig = plugIn.CreateDefaultConfig() as PeriodicInvasionConfiguration;

        Assert.That(defaultConfig, Is.Not.Null);
        Assert.That(defaultConfig!.Mobs, Has.Count.EqualTo(3));
    }

    /// <summary>
    /// Verifies that PlayTimeRewardConfiguration has appropriate defaults.
    /// </summary>
    [Test]
    public void TestPlayTimeRewardConfigurationDefaults()
    {
        var config = new PlayTimeRewardConfiguration();
        Assert.That(config.Interval, Is.EqualTo(TimeSpan.FromMinutes(30)));
        Assert.That(config.PointsPerInterval, Is.EqualTo(10));
    }

    /// <summary>
    /// Verifies that Account entity holds and increments GoblinPoints correctly.
    /// </summary>
    [Test]
    public void TestAccountGoblinPointsIncrement()
    {
        var account = new Account
        {
            LoginName = "test_user",
            GoblinPoints = 0,
            IsBot = false,
        };

        account.GoblinPoints += 10;
        Assert.That(account.GoblinPoints, Is.EqualTo(10));

        account.GoblinPoints += 50;
        Assert.That(account.GoblinPoints, Is.EqualTo(60));
    }
}
