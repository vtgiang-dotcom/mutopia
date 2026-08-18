---
slug: bot-personality-polish
status: pending
created: 2026-08-09
author: Antigravity (orchestrator)
---

# Task Brief: Bot Personality System — Polish & Fix Pass

## Context

The bot personality system was recently implemented in `d:\Project\mu\OpenMU-master`.
Six archetypes (Balanced, Greedy, Warrior, Loner, Guardian, Reckless) are now active.
This pass fixes 6 issues found during post-implementation review.
The build currently compiles with **0 errors**.

**DO NOT edit this brief file. Leave `status: pending`.**
**Write your report to: `d:\Project\mu\.gemini\antigravity\handoff\outbox\bot-personality-polish-report.md`**

---

## Rules for This Task

- Read each file before editing it (never blind-write).
- Use exact string replacement; do NOT rewrite whole files.
- After ALL edits are done, run the build verification command listed at the bottom.
- In your report, list each task with: Claim, Command run, Output (evidence).
- Do NOT write any claim you did not run a command to verify.

---

## Task 1 — Fix stale cref in BotPartyHandler.cs

**File**: `d:\Project\mu\OpenMU-master\src\GameLogic\Bots\BotPartyHandler.cs`
**Lines**: 9-12
**Problem**: Line 11 has `BotMuHelperSettings.AutoAcceptAnyone` — a broken cref. That class is no longer the production settings class. The interface `IMuHelperSettings` is the correct reference.

**TargetContent**:
```
/// <summary>
/// Lets a server-side bot party up with players who invite it (enabled by
/// <see cref="BotMuHelperSettings.AutoAcceptAnyone"/>): the invitation is accepted after a short
```

**ReplacementContent**:
```
/// <summary>
/// Lets a server-side bot party up with players who invite it (enabled by
/// <see cref="IMuHelperSettings.AutoAcceptAnyone"/>): the invitation is accepted after a short
```

**Verify after edit**:
```powershell
Select-String -Path "d:\Project\mu\OpenMU-master\src\GameLogic\Bots\BotPartyHandler.cs" -Pattern "BotMuHelperSettings.AutoAcceptAnyone"
```
Expected: **no output** (0 matches).

---

## Task 2 — Update test helper + add Loner test

**File**: `d:\Project\mu\OpenMU-master\tests\MUnique.OpenMU.Tests\Party\BotPartyHandlerTest.cs`

### 2a: Replace BotMuHelperSettings in CreateBotAsync helper

The private helper method `CreateBotAsync` at the bottom of the file (around line 188) uses the old class.

**TargetContent** (the entire helper method body):
```
    private static async ValueTask<OfflinePlayer> CreateBotAsync(IGameContext gameContext, string name, bool isBot = true)
    {
        var bot = await PlayerTestHelper.CreateOfflineLevelingPlayerAsync(gameContext).ConfigureAwait(false);
        await bot.PlayerState.TryAdvanceToAsync(PlayerState.EnteredWorld).ConfigureAwait(false);
        bot.SelectedCharacter!.Name = name;
        bot.IsAlive = true;
        bot.Account!.IsBot = isBot;
        bot.MuHelperSettings = new BotMuHelperSettings();
        return bot;
    }
```

**ReplacementContent**:
```
    private static async ValueTask<OfflinePlayer> CreateBotAsync(IGameContext gameContext, string name, bool isBot = true, BotPersonality personality = BotPersonality.Balanced)
    {
        var bot = await PlayerTestHelper.CreateOfflineLevelingPlayerAsync(gameContext).ConfigureAwait(false);
        await bot.PlayerState.TryAdvanceToAsync(PlayerState.EnteredWorld).ConfigureAwait(false);
        bot.SelectedCharacter!.Name = name;
        bot.IsAlive = true;
        bot.Account!.IsBot = isBot;
        bot.MuHelperSettings = new BotPersonalitySettings(personality);
        return bot;
    }
```

### 2b: Add Loner test BEFORE the CreateBotAsync helper

