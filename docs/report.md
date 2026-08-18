# 📝 Báo Cáo Chi Tiết Quá Trình Điều Chỉnh, Triển Khai & Đánh Giá Nâng Cấp Dự Án OpenMU

Báo cáo đầy đủ về kiến trúc, các lỗi phát sinh, lệnh biên dịch, **chi tiết từng đoạn code / file cấu hình đã được chỉnh sửa**, **kết quả kiểm tra nạp quái vật cho các bản đồ** và **danh sách các hạng mục nâng cấp tính năng trong tương lai**.

---

## 🚀 1. Tổng Quan Kiến Trúc Hệ Thống

| Thành phần | Công nghệ | Cổng Host | Cổng Container | Chức năng |
| :--- | :--- | :--- | :--- | :--- |
| **Game Client** | C++17 / C# Native AOT | - | - | Game Client Season 6E3 (`build/src/Release/Main.exe`) |
| **ConnectServer** | .NET 9 (OpenMU) | `44405` / `44406` | `44405` / `44406` | Điều hướng Client kết nối vào GameServer |
| **GameServer** | .NET 9 (OpenMU) | `55901-55906` | `55901-55906` | Các Sub-Server xử lý logic Game |
| **Admin Panel** | Blazor Server (OpenMU) | `8081` | `8080` | Quản trị Server & Quản lý nhân vật/tài khoản |
| **Web Người Chơi** | Blazor Web App (.NET 9) | `3007` | `8080` | Trang web người chơi (Đăng ký/Đăng nhập/BXH/News) |
| **Database** | PostgreSQL 15 | `5438` | `5432` | Cơ sở dữ liệu chính của OpenMU |

> **Ghi chú cập nhật (13/08/2026)**: Web Người Chơi đã được viết lại hoàn toàn từ Next.js 14 (`open-mu-web-master`) sang **Blazor Web App .NET 9** (`OpenMU.PlayerWeb`), image giảm từ 1.61GB còn 339MB. Toàn bộ dự án đã revert về **Season 6E3** (bỏ hỗ trợ Season 20 do codec model BMD v14/v15 chưa có).

---

## 🐲 2. Tùy Chỉnh Tỷ Lệ Server & Phân Bổ Bãi Quái Thông Minh

### 📊 Thống Kê Tỷ Lệ Game Hiện Tại:
* ⚡ **Kinh Nghiệm (EXP Rate & Master EXP Rate)**: **`x10000`** (Siêu tốc x10000 lần).
* 💰 **Tỷ Lệ Rớt Tiền Zen**: **`x7`** (Tỷ lệ rớt 70%).
* 💎 **Tỷ Lệ Rớt Đồ & Ngọc**: **`x5`** (Gấp 5 lần giá trị gốc).
* 🔄 **Phần Thưởng Reset**: **`15,000 điểm Point / 1 lần Reset`**.
* 📈 **Bậc Thang Level Reset**: **Bắt đầu Level 200, mỗi 5 lần Reset +10 Level (Capped max 400)**.
* 🛡️ **NPCs / Thương Nhân**: Cố định đúng **`1 NPC`** duy nhất (Chống nhân bản).

### 📊 Phân Bổ Quái Vật Chống Chồng Đè Tọa Độ (Anti-Stacking Density):
* ❌ **Bãi Điểm (`Point Spawns` - `X1 = X2` & `Y1 = Y2`)**: Cố định **`3 quái / ô điểm`** (Cho **5,565 bãi điểm**). Giải quyết triệt để lỗi 18 quái chồng đè lên 1 ô gây kẹt nhân vật.
* 🗺️ **Bãi Vùng (`Rectangle Spawns`)**: Cung cấp **`4 - 12 quái / vùng`** tùy theo diện tích (Cho **102 vùng rộng**).
* 🐲 **Tổng Quái Vật Bản Đồ**: Dungeon (**1,594** quái), Lost Tower (**1,494** quái), Atlans (**1,010** quái), Tarkan (**651** quái), Lorencia (**135** quái), Devias (**113** quái), Noria (**108** quái).

---

