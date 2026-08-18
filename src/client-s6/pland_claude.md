# Kế hoạch chi tiết: EXP Decay Schedule, Chat Anti-Flood, Bot Auto-Party Tối ưu

## I. Bộ Suy giảm EXP khi Hibernate (Diminishing EXP Decay Schedule)

### 1. Nguyên tắc thiết kế

- EXP cộng bù không được tuyến tính vô hạn theo thời gian, phải **giảm dần theo từng mốc (tier)** để tránh
  bot "nhảy cấp" bất thường khi offline quá lâu.
- Phải có **ngưỡng trần (cap)** — sau một mốc thời gian nhất định, EXP cộng thêm gần như bằng 0, mô phỏng
  đúng cơ chế "rest EXP suy giảm" phổ biến trong MMORPG.
- Phải có **yếu tố rủi ro** (risk-of-death) để mô phỏng việc farm không phải lúc nào cũng an toàn 100%,
  tránh bot trở thành "cỗ máy tăng trưởng hoàn hảo" dễ bị phát hiện.

### 2. Bảng Tier Suy giảm (Discrete Tier Table — khuyến nghị dùng cho config JSON)

| Khoảng thời gian Hibernate | Efficiency Ratio | Ghi chú |
|---|---|---|
| 0h – 1h   | 70% | Gần sát farm thực tế, khuyến khích online lại sớm |
| 1h – 3h   | 55% | Suy giảm nhẹ |
| 3h – 6h   | 40% | Suy giảm trung bình |
| 6h – 12h  | 25% | Suy giảm mạnh |
| 12h – 24h | 10% | Gần chạm sàn |
| > 24h     | 0% (Cap cứng) | Không cộng thêm EXP, giữ nguyên tại mốc 24h |

### 3. Công thức Piecewise (tổng EXP theo từng đoạn thời gian)

```
ΔEXP_total = Σ (BaseMapExpPerSec × EfficiencyRatio_tier[i] × DurationInTier[i]_seconds)
```

Ví dụ: Bot hibernate 10 giờ, BaseMapExpPerSec = 500

```
Tier 1 (0-1h):  500 × 0.70 × 3600  = 1,260,000 EXP
Tier 2 (1-3h):  500 × 0.55 × 7200  = 1,980,000 EXP
Tier 3 (3-6h):  500 × 0.40 × 10800 = 2,160,000 EXP
Tier 4 (6-10h): 500 × 0.25 × 14400 = 1,800,000 EXP
--------------------------------------------------
Tổng: 7,200,000 EXP (thay vì 18,000,000 EXP nếu tính 100% tuyến tính)
```

→ Giảm khoảng **60% tổng EXP** so với tính tuyến tính thô, đúng mục tiêu giữ động lực cho người chơi thật.

### 4. Công thức thay thế (Continuous Exponential Decay — dùng cho bản nâng cao)

```
EfficiencyRatio(t) = max(R_min, R0 × e^(-λ × t))
```

Với `R0 = 0.70`, `R_min = 0.05`, chọn `λ` sao cho tại t = 24h ratio ≈ 0.10:

```
λ = ln(R0 / 0.10) / 24 ≈ 0.081 /giờ
```

- Dùng phiên bản này nếu muốn đường suy giảm mượt (không "gãy khúc" theo tier), phù hợp khi cần tinh chỉnh
  bằng một biến số duy nhất (`λ`) thay vì sửa cả bảng tier.

### 5. Yếu tố Rủi ro Chết khi Hibernate (Risk-of-Death Factor)

```
DeathProbability(t, mapDangerLevel) = min(0.5, mapDangerLevel × t / 24)
```

- Roll xác suất này **một lần duy nhất** tại thời điểm bot "thức giấc".
- Nếu trúng: phạt 20–40% tổng EXP vừa cộng bù (random trong khoảng), hoặc lùi lại một sub-level nếu gần
  ngưỡng lên cấp — mô phỏng đúng cảm giác "farm không phải lúc nào cũng an toàn".
- `mapDangerLevel` nên lấy từ bảng cấu hình map đã có sẵn trong OpenMU (map độ khó cao hơn → risk cao hơn).

### 6. Tham số cấu hình đề xuất (đưa vào `PlugInConfiguration`)

| Tham số | Giá trị mặc định | Có thể chỉnh qua Admin Config |
|---|---|---|
| `HibernateCapHours` | 24h | ✅ |
| `EfficiencyTierTable` | Bảng ở mục 2 | ✅ (JSON array) |
| `RiskDeathEnabled` | true | ✅ |
| `RiskPenaltyMinPercent` / `MaxPercent` | 20% / 40% | ✅ |

---

## II. Chat Cooldown & Anti-Flood Protection (Tính toán chi tiết)

### 1. Bài toán cần giải quyết

Với quy mô 100–500 bot, nếu chỉ giới hạn cooldown 3s/(player, bot) mà không có lớp giới hạn toàn server,
một sự kiện đông người (event, sự kiện train, chat spam cố ý) có thể kích hoạt hàng trăm bot phản hồi gần
như đồng thời → gây "chat flood" và tăng đột biến tải CPU/DB/network.