Find the line:
```
    private static async ValueTask<OfflinePlayer> CreateBotAsync(IGameContext gameContext, string name, bool isBot = true, BotPersonality personality = BotPersonality.Balanced)
```
(after 2a is applied, this is the new signature).

Insert the following block **immediately before** that line:

```
    /// <summary>
    /// A bot with the Loner personality rejects party invitations from players.
    /// AutoAcceptAnyone is false for Loner, so TryScheduleAcceptAsync returns false.
    /// </summary>
    [Test]
    public async ValueTask LonerPersonalityRejectsPlayerInviteAsync()
    {
        var gameContext = GameContextTestHelper.CreateGameContext();
        var bot = await CreateBotAsync(gameContext, "SoloBot", personality: BotPersonality.Loner).ConfigureAwait(false);
        var requester = await CreateHumanAsync(gameContext, "Human").ConfigureAwait(false);

        var scheduled = await BotPartyHandler.TryScheduleAcceptAsync(bot, requester, TimeSpan.Zero).ConfigureAwait(false);

        Assert.That(scheduled, Is.False);
        Assert.That(bot.PendingPartyInvite, Is.Null);
    }

```

**Verify after both 2a and 2b**:
```powershell
Select-String -Path "d:\Project\mu\OpenMU-master\tests\MUnique.OpenMU.Tests\Party\BotPartyHandlerTest.cs" -Pattern "BotMuHelperSettings|BotPersonalitySettings|LonerPersonality"
```
Expected: `BotMuHelperSettings` NOT present; `BotPersonalitySettings` and `LonerPersonality` PRESENT.

---

## Task 3 — Exclude Loner bots from FormPartiesAsync

**File**: `d:\Project\mu\OpenMU-master\src\GameLogic\Bots\BotManager.cs`
**Problem**: `FormPartiesAsync` forces ALL solo bots into bot-parties, including Loner bots. This contradicts the Loner archetype.

Find this block in `FormPartiesAsync`:
```
        var candidates = this._bots.Values
            .Where(b => b.Party is null && b.Attributes is not null)
            .OrderBy(BotResetHandler.GetEffectiveLevel)
            .ToList();
```

**ReplacementContent**:
```
        var candidates = this._bots.Values
            .Where(b => b.Party is null
                     && b.Attributes is not null
                     && (b.MuHelperSettings as BotPersonalitySettings)?.Personality != BotPersonality.Loner)
            .OrderBy(BotResetHandler.GetEffectiveLevel)
            .ToList();
```

**Verify**:
```powershell
Select-String -Path "d:\Project\mu\OpenMU-master\src\GameLogic\Bots\BotManager.cs" -Pattern "Loner"
```
Expected: At least one match showing the Loner exclusion.

---

## Task 4 — Mark BotMuHelperSettings as Obsolete

**File**: `d:\Project\mu\OpenMU-master\src\GameLogic\Bots\BotMuHelperSettings.cs`
**Lines**: 9-16

**TargetContent**:
```
/// <summary>
/// Default MU Helper settings used to drive a bot's combat AI.
/// A bot never sends a client-side MU Helper configuration, so without this the player would
/// fall back to a hunting range of a single tile (see <see cref="Offline.CombatHandler"/>).
/// These defaults make the bot hunt nearby monsters, pick up the valuable drops and use
/// potions, while staying close to its spawn origin.
/// </summary>
internal sealed class BotMuHelperSettings : IMuHelperSettings
```

**ReplacementContent**:
```
/// <summary>
/// Default MU Helper settings used to drive a bot's combat AI.
/// A bot never sends a client-side MU Helper configuration, so without this the player would
/// fall back to a hunting range of a single tile (see <see cref="Offline.CombatHandler"/>).
/// These defaults make the bot hunt nearby monsters, pick up the valuable drops and use
/// potions, while staying close to its spawn origin.
/// </summary>
/// <remarks>
/// Superseded by <see cref="BotPersonalitySettings"/>, which adds personality-driven overrides
/// for heal thresholds, shopping frequency, item pickup and party acceptance.
/// Production code uses <see cref="BotPersonalitySettings"/> exclusively; this class is kept
/// as a documented reference baseline.
/// </remarks>
[System.Obsolete("Production bots use BotPersonalitySettings(BotPersonality.Balanced). This class is a reference baseline only.")]
internal sealed class BotMuHelperSettings : IMuHelperSettings
```