## 🛠️ 3. Chi Tiết Các Đoạn Code & File Cấu Hình Đã Điều Chỉnh

### 🔹 A. Cấu Hình Docker & Server Network (`OpenMU-master`)

> **⚠️ Lưu ý (13/08/2026)**: Cấu hình dưới đây là **lịch sử deploy ban đầu** (dùng `deploy/all-in-one`). Hiện tại dự án chạy bằng `docker-compose.yml` ở **thư mục gốc** `D:\Project\mu\docker-compose.yml` — đã bỏ mapping `55001-55006` (gây xung đột cổng), dùng chuẩn `55901-55906`, và web portal là `openmu-playerweb` (cổng 3007). Xem mục 6.5.

#### 1. File [`docker-compose.override.yml`](file:///d:/Project/mu/OpenMU-master/deploy/all-in-one/docker-compose.override.yml) (LỊCH SỬ)
* **Mục đích**: Bổ sung môi trường `RESOLVE_IP: loopback`, tích hợp dịch vụ Web Portal `openmu-web` và đổi cổng PostgreSQL Host `5435:5432`.
```yaml
services:
  openmu-startup:
    build:
      context: ../../src
      dockerfile: Startup/Dockerfile
    restart: always
    ports:
      - "8081:8080"
    environment:
      RESOLVE_IP: loopback

  openmu-web:
    build:
      context: ../../../open-mu-web-master
      dockerfile: Dockerfile
    container_name: openmu-web
    restart: always
    ports:
      - "3000:3000"
    environment:
      DATABASE_URL: "postgresql://postgres:admin@database:5432/openmu"
      NEXTAUTH_SECRET: "secret"
      NEXT_PUBLIC_URL: "http://localhost:3000"
      GAMESERVER_URL: "http://openmu-startup:8080"
    depends_on:
      - database
      - openmu-startup

  database:
    ports:
      - "5435:5432"
```

#### 2. File [`docker-compose.yml`](file:///d:/Project/mu/OpenMU-master/deploy/all-in-one/docker-compose.yml)
* **Mục đích**: Ánh xạ cổng GameServer công khai trên Host thành `55001-55006` để tránh bị Windows Hyper-V chặn khoảng cổng `55853 - 56052`.
```yaml
    ports:
      - "8080"
      - "55001:55901"
      - "55002:55902"
      - "55003:55903"
      - "55004:55904"
      - "55005:55905"
      - "55006:55906"
      - "44405:44405"
      - "44406:44406"
      - "55080:55980"
```

#### 3. Truy vấn CSDL PostgreSQL (Khắc phục văng khi chọn Server)
* **Mục đích**: Cập nhật giá trị `AlternativePublishedPort` để ConnectServer gửi cho Client các cổng kết nối chuẩn `55001-55006`.
```sql
UPDATE config."GameServerEndpoint" SET "AlternativePublishedPort" = 55001 WHERE "NetworkPort" = 55901;
UPDATE config."GameServerEndpoint" SET "AlternativePublishedPort" = 55002 WHERE "NetworkPort" = 55902;
UPDATE config."GameServerEndpoint" SET "AlternativePublishedPort" = 55003 WHERE "NetworkPort" = 55903;
UPDATE config."GameServerEndpoint" SET "AlternativePublishedPort" = 55004 WHERE "NetworkPort" = 55904;
UPDATE config."GameServerEndpoint" SET "AlternativePublishedPort" = 55005 WHERE "NetworkPort" = 55905;
UPDATE config."GameServerEndpoint" SET "AlternativePublishedPort" = 55006 WHERE "NetworkPort" = 55906;
```

#### 4. File [`nginx.dev.conf`](file:///d:/Project/mu/OpenMU-master/deploy/all-in-one/nginx/nginx.dev.conf)
* **Mục đích**: Loại bỏ xác thực Basic Auth để vào Admin Panel mượt mà không bị popup hỏi mật khẩu.
```nginx
  server {
    listen 80;
    proxy_http_version 1.1;
    proxy_set_header Upgrade $http_upgrade;
    proxy_set_header Connection "upgrade";

    resolver 127.0.0.11 ipv6=off;

    location / {
       proxy_pass http://openmu-startup:8080;
    }
  }
```

