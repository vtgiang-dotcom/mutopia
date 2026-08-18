// <copyright file="AtlansAbyss1.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Maps;

using MUnique.OpenMU.DataModel.Configuration;

/// <summary>
/// The initialization for the Atlans Abyss 1 map.
/// </summary>
internal class AtlansAbyss1 : BaseMapInitializer
{
    internal const byte Number = 128;
    internal const string Name = "Atlans Abyss 1";

    public AtlansAbyss1(IContext context, GameConfiguration gameConfiguration)
        : base(context, gameConfiguration)
    {
    }

    protected override byte MapNumber => Number;
    protected override string MapName => Name;

    protected override IEnumerable<MonsterSpawnArea> CreateNpcSpawns() => Enumerable.Empty<MonsterSpawnArea>();
    protected override IEnumerable<MonsterSpawnArea> CreateMonsterSpawns() => Enumerable.Empty<MonsterSpawnArea>();
}
