// <copyright file="DeepDungeon2.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Maps;

using MUnique.OpenMU.DataModel.Configuration;

/// <summary>
/// The initialization for the Deep Dungeon 2 map.
/// </summary>
internal class DeepDungeon2 : BaseMapInitializer
{
    internal const byte Number = 117;
    internal const string Name = "Deep Dungeon 2";

    public DeepDungeon2(IContext context, GameConfiguration gameConfiguration)
        : base(context, gameConfiguration)
    {
    }

    protected override byte MapNumber => Number;
    protected override string MapName => Name;

    protected override IEnumerable<MonsterSpawnArea> CreateNpcSpawns() => Enumerable.Empty<MonsterSpawnArea>();
    protected override IEnumerable<MonsterSpawnArea> CreateMonsterSpawns() => Enumerable.Empty<MonsterSpawnArea>();
}
