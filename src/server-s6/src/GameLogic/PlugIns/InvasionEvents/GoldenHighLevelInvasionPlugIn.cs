// <copyright file="GoldenHighLevelInvasionPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.InvasionEvents;

using System.Runtime.InteropServices;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Enables the High-Level Invasion at Kanturu Relics, Raklion, and Swamp of Calmness
/// for characters at level 230 and above.
/// </summary>
[PlugIn]
[Display(Name = "High-Level Invasion", Description = "Periodic powerful monster invasion at high-level maps: Kanturu Relics, Raklion, Swamp of Calmness.")]
[Guid("0CB91A38-81C2-4D35-B4B0-A74611B87044")]
public sealed class GoldenHighLevelInvasionPlugIn : SimpleInvasionPlugIn
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GoldenHighLevelInvasionPlugIn"/> class.
    /// </summary>
    public GoldenHighLevelInvasionPlugIn()
        : base(() => InvasionConfigurationDefaults.GoldenHighLevel)
    {
    }

    /// <inheritdoc />
    protected override MapEventType? EventType => MapEventType.GoldenDragonInvasion;

    /// <inheritdoc />
    protected override ushort? AnnouncedMonsterId => InvasionMonsters.IronKnight;

    /// <inheritdoc />
    protected override IReadOnlyList<ushort>? EventDisplayMapIds
        => [InvasionMaps.KanturuRelics, InvasionMaps.Raklion, InvasionMaps.SwampOfCalmness];
}
