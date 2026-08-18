# TÀI LIỆU LƯU TRỮ ĐẶC TẢ SỬA SIMULATOR KINH TẾ OPENMU S6E3 — VÒNG 3
*(Lưu trữ phục vụ việc quay lại hiệu chuẩn sau khi làm việc với Season 16)*

> **Bối cảnh & Phạm vi:**
> - Phạm vi: Chỉ sửa `Program.cs` + `simulation_parameters.json` của simulator khi quay lại.
> - KHÔNG sửa source server, KHÔNG sửa database.
> - Sau khi sửa: sinh lại TOÀN BỘ 15 file CSV trong MỘT lần chạy duy nhất.

---

## 0. BẤT BIẾN — TUYỆT ĐỐI KHÔNG SỬA

Ba phần này đã được đối chiếu với source MUnique/OpenMU (master) và khớp từng chữ số với `monster_exp_calibration.csv` / `exp_table_verification.csv`:

1. **`CalculateBaseExperience` — dạng đã xác minh:**
   ```csharp
   exp = (L + 25) * L / 3.0;
   if (L >= 65)  exp += (L - 64) * (L / 4.0);
   exp *= 1.25;
   if (killerLevel > L + 10)  exp *= (L + 10) / killerLevel; // penalty, áp SAU bonus
   ```
   *Kiểm chứng:* 
   - Iron Knight L142 $\rightarrow 167 \times 142 / 3 + 78 \times 35.5 = 10,673.67 \rightarrow \times 1.25 = 13,342.08$ ✔
   - Condra L117 $\rightarrow 8,860.31$ ✔ ; Lizard King L70 $\rightarrow 2,902.08$ ✔

2. **`InitializeExpTable`:**
   ```csharp
   cumulative(L) = 10 * (L + 8) * (L - 1)^2  [ + 1000 * (L - 247) * (L - 256)^2 nếu L >= 256 ]
   ```
   *Kiểm chứng:* 
   - `cumulative(310) = 487,337,580` ; `cumulative(10) = 14,580` ;
   - `hiệu = 487,323,000` ✔ ; `cumulative(400) = 3,822,148,080` ✔ ; liên tục tại L=256 ✔

3. **Công thức tiền rơi:** 
   - `droppedMoney = (uint)(gainedExperience + BaseMoneyDrop)`, với `BaseMoneyDrop = 7`.
   - Nhóm drop tiền có `Chance = 0.5`.
   - Tổng xác suất drop là **0.8001** ($0.5 + 0.3 + 0.0001$), KHÔNG phải 0.8011.

*Hằng số server đã xác minh lại từ source:*
`ExperienceRate = 1.0f`, `ClampMoneyOnPickup = false`, `MaximumInventoryMoney = int.MaxValue` ($2,147,483,647$), `MaximumLevel = 400`.

---

## A. LỖI CHẶN — BẢNG GATE LEVEL (ƯU TIÊN #1)

Nguồn chuẩn từ source: `src/Persistence/Initialization/VersionSeasonSix/Gates.cs` (hàm `CreateWarpEntries`).

| Map | Monster đại diện | gateLevel cũ (SAI) | Gates.cs (CHUẨN ĐÚNG) |
| :--- | :--- | :---: | :---: |
| 2 Devias | Elite Yeti | 15 | **20** |
| 4 Lost Tower | Cursed Wizard | 40 | **50** |
| 7 Atlans | Lizard King | 60 | **70** |
| 8 Tarkan | Tantallos | 130 | **140** |
| 37 Kanturu Ruins | Splinter Wolf | 150 | **160** |
| 81 Karutan 2 | Condra #575 | 160 | **170** |
| 38 Kanturu Relics | Persona #358 | 220 | **230** |
| 57 Raklion | Coolutin, Iron Knight | 240 | **280** |
| 56 Swamp of Calmness | Sapi Queen/Ice Napin/Shadow Master | (thiếu) | **400** |

### A.1 Việc phải làm:
- Cập nhật `GetRequiredLevelForMap(mapNumber)` theo cột ĐÚNG ở trên.
- Đồng bộ `AccessibleMaps` trong `simulation_parameters.json` với đúng bảng này.

### A.2 Hệ quả chấp nhận:
- Tiêu chí nghiệm thu cũ *"selector trả Iron Knight #458 cho mọi level >= 240"* là sai đề vì Raklion mở ở level 280.
- Dải **240–279**: Monster tốt nhất hợp lệ là **Persona #358** (Kanturu Relics, gate 230).
  Tại L260: Persona = 5,453 EXP/kill so với Iron Knight = 9,238 EXP/kill $\rightarrow$ Cần điều chỉnh lại bảng tính.
- `reset_27_breakdown.csv`: Bracket 260–310 tách thành **260–279 (Persona)** và **280–310 (Iron Knight)**; tính lại `RateTier` weighted average cho từng bracket mới.
- Swamp of Calmness gate = 400 $\rightarrow$ Giữ quái vật Swamp như nội dung end-game tại đúng L400.

### A.3 Cửa sổ selector dự kiến chuẩn:
```
Elite Bull Fighter #4    : 1   – 19
Elite Yeti               : 20  – 49
Cursed Wizard            : 50  – 69
Lizard King              : 70  – 139
Tantallos                : 140 – 159
Splinter Wolf            : 160 – 169
Condra #575              : 170 – 229
Persona #358             : 230 – 279
Iron Knight #458         : 280 – 400
Coolutin #457            : không bao giờ được chọn (bị Iron Knight domination)
```