**Verify**:
```powershell
Select-String -Path "d:\Project\mu\OpenMU-master\src\GameLogic\Bots\BotMuHelperSettings.cs" -Pattern "Obsolete|BotPersonalitySettings"
```
Expected: Both `Obsolete` and `BotPersonalitySettings` appear.

---

## Task 5 — Restore missing remarks in BotPersonalitySettings

**File**: `d:\Project\mu\OpenMU-master\src\GameLogic\Bots\BotPersonalitySettings.cs`

### 5a: ReturnToOriginalPosition

**TargetContent**:
```
    /// <inheritdoc />
    public bool ReturnToOriginalPosition => false;
```

**ReplacementContent**:
```
    /// <inheritdoc />
    /// <remarks>
    /// Disabled for bots: the <see cref="BotNavigator"/> is the sole driver of travel between hunting
    /// grounds, so the offline movement handler must not try to walk the bot back to its origin in parallel.
    /// </remarks>
    public bool ReturnToOriginalPosition => false;
```

### 5b: RepairItem

**TargetContent**:
```
    /// <inheritdoc />
    public bool RepairItem => false;
```

**ReplacementContent**:
```
    /// <inheritdoc />
    /// <remarks>
    /// Disabled on purpose: offline auto-repair has no NPC discount and drains Zen at an
    /// increased rate. Bots at the money cap lose that Zen permanently since sales top them
    /// back up immediately — the repair burns Zen without creating room to sell.
    /// </remarks>
    public bool RepairItem => false;
```

### 5c: AutoAcceptAnyone

**TargetContent**:
```
    /// <inheritdoc />
    public bool AutoAcceptAnyone => this._personality != BotPersonality.Loner;
```

**ReplacementContent**:
```
    /// <inheritdoc />
    /// <remarks>
    /// Most bots accept party invitations from any player like a friendly stranger would, within
    /// the safeguards applied by <see cref="BotPartyHandler"/> (busy check, re-validation on answer).
    /// <see cref="BotPersonality.Loner"/> bots refuse player invitations entirely — they are solo
    /// hunters by nature. Bot-only party formation (see BotManager.FormPartiesAsync) also
    /// excludes Loner bots, so they always hunt alone.
    /// </remarks>
    public bool AutoAcceptAnyone => this._personality != BotPersonality.Loner;
```

### 5d: OnlyHuntSafeMonsters

**TargetContent**:
```
    /// <inheritdoc />
    public bool OnlyHuntSafeMonsters => this._personality switch
    {
        BotPersonality.Warrior => false,
        BotPersonality.Reckless => false,
        _ => true,
    };
```

**ReplacementContent**:
```
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
```

**Verify all 4**:
```powershell
Select-String -Path "d:\Project\mu\OpenMU-master\src\GameLogic\Bots\BotPersonalitySettings.cs" -Pattern "<remarks>"
```
Expected: **4 matches** (one for each property above).

---

## Task 6 — Create BotPersonalityResolverTest.cs

**File to create (NEW)**:
`d:\Project\mu\OpenMU-master\tests\MUnique.OpenMU.Tests\BotPersonalityResolverTest.cs`

Write the following exact content:

