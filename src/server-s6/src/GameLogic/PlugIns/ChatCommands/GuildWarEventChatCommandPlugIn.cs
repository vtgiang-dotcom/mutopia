// <copyright file="GuildWarEventChatCommandPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.Bots;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.GameLogic.Offline;
using MUnique.OpenMU.GameLogic.Views.Character;
using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.Pathfinding;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Chat command plugin for GM to trigger automated Wave Defense Horde Battles for content creation.
/// Usage: /battle [partyCount] [countdownSeconds]
/// Example: /battle 4 20 (4 Parties = 20 Bots defending GM from 5 Waves of Monsters, 20s countdown)
/// Stop/Clean: /battle stop or /battle clear
/// </summary>
[Guid("D8E7C6B5-A4F3-4E2D-9C1B-8A7F6E5D4C3B")]
[PlugIn]
[Display(Name = "Guild War Event", Description = "Spawns defender parties with max gear and 40k stats to protect GM against monster waves.")]
[ChatCommandHelp(CommandKey, null, CharacterStatus.Normal)]
public class GuildWarEventChatCommandPlugIn : IChatCommandPlugIn
{
    private const string CommandKey = "/battle";
    private static readonly Dictionary<OfflinePlayer, Point> DefenderBots = new();
    private static readonly List<Party> ActiveParties = new();
    private static readonly List<Monster> ActiveMonsters = new();
    private static CancellationTokenSource? _activeWarCts;
    private static bool _isWarRunning;
    private static bool _isCombatStarted;

    /// <summary>
    /// Gets a value indicating whether a war event is currently running.
    /// </summary>
    public static bool IsWarRunning => _isWarRunning;

    /// <summary>
    /// Gets a value indicating whether combat has officially started after countdown.
    /// </summary>
    public static bool IsCombatStarted => _isCombatStarted;