---

### 🔹 B. Web Portal Người Chơi (`OpenMU.PlayerWeb` — Blazor .NET 9)

> **Cập nhật 13/08/2026**: Website người chơi đã viết lại bằng Blazor .NET 9, thay thế hoàn toàn Next.js (`open-mu-web-master`). Xem chi tiết tại mục 6.5.

#### 1. File [`OpenMU.PlayerWeb/Dockerfile`](file:///d:/Project/mu/OpenMU.PlayerWeb/Dockerfile)
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["OpenMU.PlayerWeb.csproj", "./"]
RUN dotnet restore "OpenMU.PlayerWeb.csproj"
COPY . .
RUN dotnet publish "OpenMU.PlayerWeb.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "OpenMU.PlayerWeb.dll"]
```

#### 2. Service trong [`docker-compose.yml`](file:///d:/Project/mu/docker-compose.yml)
```yaml
  openmu-playerweb:
    build:
      context: ./OpenMU.PlayerWeb
      dockerfile: Dockerfile
    container_name: openmu-playerweb
    ports:
      - "3007:8080"
    environment:
      ConnectionStrings__OpenMu: "Host=database;Port=5432;Database=openmu;Username=postgres;Password=admin"
      GameserverUrl: "http://openmu-server:8080"
      ASPNETCORE_ENVIRONMENT: "Production"
    depends_on:
      - database
      - openmu-server
    restart: unless-stopped
```

---

### 🔹 C. Khắc Phục Lỗi Build & Giao Diện Game Client (`MuMain-main`)

#### 1. File [`ClientLibrary/MUnique.Client.Library.csproj`](file:///d:/Project/mu/MuMain-main/ClientLibrary/MUnique.Client.Library.csproj)
* **Mục đích**: Hạ `TargetFramework` từ `net10.0` về `net9.0` để tương thích với .NET SDK 9.0 sẵn có trên máy.
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    ...
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="MUnique.OpenMU.Network.Packets" Version="0.9.0" />
    <PackageReference Include="Pipelines.Sockets.Unofficial" Version="2.2.8" />
  </ItemGroup>
```

#### 2. File [`src/CMakeLists.txt`](file:///d:/Project/mu/MuMain-main/src/CMakeLists.txt)
```cmake
set(OPENMU_PACKETS_VERSION "0.9.0" CACHE STRING
    "Version of MUnique.OpenMU.Network.Packets to source packet XML from")
set(OPENMU_PACKETS_XML
    "${MU_NUGET_CACHE_DIR}/munique.openmu.network.packets/${OPENMU_PACKETS_VERSION}/contentFiles/any/net9.0/ServerToClient/ServerToClientPackets.xml")
```

#### 3. File [`src/source/Character/CharMakeWin.cpp`](file:///d:/Project/mu/MuMain-main/src/source/Character/CharMakeWin.cpp) (Sửa Lỗi UI Tạo Nhân Vật)
* **Mục đích**: Thêm lệnh `BeginBitmap()` khôi phục lại ma trận chiếu 2D ngay sau khi vẽ mô hình 3D nhân vật, giúp khung tên, danh sách các Lớp nhân vật (DK, DW, Elf, MG, DL, Summoner, RF) và các chỉ số hiển thị chính xác.
```cpp
void CCharMakeWin::RenderControls()
{
    RenderCreateCharacter();
    BeginBitmap(); // <--- Thêm lệnh này để khôi phục ma trận 2D
    ::EnableAlphaTest();

    for (auto& sprite : m_asprBack)
    {
        sprite.Render();
    }
    CWin::RenderButtons();
    ...
```

---

## 🛠️ 4. Lệnh Biên Dịch & Khởi Chạy Từng Bước

```powershell
# 1. Khởi chạy Docker Server (toàn bộ stack: db + server + playerweb)
cd d:\Project\mu
docker compose up -d

# 2. Biên dịch Game Client (build từ build/src/Release, KHÔNG phải src/bin)
cd d:\Project\mu\MuMain-main
cmake -B build -G "Visual Studio 17 2022" -A Win32 -DENABLE_EDITOR=OFF
cmake --build build --config Release
# Hoặc build nhanh bằng MSBuild:
& "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe" `
  "D:\Project\mu\MuMain-main\build\src\MuClient.vcxproj" /p:Configuration=Release /maxcpucount /t:Build /v:minimal

