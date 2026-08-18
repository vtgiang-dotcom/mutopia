---
slug: web-ecosystem-plan
created: 2026-08-14
from: kilo
status: pending
---

# Nhiệm vụ: Triển khai Hệ sinh thái Web (Marketplace + WebShop + Payment)

> **Người giao việc:** Kilo (Solo-Code orchestrator)
> **Người thực hiện:** Gemini (Antigravity)
> **DO NOT edit this plan file. Leave `status: pending`.**
> **Write your report to: `D:\Project\mu\.gemini\antigravity\handoff\outbox\web-ecosystem-report.md`**

---

## 1. Bối cảnh & phạm vi (đã chốt)

Sau đánh giá kế hoạch `ea5fcc9b`, phân hệ **Game Server gần như không còn việc** — mọi thứ đã tồn tại và bật sẵn. Phạm vi duy nhất còn giá trị là **Web Portal** (`D:\Project\mu\OpenMU.PlayerWeb`, Blazor .NET 9).

Đã xác minh (Kilo) — đừng làm lại:

| Hạng mục | Trạng thái |
|---|---|
| SpeedHackDetectPlugIn | ✅ Đã có tại `OpenMU-master/src/GameLogic/PlugIns/SpeedHackDetectPlugIn.cs` (349 dòng, walk + attack token bucket). **Đã bật trong DB** (`IsActive=true`, `AutoBan=true`, `DisconnectOnViolation=true`, `MaxWarnings=3`) |
| MaximumConnectionsPerIpPlugIn | ❌ **Bỏ** — user không muốn giới hạn IP (300 bot chạy chung 1 IP localhost) |
| Discord RPC | ⏸️ Hoãn — user nói chưa cần |
| `/reset`, `/addstats`, invasion, Camera 360°, Map Editor | ✅ Đã có sẵn |

**Việc giao cho Gemini: chỉ 3 tính năng Web dưới đây.**

---

## 2. Kiến trúc PlayerWeb hiện tại (Kilo đã đọc, HÃY TỰ VERIFY)

