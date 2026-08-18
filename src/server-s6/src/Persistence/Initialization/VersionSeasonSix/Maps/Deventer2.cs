// <copyright file="Deventer2.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Maps;

using MUnique.OpenMU.DataModel.Configuration;

/// <summary>
/// The initialization for the Deventer 2 map.
/// </summary>
internal class Deventer2 : BaseMapInitializer
{
    /// <summary>The Number of the Map.</summary>
    internal const byte Number = 96;

    /// <summary>The Name of the Map.</summary>
    internal const string Name = "Deventer 2";

    public Deventer2(IContext context, GameConfiguration gameConfiguration)
        : base(context, gameConfiguration)
    {
    }

    protected override byte MapNumber => Number;
    protected override string MapName => Name;

    protected override IEnumerable<MonsterSpawnArea> CreateNpcSpawns() => Enumerable.Empty<MonsterSpawnArea>();
    protected override IEnumerable<MonsterSpawnArea> CreateMonsterSpawns() => Enumerable.Empty<MonsterSpawnArea>();
}
