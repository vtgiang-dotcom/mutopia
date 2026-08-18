// <copyright file="BotPersonalitySettings.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Bots;

using System;
using System.Collections.Generic;
using MUnique.OpenMU.GameLogic.MuHelper;

/// <summary>
/// Personality-driven MU Helper settings implementation for server-side bots.
/// Customizes combat thresholds, party support, item pickup, and safety rules according to the bot's <see cref="BotPersonality"/>.
/// </summary>
internal sealed class BotPersonalitySettings : IMuHelperSettings
{
    private readonly BotPersonality _personality;

    /// <summary>
    /// Initializes a new instance of the <see cref="BotPersonalitySettings"/> class.
    /// </summary>
    /// <param name="personality">The personality archetype of the bot.</param>
    public BotPersonalitySettings(BotPersonality personality)
    {
        this._personality = personality;
    }

    /// <summary>
    /// Gets the personality archetype.
    /// </summary>
    public BotPersonality Personality => this._personality;

    /// <inheritdoc />
    public int BasicSkillId => 0;

    /// <inheritdoc />
    public int ActivationSkill1Id => 0;

    /// <inheritdoc />
    public int ActivationSkill2Id => 0;

    /// <inheritdoc />
    public int DelayMinSkill1 => 0;

    /// <inheritdoc />
    public int DelayMinSkill2 => 0;

    /// <inheritdoc />
    public bool Skill1UseTimer => false;

    /// <inheritdoc />
    public bool Skill1UseCondition => false;

    /// <inheritdoc />
    public bool Skill1ConditionAttacking => false;

    /// <inheritdoc />
    public int Skill1SubCondition => 0;

    /// <inheritdoc />
    public bool Skill2UseTimer => false;

    /// <inheritdoc />
    public bool Skill2UseCondition => false;

    /// <inheritdoc />
    public bool Skill2ConditionAttacking => false;

    /// <inheritdoc />
    public int Skill2SubCondition => 0;

    /// <inheritdoc />
    public bool UseCombo => false;

    /// <inheritdoc />
    public int HuntingRange => 6;

    /// <inheritdoc />
    public int MaxSecondsAway => 30;

    /// <inheritdoc />
    public bool LongRangeCounterAttack => false;

    /// <inheritdoc />
    /// <remarks>
    /// Disabled for bots: the <see cref="BotNavigator"/> is the sole driver of travel between hunting
    /// grounds, so the offline movement handler must not try to walk the bot back to its origin in parallel.
    /// </remarks>
    public bool ReturnToOriginalPosition => false;

    /// <inheritdoc />
    public int BuffSkill0Id => 0;

    /// <inheritdoc />
    public int BuffSkill1Id => 0;

    /// <inheritdoc />
    public int BuffSkill2Id => 0;

    /// <inheritdoc />
    public bool BuffOnDuration => false;

    /// <inheritdoc />
    public bool BuffDurationForParty => false;

    /// <inheritdoc />
    public int BuffCastIntervalSeconds => 0;

    /// <inheritdoc />
    public bool AutoHeal => true;

    /// <inheritdoc />
    public int HealThresholdPercent => this._personality switch
    {
        BotPersonality.Guardian => 75,
        BotPersonality.Loner => 65,
        BotPersonality.Greedy => 50,
        BotPersonality.Warrior => 40,
        BotPersonality.Reckless => 25,
        _ => 60,
    };

    /// <inheritdoc />
    public bool UseDrainLife => false;

    /// <inheritdoc />
    public bool UseHealPotion => true;

    /// <inheritdoc />
    public int PotionThresholdPercent => this._personality switch
    {
        BotPersonality.Guardian => 70,
        BotPersonality.Loner => 65,
        BotPersonality.Greedy => 50,
        BotPersonality.Warrior => 35,
        BotPersonality.Reckless => 20,
        _ => 60,
    };

    /// <inheritdoc />
    public bool SupportParty => true;

    /// <inheritdoc />
    public bool AutoHealParty => true;

    /// <inheritdoc />
    public int HealPartyThresholdPercent => this._personality switch
    {
        BotPersonality.Guardian => 80,
        BotPersonality.Loner => 50,
        _ => 60,
    };

    /// <inheritdoc />
    public bool UseDarkRaven => false;

    /// <inheritdoc />
    public int DarkRavenMode => 0;

    /// <inheritdoc />
    public int ObtainRange => this._personality == BotPersonality.Greedy ? 8 : 6;

    /// <inheritdoc />
    public bool PickAllItems => this._personality == BotPersonality.Greedy;

    /// <inheritdoc />
    public bool PickSelectItems => true;

    /// <inheritdoc />
    public bool PickJewel => true;

    /// <inheritdoc />
    public bool PickZen => true;

    /// <inheritdoc />
    public bool PickAncient => true;

    /// <inheritdoc />
    public bool PickExcellent => true;

    /// <inheritdoc />
    public bool PickExtraItems => this._personality == BotPersonality.Greedy;

    /// <inheritdoc />
    public IReadOnlyList<string> ExtraItemNames => Array.Empty<string>();

    /// <inheritdoc />
    /// <remarks>
    /// Disabled on purpose: offline auto-repair has no NPC discount and drains Zen at an
    /// increased rate. Bots at the money cap lose that Zen permanently since sales top them
    /// back up immediately - the repair burns Zen without creating room to sell.
    /// </remarks>
    public bool RepairItem => false;

    /// <inheritdoc />
    public bool UseSelfDefense => true;

    /// <inheritdoc />
    public bool AutoAcceptFriend => false;

    /// <inheritdoc />
    public bool AutoAcceptGuild => false;

    /// <inheritdoc />
    /// <remarks>
    /// Most bots accept party invitations from any player like a friendly stranger would, within
    /// the safeguards applied by <see cref="BotPartyHandler"/> (busy check, re-validation on answer).
    /// <see cref="BotPersonality.Loner"/> bots refuse player invitations entirely - they are solo
    /// hunters by nature. Bot-only party formation (see <see cref="BotManager.FormPartiesAsync"/>)
    /// also excludes Loner bots, so they always hunt alone.
    /// </remarks>
    public bool AutoAcceptAnyone => this._personality != BotPersonality.Loner;

    /// <inheritdoc />
    public bool FallbackBasicAttack => true;

    /// <inheritdoc />
    public bool AutoSelectBestSkill => true;

    /// <inheritdoc />
    public bool AutoSelectBuffs => true;

    /// <inheritdoc />
    public bool UseManaPotion => true;

    /// <inheritdoc />
    /// <remarks>
    /// Most bots only engage monsters the navigator's safe-monster cap allows (roughly at or below
    /// the bot's own level). Without this, a bot travelling through hostile territory picks fights
    /// with monsters far above its level and dies repeatedly.
    /// <see cref="BotPersonality.Warrior"/> and <see cref="BotPersonality.Reckless"/> are
    /// deliberate exceptions: Warriors seek stronger foes for the challenge; Reckless bots
    /// simply don't care about the risk. Both types respawn automatically, so dying is acceptable.
    /// </remarks>
    public bool OnlyHuntSafeMonsters => this._personality switch
    {
        BotPersonality.Warrior => false,
        BotPersonality.Reckless => false,
        _ => true,
    };

    /// <inheritdoc />
    public bool PickUpgradeItems => true;
}
