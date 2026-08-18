# BÁO CÁO KỸ THUẬT TỔNG THỂ & THAM VẤN KIẾN TRÚC HỆ SINH THÁI MU ONLINE
## (DỰ ÁN ĐA PHIÊN BẢN SEASON 6 & SEASON 16 — DUAL-REALM ARCHITECTURE)

*Ngày cập nhật: 18/08/2026*  
*Mục đích: Cung cấp bức tranh kỹ thuật toàn diện, các phân tích đánh đổi (trade-offs) và bộ câu hỏi chuẩn hóa để gửi mô hình AI (DeepSeek) đánh giá, phản biện độc lập.*

---

## 1. TỔNG QUAN HIỆN TRẠNG MÃ NGUỒN VÀ TÀI NGUYÊN (INVENTORY)

Dự án hiện tại tại thư mục gốc `d:\Project\mu\` được cấu thành từ hai khối tài nguyên chính:

### Khối A: Hệ sinh thái OpenMU C# Hiện Có (Chuẩn Season 6 Episode 3)
1. **`OpenMU-master/` (Server C# .NET 9/10):**
   * Kiến trúc Modular, Clean Architecture, Dependency Injection, System.IO.Pipelines.
   * Cơ sở dữ liệu Entity Framework Core (PostgreSQL 15), hệ thống Plugin mở rộng cao cấp.
   * Đã hoàn thiện chuẩn Season 6 Episode 3, tích hợp hệ thống Bot AI thông minh.
2. **`MuMain-main/` (Client Hook Season 6):**
   * Dự án Hybrid: ~90% C++ (OpenGL, V-Sync, >60 FPS, Unicode UTF-16LE, UI Win32) + ~10% C# .NET Native AOT (`ClientLibrary/` xử lý mã hóa packet mạng).
3. **`OpenMU.PlayerWeb/` (Web Portal):**
   * Blazor Web App (.NET 9), kết nối PostgreSQL 15, quản lý tài khoản, nhân vật, Reset, BXH, Bot Dashboard.
   * Đã tích hợp **Account Bridge Dual-Write** ghi đồng thời vào PostgreSQL 15 và MySQL 5.6 (S16).
4. **`simulation/` (Monte-Carlo Economic Simulator):**
   * Bộ công cụ C# .NET 9 giả lập kinh tế, exp/h, drop rate, PvP combat balancing, cơ chế Reset.
5. **`MuLauncher/` (Unified Smart Launcher):**
   * Ứng dụng .NET 9 WinForms chọn server, tự động đo ping TCP thời gian thực, khởi chạy đúng Client tương ứng.

---

### Khối B: Nguồn tài nguyên mới nạp tại `sourecodeadd/` (Chuẩn Season 16 Episode 1)
1. **`Server - LgdMu s16 1.1` & `Lgd-Server-main` (Server C++ Native):**
   * Full source C++ GameServer, ConnectServer, LoginServer, ServerLink với hơn 570 source files.
   * Chứa toàn bộ công thức & logic: Hệ thống Ngũ Hành (Pentagram), Cây Kỹ Năng Majestic (Lv 800–1200), Pet Muun, Ruud System, 11 Class nhân vật, Boss & Event S16.
   * Bản đồ Packet hoàn chỉnh tại `Game/ClientPacket.h` (107 KB).
   * Cơ sở dữ liệu **MySQL 5.6** gồm 4 database: `database_login`, `database_game`, `database_characters`, `database_log`.
2. **`Client - LgdMu S16 1.1` & `Lgd-Client-main` (Client C++ Hook):**
   * Source `Main.dll` v1.1 hook vào `main.exe 1.19.46`, bảng Memory Offset chuẩn (`Offset.h`), Auto Reconnect, Anti-Cheat.
3. **`MuOnline_S16_Lgd-main` & `ZhyperMU S16 Full` (Data Đồ Họa & Client Binary):**
   * File thực thi `main.exe 1.19.46` đã gỡ bảo mật (Unpacked).
   * 100% Data đồ họa Client Season 16 (~2.5 GB: 137 Maps 3D, Set Ruud 1–8, Cánh Cấp 4, 11 Class).
4. **`MuClientTools16` & `MuOnline-WorldEditor-master` (Bộ công cụ Tooling):**
   * Bộ giải mã / đóng gói chuyên dụng cho BMD, ATT, OZ* của S16.
   * 3D In-game World Editor (ImGui / OpenGL) can thiệp map thời gian thực.

---

## 2. TRẠNG THÁI LÀM SẠCH MÃ NGUỒN & ĐỒNG BẰNG BASELINE

1. **Làm sạch Season 9 / Season 20 dở dang:**
   * Các nhánh code thử nghiệm S9/S20 dở dang trước đây (như 8 class mở rộng, skill thừa) đã được revert triệt để về chuẩn Season 6E3 thuần khiết.
   * Không còn bất kỳ mã nguồn phân mảnh hay folder rác nào trong codebase hiện hành.
2. **Định hướng chiến lược:**
   * Giữ nguyên phân vùng Season 6 làm **Cụm máy chủ Cổ điển (Classic Realm)**.
   * Mở rộng thêm phân vùng Season 16 làm **Cụm máy chủ Kỷ nguyên mới (Modern Realm)**.

---

## 3. THIẾT KẾ KIẾN TRÚC HỆ THỐNG SONG SONG (DUAL-REALM ARCHITECTURE)

```
                            +-------------------------------------------+
                            |          OPENMU.PLAYERWEB PORTAL          |
                            | (Quản lý Account, Nạp thẻ, BXH S6 & S16)  |
                            +---------------------+---------------------+
                                                  |
                                                  | Dual-Write (Mục 8.2)
                            +---------------------v---------------------+
                            |    ACCOUNT BRIDGE (AccountService.cs)     |
                            |  Ghi đồng thời 2 DB + Retry Background    |
                            +----------+--------------------+-----------+
                                       |                    |
                         BCrypt(w=11)  |                    |  SHA-1 (40 hex)
                                       v                    v
                   +-----------------------+    +-----------------------+
                   |    CỤM MÁY CHỦ S6     |    |    CỤM MÁY CHỦ S16    |
                   |  (OpenMU C# / 55901)  |    | (Lgd C++ Native/55902)|
                   |  +-----------------+  |    |  +-----------------+  |
                   |  |  PostgreSQL 15  |  |    |  |    MySQL 5.6    |  |
                   |  | data.Account    |  |    |  | database_login  |  |
                   |  | BCrypt (UUID)   |  |    |  | accounts (INT)  |  |
                   |  +-----------------+  |    |  +-----------------+  |
                   +-----------^-----------+    +-----------^-----------+
                               |                            |
       ========================|============================|========================
                               |                            |
                   +-----------+-----------+    +-----------+-----------+
                   |  CLIENT SEASON 6 (S6) |    | CLIENT SEASON 16 (S16)|
                   | - Main.exe (OpenGL S6)|    | - main.exe (1.19.46)  |
                   | - Data S6 (~1.2 GB)   |    | - Data S16 (~2.5 GB)  |
                   +-----------^-----------+    +-----------^-----------+
                               |                            |
                               +--------------+-------------+
                                              |
                               +--------------+-------------+
                               |   UNIFIED SMART LAUNCHER   |
                               | (MuLauncher.exe / .NET 9)  |
                               | [Server S6]  [Server S16]  |
                               | Ping realtime, mở đúng exe |
                               +----------------------------+
```

### Nguyên tắc vận hành:
1. **Client Isolation:** Do định dạng 3D model (`.bmd` v10/v12 vs v15), mã hóa socket và UI RAM Offset khác nhau hoàn toàn, Client S6 và Client S16 được phân tách thành 2 thư mục riêng biệt (`Client_S6/` và `Client_S16/`).
2. **Unified Launcher:** Một launcher duy nhất hiển thị danh sách máy chủ. Khi người chơi bấm vào máy chủ nào, launcher tự động kích hoạt client tương ứng với port mạng của máy chủ đó.
3. **Shared Account Bridge (Dual-Write Pattern):** Tài khoản đăng ký một lần qua Web Portal, `AccountService.cs` ghi đồng thời vào PostgreSQL 15 (S6) và MySQL 5.6 (S16). Dữ liệu nhân vật, túi đồ và cấp độ lưu tách biệt theo từng Season — không có tham chiếu chéo ở cấp dữ liệu game. Xem đặc tả kỹ thuật chi tiết tại **Mục 8.2**.

---

## 4. PHÂN TÍCH ĐÁNH ĐỔI VỀ TECH STACK & VAI TRÒ AI LẬP TRÌNH CHÍNH

### 4.1. Lựa chọn ngôn ngữ cho Game Server: C# (.NET 9/10) vs C++ Native vs Rust

| Tiêu chí | C# (.NET 9/10 Modern) | C++ Native (LgdMu) | Rust |
| :--- | :--- | :--- | :--- |
| **Mức độ tương thích khi AI code chính** | **Tốt nhất trong ba lựa chọn:** AI sinh code C# ít lỗi compile lần đầu, dễ gỡ lỗi, hỗ trợ mạnh LINQ / async / Pattern Matching. | **Trung bình:** Dễ gặp lỗi rò rỉ bộ nhớ, con trỏ treo hoặc xung đột header khi AI chỉnh sửa. | **Rủi ro cao:** Borrow Checker khắt khe với mô hình đồ thị quan hệ chéo của Game Server (Player ↔ Party ↔ Map ↔ Guild), dễ dẫn đến deadlock khi bọc `Arc<RwLock<T>>`. |
| **Tài sản kế thừa** | Đã có sẵn OpenMU với kiến trúc DI/Plugin chuẩn mực. | Đã có sẵn 570+ file logic Season 16 (chạy được ngay nhưng khó mở rộng). | 0% — phải tự viết lại toàn bộ Game Server từ đầu. |
| **Hiệu năng & Tài nguyên** | TechEmpower Round 22: ASP.NET Core đạt **91.3%** throughput so với C (H2O) ở plain-text workload. Mức RAM 400MB/5.000 CCU là ước tính cần đo thực tế (xem Mục 8.4). | Hiệu năng cao nhất, nhưng một lỗi ngoại lệ chưa bắt có thể crash toàn bộ tiến trình. | Zero-cost abstractions, không có GC, tiêu thụ tài nguyên tối thiểu. |
| **Đồng bộ hệ sinh thái** | Đồng bộ trực tiếp với Web Portal (Blazor), Simulator (C# .NET 9) và Client Library (Native AOT). | Độc lập, giao tiếp với bên ngoài qua Database queries. | Phải tự xây dựng REST API / gRPC riêng để giao tiếp với Web. |

### 4.2. Về Game Client: Vì sao bắt buộc giữ C++ Native?
* Webzen `main.exe` được biên dịch trực tiếp từ C++/Assembly thành mã máy nhị phân x86.
* Kỹ thuật mở rộng tính năng client bắt buộc phải dùng **C++ Memory Hooking (`Main.dll`)** can thiệp trực tiếp vào địa chỉ RAM của tiến trình.
* Viết lại 100% Client bằng C# hay Rust đồng nghĩa với việc phải tự xây dựng lại toàn bộ một Game Engine 3D từ con số 0 (không khả thi về mặt thời gian và nguồn lực).

---

## 5. LỘ TRÌNH TRIỂN KHAI THEO GIAI ĐOẠN (ROADMAP)

* **Giai đoạn 1 — Fast-Track Deployment (Dual-Realm Foundation)**
  * Cấu hình và chạy MySQL 5.6 Docker (Port 3307) với 4 database chuẩn S16 (`database_login`, `database_game`, `database_characters`, `database_log`).
  * Duy trì Server S6 (OpenMU C# / Docker) trên Port 55901; cấu hình Server S16 trên Port 55902.
  * Triển khai cơ chế **Dual-Write Account Bridge** trong `AccountService.cs` + hàng đợi `AccountSyncQueue` (xem Mục 8.2).
  * Đóng gói `MuLauncher` (.NET 9 WinForms) điều hướng 2 client độc lập với TCP ping realtime.
  * Áp dụng bảo mật packet tối thiểu (rate limiting, IP ban, SpeedHack plugin) — xem Mục 8.5.

* **Giai đoạn 2 — Data Extraction & Simulation** *(Phụ thuộc: MySQL 5.6 S16 đã chạy từ Giai đoạn 1)*
  * Trích xuất bảng dữ liệu quái vật, tỷ lệ rớt đồ, chỉ số trang bị S16 từ MySQL 5.6 (`database_game`).
  * Đưa vào bộ `simulation/` (C# .NET 9) để chạy Monte-Carlo cân bằng kinh tế S16.
  * Đo benchmark RAM/CPU thực tế của Server S6 dưới tải để xác nhận con số ước tính tại Mục 4.1.

* **Giai đoạn 3 — Gradual C# Porting / Modernization** *(Phụ thuộc: Ground Truth đã xác định — dùng `Server - LgdMu s16 1.1`, xem Mục 8.1)*
  * Chuyển đổi định nghĩa packet từ `ClientPacket.h` sang `OpenMU.Network.Packets` bằng XSLT generator.
  * Port dần các hệ thống cốt lõi theo thứ tự ưu tiên: **Ruud System** (đơn giản nhất) → **Majestic Skill Tree** (mở rộng XML hiện có) → **Pentagram / Elemental Combat** (phức tạp nhất, xem Mục 8.1 bottleneck).
  * Mỗi module sau khi port phải pass qua bộ `simulation/` trước khi thay thế C++ tương ứng.

---

## 6. CÁC CÂU HỎI ĐỂ GỬI CHO DEEPSEEK *(Đã được đối chiếu tại Mục 7 & 8)*

```markdown
Chào DeepSeek, tôi đang phát triển dự án hệ sinh thái máy chủ game MMORPG (MU Online) với định hướng vận hành song song 2 phiên bản: Season 6 Episode 3 (Cổ điển) và Season 16 Episode 1 (Hiện đại). AI đóng vai trò là lập trình viên chính thực thi toàn bộ mã nguồn.

Dưới đây là các tài nguyên tôi đang có:
1. Nhánh Season 6: Server C# .NET 9 OpenMU (Modular, Entity Framework Core, PostgreSQL 15) + Client Hybrid (C++ OpenGL render + C# Native AOT network).
2. Nhánh Season 16: Trọn bộ Server C++ Native (570+ files đầy đủ Pentagram, Majestic Tree, Ruud, 11 Class, MySQL 5.6 với 4 databases) + Client C++ (main.exe 1.19.46 Unpacked + Main.dll hook) + 100% Data đồ họa (~2.5 GB, 137 Maps).
3. Hệ sinh thái phụ trợ: Web Portal Blazor C# (.NET 9) với Account Bridge Dual-Write + Bộ giả lập kinh tế Monte-Carlo C# (simulation/) + Unified Launcher WinForms (.NET 9).

Nhờ DeepSeek đánh giá và phản biện chuyên sâu các vấn đề sau:

1. Đánh giá tính khả thi và rủi ro kiến trúc của mô hình "Dual-Realm" (1 Unified Launcher điều hướng 2 Client độc lập kết nối vào 2 Server S6 & S16 chạy trên 2 cổng mạng khác nhau, dùng chung Web Portal và cơ chế SSO Account Bridge). Có rủi ro tiềm ẩn nào về đồng bộ dữ liệu hoặc trải nghiệm người dùng không?
2. Về mặt công nghệ Game Server: Đánh giá việc tiếp tục phát triển trên nền C# .NET 9 (OpenMU) so với việc viết lại bằng Rust hoặc giữ nguyên C++ Native. Đặc biệt trong bối cảnh AI là người viết code chính (xét trên các khía cạnh: tốc độ sinh code của AI, xử lý Borrow Checker/Deadlock của Rust trong Game Server, và hiệu năng GC của .NET 9).
3. Đánh giá phương án Porting Season 16 từ C++ sang C# theo lộ trình phân kỳ (Dùng Server C++ chạy thử nghiệm trước, sau đó port dần Packet và Plugin sang OpenMU C#). Điểm nghẽn kỹ thuật (bottleneck) lớn nhất của quá trình này sẽ nằm ở đâu?
4. Đề xuất các giải pháp kỹ thuật cụ thể để tối ưu hóa việc quản lý tài nguyên, bảo mật packet và kiến trúc đồng bộ giữa Web Portal với 2 loại Database khác nhau (PostgreSQL 15 của S6 và MySQL 5.6 của S16).
```

---

## 7. PHỤ LỤC: ĐÁNH GIÁ ĐỘC LẬP (AUDIT) ĐÃ THỰC HIỆN

### 7.1. Các tuyên bố đã xác minh (khớp thực tế)

| # | Tuyên bố | Kết quả đối chiếu | Trạng thái |
| :--- | :--- | :--- | :---: |
| 1 | 5 thư mục chính (`OpenMU-master`, `MuMain-main`, `OpenMU.PlayerWeb`, `simulation`, `sourecodeadd`) | Tồn tại đầy đủ tại gốc project | [x] |
| 2 | `sourecodeadd/` chứa 7 tài nguyên S16 | Đủ 7 thư mục + 6 file `.zip` kèm theo | [x] |
| 3 | `ClientPacket.h` ~107 KB | `Lgd-Server-main\Game\ClientPacket.h` = **107.014 byte**; `Server - LgdMu s16 1.1\Game\ClientPacket.h` = 112.605 byte | [x] |
| 4 | OpenMU dùng `.NET 9/10` | `net9.0` chủ đạo, `ClientLauncher` = `net10.0-windows` | [x] |
| 5 | Web Portal = Blazor `.NET 9` + PostgreSQL trực tiếp + MySqlConnector | `net9.0`, `Npgsql.EntityFrameworkCore.PostgreSQL 9.0.2`, `MySqlConnector 2.4.0` | [x] |
| 6 | `ClientLibrary/` = C# Native AOT | `MUnique.Client.Library.csproj`: `PublishAot=true`, `SupportNativeAot=true`, `IsAotCompatible=true` | [x] |
| 7 | `main.exe` version 1.19.46 | `MuOnline_S16_Lgd-main\Main\main.exe` FileVersion = **1.19.46.0** | [x] |
| 8 | `simulation/` là Monte-Carlo Simulator | Có `SimulationEngine.csproj` (.NET 9) + kết quả CSV scenarios | [x] |

### 7.2. Các lỗi thực tế đã phát hiện và sửa

| # | Vị trí gốc | Sai | Thực tế | Hành động |
| :--- | :--- | :--- | :--- | :--- |
| 1 | Mục 1.4 & 5.2 | `simulation/` là "Bộ công cụ Python" | Là project C# .NET 9 (`Program.cs` + `SimulationEngine.csproj`) | Đã sửa thành "C# .NET 9" |
| 2 | Mục 1 (Khối B) & 4.1 | Server C++ "hơn 340 source files" | `Server - LgdMu s16 1.1` = 571 file (.cpp+.h); `Lgd-Server-main` = 570 file | Đã sửa thành "570+" |
| 3 | Khắp báo cáo | DB S16 là "MS SQL Server" | Là **MySQL 5.6** (`mysql.h`, 4 databases) | Đã sửa thành MySQL 5.6 |
| 4 | Khắp báo cáo | Password Hash S16 là "MD5" | Là **SHA-1** (40 hex chars) | Đã sửa thành SHA-1 |
| 5 | Sơ đồ & Mục 8.6 | DB S6 là "PostgreSQL 16" | Là **PostgreSQL 15** (`docker-compose.yml`) | Đã sửa thành PostgreSQL 15 |

---

## 8. ĐẶC TẢ KỸ THUẬT CHI TIẾT

### 8.1. Ground Truth Decision — Cây Server C++ Chuẩn Cho Giai Đoạn 3

| Tiêu chí so sánh | `Server - LgdMu s16 1.1` | `Lgd-Server-main` | Ý nghĩa |
| :--- | :--- | :--- | :--- |
| **Timestamp file mới nhất** | 03/01/2021 | **04/07/2021** (mới hơn ~3 tháng) | Lgd-Server-main được chỉnh sửa sau |
| **`ClientPacket.h` LastWrite** | 09/22/2020 | **04/07/2021** | Lgd-Server-main có packet mới hơn theo thời gian |
| **`ClientPacket.h` size** | **112.605 byte** (lớn hơn) | 107.014 byte | 1.1 lớn hơn nhưng cũ hơn về thời gian |
| **Số file** | 571 files | 570 files | Gần như tương đương |

**Quyết định chuẩn hóa:**
- Dùng **`Lgd-Server-main`** làm server runtime triển khai thực tế (chứa đầy đủ thư mục `Databases/` và 4 file config chuẩn).
- Dùng **`Server - LgdMu s16 1.1/Game/ClientPacket.h`** làm tài liệu đối chiếu packet khi porting sang C# (Giai đoạn 3).

---

### 8.2. Account Bridge — Cơ Chế Dual-Write Kỹ Thuật

Mô hình đồng bộ tài khoản giữa PostgreSQL 15 (S6) và MySQL 5.6 (S16) được thực hiện theo pattern **Dual-Write tập trung** tại `AccountService.cs` trong `OpenMU.PlayerWeb`.

```
[Người chơi Đăng ký / Đổi mật khẩu trên Web]
                         |
                         v
             AccountService.cs (Dual-Write)
                         |
           +-------------+-------------+
           |                           |
           v                           v
 [Ghi vào PostgreSQL 15]      [Ghi vào MySQL 5.6]
  Bảng: data.Account           Database: database_login
  LoginName: username          Bảng: accounts (account: username)
  PasswordHash: BCrypt(pw, 11) password: SHA-1(acc + ":" + pw)
  AccountStatus: Active        Bảng: accounts_security, accounts_status
           |                           |
           +-------------+-------------+
                         |
              [Nếu MySQL thất bại]
                         |
                         v
        Ghi vào data.AccountSyncQueue
        (AccountSyncRetryService chạy ngầm retry mỗi 30s)
```

#### Quy tắc xử lý xung đột:
1. **Không có distributed transaction** giữa 2 DB khác loại. Thứ tự: PostgreSQL ghi trước. Nếu MySQL thất bại → lưu vào `data.AccountSyncQueue`, background worker sẽ tự động retry, không rollback PostgreSQL.
2. **Không có xung đột item/character ID** vì nhân vật S6 dùng UUID (PostgreSQL) và nhân vật S16 dùng INT auto-increment (MySQL) — hai không gian ID hoàn toàn tách biệt.
3. **Kiểm tra username collision** trước khi ghi: query song song cả 2 DB. Nếu username tồn tại ở bất kỳ DB nào → từ chối đăng ký.

#### Hash Password Strategy:

| Database | Thuật toán | Bằng chứng mã nguồn | Trạng thái |
| :--- | :--- | :--- | :---: |
| **PostgreSQL 15 (S6)** | BCrypt (work factor 11) | `BCrypt.Net.BCrypt.HashPassword(pw, 11)` | [x] Hoạt động |
| **MySQL 5.6 (S16)** | **SHA-1** (40 ký tự hex) | `S16PasswordHasher.Hash(account, pw)` | [x] Hoạt động |

---

### 8.3. Cơ Chế Đăng Nhập Thực Tế (Login Flow)

```
Người chơi → Launcher → Chọn Server → Mở Client tương ứng
                                          |
                                          v
                               Màn hình Login in-game
                               (Username + Password)
                                          |
                     +--------------------+--------------------+
                     |                                         |
                     v                                         v
          S6 LoginServer (C#)                      S16 LoginServer (C++)
          Xác thực qua PostgreSQL 15               Xác thực qua MySQL 5.6
          BCrypt.Verify(input, hash)               SHA-1(input) == accounts.password
```

---

### 8.4. Benchmark — Cơ Sở Dẫn Chứng Cho Luận Điểm Tại Mục 4.1

| Luận điểm | Nguồn | Dữ liệu thực tế | Trạng thái |
| :--- | :--- | :--- | :---: |
| *.NET đạt 90–95% throughput C++* | TechEmpower Framework Benchmarks Round 22, plain-text workload | ASP.NET Core: 7,02M req/s; C (H2O): 7,69M req/s → **91,3%**. (Lưu ý: Plain-text HTTP throughput là tham chiếu, game server có đặc thù stateful riêng). | [x] Có nguồn |
| *400MB RAM / 5.000 CCU* | OpenMU GitHub README + community private server reports | Ước tính cần đo lại trên phần cứng thực tế ở Giai đoạn 2 | [!] Cần đo sau |
| *Rust Borrow Checker rủi ro với Game Server* | Quan sát thực nghiệm: shared mutable state (Player ↔ Party ↔ Map) đòi hỏi `Arc<RwLock<T>>` lồng nhau, dễ deadlock khi N thread PK đồng thời | Nhận định kiến trúc dựa trên mô hình đồ thị quan hệ | [!] Định tính |

---

### 8.5. Chiến Lược Bảo Mật Packet (Packet Security Strategy)

| Realm | Mã hóa packet | Anti-Cheat | Nguy cơ còn mở |
| :--- | :--- | :--- | :--- |
| **S6 (OpenMU C#)** | SimpleModulus + XOR (ClientLibrary C# Native AOT) | `SpeedHackDetectPlugIn.cs` (Server-side) | Packet injection từ client custom |
| **S16 (LgdMu C++)** | SimpleModulus S16 + XOR Custom (PacketEncDec.cpp) | AntiCheat.cpp + HackCheck.cpp trong Main.dll | main.exe đã unpacked — có thể bị decompile |

**Các biện pháp bảo mật đã/đang áp dụng:**
1. **Rate limiting** trên Web Portal & ConnectServer: tối đa 5 lần kết nối/phút cho đăng nhập.
2. **IP ban tự động** khi phát hiện spam kết nối bất thường.
3. **Server-side coordinate validation**: plugin `SpeedHackDetectPlugIn.cs` kiểm tra tốc độ di chuyển của người chơi.
4. **Packet size validation**: từ chối packet có kích thước nằm ngoài khoảng hợp lệ của opcode tương ứng.

---

### 8.6. Ước Tính Hạ Tầng (Infrastructure Specs)

#### Cấu hình tối thiểu để vận hành cả 2 Realm (500–1.000 CCU)

| Thành phần | Nền tảng | RAM | CPU |
| :--- | :--- | :---: | :---: |
| OpenMU Server S6 | Docker / Linux | ~400 MB | 2 vCPU |
| PostgreSQL 15 (DB S6) | Docker / Linux | ~300 MB | 1 vCPU |
| OpenMU.PlayerWeb (Blazor) | Docker / Linux | ~200 MB | 0.5 vCPU |
| Server S16 C++ (LgdMu) | Windows Server 2022 | ~1.2 GB | 2 vCPU |
| **MySQL 5.6** (DB S16) | Docker / Windows Container | ~500 MB | 1 vCPU |
| **Tổng** | | **~2.8 GB** | **~6.5 vCPU** |

**Đề xuất máy chủ thực tế:** 1 VPS / Dedicated Server với **8 GB RAM / 4–6 Cores / 100 GB SSD NVMe** (Windows Server 2022 kèm Docker Desktop / WSL2).