### 2. Kiến trúc 4 Lớp Throttle (Layered Rate Limiting)

**Lớp 1 — Cooldown theo cặp (Player, Bot):**
- `Cooldown_PB = 4 giây`
- Một bot cụ thể không được trả lời cùng một người chơi quá 1 lần trong 4 giây.

**Lớp 2 — Cooldown toàn cục theo từng Bot:**
- `Cooldown_Bot = 2 giây`
- Một bot không được phản hồi BẤT KỲ ai (dù người chơi khác nhau) quá 1 lần trong 2 giây.
- Chặn trường hợp 1 bot bị nhiều người chơi khác nhau "gọi" liên tục trong thời gian ngắn.

**Lớp 3 — Giới hạn số bot phản hồi trên mỗi tin nhắn (Fan-out Cap):**
- `MaxRespondersPerMessage = 2`
- Nếu một tin nhắn khiến 5-10 bot cùng match Intent trong bán kính 8 ô, chỉ chọn **2 bot có điểm
  Sociability cao nhất hoặc gần nhất** để trả lời, các bot còn lại im lặng.

**Lớp 4 — Token Bucket toàn Server (Global Rate Limiter):**
- `Capacity C = 30 token` (burst tối đa)
- `RefillRate r = 15 token/giây` (tốc độ phản hồi bền vững tối đa)
- `MaxQueueSize = 50` (tin chờ xử lý)
- Nếu bucket rỗng và queue đầy → **âm thầm bỏ (drop)** yêu cầu phản hồi đó, bot coi như "không nghe thấy"
  (tự nhiên vì người chơi thật cũng không phải lúc nào cũng được NPC/bot khác trả lời).

### 3. Tính toán Worst-Case (kịch bản xấu nhất)

Giả định: 200 người chơi cùng chat trong 1 giây (sự kiện đông), mỗi tin nhắn trung bình match 1.5 bot sau
khi qua Lớp 1-3 filter:

```
Raw response attempts/giây = 200 × 1.5 = 300 yêu cầu/giây
```

Với Token Bucket (`C=30`, `r=15/s`):

```
- 30 yêu cầu đầu tiên được xử lý ngay (burst capacity)
- 270 yêu cầu còn lại vào hàng chờ (queue)
- Queue chỉ giữ tối đa 50 → 220 yêu cầu bị drop ngay
- 50 yêu cầu trong queue được xử lý dần với tốc độ 15/s
  → Thời gian xử lý hết queue = 50 / 15 ≈ 3.3 giây
```

→ **Kết luận:** dù có 300 yêu cầu/giây trong tình huống xấu nhất, server chỉ thực sự xử lý tối đa
~15 phản hồi AI/giây bền vững, hoàn toàn nằm trong khả năng chịu tải của một server MU thông thường
(vốn đã xử lý hàng nghìn packet chat/giây từ người chơi thật).

### 4. Bảng tham số đề xuất

| Tham số | Giá trị | Mục đích |
|---|---|---|
| `Cooldown_PlayerBot` | 4s | Chặn spam 1 người → 1 bot |
| `Cooldown_BotGlobal` | 2s | Chặn 1 bot bị nhiều người gọi liên tục |
| `MaxRespondersPerMessage` | 2 | Chặn hiệu ứng "cả đàn bot cùng trả lời" |
| `TokenBucketCapacity` | 30 | Cho phép burst ngắn tự nhiên |
| `TokenBucketRefillRate` | 15/s | Tốc độ xử lý bền vững |
| `MaxQueueSize` | 50 | Chặn backlog phình to vô hạn |

### 5. Pseudocode luồng kiểm tra (trước khi sinh phản hồi)

```
function TryRespond(bot, player, message):
    if not PassCooldown_PlayerBot(bot, player): return SUPPRESS
    if not PassCooldown_BotGlobal(bot):         return SUPPRESS
    if CountRespondersAlready(message) >= MaxRespondersPerMessage: return SUPPRESS
    if not GlobalTokenBucket.TryConsume():
        if not GlobalQueue.TryEnqueue(bot, player, message): return DROP
        return QUEUED
    return GenerateResponse(bot, player, message)  // + typing delay 500-1500ms như hiện tại
```

---

## III. Bot Auto-Party Tối ưu theo chuẩn MU Online

### 1. Nguyên tắc chuẩn Party Bonus của MU Online (Classic)

Theo cơ chế MU Online truyền thống, Party Bonus là **% cộng thêm trên EXP cá nhân của từng thành viên**,
**không phải chia sẻ (split) một pool EXP chung**:

| Số thành viên Party | Bonus EXP (Chuẩn) | Set Party (cùng Guild, biến thể một số season) |
|---|---|---|
| 2 | +2% | — |
| 3 | +3% | +5% |
| 4 | +4% | +6% |
| 5 | +5% | +7% |

