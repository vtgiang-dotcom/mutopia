# Memory Index

> Persistent cross-session memory for THIS project. Full history archived at
> `.kilo/memory/decisions-archive.md` (grep-able, not auto-loaded).

## Project
- Dual-realm MU Online: Season 6 (C# OpenMU, `src/server-s6`+`src/client-s6`) + Season 16 (C++ LgdMu, `src/server-s16`+`src/client-s16`). Runtime lives outside repo at `D:\Project\mutopia\`.

## Rules
- Load AGENTS.md for all behavior rules → [[AGENTS.md]]

## Key Decisions (most recent)

- **S16 Multi-Realm Strategy (2026-08-19)**: `src/server-s16` (C++) compiles clean, 63-table schema validated. Client options compared: `client-s16`/Lgd-Client (patched, works), `MuEmu-master` (C# port, compiles clean), `MU VoZ SS16.1` (recommended production client, no proprietary wrapper). Two paths: (A) native S16 + VoZ client, (B) port formulas into C# MuEmu.

- **S16 Runtime Restored (2026-08-18)**: Fixed 3 root causes for GameServer boot: UTF-8 BOM in `.conf` files broke boost ini_parser; `server_list` table missing `display_id` column shifted fields; missing `libmysql.dll` in `GameServer\`. Stack: MySQL 5.6 Docker (port 3307, 4 DBs), binaries+conf in `D:\Project\mutopia\server-s16\`, start order LoginServer→ConnectServer→ServerLink→GameServer.

- **S16 Crash Fix - monster_template empty (2026-08-18)**: Null deref when spawning monsters with no template loaded. Fix: import `monster - complete.sql` LAST (contains both template + spawn data) — importing schema-only `monster_template.sql` after it wipes templates via DROP TABLE.

- **S6 client authoritative binary lost (2026-08-18)**: Repo restructure deleted `MuMain-main/build/` (3.5GB) holding authoritative `Main.exe`. Only stale `src/client-s6/src/bin/Main.exe` (2026-08-11) remains. Rebuild via CMake/MSBuild when fresh binary needed.

- **S20 Character Creation Fix (2026-08-13)**: `CreateCharacter` packet's `Class` field already does `<<2` + 6-bit mask via `LeftShifted=2`. Client was double-shifting. Fixed via `ClientClassTypeToServerClassNumber()` in `CharacterManager.h/.cpp`. Do NOT re-add manual `<<2` in create path.

- **Run game from `build/src/Release`, NOT `src/bin` (2026-08-13)**: Two `Main.exe` exist and desync. Authoritative = `MuMain-main/build/src/Release/Main.exe` (MSBuild output, has all fixes).

- **Server AppearanceSerializer version gates (2026-08-13)**: Fixed plugin selection for S6+ clients — `AppearanceSerializerExtended.cs` MinimumClient (106,3)->(6,3); added MaximumClient caps to 075/095/default serializers.

- **Critical Issues Fix Round 1 (2026-08-27)**: Fixed hardcoded DB passwords (moved to env vars) in web-portal/server-s6 configs, removed Docker port 8080 duplication, added Docker healthchecks (db/server/web), fixed AllowedHosts wildcards, created `.github/workflows/ci.yml` + `security.yml`, wrote `docs/DEPLOYMENT.md`. Remaining: git repo corruption (missing `.git/objects`+`.git/refs`), 850 TODO markers, integration tests.

## Older history
See `.kilo/memory/decisions-archive.md` for full pre-2026-08-19 decision log (client data sync, S20 data completeness audit, Season 16 asset ecosystem inventory, etc).