---

## B. QUẢN LÝ RESET KHI KHÔNG ĐỦ TIỀN TRẢ PHÍ (ƯU TIÊN #2)

- Khi `TryRemoveMoney` trả `false`: **KHÔNG tăng `ResetCount`**, **KHÔNG cộng points**, **KHÔNG tăng `TargetLevel`**; đánh dấu `IsBlocked = true` và dừng chuỗi tiến trình.
- `scenario_c2_d2_audited.csv`: Cột `C2_ZenBalance` phải được tái lập một cách nhất quán từ `C2_NetZen` và `C2_ResetCost`.

---

## C. CƠ CHẾ CAP TRẦN ZEN Ở MỨC TỪNG LẦN KILL (ƯU TIÊN #3)

- OpenMU từ chối theo **từng lần pickup vài nghìn Zen**, KHÔNG phải theo cả chu kỳ hàng trăm triệu.
- Cộng tiền **theo từng kill** trong `SimulateLevelRange` và chỉ từ chối đúng những drop lẻ gây tràn để tránh phóng đại `DiscardedZen`. (Hoặc dùng tích phân xấp xỉ giải tích: lấp đầy tới cap rồi từ chối phần dư theo đơn vị `moneyPerKill`).

---

## D. DỌN DẸP VÀ ĐỒNG BỘ 15 FILE CSV

1. **`reset_27_breakdown.csv`**: Đổi nhãn `MapZone` thành danh sách monster thực tế pha trộn trong bracket, kèm tỉ lệ.
2. **`monster_exp_calibration.csv`**: Xoá và sinh lại từ selector động mới.
3. **`gold_preset_audited.csv` vs `gold_preset_audited_timeline.csv`**: Xóa bản cũ `gold_preset_audited.csv`, giữ bản timeline đầy đủ `RoutineCosts` & `ResetPaid`.
4. **`jewel_economy.csv`**: Bổ sung cột `RateProfile` và xoá bản cũ không ghi rate.
5. **`scenario_c2.csv`**: Đồng bộ định nghĩa level cap (400) hoặc đổi tên phân biệt.

*Danh sách file xóa trước khi sinh lại:* `monster_exp_calibration.csv`, `gold_preset_audited.csv`, `scenario_c2.csv`, `jewel_economy.csv`.

---

## E. ĐIỀU CHỈNH CỘT ZEN TRONG `scenario_f_timeline.csv`

Tách rõ 3 cột tiến triển theo ngày:
- `ZenBalanceEndOfDay`
- `CumulativeGrossZen`
- `CumulativeDiscardedZen`

---

## F. BỘ 9 TIÊU CHÍ NGHIỆM THU VÒNG 3 (KHI MỞ LẠI)

1. `cumulative(310) - cumulative(10) = 487,323,000`
2. `Gross/Exp ≈ 35%` ($= 0.5 \times 0.7$); reset 27 scenario A $\approx$ 170.6–170.8M.
3. `RateTier` bracket được tính weighted average.
4. $0$ vi phạm map gate ở mọi level 1–400 (theo bảng gate mới mục A).
5. Selector trả đúng 10 cửa sổ ở mục **A.3** (Iron Knight từ **280**, Persona **230–279**).
6. `monster_coverage_report.csv` có cột `gateLevel` khớp `Gates.cs`, và ghi Coolutin #457 bị Iron Knight áp đảo.
7. Không có dòng nào `ResetPaid = False` mà vẫn tăng `ResetCount` / `TargetLevel` / points.
8. Unit test cap Zen ở cấp độ từng kill: overflow guard hoạt động chuẩn xác, `DiscardedZen` đơn điệu không giảm.
9. Mọi cột balance phải tái lập được từ các cột khác trong cùng file (audit trail đóng).

---

## G. BẢNG MONSTER GỐC CHUẨN ĐÃ XÁC MINH

| ID | Tên | Level | HP | DefenseBase | Map |
| :---: | :--- | :---: | :---: | :---: | :--- |
| **358** | Persona | 118 | 68,000 | — | 38 Kanturu Relics (Gate 230) |
| **575** | Condra | 117 | 90,000 | — | 81 Karutan 2 (Gate 170) |
| **454** | Ice Walker | 102 | 68,000 | 480 | 57 Raklion (Gate 280) |
| **455** | Giant Mammoth | 112 | 77,000 | 550 | 57 Raklion (Gate 280) |
| **456** | Ice Giant | 122 | 84,000 | 620 | 57 Raklion (Gate 280) |
| **457** | Coolutin | 132 | 88,000 | 700 | 57 Raklion (Gate 280) |
| **458** | Iron Knight | 142 | 95,000 | 790 | 57 Raklion (Gate 280) |
| **562** | Dark Mammoth | 140 | 237,000 | 820 | 57 Raklion (Gate 280) |
| **563** | Dark Giant | 143 | 254,000 | 835 | 57 Raklion (Gate 280) |
| **564** | Dark Coolutin | 145 | 248,000 | 845 | 57 Raklion (Gate 280) |
| **565** | Dark Iron Knight | 148 | 265,000 | 860 | 57 Raklion (Gate 280) |
| **557** | Sapi Queen | 131 | 218,000 | 670 | 56 Swamp (Gate 400) |
| **558** | Ice Napin | 135 | 230,000 | 730 | 56 Swamp (Gate 400) |
| **559** | Shadow Master | 137 | 242,000 | 700 | 56 Swamp (Gate 400) |
