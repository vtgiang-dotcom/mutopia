// <copyright file="BotPartyFsm.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.BotAI.Party;

/// <summary>
/// FSM States for Bot Auto-Party Manager.
/// </summary>
public enum BotPartyState
{
    /// <summary>
    /// Bot is solo.
    /// </summary>
    Solo,

    /// <summary>
    /// Scanning for candidate farmer bots.
    /// </summary>
    Scanning,

    /// <summary>
    /// Forming party with candidate bots.
    /// </summary>
    Forming,

    /// <summary>
    /// Party is active and receiving EXP bonus.
    /// </summary>
    Active,

    /// <summary>
    /// Real player detected -> yielding slot to real player.
    /// </summary>
    Yielding,

    /// <summary>
    /// Disbanding party after lifetime or spot change.
    /// </summary>
    Disbanding,
}

/// <summary>
/// Manages Auto-Party state machine for a Bot.
/// </summary>
public class BotPartyFsm
{
    /// <summary>
    /// Gets current FSM State.
    /// </summary>
    public BotPartyState State { get; private set; } = BotPartyState.Solo;

    /// <summary>
    /// Gets timestamp when party was formed.
    /// </summary>
    public DateTime PartyFormedUtc { get; private set; } = DateTime.MinValue;

    /// <summary>
    /// Gets timestamp when bot last left a party.
    /// </summary>
    public DateTime LastPartyLeftUtc { get; private set; } = DateTime.MinValue;

    /// <summary>
    /// Evaluates state transition based on player proximity and party lifetime.
    /// </summary>
    /// <param name="hasRealPlayerInFarmZone">Whether any real player is within 25 tiles.</param>
    /// <param name="realPlayerRequestedParty">Whether a real player sent party request.</param>
    /// <param name="botCountInZone">Number of rảnh candidate farmer bots nearby.</param>
    public void UpdateState(bool hasRealPlayerInFarmZone, bool realPlayerRequestedParty, int botCountInZone)
    {
        var now = DateTime.UtcNow;

        switch (this.State)
        {
            case BotPartyState.Solo:
                if (!hasRealPlayerInFarmZone && (now - this.LastPartyLeftUtc).TotalSeconds >= 120.0) // RejoinCooldown = 120s
                {
                    this.State = BotPartyState.Scanning;
                }

                break;

            case BotPartyState.Scanning:
                if (hasRealPlayerInFarmZone)
                {
                    this.State = BotPartyState.Solo;
                }
                else if (botCountInZone >= 1)
                {
                    this.State = BotPartyState.Forming;
                }

                break;

            case BotPartyState.Forming:
                this.PartyFormedUtc = now;
                this.State = BotPartyState.Active;
                break;

            case BotPartyState.Active:
                if (hasRealPlayerInFarmZone || realPlayerRequestedParty)
                {
                    this.State = BotPartyState.Yielding;
                }
                else if ((now - this.PartyFormedUtc).TotalSeconds >= 300.0 && botCountInZone == 0) // MinPartyLifetime = 300s
                {
                    this.State = BotPartyState.Disbanding;
                }

                break;

            case BotPartyState.Yielding:
                // After kicking lowest priority bot to yield slot to real player, return to Active
                this.State = BotPartyState.Active;
                break;

            case BotPartyState.Disbanding:
                this.LastPartyLeftUtc = now;
                this.State = BotPartyState.Solo;
                break;
        }
    }
}
