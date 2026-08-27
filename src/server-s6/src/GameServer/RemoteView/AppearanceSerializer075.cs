// <copyright file="AppearanceSerializer075.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.Network.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Serializer for the appearance of a player for version 0.75.
/// </summary>
[Guid("98C51A67-24B3-4712-B673-19598CA5F411")]
[PlugIn]
[Display(Name = nameof(PlugInResources.AppearanceSerializer075_Name), Description = nameof(PlugInResources.AppearanceSerializer075_Description), ResourceType = typeof(PlugInResources))]
[MinimumClient(0, 75, ClientLanguage.Invariant)]
public class AppearanceSerializer075 : IAppearanceSerializer
{
    /// <inheritdoc/>
    public int NeededSpace => 11;

    /// <inheritdoc/>
    public void InvalidateCache(IAppearanceData appearance)
    {
    }

    /// <inheritdoc/>
    public void WriteAppearanceData(Span<byte> target, IAppearanceData appearance, bool useCache)
    {
        if (target.Length < this.NeededSpace)
        {
            throw new ArgumentException($"Target span too small. Actual size: {target.Length}; Required: {this.NeededSpace}.", nameof(target));
        }

        target.Clear();
        target[0] = (byte)((appearance.CharacterClass?.Number ?? 0) << 3);
    }
}
