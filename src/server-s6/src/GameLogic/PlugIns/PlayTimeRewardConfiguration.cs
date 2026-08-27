// <copyright file="PlayTimeRewardConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System;

/// <summary>
/// Configuration for <see cref="PlayTimeRewardPlugIn"/>.
/// </summary>
public class PlayTimeRewardConfiguration
{
    /// <summary>
    /// Gets or sets the interval between each reward tick.
    /// Default: 30 minutes.
    /// </summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Gets or sets the number of Goblin Points awarded per interval tick.
    /// Default: 10 points.
    /// </summary>
    public int PointsPerInterval { get; set; } = 10;
}
