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
