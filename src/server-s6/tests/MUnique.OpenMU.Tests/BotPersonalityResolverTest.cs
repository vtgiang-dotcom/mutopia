// <copyright file="BotPersonalityResolverTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.GameLogic.Bots;

/// <summary>
/// Tests <see cref="BotPersonalityResolver"/> — the deterministic name-to-personality mapper.
/// </summary>
[TestFixture]
public class BotPersonalityResolverTest
{
    /// <summary>
    /// A null character name always produces the safe fallback personality.
    /// </summary>
    [Test]
    public void NullNameReturnsBalanced()
    {
        var result = BotPersonalityResolver.Resolve(null);
        Assert.That(result, Is.EqualTo(BotPersonality.Balanced));
    }

    /// <summary>
    /// An empty or whitespace-only name also produces the safe fallback personality.
    /// </summary>
    [TestCase("")]
    [TestCase("   ")]
    public void EmptyOrWhitespaceNameReturnsBalanced(string name)
    {
        var result = BotPersonalityResolver.Resolve(name);
        Assert.That(result, Is.EqualTo(BotPersonality.Balanced));
    }

    /// <summary>
    /// The same name always produces the same personality across repeated calls.
    /// </summary>
    [TestCase("Joremir")]
    [TestCase("Valdris")]
    [TestCase("SoloBeast")]
    public void SameNameAlwaysProducesSamePersonality(string name)
    {
        var first = BotPersonalityResolver.Resolve(name);
        var second = BotPersonalityResolver.Resolve(new string(name.ToCharArray()));
        Assert.That(first, Is.EqualTo(second));
    }

    /// <summary>
    /// Resolve never returns a value outside the defined enum range (0-5).
    /// </summary>
    [Test]
    public void ResolvedPersonalityIsAlwaysADefinedEnumValue()
    {
        var names = new[] { "Alpha", "Beta", "Gamma", "Delta", "Epsilon", "Zeta", "Eta", "Theta", "Iota", "Kappa" };
        foreach (var name in names)
        {
            var p = BotPersonalityResolver.Resolve(name);
            Assert.That(Enum.IsDefined(p), Is.True, $"Name '{name}' resolved to undefined value {(int)p}");
        }
    }

    /// <summary>
    /// Ten distinct names should produce at least three distinct personalities,
    /// confirming the hash distributes across archetypes rather than clustering on one.
    /// </summary>
    [Test]
    public void TenDistinctNamesProduceAtLeastThreeDistinctPersonalities()
    {
        var names = new[] { "Alpha", "Beta", "Gamma", "Delta", "Epsilon", "Zeta", "Eta", "Theta", "Iota", "Kappa" };
        var distinct = names.Select(n => BotPersonalityResolver.Resolve(n)).Distinct().Count();
        Assert.That(distinct, Is.GreaterThanOrEqualTo(3), $"Expected distribution across at least 3 archetypes, got {distinct}");
    }
}
