---
slug: bot-ai-consolidation
created: 2026-08-13
from: kilo
status: pending
---

# Nhiệm vụ: Phân tích & hợp nhất hai hệ thống AI bot, rồi bổ sung độ tự nhiên cho hệ thống đang chạy

> **Người giao việc:** Kilo (Solo-Code orchestrator)
> **Người thực hiện:** Gemini (Antigravity)
> **DO NOT edit this plan file. Leave `status: pending`.**
> **Write your report to: `D:\Project\mu\.gemini\antigravity\handoff\outbox\bot-ai-consolidation-report.md`**

---

## 1. Bối cảnh — hai hệ thống AI bot đang cùng tồn tại

Repo `D:\Project\mu\OpenMU-master\src\GameLogic\` chứa **hai** cụm code "bot AI" riêng biệt, chồng lấn về khái niệm nhưng không nối với nhau:

| Cụm | Đường dẫn | Trạng thái (theo quan sát của Kilo — HÃY TỰ VERIFY) |
|---|---|---|
| **A. Bot system đang chạy** | `GameLogic/Bots/*` (26 file) | Được nối thật: `BotFeaturePlugIn` → `BotManager` → `BotPlayer` → `BotNavigator` + `OfflinePlayer` MU-Helper. Đây là thứ thực sự điều khiển bot trong game. |
| **B. "SmartBot" engine** | `GameLogic/PlugIns/BotAI/*` (12 file) | Khai báo Utility AI + GOAP + Markov + Non-LLM chat + Hibernate EXP + Auto-Party FSM, nhưng **không ai gọi** ngoài chính nó. Chỉ `SmartBotFeaturePlugIn : IChatMessageReceivedPlugIn` có thể được plugin manager nạp (nó là `[PlugIn]`). |

Tên lớp **đụng độ** giữa hai cụm:
- `GameLogic/Bots/BotPersonality.cs` = `enum BotPersonality { Balanced, Greedy, Warrior, Loner, Guardian, Reckless }` (đang dùng, phân giải theo hash tên).
- `GameLogic/PlugIns/BotAI/Humanization/BotPersonality.cs` = `class BotPersonality { Sociability, Aggression, Patience, Mood }` (chết, random).

**Hệ quả then chốt:** hệ A (đang chạy) **hoàn toàn câm** — bot nhận tin nhắn nhưng không bao giờ chat. Hệ B (chết) có chat engine nhưng không nối vào vòng lặp bot, và có bug.

---

## 2. Giả thuyết cần VERIFY (đừng tin Kilo — chạy lệnh, báo bằng chứng)

Trước khi viết bất kỳ code nào, chạy các lệnh này và ghi output thật vào báo cáo. Mỗi claim phải có một dòng bằng chứng.

1. Đếm số file mỗi cụm:
   ```powershell
   (Get-ChildItem "D:\Project\mu\OpenMU-master\src\GameLogic\Bots" -Filter *.cs).Count
   (Get-ChildItem "D:\Project\mu\OpenMU-master\src\GameLogic\PlugIns\BotAI" -Recurse -Filter *.cs).Count
   ```

2. Kiểm tra `EvaluateBotDecision` / `UtilityCalculator` / `GoapPlanner` có ai gọi ngoài `PlugIns/BotAI/` không:
   ```powershell
   Select-String -Path "D:\Project\mu\OpenMU-master\src\GameLogic\**\*.cs" -Pattern "EvaluateBotDecision|UtilityCalculator|GoapPlanner|HibernateSimulator|BotPartyFsm" | Where-Object { $_.Path -notmatch "PlugIns\\BotAI" }
   ```
   Kỳ vọng (Kilo đoán): **0 kết quả** → cụm B là code chết. Hãy xác nhận.

3. Kiểm tra ai implement `IChatMessageReceivedPlugIn`:
   ```powershell
   Select-String -Path "D:\Project\mu\OpenMU-master\src\**\*.cs" -Pattern "IChatMessageReceivedPlugIn"
   ```
   Ghi lại danh sách. Hỏi cụ thể: hệ A (`Bots/`) có class nào implement không?

4. Kiểm tra cơ chế gửi chat mà bot có thể dùng (đã nối sẵn trong engine):
   ```powershell
   Select-String -Path "D:\Project\mu\OpenMU-master\src\GameLogic\GameContext.cs" -Pattern "SendGlobalChatMessageAsync|SendGlobalMessageAsync|ForEachPlayerAsync"
   Select-String -Path "D:\Project\mu\OpenMU-master\src\GameLogic\Player.cs" -Pattern "ShowBlueMessageAsync|InvokeViewPlugInAsync<IChatViewPlugIn>"
   ```

5. Build sạch trước khi đụng vào (baseline):
   ```powershell
   cd D:\Project\mu
   dotnet build OpenMU-master\src\GameLogic\MUnique.OpenMU.GameLogic.csproj --configuration Release --nologo 2>&1 | Select-String "(error|Build succeeded|Build FAILED)"
   ```

---

## 3. Phạm vi công việc

### Phase A — Audit & Verdict (bắt buộc, làm trước)

Ra một phán quyết kiến trúc có dẫn chứng: **cụm A hay cụm B là "source of truth"?** Kilo nghiêng về **cụm A** (vì nó được nối thật vào `BotManager`/`BotNavigator` và có hàng loạt cơ chế đã chạy: personality, party, revenge, presence rotation, jewel upgrade). Nhưng hãy tự xác minh rồi mới chốt.

Trong báo cáo phải có:
- Bảng `| Claim | Command run | Output |` cho 5 lệnh ở mục 2.
- Verdict 1 đoạn: giữ cụm nào, xử lý cụm còn lại thế nào.

### Phase B — Gắn chat vào hệ thống đang chạy (ưu tiên số 1)

Mục tiêu: bot hết câm, trả lời người chơi + chủ động nói. Thiết kế cụ thể (dựa trên hạ tầng đã có):

1. **Tạo file mới** `GameLogic/Bots/BotChatHandler.cs` (internal static, cùng style XML-doc như các file `Bots/*` khác). Nó implement `IChatMessageReceivedPlugIn` để hứng tin nhắn người chơi, và cung cấp phương thức phát chat chủ động.

2. **Intent matching**: tái sử dụng logic phân loại intent (greeting / request party / request item / location query) — hoặc copy từ `PlugIns/BotAI/Chat/IntentMatcher.cs` (nếu quyết định "salvage"), hoặc viết mới trong `BotChatHandler.cs` (nếu quyết định "bỏ cụm B"). Quyết định nào cũng phải ghi rõ trong báo cáo.

3. **Trả lời đúng ngữ cảnh + không nói dối** — ĐIỂM MẤU CHỐT:
   - Cụm B hiện có bug nghiêm trọng: `TemplateChatGenerator` trả lời `"Ok tui pt rồi đó"` / `"Cho ít zen nè"` nhưng **không thực sự** tạo party hay trao item — đây là "nói dối" người chơi.
   - **Bắt buộc:** lời nhắn phải khớp hành động thật. Nếu bot nhận party, phải gọi `PartyManager`/`party.AddAsync` thật (xem `BotPartyHandler.cs` đang làm đúng cách này). Nếu không trao được item, phải trả lời từ chối, KHÔNG được hứa suông.
   - Gửi reply đúng kênh: dùng `GameContext.SendGlobalChatMessageAsync(senderName, message, ChatMessageType.Normal)` để **mọi người xung quanh thấy**, KHÔNG dùng `sender.InvokeViewPlugInAsync<IChatViewPlugIn>` (cách hiện tại của cụm B chỉ gửi cho đúng 1 người — bot reply mà người khác không thấy, trông như bug).

4. **Rate-limit** (chống spam): dùng lại ý tưởng token bucket / cooldown theo cặp (player,bot) từ `PlugIns/BotAI/Chat/ChatRateLimiter.cs`, nhưng sửa lỗi rò bộ nhớ: các `ConcurrentDictionary` tĩnh không bao giờ dọn → phải có cơ chế prune entry quá hạn, hoặc dùng `MemoryCache`/TtlDictionary.

5. **Chat chủ động (proactive)**, không chỉ phản ứng:
   - Khi bot bị PK: phát tin cầu cứu (hook vào `BotRevengePlugIn` / `BotSelfDefensePlugIn`).
   - Thỉnh thoảng ở town (trong lúc shopping trip): phát một câu vu vơ theo personality (Greedy: `"Ai bán Bless rẻ không?"`; Guardian: `"Cần pt đỡ đòn không?"`; Loner: im lặng).
   - Rate-limit nghiêm ngặt: không quá 1 câu chủ động / vài phút / bot, jitter theo `Rand`.

### Phase C — Micro-jitter (ưu tiên số 2, làm sau khi chat chạy ổn)

Cụm B có `MarkovHumanizer.cs` (AFK pause / jitter step / inventory pause) nhưng chết. Mục tiêu: đưa các "khoảng dừng người thật" vào hệ đang chạy:
- Delay nhặt đồ 0.5–3s ngẫu nhiên.
- Khi bãi trống, bot bước ngẫu nhiên 1–2 tile (giả "tìm quái") thay vì đứng im chờ `EmptyGroundGrace`.
- Chu kỳ AFK ngắn ở safezone (1–2 phút, mỗi ~30–45 phút, tỷ lệ theo personality).

**Ràng buộc kỹ thuật:** mọi thứ phải qua `OfflinePlayer.PendingBotActions.Enqueue(...)` khi đụng tới inventory/attribute/skill (xem comment `BotNavigator.cs` về race condition với combat tick). Không được làm bot đứng im vượt `StuckTimeout` (8s) ngoài ý muốn — AFK có chủ đích phải được phân biệt với "stuck".

### Phase D — Tùy chọn (chỉ làm nếu A/B/C hoàn tất sạch)

- Personal store: bot mở store bán đồ Exc/Ancient thừa (dùng field `Item.StorePrice`, `IsStoreOpened` đã có).
- Combat variety: thinh thoảng dùng skill thường thay vì "skill mạnh nhất" (`BotPersonalitySettings.AutoSelectBestSkill`).

---

## 4. Ràng buộc (Fence) — KHÔNG được làm

1. **KHÔNG xóa file nào trong `PlugIns/BotAI/`** (hành vi destructive, cần phê duyệt riêng). Nếu verdict là "bỏ cụm B", chỉ đánh dấu `[Obsolete]` + ghi chú, KHÔNG `Remove-Item`.
2. **KHÔNG sửa file ngoài phạm vi đã nêu.** Chỉ được viết:
   - `OpenMU-master/src/GameLogic/Bots/BotChatHandler.cs` (mới)
   - các file `OpenMU-master/src/GameLogic/Bots/*.cs` đã liệt kê ở Phase B/C (sửa đúng chỗ cần)
   - đăng ký plugin nếu cần trong file khai báo plugin tương ứng
   - `OpenMU-master/tests/MUnique.OpenMU.Tests/` cho test mới
   - KHÔNG đụng `.github/`, CMake, `.kilo/`, `.gemini/`, `docs/CODING_RULES.md`.
3. **KHÔNG `git commit` / `git push`** — để working tree dirty, Kilo review và commit.
4. **Mọi mutation inventory/attribute/skill phải qua `PendingBotActions`** (đọc comment ở `BotNavigator.cs` để hiểu vì sao).
5. **Match style**: `internal static`/`internal sealed` + XML-doc đầy đủ + `Copyright (c) MUnique` header, như các file `Bots/*` hiện có.
6. Nếu cần thêm dependency NuGet → **dừng lại, báo trong report**, không tự cài.

---

## 5. Tiêu chí nghiệm thu (Acceptance)

- [ ] `dotnet build OpenMU-master\src\GameLogic\MUnique.OpenMU.GameLogic.csproj --configuration Release` → 0 Error.
- [ ] `dotnet test OpenMU-master\tests\MUnique.OpenMU.Tests\MUnique.OpenMU.Tests.csproj --filter "FullyQualifiedName~Bot" --configuration Release` → all passed.
- [ ] Bot trả lời chat theo intent, và reply đi qua `SendGlobalChatMessageAsync` (mọi người thấy), không phải `InvokeViewPlugInAsync` riêng.
- [ ] Không còn lời nhắn "hứa suông" (party/item) mà không thực hiện.
- [ ] Rate-limit có prune entry quá hạn (không rò bộ nhớ tăng không giới hạn).
- [ ] Mỗi thay đổi có lệnh verify kèm output thật trong report.

---

## 6. Định dạng báo cáo

Viết vào `D:\Project\mu\.gemini\antigravity\handoff\outbox\bot-ai-consolidation-report.md`, frontmatter:

```markdown
---
slug: bot-ai-consolidation
completed: <ISO date>
from: gemini
---
```

Nội dung bắt buộc:
1. **Phase A verdict** + bảng bằng chứng `| Claim | Command run | Output |` cho 5 lệnh mục 2.
2. **Quyết định** giữ cụm nào / salvage hay bỏ cụm B (có lý do).
3. **Danh sách file đã tạo/sửa** kèm từng mục đích.
4. **Kết quả build + test** (paste output thật).
5. **Rủi ro / điều chưa làm** — đặc biệt nếu Phase D bỏ qua, phải nói rõ.

**Quy tắc bằng chứng:** đừng viết claim nào mà bạn chưa chạy lệnh để kiểm tra. Nếu không verify được điều gì, ghi một dòng riêng với ô output trống.