→ **Kết luận quan trọng:** vì bonus là cộng thêm (additive) trên EXP tự thân, không phải chia sẻ, việc lập
party giữa các bot **không có nhược điểm nào** (không dilute EXP của ai), chỉ có lợi. Do đó chiến lược tối
ưu cho AI là: **luôn cố gắng lấp đầy party tới tối đa (5 thành viên)** mỗi khi có ≥2 bot Farmer cùng farm
một bãi, để tối đa hóa % bonus miễn phí cho tất cả.

> Lưu ý: một số season/server custom có thể đổi công thức thành dạng "share pool theo level cao nhất" —
> nên đọc giá trị bonus từ cấu hình Experience Formula hiện có của OpenMU (nếu đã implement) thay vì
> hard-code, để đảm bảo đúng với season đang chạy.

### 2. Điều kiện kích hoạt lập Party tự động (chỉ khi vắng người chơi thật)

- Distance Observer giám sát một bán kính riêng cho mục đích Party — **`FarmZoneRadius = 25 ô`**
  (rộng hơn bán kính chat 8 ô, vì phạm vi ảnh hưởng của một bãi farm lớn hơn phạm vi nghe chat).
- Chỉ kích hoạt lập party bot-only khi **không có bất kỳ người chơi thật nào** trong `FarmZoneRadius`
  liên tục trong **`StabilityWindow = 60 giây`** (tránh flicker khi có người đi ngang qua bãi).

### 3. Quy tắc chọn thành viên khi mời (Priority Selection)

1. **Cùng đúng bãi farm** (map + zone/spot ID cụ thể) — không mời bot ở bãi khác dù cùng map lớn.
2. **Cùng Role `Farmer`** — không trộn `BuffElf`/`PKGuard`/`Trader` vào party farm, giữ vai trò rõ ràng
   (trừ khi có Role `Party Healer` chuyên dụng được thiết kế làm vệ tinh hỗ trợ cho đúng party đó).
3. **Không cần khớp level** — vì bonus là cộng thêm trên EXP cá nhân, không cần cân bằng level giữa các
   thành viên, đơn giản hóa AI: cứ mời bot Farmer rảnh (chưa có party) gần nhất cho tới khi đủ 5.
4. **Ưu tiên lấp đầy nhanh nhất** để đạt tier bonus cao nhất (+5%) sớm nhất có thể.
5. **Leader do bot có thời gian farm lâu nhất tại bãi đó đảm nhận** (chỉ mang tính tổ chức nội bộ, không
   ảnh hưởng người chơi thật).

### 4. Quy tắc Nhường Slot cho Người chơi Thật (Player Priority Override)

- Nếu một người chơi thật gửi Intent `RequestParty` tới bất kỳ bot Farmer nào trong party (dù party đã
  đầy 5/5): **luôn chấp nhận ngay**, tự động kick thành viên bot có độ ưu tiên thấp nhất (thường là bot
  join sau cùng — "expendable filler") để nhường slot.
- Nếu Distance Observer phát hiện người chơi thật bước vào `FarmZoneRadius` (không cần họ chủ động chat),
  party bot-only chuyển sang trạng thái `Yielding`: chủ động nhường bãi/slot nếu người chơi có ý định farm
  chung, thay vì "chiếm chỗ" im lặng.

### 5. Chống Party Thrashing (Hysteresis chống lập/tan liên tục)

| Tham số | Giá trị | Mục đích |
|---|---|---|
| `MinPartyLifetime` | 300s (5 phút) | Party mới lập phải tồn tại tối thiểu 5 phút trước khi được xét tan |
| `RejoinCooldown` | 120s (2 phút) | Sau khi rời/tan party, bot phải chờ 2 phút mới được lập/tham gia party mới |
| `MaxPartySize` | 5 (theo chuẩn MU Classic, đọc từ config season) | Giới hạn cứng theo game rule |

### 6. FSM cho Bot Auto-Party Module

```
[Solo]
   → (không có player thật trong FarmZoneRadius đủ StabilityWindow) → [Scanning]
[Scanning]
   → (tìm được ≥1 bot Farmer rảnh cùng bãi) → [Forming]
[Forming]
   → (gửi lời mời tới các bot ứng viên theo Priority Selection, tới khi đủ 5 hoặc hết ứng viên) → [Active]
[Active]
   → (phát hiện player thật vào FarmZoneRadius, hoặc nhận Intent RequestParty) → [Yielding]
   → (MinPartyLifetime đã qua & điều kiện Scanning không còn đúng, ví dụ hết mob để farm) → [Disbanding]
[Yielding]
   → (kick bot ưu tiên thấp nhất, nhường slot cho player) → [Active] (party mới có player thật)
[Disbanding]
   → (tan party, mỗi bot chịu RejoinCooldown) → [Solo]
```

### 7. Bảng tham số tổng hợp (đưa vào `PlugInConfiguration`)

| Tham số | Giá trị mặc định |
|---|---|
| `FarmZoneRadius` | 25 ô |
| `StabilityWindow` | 60s |
| `MinPartyLifetime` | 300s |
| `RejoinCooldown` | 120s |
| `MaxPartySize` | 5 (theo config season) |
| `PartyBonusTable` | {2:2%, 3:3%, 4:4%, 5:5%} (đọc từ Experience Formula nếu có) |
