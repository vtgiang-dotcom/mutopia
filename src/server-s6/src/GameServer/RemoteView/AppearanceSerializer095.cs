// <copyright file="AppearanceSerializer095.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.Network.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Serializer for the appearance of a player for version 0.95.
/// </summary>
[Guid("7F2A1E9B-4D1C-4B9F-8A2D-6F4C7E8B9A0C")]
[PlugIn]
[Display(Name = nameof(PlugInResources.AppearanceSerializer095_Name), Description = nameof(PlugInResources.AppearanceSerializer095_Description), ResourceType = typeof(PlugInResources))]
[MinimumClient(0, 95, ClientLanguage.Invariant)]
public class AppearanceSerializer095 : IAppearanceSerializer
{
    /// <inheritdoc/>
    public int NeededSpace => 15;

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