# 3. Biên dịch Web Portal (Blazor .NET 9)
cd d:\Project\mu\OpenMU.PlayerWeb
dotnet build
# Docker image:
docker compose build openmu-playerweb
```

---

## 📌 5. Kiểm Tra & Truy Cập

* 🌐 **Trang Web Người Chơi (Blazor .NET 9)**: [`http://localhost:3007`](http://localhost:3007)
* 🖥️ **OpenMU Admin Panel**: [`http://localhost:8081`](http://localhost:8081)
* 🎮 **File Khởi Chạy Game Client**: **[`D:\Project\mu\MuMain-main\build\src\Release\Main.exe`](file:///d:/Project/mu/MuMain-main\build\src\Release\Main.exe)** ⚠️ *Chạy từ `build/src/Release`, KHÔNG phải `src/bin` (bản cũ)*

---

## 🔍 6. Phân Tích Đánh Giá & Hướng Nâng Cấp Tính Năng

### 🎮 1. Nâng Cấp Cho GAME CLIENT (`MuMain-main`)
* **Chế độ MU Editor (ImGui - Bật F12)**: Cho phép sửa trực tiếp Map, chỉnh vị trí quái và hiệu ứng 3D trong game (`-DENABLE_EDITOR=ON --editor`).
* **Đồ họa Shader RHI (60-144 FPS & Màn hình Rộng 16:9/21:9)**: Mang lại trải nghiệm chơi mượt mà gấp 6 lần Client gốc.
* **Tích hợp Discord Rich Presence**: Hiển thị thông tin Tên nhân vật, Level và Bản đồ đang đứng lên Discord.
* **Camera 3D 360 độ (Orbital Camera)**: Cho phép xoay camera tự do và thu phóng góc nhìn.

### 🖥️ 2. Nâng Cấp Cho SERVER (`OpenMU C# Plugins`)
* **Lệnh Chat nâng cao (ChatCommands)**: Thêm các lệnh `/reset` tự động trong game, `/addstats`, `/post`, `/skin`, `/makeitem`.
* **Hệ thống Sự kiện (Invasions & Bosses)**: Sự kiện Rồng Đỏ, Binh Đoàn Hoàng Kim, Thỏ Ngọc, Săn Boss Kundun / Medusa.
* **Anti-Cheat & Giới hạn Tài khoản**: `SpeedHackDetectPlugIn` chống hack tốc độ và `MaximumConnectionsPerIpPlugIn` giới hạn số nick/IP.
* **Hệ thống VIP System & Vòng Quay May Mắn (Lucky Wheel)**: Tăng EXP/Drop rate cho VIP và đổi ngọc lấy lượt quay đồ.

### 🌐 3. Nâng Cấp Cho WEB PORTAL (`OpenMU.PlayerWeb` — Blazor .NET 9)
* **Dịch vụ Nhân vật Web**: Reset Web, Tẩy điểm (Reset Stats), Đổi giới tính, Đổi tên nhân vật trực tuyến.
* **Cửa hàng WebShop & Chợ Trời Web (Marketplace)**: Đăng bán vật phẩm trên Web giữa người chơi với nhau.
* **Cổng Thanh Toán Tự Động**: Tích hợp nạp tiền WCoin tự động qua VietQR / MoMo / ZaloPay.
* **Thay ảnh thật**: Thay các ảnh placeholder Google (class portrait, hero, news thumbnail) bằng ảnh server thật.

---

## 🔄 6.5. Nhật Ký Phiên Làm Việc 13/08/2026 — Revert S20 & Viết Lại Web Portal

### 📌 A. Revert Season 20 → Season 6E3 (Quyết Định Kiến Trúc)

**Lý do kỹ thuật**: Client S6E3 có bộ giải mã model BMD (`ZzzBMD.cpp`) chỉ hỗ trợ version `0x0A`/`0x0C`/`0x0E`. Season 20 dùng BMD version `0x0F` (và S9 dùng `0x0E`) với thuật toán mã hóa khác, chưa có codec. Kết quả: 76% model Item S20, ~36% Monster, 100% Object1 map sẽ bị "tàng hình". Đã thử 4 biến thể decrypt key `webzen#@!01...` — không thành công. **Mức season tối đa thực tế là S6E3** (trừ khi tìm được codec v14/v15 từ cộng đồng).

**Phạm vi revert**:
- **Client (12 file)**: khôi phục từ `update/MuMain-main` (`_enum.h`, `_define.h`, `CharacterManager.cpp/h`, `CharMakeWin.cpp/h`, `PacketFunctions_CommonEnums.h`, `WSclient.h/cpp`, `ZzzCharacter.cpp`, `ZzzObject.cpp`, `ZzzOpenData.cpp`).
- **Server (xóa 22 file)**: 8 `Class*.cs` S20 + 11 `AddLanceFor*/AddMagicalShotFor*/AddClash*/AddCrescentMoonSlashFor*`; revert `CharacterClassNumber.cs`, `CharacterClasses.cs`, `CharacterClassHelper.cs`, `CharacterClassInitialization.cs`, `CommonEnums.xml/cs`, `SkillNumber.cs`, `MasterSkillTree.xml`, 4 serializer + `ShowCharacterListExtendedPlugIn.cs`, `SkillsInitializer.cs`.
- **DB**: xóa 53 nhân vật S20, 8 dòng `AccountCharacterClass`, 23 định nghĩa class (number >= 28) — còn đúng 18 class S6 (0-25).

### 📌 B. Viết Lại Web Portal — `OpenMU.PlayerWeb` (Blazor .NET 9)

**Quyết định**: Thay website người chơi Next.js 14 (image 1.61GB) bằng Blazor Web App .NET 9 — đồng bộ toàn bộ dự án về C#, image 339MB.

**Kiến trúc**:
- **Render**: Static SSR + Interactive Server (chỉ các trang có form)
- **Auth**: Cookie Authentication tự viết (bỏ Identity UI)
- **Data**: EF Core + Npgsql map trực tiếp vào 3 schema OpenMU (`config`/`data`/`guild`)
- **Password**: BCrypt.Net-Next work factor 11 (khớp `$2a$11$` của OpenMU)
- **Design**: theo template "Blood & Gold Eternal" (`template/`) — dark fantasy gothic (Newsreader/Work Sans/JetBrains Mono, đỏ máu #8B0000, vàng #FFD700)

**Cấu trúc**:
```
OpenMU.PlayerWeb/
├── Program.cs                  # Cookie auth, DbContextFactory, DI
├── appsettings.json            # ConnectionStrings.OpenMu + GameSettings
├── Dockerfile                  # Multi-stage .NET 9
├── Data/                       # 10 entity + AppDbContext
├── Services/                   # Account, Character, Ranking, News, Guild, ServerStatus, Bot
└── Components/
    ├── Layout/                 # MainLayout + NavMenu (template gothic)
    └── Pages/                  # Home, Classes, Maps, Rankings, News, NewsDetail,
                                # Download, Info, Terms, Account/*, Login, Register,
                                # Logout, Admin/News, Admin/Bots
```

**Chức năng (port đầy đủ từ Next.js cũ)**:
- Đăng ký / đăng nhập / đổi mật khẩu (BCrypt)
- News CRUD (bảng `data.OpenMuWeb_News`) — GM role = `CharacterStatus 32`
- Ranking: Reset / Killers / Online / Guilds
- Quản lý nhân vật: AddStats / Reset / ResetStats / PkClear (cùng UUID stat/map với bản cũ)
- Quản lý bot (list + delete)

**Docker**: service `openmu-playerweb` thay thế `openmu-web`, cổng `3007:8080`. Đã xóa image Next.js cũ + container orphan + **xóa toàn bộ thư mục `open-mu-web-master`** (code Next.js chết, giải phóng ~1.6GB image + source).

**Điều chỉnh `docker-compose.yml`**: bỏ mapping trùng `55001-55006` (gây xung đột cổng với `msedgewebview2`), giữ chuẩn `55901-55906`.

### 📌 C. Khôi Phục Plugin Bot (Roslyn) & Tạo DB Sạch

**🔴 Lỗi nghiêm trọng phát hiện (13/08)**: Việc tối ưu Docker image trước đó đã xóa nhầm `Microsoft.CodeAnalysis*.dll` (tưởng là build-only). Thực tế OpenMU **biên dịch plugin lúc runtime bằng Roslyn**, nên server log `FileNotFoundException: Could not load file or assembly 'Microsoft.CodeAnalysis.CSharp, Version=4.14.0.0'` → toàn bộ plugin động (gồm BotGenerator) không load → bot tạo account nhưng **không spawn được nhân vật** (51/100 account trống nhân vật).

**Fix**: Bỏ dòng `find -delete Microsoft.CodeAnalysis*.dll` khỏi `src/Startup/Dockerfile` (chỉ giữ xóa `*.pdb`). Image giữ nguyên 374MB.

**🔴 Lưu ý an toàn (tránh tái phạm)**: CHỈ được xóa `*.pdb` trong image server. KHÔNG xóa `*.xml`, `*.staticwebassets.*.json`, `Microsoft.CodeAnalysis*.dll` — đều cần thiết lúc runtime.

### 📌 D. Tạo Database Sạch + Account GM Mặc Định

**Drop & recreate toàn bộ DB `openmu`** (theo yêu cầu để test bot với config vanilla):

| Hành động | Chi tiết |
|-----------|---------|
| Drop DB | `DROP DATABASE IF EXISTS openmu;` → `CREATE DATABASE openmu;` |
| Seed vanilla | Server tự seed lại 18 class S6 (Dark Wizard → Fist Master) |
| Bật Reset | `UPDATE config."PlugInConfiguration" SET "IsActive"=true WHERE "Id"='6a9d585d-79d7-4674-b6ea-7e87392fa501'` (ResetFeaturePlugIn — mặc định là `IDisabledByDefault`) |

> ⚠️ **Toàn bộ tùy chỉnh cũ đã mất**: EXP x10000, drop x5/x7, 15000 điểm reset, anti-stacking quái... phải cấu hình lại sau.

**Account GM mặc định** (đổi từ `testgm` seed vanilla):

| Thuộc tính | Giá trị |
|-----------|---------|
| Login name | `GameMaster` |
| Mật khẩu | `Openmu` (BCrypt work factor 11) |
| State | 2 (GameMaster) |
| Nhân vật GM | 5 nhân vật level 400 full skill (CharacterStatus = 32): Blade Master, Grand Master, High Elf, Lord Emperor, Duel Master |

### 📌 E. Danh Sách Tên Bot Tuyển Chọn (698 tên)

**Thay đổi `BotNameGenerator.cs`**: Từ sinh tên procedural (ghép âm tiết) → **danh sách 698 tên cố định** theo chủ đề LOTR / Game of Thrones / The Witcher / Star Wars / Star Trek / Warcraft / Dune / MU.

- Tất cả tên thỏa `CharacterNameRegex = ^[a-zA-Z0-9]{3,10}$` (quy tắc vanilla MU — `GameConfigurationInitializerBase.cs:55`).
- Tên dài >10 ký tự bị cắt cụt (vd `GrandMaster`→`GrandMaste`, `Frostmourne`→`Frostmourn`) — chấp nhận vì giới hạn cứng của MU.
- Danh sách đầy đủ nằm trong `OpenMU-master/src/GameLogic/Bots/BotNameGenerator.cs`.

---

## 🔄 7. Nhật Ký Thay Đổi & Hướng Dẫn Khôi Phục Bản Gốc (Rollback Guide)

Bảng tổng hợp toàn bộ các chỉnh sửa đã thực hiện so với mã nguồn và CSDL gốc, kèm lệnh khôi phục nguyên bản (Rollback):

### 1. ⚙️ Tỷ Lệ Server & Tải Trọng CSDL PostgreSQL

| Hạng mục | Bản Hiện Tại (Custom) | Bản Gốc (Vanilla) | Lệnh Khôi Phục Về Bản Gốc (Rollback SQL) |
| :--- | :--- | :--- | :--- |
| **ExperienceRate** | `10000` (x10000) | `1` (x1) | `UPDATE config."GameConfiguration" SET "ExperienceRate" = 1, "MasterExperienceRate" = 1;` |
| **Monster Density**| `3` (Point), `4-12` (Rect) | `1` (Mọi spawn) | `UPDATE config."MonsterSpawnArea" SET "Quantity" = 1;` |
| **Drop Rate (Item)**| `5x` base chance | `1x` base chance | `UPDATE config."DropItemGroup" SET "Chance" = "Chance" / 5.0 WHERE "Id" != '00000200-0001-0000-0000-000000000000';` |
| **Drop Rate (Zen)** | `0.7` (70%) | `0.1` (10%) | `UPDATE config."DropItemGroup" SET "Chance" = 0.1 WHERE "Id" = '00000200-0001-0000-0000-000000000000';` |
| **PointsPerReset** | `15000` point/reset | `1500` point/reset | `UPDATE config."PlugInConfiguration" SET "CustomConfiguration" = REPLACE("CustomConfiguration", '"PointsPerReset": 15000,', '"PointsPerReset": 1500,') WHERE "Id" = '6a9d585d-79d7-4674-b6ea-7e87392fa501';` |
| **Reset Plugin** | `IsActive = true` | `IsActive = false` | `UPDATE config."PlugInConfiguration" SET "IsActive" = false WHERE "Id" = '6a9d585d-79d7-4674-b6ea-7e87392fa501';` |

### 2. 💻 Code Backend C# (`OpenMU-master/src/GameLogic/Resets/`)

* **File chỉnh sửa**:
  * [`ResetProgressionCalculator.cs`](file:///d:/Project/mu/OpenMU-master/src/GameLogic/Resets/ResetProgressionCalculator.cs)
  * [`ResetCharacterAction.cs`](file:///d:/Project/mu/OpenMU-master/src/GameLogic/Resets/ResetCharacterAction.cs)
  * [`ResetInfoChatCommandPlugIn.cs`](file:///d:/Project/mu/OpenMU-master/src/GameLogic/Resets/ResetInfoChatCommandPlugIn.cs)
* **Thay đổi**: Đã thêm hàm `GetRequiredLevel(configuration, resetCount)` để tính cấp độ yêu cầu reset theo bậc thang (Level 200 + 10 level / 5 reset).
* **Khôi phục về gốc**: Thay `ResetProgressionCalculator.GetRequiredLevel(...)` bằng `configuration.RequiredLevel`.

### 3. 🎨 Game Client C++ (`MuMain-main/src/source/Character/CharMakeWin.cpp`)

* **File chỉnh sửa**: [`CharMakeWin.cpp`](file:///d:/Project/mu/MuMain-main/src/source/Character/CharMakeWin.cpp) dòng 391.
* **Thay đổi**: Thêm `BeginBitmap()` sau khi vẽ nhân vật 3D để khôi phục ma trận chiếu 2D orthographic cho các nút tạo nhân vật.
* **Khôi phục về gốc**: Xóa dòng `BeginBitmap();` tại dòng 391 trong `CharMakeWin.cpp`.

### 4. 🎵 Đồng Bộ Dữ Liệu Client Data Assets & Ghi Nhớ Harness (`.kilo/memory/MEMORY.md`)

* **Hạng mục nâng cấp**: Đồng bộ 100% tài nguyên Game Client (975 files gồm `Sound/`, `Music/`, `Local/`, `Macro.txt`, `Object*`, `World*`) từ bộ cài `MU Client 1.04d - Season 6E3` sang `MuMain-main/src/bin/Data` và `MuMain-main/build/src/Release/Data`.
* **Ghi nhớ Harness**: Đã cập nhật nhật ký quyết định (Decisions Log) vào `.kilo/memory/MEMORY.md` đảm bảo bộ nhớ Harness duy trì ngữ cảnh nhất quán giữa các session làm việc.

---