```csharp
// <copyright file="BotPersonalityResolverTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.GameLogic.Bots;

/// <summary>
/// Tests <see cref="BotPersonalityResolver"/> — the deterministic name-to-personality mapper.
/// </summary>
[TestFixture]
public class BotPersonalityResolverTest
{
    /// <summary>
    /// A null character name always produces the safe fallback personality.
    /// </summary>
    [Test]
    public void NullNameReturnsBalanced()
    {
        var result = BotPersonalityResolver.Resolve(null);
        Assert.That(result, Is.EqualTo(BotPersonality.Balanced));
    }

    /// <summary>
    /// An empty or whitespace-only name also produces the safe fallback personality.
    /// </summary>
    [TestCase("")]
    [TestCase("   ")]
    public void EmptyOrWhitespaceNameReturnsBalanced(string name)
    {
        var result = BotPersonalityResolver.Resolve(name);
        Assert.That(result, Is.EqualTo(BotPersonality.Balanced));
    }

    /// <summary>
    /// The same name always produces the same personality across repeated calls.
    /// </summary>
    [TestCase("Joremir")]
    [TestCase("Valdris")]
    [TestCase("SoloBeast")]
    public void SameNameAlwaysProducesSamePersonality(string name)
    {
        var first = BotPersonalityResolver.Resolve(name);
        var second = BotPersonalityResolver.Resolve(new string(name.ToCharArray()));
        Assert.That(first, Is.EqualTo(second));
    }

    /// <summary>
    /// Resolve never returns a value outside the defined enum range (0-5).
    /// </summary>
    [Test]
    public void ResolvedPersonalityIsAlwaysADefinedEnumValue()
    {
        var names = new[] { "Alpha", "Beta", "Gamma", "Delta", "Epsilon", "Zeta", "Eta", "Theta", "Iota", "Kappa" };
        foreach (var name in names)
        {
            var p = BotPersonalityResolver.Resolve(name);
            Assert.That(Enum.IsDefined(p), Is.True, $"Name '{name}' resolved to undefined value {(int)p}");
        }
    }

    /// <summary>
    /// Ten distinct names should produce at least three distinct personalities,
    /// confirming the hash distributes across archetypes rather than clustering on one.
    /// </summary>
    [Test]
    public void TenDistinctNamesProduceAtLeastThreeDistinctPersonalities()
    {
        var names = new[] { "Alpha", "Beta", "Gamma", "Delta", "Epsilon", "Zeta", "Eta", "Theta", "Iota", "Kappa" };
        var distinct = names.Select(n => BotPersonalityResolver.Resolve(n)).Distinct().Count();
        Assert.That(distinct, Is.GreaterThanOrEqualTo(3), $"Expected distribution across at least 3 archetypes, got {distinct}");
    }
}
```

**Verify**:
```powershell
Test-Path "d:\Project\mu\OpenMU-master\tests\MUnique.OpenMU.Tests\BotPersonalityResolverTest.cs"
```
Expected: `True`

---

## Build + Test Verification (run AFTER all 6 tasks)

### Build check:
```powershell
cd d:\Project\mu
dotnet build OpenMU-master\src\GameLogic\MUnique.OpenMU.GameLogic.csproj --configuration Release --nologo 2>&1 | Select-String "(error|CS1574|BotMuHelperSettings.AutoAcceptAnyone|0 Error)"
```
Expected: `0 Error(s)`, no CS1574 for `BotMuHelperSettings.AutoAcceptAnyone`

### Test run:
```powershell
cd d:\Project\mu
dotnet test OpenMU-master\tests\MUnique.OpenMU.Tests\MUnique.OpenMU.Tests.csproj --filter "FullyQualifiedName~BotPartyHandler|FullyQualifiedName~BotPersonalityResolver" --configuration Release --nologo 2>&1 | Select-String "(Passed|Failed|passed|failed|Error)"
```
Expected: All Passed, 0 Failed

---

## Report Format

Write your report to:
`d:\Project\mu\.gemini\antigravity\handoff\outbox\bot-personality-polish-report.md`

```markdown
# Bot Personality Polish — Execution Report
Date: <date>

## Summary
<1-2 sentences>

## Task Results

| Task | Claim | Command run | Output |
|------|-------|-------------|--------|
| 1    | cref fixed | Select-String ... | (no output) |
| 2    | test updated + Loner test added | Select-String ... | ... |
| 3    | Loner excluded from FormParties | Select-String ... | ... |
| 4    | Obsolete added | Select-String ... | ... |
| 5    | 4 remarks restored | Select-String ... | 4 matches |
| 6    | ResolverTest created | Test-Path ... | True |

## Build Result
<paste>

## Test Result
<paste>

## Issues Encountered
<any problems or none>
```
