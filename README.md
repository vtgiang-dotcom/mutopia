# MU Online Dual-Realm (Season 6 + Season 16)

Dự án vận hành song song 2 phiên bản MU Online: Season 6 Episode 3 (C# OpenMU) và Season 16 Episode 1 (C++ LgdMu).

## Cấu trúc
- `src/`         — Mã nguồn phát triển (S6 + S16)
- `reference/`   — Data & binary tham khảo (KHÔNG push — bản quyền Webzen)
- `docs/`        — Báo cáo kiến trúc, audit, roadmap
- `tools/`       — Dev tooling

## Runtime
Môi trường vận hành nằm ngoài repo tại `D:\Project\mutopia\` (server binary, client data, DB volume).

## Bản quyền
Xem `LICENSE-NOTICE.md`. Repo chỉ chứa mã nguồn; mọi binary/data đồ họa Webzen đều bị `.gitignore` loại trừ.

## Nguồn tham khảo từng dự án

| Thư mục | Nguồn gốc | Bản quyền | Đẩy GitHub |
| :--- | :--- | :--- | :--- |
| `src/server-s6` | [MUnique OpenMU](https://github.com/MUnique/OpenMU) (open-source C#) | MIT | ✅ |
| `src/client-s6` | MuMain (C++ hook client S6, cộng đồng private-server) | Cộng đồng | ✅ |
| `src/web-portal` | OpenMU.PlayerWeb — tự phát triển (Blazor .NET 9) | Tự phát triển | ✅ |
| `src/launcher` | MuLauncher — tự phát triển (WinForms .NET 9) | Tự phát triển | ✅ |
| `src/simulation` | Simulation engine — tự phát triển (C# .NET 9) | Tự phát triển | ✅ |
| `src/database` | mysql-s16-init — schema tự tạo | Tự phát triển | ✅ |
| `src/server-s16` | Lgd-Server-main (C++ GameServer S16, bên thứ ba) | LgdMu | ✅ |
| `src/client-s16` | Lgd-Client-main + Client/Server LgdMu 1.1 (bên thứ ba) | LgdMu | ✅ |
| `reference/s16-data` | ZhyperMU S16 Full + MuOnline_S16_Lgd-main | Webzen + bên thứ ba | ❌ |
| `reference/s16-tools` | MuOnline-WorldEditor-master + MuClientTools16 | Bên thứ ba | ❌ |
| `reference/archives` | 6 file `.zip` gốc (data + source nén) | Hỗn hợp | ❌ |
