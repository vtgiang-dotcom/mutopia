// <copyright file="LegacyQuestRewardPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView.Quest;

using System.Runtime.InteropServices;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration.Quests;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.GameLogic.Views.Quest;
using MUnique.OpenMU.Network.Packets.ServerToClient;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// The default implementation of the <see cref="ILegacyQuestRewardPlugIn"/> which is forwarding everything to the game client with specific data packets.
/// </summary>
[PlugIn]
[Display(Name = nameof(PlugInResources.LegacyQuestRewardPlugIn_Name), Description = nameof(PlugInResources.LegacyQuestRewardPlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("41A6F9E1-C450-4822-9F12-701EA8F7B5E1")]
public class LegacyQuestRewardPlugIn : ILegacyQuestRewardPlugIn
{
    private readonly RemotePlayer _player;

    /// <summary>
    /// Initializes a new instance of the <see cref="LegacyQuestRewardPlugIn"/> class.
    /// </summary>
    /// <param name="player">The player.</param>
    public LegacyQuestRewardPlugIn(RemotePlayer player)
    {
        this._player = player;
    }

    /// <inheritdoc />
    public async ValueTask ShowAsync(Player player, QuestRewardType rewardType, int value, AttributeDefinition? attributeReward)
    {
        if (this._player.Connection is not { } connection)
        {
            return;
        }

        var reward = rewardType switch
        {
            QuestRewardType.LevelUpPoints => LegacyQuestReward.QuestRewardType.LevelUpPoints,
            QuestRewardType.CharacterEvolutionFirstToSecond => LegacyQuestReward.QuestRewardType.CharacterEvolutionFirstToSecond,
            QuestRewardType.CharacterEvolutionSecondToThird => LegacyQuestReward.QuestRewardType.CharacterEvolutionSecondToThird,
            QuestRewardType.Attribute => LegacyQuestReward.QuestRewardType.LevelUpPointsPerLevelIncrease,
            QuestRewardType.Skill => LegacyQuestReward.QuestRewardType.ComboSkill,
            _ => (LegacyQuestReward.QuestRewardType)200,
        };

        await connection.SendLegacyQuestRewardAsync(player.GetId(this._player), reward, (byte)value).ConfigureAwait(false);
    }
}
