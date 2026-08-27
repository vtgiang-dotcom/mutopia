// <copyright file="BotAiTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.GameLogic.Bots;
using NUnit.Framework;

/// <summary>
/// Unit tests for Smart Bot AI Framework components.
/// </summary>
[TestFixture]
public class BotAiTests
{
    /// <summary>
    /// Verifies Bot PVP rules can evaluate null inputs safely.
    /// </summary>
    [Test]
    public void TestBotPvpRulesNullHandling()
    {
        Assert.That(BotPvpRules.IsLegalPvpTarget(null!, null!), Is.False);
    }
}