    /// <summary>
    /// Checks if two players belong to the same defender faction (all defenders are allies).
    /// </summary>
    public static bool IsInSameFaction(Player p1, Player p2)
    {
        if (!_isWarRunning)
        {
            return false;
        }

        if (p1 is OfflinePlayer bot1 && p2 is OfflinePlayer bot2)
        {
            lock (DefenderBots)
            {
                if (DefenderBots.ContainsKey(bot1) && DefenderBots.ContainsKey(bot2))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if a player is an active defender bot in the war event.
    /// </summary>
    public static bool IsDefenderBot(Player player)
    {
        if (!_isWarRunning || player is not OfflinePlayer bot)
        {
            return false;
        }

        lock (DefenderBots)
        {
            return DefenderBots.ContainsKey(bot);
        }
    }

    /// <summary>
    /// Checks if two players belong to opposing war factions (no opposing players in defense mode).
    /// </summary>
    public static bool IsOpposingFaction(Player p1, Player p2)
    {
        return false;
    }

    /// <inheritdoc />
    public string Key => CommandKey;

    /// <inheritdoc />
    public CharacterStatus MinCharacterStatusRequirement => CharacterStatus.Normal;

    /// <inheritdoc />
    public async ValueTask HandleCommandAsync(Player player, string command)
    {
        try
        {
            var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var subCommand = parts.Length > 1 ? parts[1].ToLowerInvariant() : string.Empty;

            if (subCommand is "stop" or "clear" or "end" or "clean")
            {
                await CleanUpWarAsync(player).ConfigureAwait(false);
                return;
            }

            if (_isWarRunning)
            {
                await player.ShowBlueMessageAsync("[Đại Chiến] Đang có trận chiến diễn ra! Hãy gõ '/battle stop' để dọn dẹp trước khi tạo trận mới.").ConfigureAwait(false);
                return;
            }

            var partyCount = 4; // Mặc định 4 Party = 20 Bot
            if (int.TryParse(subCommand, out var parsedPartyCount) && parsedPartyCount > 0)
            {
                partyCount = Math.Clamp(parsedPartyCount, 1, 8);
            }

            var countdownSeconds = 20; // Mặc định 20s chuẩn bị góc máy
            if (parts.Length > 2 && int.TryParse(parts[2], out var parsedCountdown) && parsedCountdown > 0)
            {
                countdownSeconds = Math.Clamp(parsedCountdown, 5, 120);
            }

            await StartDefenseEventAsync(player, partyCount, countdownSeconds).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            player.Logger.LogError(ex, "Lỗi khi thực thi lệnh /battle");
            await player.ShowBlueMessageAsync($"[Đại Chiến] Lỗi: {ex.Message}").ConfigureAwait(false);
        }
    }

    private static async Task StartDefenseEventAsync(Player gm, int partyCount, int countdownSeconds)
    {
        _isWarRunning = true;
        _isCombatStarted = false;

        if (_activeWarCts is not null)
        {
            await _activeWarCts.CancelAsync().ConfigureAwait(false);
        }

        _activeWarCts = new CancellationTokenSource();
        var ct = _activeWarCts.Token;

        var gameContext = gm.GameContext;
        var map = gm.CurrentMap;
        var centerPos = gm.Position;

        if (map is null)
        {
            await gm.ShowBlueMessageAsync("[Đại Chiến] Lỗi: Không xác định được Map của GM!").ConfigureAwait(false);
            _isWarRunning = false;
            return;
        }

        var totalBotsNeeded = partyCount * 5;
        var botPlugin = gameContext.FeaturePlugIns.GetPlugIn<BotFeaturePlugIn>();
        var allBots = botPlugin?.GetAllActiveBots() ?? new List<BotPlayer>();

        if (allBots.Count < totalBotsNeeded)
        {
            var generator = new BotGenerator(gameContext, gm.Logger);
            await generator.EnsureBotsAsync(totalBotsNeeded, 1, ct).ConfigureAwait(false);
            if (botPlugin is not null)
            {
                botPlugin.Configuration ??= new BotConfiguration();
                botPlugin.Configuration.Enabled = true;
                botPlugin.ForceStart();
            }

            await Task.Delay(2000, ct).ConfigureAwait(false);
            allBots = botPlugin?.GetAllActiveBots() ?? new List<BotPlayer>();
        }

        var selectedBots = allBots.Take(totalBotsNeeded).ToList();
        if (selectedBots.Count < totalBotsNeeded)
        {
            await gm.ShowBlueMessageAsync($"[Đại Chiến] Cần {totalBotsNeeded} bot nhưng chỉ có {selectedBots.Count} bot sẵn sàng. Hãy thử lại sau vài giây.").ConfigureAwait(false);
            _isWarRunning = false;
            return;
        }

        lock (DefenderBots)
        {
            DefenderBots.Clear();
        }

        lock (ActiveParties)
        {
            ActiveParties.Clear();
        }

        lock (ActiveMonsters)
        {
            ActiveMonsters.Clear();
        }

        await gameContext.SendGlobalMessageAsync($"[PHÒNG THỦ ĐẠI CHIẾN] 🛡️ {partyCount} PARTY LIÊN MINH ({totalBotsNeeded} CHIẾN BINH 40K STATS FULL EXL +15) BẮT ĐẦU THIẾT LẬP VÀNH ĐAI BẢO VỆ GM!", MessageType.GoldenCenter).ConfigureAwait(false);

        try
        {
            // 1. Dàn trận các Party theo vòng tròn/4 hướng quanh GM
            for (int p = 0; p < partyCount; p++)
            {
                var partyBots = selectedBots.Skip(p * 5).Take(5).ToList();
                var partyLogger = gameContext.LoggerFactory.CreateLogger<Party>();
                var party = new Party(gameContext.PartyManager, 5, partyLogger);
                lock (ActiveParties)
                {
                    ActiveParties.Add(party);
                }

                var formulaType = p % 4; // 4 công thức phối hợp tối ưu

                for (int slot = 0; slot < 5; slot++)
                {
                    var bot = partyBots[slot];
                    var roleIndex = GetRoleForFormula(formulaType, slot);
                    var spawnPos = CalculateFormationPosition(centerPos, p, slot, partyCount);

                    await SetupDefenderBotAsync(bot, spawnPos, centerPos, map, roleIndex).ConfigureAwait(false);
                    await party.AddAsync(bot).ConfigureAwait(false);

                    lock (DefenderBots)
                    {
                        DefenderBots[bot] = spawnPos;
                    }
                }
            }

            // Dọn dẹp các bot không tham gia sự kiện trên cùng bản đồ về Safezone thị trấn
            if (botPlugin is not null)
            {
                var townGate = map.Definition.ExitGates.FirstOrDefault(g => g.IsSpawnGate) ?? map.Definition.ExitGates.FirstOrDefault();
                if (townGate is not null)
                {
                    foreach (var b in allBots)
                    {
                        if (!selectedBots.Contains(b) && ReferenceEquals(b.CurrentMap, map))
                        {
                            _ = b.WarpToAsync(townGate);
                        }
                    }
                }
            }

            await gm.ShowBlueMessageAsync($"[Đại Chiến] ✅ Đã dàn trận thành công {partyCount} Party ({totalBotsNeeded} bot)! GM có {countdownSeconds}s chuẩn bị góc máy.").ConfigureAwait(false);

            // 2. Đếm ngược chuẩn bị
            for (int countdown = countdownSeconds; countdown > 0; countdown--)
            {
                if (ct.IsCancellationRequested)
                {
                    return;
                }

                lock (DefenderBots)
                {
                    foreach (var (bot, pos) in DefenderBots)
                    {
                        if (bot.Position != pos)
                        {
                            bot.Position = pos;
                        }
                    }
                }

                if (countdown == countdownSeconds || countdown == 10 || countdown == 5 || countdown <= 3)
                {
                    await gameContext.SendGlobalMessageAsync($"[PHÒNG THỦ ĐẠI CHIẾN] ⏳ Đợt quái đầu tiên tấn công sau: {countdown}s...", MessageType.GoldenCenter).ConfigureAwait(false);
                }

                await Task.Delay(1000, ct).ConfigureAwait(false);
            }

            // 3. Bắt đầu kích hoạt chiến đấu & Spawn 5 Đợt Quái Vật
            _isCombatStarted = true;
            await gameContext.SendGlobalMessageAsync("[PHÒNG THỦ ĐẠI CHIẾN] ⚔️ CHIẾN ĐẤU BẮT ĐẦU! CÁC PARTY TOÀN LỰC TIÊU DIỆT LÀN SÓNG QUÁI VẬT!", MessageType.GoldenCenter).ConfigureAwait(false);

            _ = Task.Run(() => RunMonsterWavesLoopAsync(gameContext, map, centerPos, gm, partyCount, ct), ct);
        }
        catch (Exception ex)
        {
            gm.Logger.LogError(ex, "Error executing defense event");
            await gm.ShowBlueMessageAsync("[Đại Chiến] Lỗi khi tạo trận chiến: " + ex.Message).ConfigureAwait(false);
            _isWarRunning = false;
            _isCombatStarted = false;
        }
    }

    private static int GetRoleForFormula(int formulaType, int slot)
    {
        // 4 công thức Party 5 người chuẩn MU
        return formulaType switch
        {
            // Formula 0: Balanced Rainbow (DK Combo, DW Phép, Elf Buff, DL Chiến, MG Phép)
            0 => slot switch
            {
                0 => 0, // Blade Master Combo
                1 => 4, // Grand Master Magic
                2 => 7, // High Elf Buff (Max Ene)
                3 => 1, // Lord Emperor Battle
                _ => 3, // Duel Master Magic
            },
            // Formula 1: Striker Burst (DK Combo, RF Đấm xuyên giáp, Elf Buff, SUM Hút máu, DL)
            1 => slot switch
            {
                0 => 0, // Blade Master Combo
                1 => 2, // Fist Master Pierce
                2 => 7, // High Elf Buff
                3 => 5, // Dimension Master Curse
                _ => 1, // Lord Emperor Battle
            },
            // Formula 2: Magic & Ranged (DW AoE Mưa Băng Tuyết, Elf Chiến Agi, Elf Buff, MG Phép, SUM)
            2 => slot switch
            {
                0 => 4, // Grand Master Magic
                1 => 6, // High Elf Agility DPS
                2 => 7, // High Elf Buff
                3 => 3, // Duel Master Magic
                _ => 5, // Dimension Master Curse
            },
            // Formula 3: Titan Heavy Vanguard (DK Máu Tanker, DK Combo, Elf Buff, RF Bão Vũ, DL)
            _ => slot switch
            {
                0 => 8, // Blade Master Tanker
                1 => 0, // Blade Master Combo
                2 => 7, // High Elf Buff
                3 => 2, // Fist Master Pierce
                _ => 1, // Lord Emperor Battle
            },
        };
    }

    private static Point CalculateFormationPosition(Point centerPos, int partyIndex, int slotIndex, int totalParties)
    {
        // Phân bổ các Party theo các hướng quanh GM
        var angleOffset = (partyIndex * (2 * Math.PI / Math.Max(totalParties, 1))) + ((slotIndex - 2) * 0.18);
        var radius = 3.5 + (slotIndex % 2 == 0 ? 0.0 : 1.2);

        var targetX = centerPos.X + (int)Math.Round(Math.Cos(angleOffset) * radius);
        var targetY = centerPos.Y + (int)Math.Round(Math.Sin(angleOffset) * radius);

        return new Point((byte)Math.Clamp(targetX, 10, 240), (byte)Math.Clamp(targetY, 10, 240));
    }

    private static async Task RunMonsterWavesLoopAsync(IGameContext gameContext, GameMap map, Point centerPos, Player gm, int partyCount, CancellationToken ct)
    {
        var waves = GetWaveDefinitions(partyCount);

        for (int w = 0; w < waves.Count; w++)
        {
            if (!_isWarRunning || ct.IsCancellationRequested)
            {
                break;
            }

            var wave = waves[w];
            await gameContext.SendGlobalMessageAsync($"[LÀN SÓNG {w + 1}/{waves.Count}] 🚨 {wave.Title.ToUpperInvariant()} ĐANG TRÀN VÀO TRUNG TÂM! ({wave.Monsters.Sum(m => m.Quantity)} QUÁI VẬT)", MessageType.GoldenCenter).ConfigureAwait(false);

            // Spawn quái vật của wave từ vòng ngoài (bán kính 18-24)
            await SpawnWaveMonstersAsync(gameContext, map, centerPos, gm, wave, ct).ConfigureAwait(false);

            // Vòng lặp chờ dọn sạch wave hiện tại hoặc tối đa 90 giây
            var waveTimeout = DateTime.UtcNow.AddSeconds(90);
            while (_isWarRunning && !ct.IsCancellationRequested && DateTime.UtcNow < waveTimeout)
            {
                int livingMonsters;
                lock (ActiveMonsters)
                {
                    ActiveMonsters.RemoveAll(m => !m.IsAlive);
                    livingMonsters = ActiveMonsters.Count;
                }

                if (livingMonsters == 0)
                {
                    await gameContext.SendGlobalMessageAsync($"[LÀN SÓNG {w + 1}/{waves.Count}] ⭐ ĐÃ TIÊU DIỆT TOÀN BỘ QUÂN ĐỊCH CỦA ĐỢT {w + 1}!", MessageType.GoldenCenter).ConfigureAwait(false);
                    await Task.Delay(3000, ct).ConfigureAwait(false);
                    break;
                }

                await Task.Delay(1000, ct).ConfigureAwait(false);
            }
        }

        if (_isWarRunning && !ct.IsCancellationRequested)
        {
            await gameContext.SendGlobalMessageAsync("[PHÒNG THỦ ĐẠI CHIẾN] 🏆 TOÀN THẮNG! LIÊN MINH CÁC PARTY ĐÃ BẢO VỆ THÀNH CÔNG GM VÀ ĐẨY LÙI MỌI ĐỢT TẤN CÔNG CỦA QUÂN ĐOÀN QUÁI VẬT!", MessageType.GoldenCenter).ConfigureAwait(false);
        }
    }

    private static async Task SpawnWaveMonstersAsync(IGameContext gameContext, GameMap map, Point centerPos, Player gm, WaveDefinition wave, CancellationToken ct)
    {
        var spawnAngles = new[] { 0.0, Math.PI / 2, Math.PI, 3 * Math.PI / 2, Math.PI / 4, 3 * Math.PI / 4, 5 * Math.PI / 4, 7 * Math.PI / 4 };
        var angleIdx = 0;

        foreach (var entry in wave.Monsters)
        {
            if (ct.IsCancellationRequested)
            {
                return;
            }

            var monsterDef = gameContext.Configuration.Monsters.FirstOrDefault(m => m.Number == entry.MonsterNumber);
            if (monsterDef is null)
            {
                continue;
            }

            for (int i = 0; i < entry.Quantity; i++)
            {
                var angle = spawnAngles[angleIdx % spawnAngles.Length] + ((Rand.NextDouble() - 0.5) * 0.4);
                angleIdx++;
                var dist = 18.0 + (Rand.NextDouble() * 6.0);
                var spawnX = (byte)Math.Clamp(centerPos.X + (int)Math.Round(Math.Cos(angle) * dist), 10, 240);
                var spawnY = (byte)Math.Clamp(centerPos.Y + (int)Math.Round(Math.Sin(angle) * dist), 10, 240);

                var area = new MonsterSpawnArea
                {
                    GameMap = map.Definition,
                    MonsterDefinition = monsterDef,
                    SpawnTrigger = SpawnTrigger.OnceAtEventStart,
                    Quantity = 1,
                    X1 = spawnX,
                    X2 = spawnX,
                    Y1 = spawnY,
                    Y2 = spawnY,
                };

                var intelligence = new HordeMonsterIntelligence(centerPos, gm);
                var monster = new Monster(
                    area,
                    monsterDef,
                    map,
                    gameContext.DropGenerator,
                    intelligence,
                    gameContext.PlugInManager,
                    gameContext.PathFinderPool);

                intelligence.Npc = monster;
                monster.Initialize();
                await map.AddAsync(monster).ConfigureAwait(false);
                monster.OnSpawn();

                lock (ActiveMonsters)
                {
                    ActiveMonsters.Add(monster);
                }
            }
        }
    }

    private static CharacterClass? GetCharacterClassForRole(IGameContext context, int roleIndex)
    {
        var classes = context.Configuration.CharacterClasses;
        return roleIndex switch
        {
            0 => classes.FirstOrDefault(c => c.Number is 7 or 19) ?? classes.FirstOrDefault(c => c.Number is 6 or 17), // Blade Master (DK Cấp 3)
            1 => classes.FirstOrDefault(c => c.Number is 17 or 66) ?? classes.FirstOrDefault(c => c.Number is 16 or 64), // Lord Emperor (DL Cấp 3)
            2 => classes.FirstOrDefault(c => c.Number is 25 or 98) ?? classes.FirstOrDefault(c => c.Number is 24 or 96), // Fist Master (RF Cấp 3)
            3 => classes.FirstOrDefault(c => c.Number is 13 or 50) ?? classes.FirstOrDefault(c => c.Number is 12 or 48), // Duel Master (MG Cấp 3)
            4 => classes.FirstOrDefault(c => c.Number is 3) ?? classes.FirstOrDefault(c => c.Number is 2 or 1), // Grand Master (DW Cấp 3)
            5 => classes.FirstOrDefault(c => c.Number is 23 or 83) ?? classes.FirstOrDefault(c => c.Number is 22 or 80), // Dimension Master (SUM Cấp 3)
            6 => classes.FirstOrDefault(c => c.Number is 11 or 35) ?? classes.FirstOrDefault(c => c.Number is 10 or 33), // High Elf (Elf Chiến Cấp 3)
            7 => classes.FirstOrDefault(c => c.Number is 11 or 35) ?? classes.FirstOrDefault(c => c.Number is 10 or 33), // High Elf (Elf Buff Cấp 3)
            8 => classes.FirstOrDefault(c => c.Number is 7 or 19) ?? classes.FirstOrDefault(c => c.Number is 6 or 17), // Blade Master (DK Tank Cấp 3)
            _ => classes.FirstOrDefault(),
        };
    }

    private static async ValueTask SetupDefenderBotAsync(OfflinePlayer bot, Point position, Point centerPos, GameMap targetMap, int roleIndex)
    {
        if (bot.Attributes is null || bot.SelectedCharacter is not { } character)
        {
            return;
        }

        if (GetCharacterClassForRole(bot.GameContext, roleIndex) is { } targetClass)
        {
            character.CharacterClass = targetClass;
        }

        var characterClass = character.CharacterClass;
        if (characterClass is null)
        {
            return;
        }

        character.State = HeroState.Normal;
        character.PlayerKillCount = 0;
        character.StateRemainingSeconds = 0;
        bot.Attributes[Stats.Level] = 400;
        if (character.Attributes.FirstOrDefault(a => a.Definition == Stats.Level) is { } levelAttr)
        {
            levelAttr.Value = 400;
        }

        // CỘNG 40.000 STATS TỐI ƯU THEO TỪNG NHÁNH VÀ CLASS
        ApplyOptimized40kStats(bot, roleIndex);

        bot.Attributes[Stats.CurrentHealth] = bot.Attributes[Stats.MaximumHealth];
        bot.Attributes[Stats.CurrentMana] = bot.Attributes[Stats.MaximumMana];

        await EquipDiverseWarGearAsync(bot, roleIndex).ConfigureAwait(false);
        await EquipWarSkillsAsync(bot).ConfigureAwait(false);
        await EquipWarPotionsAsync(bot).ConfigureAwait(false);
        await bot.ForEachWorldObserverAsync<IUpdateCharacterHeroStatePlugIn>(o => o.UpdateCharacterHeroStateAsync(bot), true).ConfigureAwait(false);

        if (!ReferenceEquals(bot.CurrentMap, targetMap))
        {
            var gate = targetMap.Definition.ExitGates.FirstOrDefault(g => g.IsSpawnGate) ?? targetMap.Definition.ExitGates.FirstOrDefault();
            if (gate is not null)
            {
                await bot.WarpToAsync(gate).ConfigureAwait(false);
                await bot.ClientReadyAfterMapChangeAsync().ConfigureAwait(false);
            }
        }

        character.PositionX = position.X;
        character.PositionY = position.Y;
        bot.Position = position;
        bot.HuntingOrigin = position;

        if (bot.CurrentMap is { } botMap)
        {
            await botMap.RespawnAsync(bot).ConfigureAwait(false);
        }
    }

    private static void ApplyOptimized40kStats(OfflinePlayer bot, int roleIndex)
    {
        if (bot.Attributes is null)
        {
            return;
        }

        switch (roleIndex)
        {
            case 7: // High Elf (Elf Buff Max Energy & Hồi Máu)
                bot.Attributes[Stats.BaseStrength] = 2000;
                bot.Attributes[Stats.BaseAgility] = 8000;
                bot.Attributes[Stats.BaseVitality] = 5000;
                bot.Attributes[Stats.BaseEnergy] = 25000;
                break;
            case 6: // High Elf (Elf Chiến Cung Nỏ Agility Max Speed & DPS)
                bot.Attributes[Stats.BaseStrength] = 3000;
                bot.Attributes[Stats.BaseAgility] = 25000;
                bot.Attributes[Stats.BaseVitality] = 5000;
                bot.Attributes[Stats.BaseEnergy] = 7000;
                break;
            case 0: // Blade Master (DK Combo Sát Thương Str/Agi)
                bot.Attributes[Stats.BaseStrength] = 20000;
                bot.Attributes[Stats.BaseAgility] = 12000;
                bot.Attributes[Stats.BaseVitality] = 6000;
                bot.Attributes[Stats.BaseEnergy] = 2000;
                break;
            case 8: // Blade Master (DK Máu Tanker Gồng HP)
                bot.Attributes[Stats.BaseStrength] = 10000;
                bot.Attributes[Stats.BaseAgility] = 8000;
                bot.Attributes[Stats.BaseVitality] = 15000;
                bot.Attributes[Stats.BaseEnergy] = 7000;
                break;
            case 4: // Grand Master (DW Phép Thuật Energy AoE)
                bot.Attributes[Stats.BaseStrength] = 2000;
                bot.Attributes[Stats.BaseAgility] = 12000;
                bot.Attributes[Stats.BaseVitality] = 6000;
                bot.Attributes[Stats.BaseEnergy] = 20000;
                break;
            case 1: // Lord Emperor (DL Chiến Mã & Chỉ Huy)
                bot.Attributes[Stats.BaseStrength] = 15000;
                bot.Attributes[Stats.BaseAgility] = 10000;
                bot.Attributes[Stats.BaseVitality] = 5000;
                bot.Attributes[Stats.BaseEnergy] = 6000;
                bot.Attributes[Stats.BaseLeadership] = 4000;
                break;
            case 2: // Fist Master (RF Đấm Xuyên Giáp Sát Thương Vật Lý)
                bot.Attributes[Stats.BaseStrength] = 18000;
                bot.Attributes[Stats.BaseAgility] = 10000;
                bot.Attributes[Stats.BaseVitality] = 8000;
                bot.Attributes[Stats.BaseEnergy] = 4000;
                break;
            case 5: // Dimension Master (Summoner Hút Máu & Giảm Thủ)
                bot.Attributes[Stats.BaseStrength] = 2000;
                bot.Attributes[Stats.BaseAgility] = 12000;
                bot.Attributes[Stats.BaseVitality] = 6000;
                bot.Attributes[Stats.BaseEnergy] = 20000;
                break;
            case 3: // Duel Master (MG Phép Bão Điện & Sát Thương Phép)
                bot.Attributes[Stats.BaseStrength] = 3000;
                bot.Attributes[Stats.BaseAgility] = 12000;
                bot.Attributes[Stats.BaseVitality] = 4000;
                bot.Attributes[Stats.BaseEnergy] = 21000;
                break;
            default: // Mặc định cân bằng 40k
                bot.Attributes[Stats.BaseStrength] = 12000;
                bot.Attributes[Stats.BaseAgility] = 12000;
                bot.Attributes[Stats.BaseVitality] = 8000;
                bot.Attributes[Stats.BaseEnergy] = 8000;
                break;
        }
    }

    private static async ValueTask EquipDiverseWarGearAsync(OfflinePlayer bot, int roleIndex)
    {
        if (bot.Inventory is not { } inventory || bot.SelectedCharacter is null)
        {
            return;
        }

        var context = bot.PersistenceContext;
        var itemsConfig = bot.GameContext.Configuration.Items;

        for (byte slot = InventoryConstants.FirstEquippableItemSlotIndex; slot <= InventoryConstants.LastEquippableItemSlotIndex; slot++)
        {
            if (inventory.GetItem(slot) is { } oldItem)
            {
                await inventory.RemoveItemAsync(oldItem).ConfigureAwait(false);
                await context.DeleteAsync(oldItem).ConfigureAwait(false);
            }
        }

        async ValueTask EquipItemAsync(byte group, short number, byte slot)
        {
            if (itemsConfig.FirstOrDefault(d => d.Group == group && d.Number == number) is { } def)
            {
                var item = context.CreateNew<Item>();
                item.Definition = def;
                item.Level = 15;
                item.Durability = def.Durability;
                item.ItemSlot = slot;
                await inventory.AddItemAsync(slot, item).ConfigureAwait(false);
            }
        }

        // Đa dạng hóa bộ Set +15 ngẫu nhiên cho từng vai trò
        var variant = Rand.NextInt(0, 3);
        switch (roleIndex)
        {
            case 0: // Blade Master (DK Combo)
            case 8: // Blade Master (DK Tank)
                await EquipItemAsync(12, 36, InventoryConstants.WingsSlot).ConfigureAwait(false); // Wings of Storm
                if (variant == 0)
                {
                    await EquipItemAsync(0, 22, InventoryConstants.LeftHandSlot).ConfigureAwait(false); // Bone Blade
                    await EquipItemAsync(0, 22, InventoryConstants.RightHandSlot).ConfigureAwait(false);
                    await EquipItemAsync(7, 29, InventoryConstants.HelmSlot).ConfigureAwait(false); // Dragon Knight Set
                    await EquipItemAsync(8, 29, InventoryConstants.ArmorSlot).ConfigureAwait(false);
                    await EquipItemAsync(9, 29, InventoryConstants.PantsSlot).ConfigureAwait(false);
                    await EquipItemAsync(10, 29, InventoryConstants.GlovesSlot).ConfigureAwait(false);
                    await EquipItemAsync(11, 29, InventoryConstants.BootsSlot).ConfigureAwait(false);
                }
                else if (variant == 1)
                {
                    await EquipItemAsync(0, 19, InventoryConstants.LeftHandSlot).ConfigureAwait(false); // Great Dragon Sword
                    await EquipItemAsync(0, 19, InventoryConstants.RightHandSlot).ConfigureAwait(false);
                    await EquipItemAsync(7, 21, InventoryConstants.HelmSlot).ConfigureAwait(false); // Great Dragon Set
                    await EquipItemAsync(8, 21, InventoryConstants.ArmorSlot).ConfigureAwait(false);
                    await EquipItemAsync(9, 21, InventoryConstants.PantsSlot).ConfigureAwait(false);
                    await EquipItemAsync(10, 21, InventoryConstants.GlovesSlot).ConfigureAwait(false);
                    await EquipItemAsync(11, 21, InventoryConstants.BootsSlot).ConfigureAwait(false);
                }
                else
                {
                    await EquipItemAsync(0, 21, InventoryConstants.LeftHandSlot).ConfigureAwait(false); // Knight Blade
                    await EquipItemAsync(6, 16, InventoryConstants.RightHandSlot).ConfigureAwait(false); // Dragon Shield
                    await EquipItemAsync(7, 36, InventoryConstants.HelmSlot).ConfigureAwait(false); // Brave Set
                    await EquipItemAsync(8, 36, InventoryConstants.ArmorSlot).ConfigureAwait(false);
                    await EquipItemAsync(9, 36, InventoryConstants.PantsSlot).ConfigureAwait(false);
                    await EquipItemAsync(10, 36, InventoryConstants.GlovesSlot).ConfigureAwait(false);
                    await EquipItemAsync(11, 36, InventoryConstants.BootsSlot).ConfigureAwait(false);
                }
                break;

            case 1: // Lord Emperor (DL)
                await EquipItemAsync(12, 40, InventoryConstants.WingsSlot).ConfigureAwait(false); // Mantle of Monarch
                await EquipItemAsync(13, 4, InventoryConstants.PetSlot).ConfigureAwait(false); // Dark Horse
                if (variant == 0)
                {
                    await EquipItemAsync(2, 13, InventoryConstants.LeftHandSlot).ConfigureAwait(false); // Shining Scepter
                    await EquipItemAsync(7, 33, InventoryConstants.HelmSlot).ConfigureAwait(false); // Soleil Set
                    await EquipItemAsync(8, 33, InventoryConstants.ArmorSlot).ConfigureAwait(false);
                    await EquipItemAsync(9, 33, InventoryConstants.PantsSlot).ConfigureAwait(false);
                    await EquipItemAsync(10, 33, InventoryConstants.GlovesSlot).ConfigureAwait(false);
                    await EquipItemAsync(11, 33, InventoryConstants.BootsSlot).ConfigureAwait(false);
                }
                else
                {
                    await EquipItemAsync(2, 11, InventoryConstants.LeftHandSlot).ConfigureAwait(false); // Great Lord Scepter
                    await EquipItemAsync(7, 26, InventoryConstants.HelmSlot).ConfigureAwait(false); // Dark Steel Set
                    await EquipItemAsync(8, 26, InventoryConstants.ArmorSlot).ConfigureAwait(false);
                    await EquipItemAsync(9, 26, InventoryConstants.PantsSlot).ConfigureAwait(false);
                    await EquipItemAsync(10, 26, InventoryConstants.GlovesSlot).ConfigureAwait(false);
                    await EquipItemAsync(11, 26, InventoryConstants.BootsSlot).ConfigureAwait(false);
                }
                break;

            case 2: // Fist Master (RF)
                await EquipItemAsync(12, 50, InventoryConstants.WingsSlot).ConfigureAwait(false); // Cape of Overrule
                await EquipItemAsync(0, 35, InventoryConstants.LeftHandSlot).ConfigureAwait(false); // Phoenix Soul Star
                await EquipItemAsync(0, 35, InventoryConstants.RightHandSlot).ConfigureAwait(false);
                await EquipItemAsync(7, 73, InventoryConstants.HelmSlot).ConfigureAwait(false); // Phoenix Soul Set
                await EquipItemAsync(8, 73, InventoryConstants.ArmorSlot).ConfigureAwait(false);
                await EquipItemAsync(9, 73, InventoryConstants.PantsSlot).ConfigureAwait(false);
                await EquipItemAsync(11, 73, InventoryConstants.BootsSlot).ConfigureAwait(false);
                break;

            case 3: // Duel Master (MG)
                await EquipItemAsync(12, 39, InventoryConstants.WingsSlot).ConfigureAwait(false); // Wings of Ruin
                if (variant == 0)
                {
                    await EquipItemAsync(0, 24, InventoryConstants.LeftHandSlot).ConfigureAwait(false); // Explosion Blade
                    await EquipItemAsync(0, 24, InventoryConstants.RightHandSlot).ConfigureAwait(false);
                    await EquipItemAsync(8, 32, InventoryConstants.ArmorSlot).ConfigureAwait(false); // Volcano Set
                    await EquipItemAsync(9, 32, InventoryConstants.PantsSlot).ConfigureAwait(false);
                    await EquipItemAsync(10, 32, InventoryConstants.GlovesSlot).ConfigureAwait(false);
                    await EquipItemAsync(11, 32, InventoryConstants.BootsSlot).ConfigureAwait(false);
                }
                else
                {
                    await EquipItemAsync(0, 20, InventoryConstants.LeftHandSlot).ConfigureAwait(false); // Rune Bastard Sword
                    await EquipItemAsync(0, 20, InventoryConstants.RightHandSlot).ConfigureAwait(false);
                    await EquipItemAsync(8, 25, InventoryConstants.ArmorSlot).ConfigureAwait(false); // Hurricane Set
                    await EquipItemAsync(9, 25, InventoryConstants.PantsSlot).ConfigureAwait(false);
                    await EquipItemAsync(10, 25, InventoryConstants.GlovesSlot).ConfigureAwait(false);
                    await EquipItemAsync(11, 25, InventoryConstants.BootsSlot).ConfigureAwait(false);
                }
                break;

            case 4: // Grand Master (DW)
                await EquipItemAsync(12, 37, InventoryConstants.WingsSlot).ConfigureAwait(false); // Wings of Eternal
                if (variant == 0)
                {
                    await EquipItemAsync(5, 13, InventoryConstants.LeftHandSlot).ConfigureAwait(false); // Kundun Staff
                    await EquipItemAsync(6, 15, InventoryConstants.RightHandSlot).ConfigureAwait(false); // Grand Soul Shield
                    await EquipItemAsync(7, 30, InventoryConstants.HelmSlot).ConfigureAwait(false); // Venom Mist Set
                    await EquipItemAsync(8, 30, InventoryConstants.ArmorSlot).ConfigureAwait(false);
                    await EquipItemAsync(9, 30, InventoryConstants.PantsSlot).ConfigureAwait(false);
                    await EquipItemAsync(10, 30, InventoryConstants.GlovesSlot).ConfigureAwait(false);
                    await EquipItemAsync(11, 30, InventoryConstants.BootsSlot).ConfigureAwait(false);
                }
                else
                {
                    await EquipItemAsync(5, 9, InventoryConstants.LeftHandSlot).ConfigureAwait(false); // Staff of Destruction
                    await EquipItemAsync(6, 15, InventoryConstants.RightHandSlot).ConfigureAwait(false);
                    await EquipItemAsync(7, 17, InventoryConstants.HelmSlot).ConfigureAwait(false); // Grand Soul Set
                    await EquipItemAsync(8, 17, InventoryConstants.ArmorSlot).ConfigureAwait(false);
                    await EquipItemAsync(9, 17, InventoryConstants.PantsSlot).ConfigureAwait(false);
                    await EquipItemAsync(10, 17, InventoryConstants.GlovesSlot).ConfigureAwait(false);
                    await EquipItemAsync(11, 17, InventoryConstants.BootsSlot).ConfigureAwait(false);
                }
                break;

            case 5: // Dimension Master (SUM)
                await EquipItemAsync(12, 43, InventoryConstants.WingsSlot).ConfigureAwait(false); // Wings of Dimension
                await EquipItemAsync(5, 33, InventoryConstants.LeftHandSlot).ConfigureAwait(false); // Red Wing Stick
                await EquipItemAsync(5, 34, InventoryConstants.RightHandSlot).ConfigureAwait(false); // Book of Neil
                await EquipItemAsync(7, 35, InventoryConstants.HelmSlot).ConfigureAwait(false); // Storm Jahad Set
                await EquipItemAsync(8, 35, InventoryConstants.ArmorSlot).ConfigureAwait(false);
                await EquipItemAsync(9, 35, InventoryConstants.PantsSlot).ConfigureAwait(false);
                await EquipItemAsync(10, 35, InventoryConstants.GlovesSlot).ConfigureAwait(false);
                await EquipItemAsync(11, 35, InventoryConstants.BootsSlot).ConfigureAwait(false);
                break;

            case 6: // High Elf (Elf Chiến)
            case 7: // High Elf (Elf Buff)
            default:
                await EquipItemAsync(12, 38, InventoryConstants.WingsSlot).ConfigureAwait(false); // Wings of Illusion
                if (variant == 0)
                {
                    await EquipItemAsync(4, 21, InventoryConstants.RightHandSlot).ConfigureAwait(false); // Sylph Wind Bow
                    await EquipItemAsync(7, 31, InventoryConstants.HelmSlot).ConfigureAwait(false); // Sylpid Ray Set
                    await EquipItemAsync(8, 31, InventoryConstants.ArmorSlot).ConfigureAwait(false);
                    await EquipItemAsync(9, 31, InventoryConstants.PantsSlot).ConfigureAwait(false);
                    await EquipItemAsync(10, 31, InventoryConstants.GlovesSlot).ConfigureAwait(false);
                    await EquipItemAsync(11, 31, InventoryConstants.BootsSlot).ConfigureAwait(false);
                }
                else
                {
                    await EquipItemAsync(4, 20, InventoryConstants.RightHandSlot).ConfigureAwait(false); // Albatross Bow
                    await EquipItemAsync(7, 18, InventoryConstants.HelmSlot).ConfigureAwait(false); // Divine Set
                    await EquipItemAsync(8, 18, InventoryConstants.ArmorSlot).ConfigureAwait(false);
                    await EquipItemAsync(9, 18, InventoryConstants.PantsSlot).ConfigureAwait(false);
                    await EquipItemAsync(10, 18, InventoryConstants.GlovesSlot).ConfigureAwait(false);
                    await EquipItemAsync(11, 18, InventoryConstants.BootsSlot).ConfigureAwait(false);
                }
                break;
        }
    }

    private static async ValueTask EquipWarSkillsAsync(OfflinePlayer bot)
    {
        if (bot.SelectedCharacter is not { CharacterClass: { } characterClass } character)
        {
            return;
        }

        var context = bot.PersistenceContext;
        var existingSkillIds = new HashSet<short>(character.LearnedSkills.Select(s => s.Skill!.Number));
        var qualifiedSkills = bot.GameContext.Configuration.Skills
            .Where(s => s.QualifiedCharacters.Contains(characterClass) && !existingSkillIds.Contains(s.Number) && s.MasterDefinition is null && !BotProgression.IsCastleSiegeOnly(s))
            .ToList();

        foreach (var skill in qualifiedSkills)
        {
            var entry = context.CreateNew<SkillEntry>();
            entry.Skill = skill;
            entry.Level = 0;
            character.LearnedSkills.Add(entry);
        }
    }

    private static async ValueTask EquipWarPotionsAsync(OfflinePlayer bot)
    {
        if (bot.Inventory is not { } inventory)
        {
            return;
        }

        var context = bot.PersistenceContext;
        var hpDef = bot.GameContext.Configuration.Items.FirstOrDefault(i => i.Group == 14 && i.Number == 3); // Large Healing Potion
        var mpDef = bot.GameContext.Configuration.Items.FirstOrDefault(i => i.Group == 14 && i.Number == 6); // Large Mana Potion

        // Cung cấp 4 stack Máu Lớn (Mỗi stack 200 bình)
        if (hpDef is not null)
        {
            for (int i = 0; i < 4; i++)
            {
                var hpItem = context.CreateNew<Item>();
                hpItem.Definition = hpDef;
                hpItem.Durability = 200;
                if (!await inventory.AddItemAsync(hpItem).ConfigureAwait(false))
                {
                    await context.DeleteAsync(hpItem).ConfigureAwait(false);
                    break;
                }
            }
        }

        // Cung cấp 4 stack Mana Lớn (Mỗi stack 200 bình)
        if (mpDef is not null)
        {
            for (int i = 0; i < 4; i++)
            {
                var mpItem = context.CreateNew<Item>();
                mpItem.Definition = mpDef;
                mpItem.Durability = 200;
                if (!await inventory.AddItemAsync(mpItem).ConfigureAwait(false))
                {
                    await context.DeleteAsync(mpItem).ConfigureAwait(false);
                    break;
                }
            }
        }

        // Cung cấp tên cho Elf
        var arrowDef = bot.GameContext.Configuration.Items.FirstOrDefault(i => i.Group == 4 && i.Number == 15);
        if (arrowDef is not null && (bot.SelectedCharacter?.CharacterClass?.Number is 2 or 3 or 6 or 7 or 18 or 19))
        {
            var arrowItem = context.CreateNew<Item>();
            arrowItem.Definition = arrowDef;
            arrowItem.Durability = 255;
            if (!await inventory.AddItemAsync(arrowItem).ConfigureAwait(false))
            {
                await context.DeleteAsync(arrowItem).ConfigureAwait(false);
            }
        }
    }

    private static List<WaveDefinition> GetWaveDefinitions(int partyCount)
    {
        var scale = Math.Max(1, partyCount);
        return new List<WaveDefinition>
        {
            new("Đợt 1: Binh Đoàn Rồng Đỏ & Chiến Binh Hoàng Kim Tiên Phong", new List<(short, int)>
            {
                (79, 10 * scale), // Golden Dragon
                (78, 12 * scale), // Golden Goblin
                (53, 10 * scale), // Golden Titan
                (54, 12 * scale), // Golden Soldier
                (41, 8 * scale),  // Red Dragon
            }),
            new("Đợt 2: Quân Đoàn Thủy Quái Hoàng Kim & Thần Chết Death Beam Knight", new List<(short, int)>
            {
                (80, 10 * scale), // Golden Lizard King
                (81, 10 * scale), // Golden Vepar
                (83, 12 * scale), // Golden Wheel
                (49, 10 * scale), // Death Beam Knight
                (41, 8 * scale),  // Red Dragon
            }),
            new("Đợt 3: Quân Đoàn Hoàng Kim Tantallos & Ma Quái Tarkan - Rừng Aida", new List<(short, int)>
            {
                (82, 12 * scale), // Golden Tantallos
                (58, 12 * scale), // Iron Wheel
                (57, 12 * scale), // Mutant
                (60, 12 * scale), // Bloody Wolf
                (49, 10 * scale), // Death Beam Knight
            }),
            new("Đợt 4: Liên Minh Ma Vương & Chúa Tể Địa Ngục (Balrog, Zaikan, Hell Maine, Rồng Vàng)", new List<(short, int)>
            {
                (40, 4 * scale),  // Balrog
                (44, 4 * scale),  // Zaikan
                (309, 4 * scale), // Hell Maine
                (79, 12 * scale), // Golden Dragon
                (82, 12 * scale), // Golden Tantallos
                (41, 10 * scale), // Red Dragon
                (49, 10 * scale), // Death Beam Knight
            }),
            new("Đợt 5: ĐẠI CHIẾN HUYỀN THOẠI — TỔNG LỰC ĐẠI MA VƯƠNG KUNDUN, MEDUSA, EROHIM, NIGHTMARE & QUÂN ĐOÀN HOÀNG KIM TỐI THƯỢNG", new List<(short, int)>
            {
                (275, Math.Max(2, scale)), // Kundun
                (561, Math.Max(2, scale)), // Medusa
                (361, Math.Max(2, scale)), // Erohim
                (363, Math.Max(2, scale)), // Nightmare
                (40, 4 * scale),           // Balrog
                (44, 4 * scale),           // Zaikan
                (309, 4 * scale),          // Hell Maine
                (79, 15 * scale),          // Golden Dragon
                (82, 15 * scale),          // Golden Tantallos
                (80, 15 * scale),          // Golden Lizard King
                (41, 12 * scale),          // Red Dragon
                (49, 12 * scale),          // Death Beam Knight
            }),
        };
    }

    private static async Task CleanUpWarAsync(Player gm)
    {
        if (_activeWarCts is not null)
        {
            await _activeWarCts.CancelAsync().ConfigureAwait(false);
        }

        _isWarRunning = false;
        _isCombatStarted = false;

        // 1. Xóa toàn bộ quái vật event trên map
        lock (ActiveMonsters)
        {
            foreach (var monster in ActiveMonsters)
            {
                try
                {
                    if (monster.CurrentMap is { } map)
                    {
                        _ = map.RemoveAsync(monster);
                    }

                    monster.Dispose();
                }
                catch (Exception ex)
                {
                    gm.Logger.LogWarning(ex, "Error disposing wave monster {Id}", monster.Id);
                }
            }

            ActiveMonsters.Clear();
        }

        // 2. Giải tán các Party
        lock (ActiveParties)
        {
            foreach (var party in ActiveParties)
            {
                try
                {
                    _ = party.DisposeAsync();
                }
                catch (Exception ex)
                {
                    gm.Logger.LogWarning(ex, "Error disposing party");
                }
            }

            ActiveParties.Clear();
        }

        List<OfflinePlayer> allWarBots;
        lock (DefenderBots)
        {
            allWarBots = DefenderBots.Keys.ToList();
            DefenderBots.Clear();
        }

        var botPlugin = gm.GameContext.FeaturePlugIns.GetPlugIn<BotFeaturePlugIn>();
        var botsToClean = new HashSet<OfflinePlayer>(allWarBots);
        if (botPlugin is not null)
        {
            foreach (var b in botPlugin.GetAllActiveBots())
            {
                if (b.Attributes?[Stats.Level] == 400 || b.SelectedCharacter?.Attributes.Any(a => a.Definition == Stats.Level && a.Value == 400) == true || allWarBots.Contains(b))
                {
                    botsToClean.Add(b);
                }
            }
        }

        // 3. Dọn dẹp bot chiến trường
        foreach (var bot in botsToClean)
        {
            try
            {
                if (bot.CurrentMap is { } map)
                {
                    await map.RemoveAsync(bot).ConfigureAwait(false);
                }

                await bot.DisconnectAsync().ConfigureAwait(false);
                await bot.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                gm.Logger.LogWarning(ex, "Error disposing war bot {Name}", bot.Name);
            }
        }

        try
        {
            var generator = new BotGenerator(gm.GameContext, gm.Logger);
            await generator.DeleteAllBotsAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            gm.Logger.LogWarning(ex, "Error clearing old bot database records.");
        }

        if (botPlugin?.Configuration is not null)
        {
            botPlugin.Configuration.Enabled = true;
            botPlugin.ForceStart();
        }

        await gm.GameContext.SendGlobalMessageAsync("[PHÒNG THỦ ĐẠI CHIẾN] 🧹 SỰ KIỆN KẾT THÚC! Toàn bộ Quái vật và Chiến binh phòng thủ đã được dọn dẹp.", MessageType.GoldenCenter).ConfigureAwait(false);
        await gm.ShowBlueMessageAsync("[Đại Chiến] ✅ Đã dọn dẹp sạch sẽ 100% chiến trường & quái vật đợt sóng!").ConfigureAwait(false);
    }

    private record WaveDefinition(string Title, List<(short MonsterNumber, int Quantity)> Monsters);

    /// <summary>
    /// Custom Monster AI which marches towards the GM beacon position and fights defender bots, ignoring the GM.
    /// </summary>
    private class HordeMonsterIntelligence : BasicMonsterIntelligence
    {
        private readonly Point _beaconPosition;
        private readonly Player _gmPlayer;

        public HordeMonsterIntelligence(Point beaconPosition, Player gmPlayer)
        {
            this._beaconPosition = beaconPosition;
            this._gmPlayer = gmPlayer;
        }

        protected override async ValueTask<IAttackable?> SearchNextTargetAsync()
        {
            var target = await base.SearchNextTargetAsync().ConfigureAwait(false);
            if (target is Player p && (p.SelectedCharacter?.CharacterStatus == CharacterStatus.GameMaster || ReferenceEquals(p, this._gmPlayer)))
            {
                return null;
            }

            return target;
        }

        public override void RegisterHit(IAttacker attacker)
        {
            if (attacker is Player p && (p.SelectedCharacter?.CharacterStatus == CharacterStatus.GameMaster || ReferenceEquals(p, this._gmPlayer)))
            {
                return;
            }

            base.RegisterHit(attacker);
        }

        protected override async ValueTask TickWithoutTargetAsync()
        {
            if (this.Monster.Attributes[Stats.IsFrozen] > 0 || this.Monster.IsWalking)
            {
                return;
            }

            // Hành quân về hướng GM cọc tiêu
            if (this.Monster.GetDistanceTo(this._beaconPosition) > 3)
            {
                await this.Monster.WalkToAsync(this._beaconPosition).ConfigureAwait(false);
            }
            else
            {
                await base.TickWithoutTargetAsync().ConfigureAwait(false);
            }
        }
    }
}
