// <copyright file="CharacterClassHelper.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.CharacterClasses;

using System.Collections.Generic;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration;

/// <summary>
/// Helper class for creating character class attributes, relationships, and queries.
/// </summary>
public static class CharacterClassHelper
{
    /// <summary>
    /// Determines character classes matching the given bitmask flags.
    /// </summary>
    /// <param name="gameConfiguration">The game configuration.</param>
    /// <param name="characterClasses">The character classes bitmask.</param>
    /// <returns>Matching character classes.</returns>
    public static IEnumerable<CharacterClass> DetermineCharacterClasses(this GameConfiguration gameConfiguration, CharacterClasses characterClasses)
    {
        if (characterClasses == CharacterClasses.None)
        {
            yield break;
        }

        foreach (var characterClass in gameConfiguration.CharacterClasses)
        {
            var flag = characterClass.Number switch
            {
                (byte)CharacterClassNumber.DarkWizard => CharacterClasses.DarkWizard,
                (byte)CharacterClassNumber.SoulMaster => CharacterClasses.SoulMaster,
                (byte)CharacterClassNumber.GrandMaster => CharacterClasses.GrandMaster,
                (byte)CharacterClassNumber.DarkKnight => CharacterClasses.DarkKnight,
                (byte)CharacterClassNumber.BladeKnight => CharacterClasses.BladeKnight,
                (byte)CharacterClassNumber.BladeMaster => CharacterClasses.BladeMaster,
                (byte)CharacterClassNumber.FairyElf => CharacterClasses.FairyElf,
                (byte)CharacterClassNumber.MuseElf => CharacterClasses.MuseElf,
                (byte)CharacterClassNumber.HighElf => CharacterClasses.HighElf,
                (byte)CharacterClassNumber.MagicGladiator => CharacterClasses.MagicGladiator,
                (byte)CharacterClassNumber.DuelMaster => CharacterClasses.DuelMaster,
                (byte)CharacterClassNumber.DarkLord => CharacterClasses.DarkLord,
                (byte)CharacterClassNumber.LordEmperor => CharacterClasses.LordEmperor,
                (byte)CharacterClassNumber.Summoner => CharacterClasses.Summoner,
                (byte)CharacterClassNumber.BloodySummoner => CharacterClasses.BloodySummoner,
                (byte)CharacterClassNumber.DimensionMaster => CharacterClasses.DimensionMaster,
                (byte)CharacterClassNumber.RageFighter => CharacterClasses.RageFighter,
                (byte)CharacterClassNumber.FistMaster => CharacterClasses.FistMaster,
                _ => CharacterClasses.None,
            };

            if (flag != CharacterClasses.None && characterClasses.HasFlag(flag))
            {
                yield return characterClass;
            }
        }
    }

    /// <summary>
    /// Determines character classes matching boolean flags for first class level.
    /// </summary>
    public static IEnumerable<CharacterClass> DetermineCharacterClasses(
        this GameConfiguration gameConfiguration,
        bool wizard,
        bool knight,
        bool elf,
        bool magicGladiator = false,
        bool darkLord = false,
        bool summoner = false,
        bool ragefighter = false)
    {
        return gameConfiguration.DetermineCharacterClasses(
            wizard ? 1 : 0,
            knight ? 1 : 0,
            elf ? 1 : 0,
            magicGladiator ? 1 : 0,
            darkLord ? 1 : 0,
            summoner ? 1 : 0,
            ragefighter ? 1 : 0);
    }

    /// <summary>
    /// Determines character classes matching class level requirement thresholds.
    /// </summary>
    public static IEnumerable<CharacterClass> DetermineCharacterClasses(
        this GameConfiguration gameConfiguration,
        int wizardClassLevel,
        int knightClassLevel,
        int elfClassLevel,
        int magicGladiatorClassLevel = 0,
        int darkLordClassLevel = 0,
        int summonerClassLevel = 0,
        int ragefighterClassLevel = 0)
    {
        foreach (var characterClass in gameConfiguration.CharacterClasses)
        {
            var baseNumber = characterClass.Number & 0xF0;
            var classLevel = (characterClass.Number & 0x0F) + 1;
            var reqLevel = baseNumber switch
            {
                0x00 => wizardClassLevel,
                0x10 => knightClassLevel,
                0x20 => elfClassLevel,
                0x30 => magicGladiatorClassLevel,
                0x40 => darkLordClassLevel,
                0x50 => summonerClassLevel,
                0x60 => ragefighterClassLevel,
                _ => 0,
            };

            if (reqLevel > 0 && classLevel >= reqLevel)
            {
                yield return characterClass;
            }
        }
    }

    /// <summary>
    /// Creates an attribute relationship with a constant multiplier.
    /// </summary>
    public static AttributeRelationship CreateAttributeRelationship(
        IContext context,
        GameConfiguration gameConfiguration,
        AttributeDefinition targetAttribute,
        float multiplier,
        AttributeDefinition sourceAttribute,
        InputOperator inputOperator = InputOperator.Multiply,
        AggregateType aggregateType = AggregateType.AddRaw)
    {
        var relationship = context.CreateNew<AttributeRelationship>();
        relationship.TargetAttribute = targetAttribute.GetPersistent(gameConfiguration);
        relationship.InputAttribute = sourceAttribute.GetPersistent(gameConfiguration);
        relationship.InputOperand = multiplier;
        relationship.InputOperator = inputOperator;
        relationship.AggregateType = aggregateType;
        return relationship;
    }

    /// <summary>
    /// Creates an attribute relationship with a multiplier attribute.
    /// </summary>
    public static AttributeRelationship CreateAttributeRelationship(
        IContext context,
        GameConfiguration gameConfiguration,
        AttributeDefinition targetAttribute,
        AttributeDefinition multiplierAttribute,
        AttributeDefinition sourceAttribute,
        InputOperator inputOperator = InputOperator.Multiply,
        AggregateType aggregateType = AggregateType.AddRaw)
    {
        var relationship = context.CreateNew<AttributeRelationship>();
        relationship.TargetAttribute = targetAttribute.GetPersistent(gameConfiguration);
        relationship.InputAttribute = sourceAttribute.GetPersistent(gameConfiguration);
        relationship.OperandAttribute = multiplierAttribute.GetPersistent(gameConfiguration);
        relationship.InputOperator = inputOperator;
        relationship.AggregateType = aggregateType;
        return relationship;
    }

    /// <summary>
    /// Creates a conditional relationship.
    /// </summary>
    public static AttributeRelationship CreateConditionalRelationship(
        IContext context,
        GameConfiguration gameConfiguration,
        AttributeDefinition targetAttribute,
        AttributeDefinition conditionalAttribute,
        AttributeDefinition sourceAttribute,
        AggregateType aggregateType = AggregateType.AddRaw)
    {
        var relationship = context.CreateNew<AttributeRelationship>();
        relationship.TargetAttribute = targetAttribute.GetPersistent(gameConfiguration);
        relationship.InputAttribute = sourceAttribute.GetPersistent(gameConfiguration);
        relationship.OperandAttribute = conditionalAttribute.GetPersistent(gameConfiguration);
        relationship.InputOperator = InputOperator.Multiply;
        relationship.AggregateType = aggregateType;
        return relationship;
    }

    /// <summary>
    /// Creates a const value attribute.
    /// </summary>
    public static ConstValueAttribute CreateConstValueAttribute(
        IContext context,
        GameConfiguration gameConfiguration,
        float value,
        AttributeDefinition attribute)
    {
        return context.CreateNew<ConstValueAttribute>(value, attribute.GetPersistent(gameConfiguration));
    }
}
