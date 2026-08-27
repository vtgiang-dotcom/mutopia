// <copyright file="CharacterClasses.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.CharacterClasses;

using System;

/// <summary>
/// Bitflags representing character classes for skill and item requirements.
/// </summary>
[Flags]
public enum CharacterClasses : long
{
    /// <summary>None.</summary>
    None = 0,

    /// <summary>Dark Wizard.</summary>
    DarkWizard = 1 << 0,

    /// <summary>Soul Master.</summary>
    SoulMaster = 1 << 1,

    /// <summary>Grand Master.</summary>
    GrandMaster = 1 << 2,

    /// <summary>Dark Knight.</summary>
    DarkKnight = 1 << 3,

    /// <summary>Blade Knight.</summary>
    BladeKnight = 1 << 4,

    /// <summary>Blade Master.</summary>
    BladeMaster = 1 << 5,

    /// <summary>Fairy Elf.</summary>
    FairyElf = 1 << 6,

    /// <summary>Muse Elf.</summary>
    MuseElf = 1 << 7,

    /// <summary>High Elf.</summary>
    HighElf = 1 << 8,

    /// <summary>Magic Gladiator.</summary>
    MagicGladiator = 1 << 9,

    /// <summary>Duel Master.</summary>
    DuelMaster = 1 << 10,

    /// <summary>Dark Lord.</summary>
    DarkLord = 1 << 11,

    /// <summary>Lord Emperor.</summary>
    LordEmperor = 1 << 12,

    /// <summary>Summoner.</summary>
    Summoner = 1 << 13,

    /// <summary>Bloody Summoner.</summary>
    BloodySummoner = 1 << 14,

    /// <summary>Dimension Master.</summary>
    DimensionMaster = 1 << 15,

    /// <summary>Rage Fighter.</summary>
    RageFighter = 1 << 16,

    /// <summary>Fist Master.</summary>
    FistMaster = 1 << 17,

    /// <summary>All Magicians.</summary>
    AllMagicians = DarkWizard | SoulMaster | GrandMaster,

    /// <summary>All Knights.</summary>
    AllKnights = DarkKnight | BladeKnight | BladeMaster,

    /// <summary>All Elfs.</summary>
    AllElfs = FairyElf | MuseElf | HighElf,

    /// <summary>All Magic Gladiators.</summary>
    AllMGs = MagicGladiator | DuelMaster,

    /// <summary>All Dark Lords.</summary>
    AllLords = DarkLord | LordEmperor,

    /// <summary>All Summoners.</summary>
    AllSummoners = Summoner | BloodySummoner | DimensionMaster,

    /// <summary>All Rage Fighters.</summary>
    AllFighters = RageFighter | FistMaster,

    /// <summary>All Knights, Lords, and Magic Gladiators.</summary>
    AllKnightsLordsAndMGs = AllKnights | AllLords | AllMGs,

    /// <summary>Soul Master and Grand Master.</summary>
    SoulMasterAndGrandMaster = SoulMaster | GrandMaster,

    /// <summary>Blade Knight and Blade Master.</summary>
    BladeKnightAndBladeMaster = BladeKnight | BladeMaster,

    /// <summary>Muse Elf and High Elf.</summary>
    MuseElfAndHighElf = MuseElf | HighElf,

    /// <summary>Bloody Summoner and Dimension Master.</summary>
    BloodySummonerAndDimensionMaster = BloodySummoner | DimensionMaster,

    /// <summary>All Master classes except Fist Master.</summary>
    AllMastersExceptFistMaster = GrandMaster | BladeMaster | HighElf | DuelMaster | LordEmperor | DimensionMaster,

    /// <summary>All Master classes.</summary>
    AllMasters = AllMastersExceptFistMaster | FistMaster,

    /// <summary>All Master classes and second classes.</summary>
    AllMastersAndSecondClass = SoulMaster | BladeKnight | MuseElf | BloodySummoner | AllMasters,

    /// <summary>All Characters.</summary>
    All = AllMagicians | AllKnights | AllElfs | AllMGs | AllLords | AllSummoners | AllFighters,
}
