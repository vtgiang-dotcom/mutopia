// <copyright file="Acheron1.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Maps;

using MUnique.OpenMU.DataModel.Configuration;

/// <summary>
/// The initialization for the Acheron 1 (Alkmar) map.
/// </summary>
internal class Acheron1 : BaseMapInitializer
{
    /// <summary>The Number of the Map.</summary>
    internal const byte Number = 91;

    /// <summary>The Name of the Map.</summary>
    internal const string Name = "Acheron 1";

    public Acheron1(IContext context, GameConfiguration gameConfiguration)
        : base(context, gameConfiguration)
    {
    }

    protected override byte MapNumber => Number;
    protected override string MapName => Name;

    protected override IEnumerable<MonsterSpawnArea> CreateNpcSpawns() => Enumerable.Empty<MonsterSpawnArea>();

    protected override IEnumerable<MonsterSpawnArea> CreateMonsterSpawns()
    {
        var monster = this.NpcDictionary.Values.FirstOrDefault(m => m.NpcWindow == NpcWindow.Undefined) ?? this.NpcDictionary.Values.First();
        yield return this.CreateMonsterSpawn(100, monster, 100, 100, 150, 150);
        yield return this.CreateMonsterSpawn(101, monster, 150, 100, 200, 150);
        yield return this.CreateMonsterSpawn(102, monster, 100, 150, 150, 200);
    }
}
