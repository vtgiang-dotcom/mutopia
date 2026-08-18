// <copyright file="BotPersonalityResolver.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Bots;

using System;

/// <summary>
/// Resolves a deterministic personality archetype for a character based on its name.
/// </summary>
public static class BotPersonalityResolver
{
    private const int ArchetypeCount = 6;

    /// <summary>
    /// Resolves the personality of a character deterministically by hashing its name.
    /// The same character name always maps to the same personality across server restarts.
    /// </summary>
    /// <param name="characterName">The character name.</param>
    /// <returns>The resolved <see cref="BotPersonality"/>.</returns>
    public static BotPersonality Resolve(string? characterName)
    {
        if (string.IsNullOrWhiteSpace(characterName))
        {
            return BotPersonality.Balanced;
        }

        var hash = Math.Abs(characterName.GetHashCode(StringComparison.OrdinalIgnoreCase));
        return (BotPersonality)(hash % ArchetypeCount);
    }
}