`D:\Project\mu\OpenMU.PlayerWeb\` dùng EF Core (`Data/AppDbContext.cs`) map **trực tiếp vào DB OpenMU** (schema `data` + `config`). Các model sẵn có:

| Model | File | Ghi chú |
|---|---|---|
| `Account` | `Data/Account.cs` | **Đã có cột `WCoin` (int, line 55)**, `VipTier`, `VipExpiry`, `WheelSpins`, `IsBot` |
| `Character` | `Data/Character.cs` | Có `InventoryId` (Guid?) → `ItemStorage` |
| `ItemStorage` | `Data/ItemStorage.cs` | **Chỉ có `Id` + `Money`** — CHƯA map `Item` |
| `StatAttribute`, `Guild`, `GuildMember`, `NewsItem`, `WheelSpin`, `GameMapDefinition`, `CharacterClass` | `Data/*.cs` | Có sẵn |

**Services sẵn có:** `AccountService`, `CharacterService`, `VipService`, `WheelService`, `BotService`, `NewsService`, `RankingService`, `GuildService`, `ServerStatusService`.

**Pages sẵn có:** `Account/Index`, `Account/Rename`, `Account/Login`, `Account/Register`, `Account/ChangePassword`, `LuckyWheel`, `Rankings`, `News`, `Maps`, `Classes`, `Admin/Bots`, `Admin/Vip`, `Admin/News`.

**Điểm mấu chốt:** `WCoin` đã nằm sẵn trên `Account` và được dùng bởi `VipService` + `WheelService`. **Ba tính năng mới PHẢI dùng chung `WCoin`, không tạo tiền tệ riêng.**

---

## 3. Ba tính năng cần triển khai

### A. `MarketplaceService.cs` (Chợ trời — người chơi treo bán item bằng WCoin)

**Mục tiêu:** người chơi treo item từ rương (vault) lên chợ web, người khác mua bằng WCoin.

**Rào cản kỹ thuật (bắt buộc đọc kỹ):**
1. **Item CHƯA có model trong PlayerWeb.** Cần thêm model `Item` map bảng `data.Item` (cột: `Id`, `DefinitionId`, `Durability`, `Level`, `HasSkill`, `SocketCount`, `ItemSlot`, `ItemStorageId`, `PetExperience`, ...). Tham chiếu `config.ItemDefinition` (`Group`, `Number`, `Name`, `Width`, `Height`) để hiển thị tên/icon.
2. **Vault của account**: `Account.VaultId` → `ItemStorage`. PlayerWeb hiện chưa map `VaultId` trên `Account` model (chỉ có `VaultPassword`, `IsVaultExtended`). Cần thêm cột `VaultId`.
3. **Khóa item khi treo bán (lock state):** đây là phần KHÓ nhất. Item đang online (trong game) sẽ bị game server periodic-save đụng độ. **Cách an toàn nhất:** chỉ cho phép treo bán item đang ở **vault** (không phải inventory đang online), và di chuyển item sang một `ItemStorage` riêng của marketplace (hoặc đổi `ItemStorageId` sang một storage "marketplace holding") trong transaction DB. KHÔNG xóa item khỏi vault trực tiếp khi người chơi đang online — phải có cơ chế nhất quán.
4. **Giao dịch an toàn:** transaction SQL — trừ `WCoin` người mua, cộng `WCoin` người bán, chuyển `ItemStorageId` của item sang vault người mua. Nếu bất kỳ bước nào fail → rollback.

### B. `WebShop.razor` (Shop chính thức — server bán item bằng WCoin)

**Mục tiêu:** trang hiển thị danh mục item do admin cấu hình, người chơi mua bằng WCoin, item vào vault.

**Thiết kế:** tạo bảng cấu hình `OpenMuWeb_ShopItem` (hoặc dùng `NewsItem`-style) chứa: `ItemGroup`, `ItemNumber`, `Level`, `Option`, `PriceWCoin`, `Stock`. Trang filter theo loại (Vũ khí/Giáp/Cánh...) + sort theo giá. Mua → trừ WCoin → tạo item vào vault người mua.

### C. `PaymentService.cs` (Nạp tiền — PayOS/VietQR)

**Mục tiêu:** tạo QR nạp, webhook callback thành công → cộng WCoin.

**Thiết kế:**
- Endpoint tạo đơn: gọi PayOS API tạo QR (nội dung = account hash / order id), trả về QR cho trang.
- Webhook endpoint: nhận callback, verify signature, nếu thành công → `+WCoin` vào `Account`.
- Dùng **PayOS** (phổ biến VN, sandbox dễ test, webhook chuẩn). Cần `PAYOS_CLIENT_ID`, `PAYOS_API_KEY`, `PAYOS_CHECKSUM_KEY` qua env var (KHÔNG hardcode).

---

## 4. Ràng buộc (Fence) — KHÔNG được làm

1. **KHÔNG sửa bất kỳ file nào trong `OpenMU-master/`** (game server đã hoàn thiện, đừng đụng).
2. **Chỉ được sửa `OpenMU.PlayerWeb/`**: thêm model/service/page + migration (nếu cần).
3. **KHÔNG `git commit` / `git push`** — để working tree dirty, Kilo review + commit.
4. **KHÔNG tạo tiền tệ mới** — dùng `Account.WCoin` chung.
5. **KHÔNG hardcode credential** — PayOS keys qua env var / `appsettings.json` + `User Secrets`.
6. **Item đang online trong game** — không được xóa/sửa trực tiếp khi người chơi đang đăng nhập; phải ghi rõ cơ chế lock/sync trong report.
7. Nếu cần thêm NuGet package (vd `payos-dotnet` hoặc gọi REST trực tiếp) → **ghi rõ trong report, không tự cài**.

---

## 5. Tiêu chí nghiệm thu (Acceptance)

- [ ] `dotnet build OpenMU.PlayerWeb\OpenMU.PlayerWeb.csproj --configuration Release` → 0 Error.
- [ ] Marketplace: treo item từ vault → item biến mất khỏi vault + xuất hiện trên chợ; mua bằng account phụ → WCoin trừ đúng, item vào vault người mua, WCoin cộng cho người bán (không thất thoát).
- [ ] WebShop: mua item → WCoin trừ, item vào vault.
- [ ] Payment: gọi webhook giả lập (fake request) → WCoin tăng đúng số.
- [ ] Mọi thay đổi có lệnh verify kèm output thật trong report.

---

## 6. Định dạng báo cáo

Viết vào `D:\Project\mu\.gemini\antigravity\handoff\outbox\web-ecosystem-report.md`, frontmatter:

```markdown
---
slug: web-ecosystem
completed: <ISO date>
from: gemini
---
```

Nội dung bắt buộc:
1. **Danh sách file đã tạo/sửa** kèm mục đích.
2. **Cơ chế lock item khi treo bán** — giải thích cụ thể (đây là phần rủi ro nhất, cần trình bày rõ).
3. **Kết quả build** (paste output thật).
4. **Rủi ro / điều chưa làm** — đặc biệt nếu Payment chưa test được với PayOS sandbox thật, phải nói rõ.

**Quy tắc bằng chứng:** đừng viết claim nào mà bạn chưa chạy lệnh kiểm tra. Nếu không verify được điều gì, ghi ô output trống.

---

## Phụ lục: Đánh giá tổng hợp kế hoạch `ea5fcc9b` (để Gemini nắm toàn cảnh)

### Đã tiếp thu đúng (từ báo cáo trước)
- Bỏ cảnh báo "Vanilla state" sai → ghi nhận DB đầy đủ, 300 bot online.
- Loại đúng các task trùng: Camera 360°, Map Editor, `/reset`, `/addstats`, invasion Rồng Đỏ + Hoàng Kim, Discord RPC.
- Hấp thu đúng cảnh báo false-positive (sliding window + soft threshold + whitelist GM).

### Phát hiện mới (sau đánh giá lần này)
- **SpeedHackDetectPlugIn ĐÃ tồn tại đầy đủ** tại `GameLogic/PlugIns/SpeedHackDetectPlugIn.cs` + `SpeedHackDetectConfiguration.cs`, **đã bật trong DB** với cấu hình chặn cứng (`AutoBan=true`, `DisconnectOnViolation=true`, `MaxWarnings=3`). Kế hoạch đề xuất "tạo mới" là dư thừa.
- Điểm hook đúng của anti-cheat là `ISpeedHackCheatCheckPlugIn` (strategy plugin gọi từ `WalkToAsync` + attack action), không phải packet handler trực tiếp — implementation sẵn có đã đi đúng hướng này.
- `MaximumConnectionsPerIpPlugIn` bị **bỏ** vì user không muốn giới hạn IP (300 bot chung IP).

### Kết luận
Kế hoạch `ea5fcc9b` sau khi loại hết phần trùng lặp chỉ còn **3 việc Web thật**: Marketplace, WebShop, Payment. Đây chính là phạm vi của plan này.
