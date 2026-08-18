---
slug: bot-ai-consolidation
completed: 2026-08-13T17:21:00Z
from: gemini
---

# Báo cáo: Phân tích & Hợp nhất Hệ thống Bot AI và Kích hoạt Bot Chat

> **Người thực hiện:** Gemini (Antigravity Senior AI Assistant)  
> **Người nhận:** Kilo (Solo-Code orchestrator)  
> **Repo target:** `D:\Project\mu\OpenMU-master\src\GameLogic\`  

---

## 1. Phase A: Audit & Verification (Thực tế nghiệm thu)

Bảng đối chiếu 5 câu lệnh kiểm tra thực tế theo yêu cầu:

| Claim | Command run | Output thực tế |
|---|---|---|
| 1. Đếm số file CS mỗi cụm | `(Get-ChildItem "D:\Project\mu\OpenMU-master\src\GameLogic\Bots" -Filter *.cs).Count`<br>`(Get-ChildItem "D:\Project\mu\OpenMU-master\src\GameLogic\PlugIns\BotAI" -Recurse -Filter *.cs).Count` | Cụm A (`Bots`): **26 files**<br>Cụm B (`BotAI`): **12 files** |
| 2. Kiểm tra call site ngoài `PlugIns/BotAI/` | `Get-ChildItem "D:\Project\mu\OpenMU-master\src\GameLogic" -Recurse -Filter *.cs \| Select-String -Pattern "EvaluateBotDecision\|UtilityCalculator\|GoapPlanner\|HibernateSimulator\|BotPartyFsm" \| Where-Object { $_.Path -notmatch "PlugIns\\BotAI" }` | **0 kết quả** (Cụm B hoàn toàn là code chết) |
| 3. Kiểm tra class implement `IChatMessageReceivedPlugIn` | `Get-ChildItem "D:\Project\mu\OpenMU-master\src" -Recurse -Filter *.cs \| Select-String -Pattern "IChatMessageReceivedPlugIn"` | Cụm B: `SmartBotFeaturePlugIn.cs`<br>Cụm A (`Bots`): **0 class** (Bot đang câm) |
| 4. Kiểm tra phương thức phát Chat | `Get-ChildItem "D:\Project\mu\OpenMU-master\src" -Recurse -Filter *.cs \| Select-String -Pattern "SendGlobalChatMessageAsync\|SendGlobalMessageAsync\|InvokeViewPlugInAsync<IChatViewPlugIn>"` | `GameContext.SendGlobalChatMessageAsync`<br>`player.ForEachWorldObserverAsync<IChatViewPlugIn>(...)` |
| 5. Build sạch dự án baseline | `dotnet build OpenMU-master\src\GameLogic\MUnique.OpenMU.GameLogic.csproj --configuration Release` | `Build succeeded. 0 Error(s)` |

### Phán quyết Kiến trúc (Verdict):
- **Source of Truth chính thức:** **Cụm A (`GameLogic/Bots/*`)**. Đây là hệ thống thực sự được nối với `BotFeaturePlugIn` -> `BotManager` -> `BotPlayer` -> `BotNavigator` + `OfflinePlayer` MU Helper.
- **Xử lý Cụm B (`PlugIns/BotAI/*`)**: Cụm B chứa các file không được kết nối và có xung đột tên (`BotPersonality`). Đã đánh dấu `[System.Obsolete]` cho `SmartBotFeaturePlugIn` và tháo attribute `[PlugIn]` để tránh xung đột plugin loader. **Không xóa bất kỳ file nào** (tuân thủ tuyệt đối Fence Rule 1).

---

## 2. Các Thay Đổi Đã Thực Hiện (Implementation Details)

### Phase B: Chat Hệ Thống Cho Bot (`BotChatHandler.cs`)
1. **Tạo mới `GameLogic/Bots/BotChatHandler.cs`**:
   - Implement `IChatMessageReceivedPlugIn` để nhận tin nhắn chat từ người chơi.
   - Nhận diện Intent chính xác:
     - **Tổ đội (Party)**: Nếu Bot có tính cách `Loner` (Độc hành) ➔ Từ chối lịch sự (`"Tui thích đi solo hơn, cảm ơn nhé!"`). Nếu không phải `Loner` ➔ Gọi thực tế `BotPartyHandler.TryScheduleAcceptAsync` để tạo/vào party thật (KHÔNG HỨA SUÔNG). Phản hồi đúng trạng thái (`"Ok, chờ 2s nhận pt nha!"` hoặc `"Tổ đội đã đầy rồi bạn ơi"`).
     - **Chào hỏi (Greeting)**: Trả lời thân thiện kèm tên người chơi (`"Chào @Player! Chúc cày cuốc lượm nhiều ngọc nhé!"`).
     - **Hỏi Vị trí/Level**: Phản hồi Level và Map thực tế Bot đang cày.
     - **Xin Zen/Đồ**: Phản hồi theo tính cách (`Greedy` từ chối dí dỏm, các tính cách khác trả lời chưa có đồ thừa).
2. **Phát Chat Rộng Cho Mọi Người Xung Quanh (Broadcast Chat)**:
   - Thay vì gửi packet riêng tư cho 1 người chơi (`InvokeViewPlugInAsync`), `BotChatHandler.BroadcastBotMessageAsync` sử dụng `bot.ForEachWorldObserverAsync<IChatViewPlugIn>(..., true)`, phát khung chat hiển thị trên đầu Bot cho tất cả người chơi ở gần xung quanh cùng thấy.
3. **Chống Spam & Leak Bộ Nhớ (TTL Pruned Rate Limiter)**:
   - Dùng `ConcurrentDictionary<(string Sender, string Bot), DateTime>` với Cooldown 4 giây/cặp.
   - Hàm `PruneExpiredCooldowns()` tự động dọn dẹp các entry quá hạn khi bảng ghi vượt quá 200 items, đảm bảo không rò rỉ bộ nhớ khi máy chủ chạy lâu dài.
4. **Phát Chat Chủ Động Khi Bị PK (`BotRevengePlugIn.cs`)**:
   - Khi Bot bị người chơi khác hạ gục, Bot phát tin nhắn chat phẫn nộ trước khi respawn: `"Chờ tui quay lại tính sổ với @Killer!"`.

---

## 3. Kết Quả Kiểm Thử & Build (Build & Test Verification)

- **Biên dịch `GameLogic.csproj`**:
  ```powershell
  dotnet build OpenMU-master\src\GameLogic\MUnique.OpenMU.GameLogic.csproj --configuration Release
  ```
  ➔ **Build succeeded. 0 Error(s)**.

- **Chạy Unit Tests**:
  ```powershell
  dotnet test OpenMU-master\tests\MUnique.OpenMU.Tests\MUnique.OpenMU.Tests.csproj --filter "FullyQualifiedName~Bot" --configuration Release
  ```
  ➔ **Passed! - Failed: 0, Passed: 35, Skipped: 0, Total: 35**.

- **Biên dịch Server Startup**:
  ```powershell
  dotnet build OpenMU-master\src\Startup\MUnique.OpenMU.Startup.csproj --configuration Release
  ```
  ➔ **Build succeeded. 0 Error(s)**.

- **Deploy Docker**:
  ```powershell
  docker compose build openmu-server
  docker compose up -d --force-recreate openmu-server
  ```
  ➔ **Container `openmu-server` Running & Healthy**.

---

## 4. Danh Sách File Đã Tạo / Thay Đổi

1. `[NEW]` [BotChatHandler.cs](file:///d:/Project/mu/OpenMU-master/src/GameLogic/Bots/BotChatHandler.cs)
2. `[MODIFY]` [BotRevengePlugIn.cs](file:///d:/Project/mu/OpenMU-master/src/GameLogic/Bots/BotRevengePlugIn.cs)
3. `[MODIFY]` [SmartBotFeaturePlugIn.cs](file:///d:/Project/mu/OpenMU-master/src/GameLogic/PlugIns/BotAI/SmartBotFeaturePlugIn.cs)

---

## 5. Rủi Ro & Hạng Mục Tùy Chọn (Open Items)
- **Phase D (Personal Store)**: Tùy chọn mở shop bán đồ chưa triển khai trong đợt này (giữ nguyên scope).
- Working tree hiện giữ nguyên trạng thái dirty (không git commit/push) theo đúng quy định Fence rule 3 để Kilo review và commit.
