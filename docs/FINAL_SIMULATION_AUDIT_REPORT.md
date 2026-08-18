# BÁO CÁO TỔNG HỢP KIỂM TOÁN & MÔ PHỎNG KINH TẾ TIẾN TRÌNH RESET OPENMU S6E3
*(Tài liệu đặc tả kỹ thuật & số liệu thực nghiệm — Kèm bộ dữ liệu 10 file CSV)*

> **Mục đích tài liệu:** Cung cấp toàn bộ công thức giải tích, đối chứng mã nguồn, số liệu mô phỏng từng bước và bảng phân tích cung cầu kinh tế cho hệ thống Reset bậc thang OpenMU Season 6 Episode 3 để chuyển giao phân tích / thẩm định.
> **Trạng thái kiểm toán:** Toàn bộ số liệu đã được chạy lại đồng bộ sau khi hiệu chuẩn theo đúng công thức gốc của Game Core (.NET 9.0 Simulation Engine).

---

## 1. TỔNG QUAN HỆ THỐNG & ĐẶC TẢ THIẾT KẾ

* **Phiên bản Game:** OpenMU Season 6 Episode 3 (7 Class nhân vật).
* **Mô hình Tiến trình Reset:** Bậc thang lũy tiến 36 lần (Reset Ladder).
  * **Level Reset:** $L_{\text{target}}(k) = 50 + (k - 1) \times 10$ ($k = 1 \rightarrow 36$). Mốc 1: Lv 50 $\rightarrow$ Mốc 36: Lv 400.
  * **Điểm thưởng tiềm năng:** $P(k) = 5 \times L_{\text{target}}(k)$ (hoặc $7 \times L_{\text{target}}$ với MG/DL/RF).
  * **Cơ chế tẩy điểm:** `ResetStats = true`, `ReplacePointsPerReset = true` $\rightarrow$ Tích lũy tối đa $40,500$ Point sau 36 lần reset (chuẩn Webzen giữ giá trị từng cấp).
* **Cơ chế Kinh tế:**
  * **Zen Drop:** Rơi theo công thức $0.50 \times (\text{EXP}_{\text{kill}} + 7)$ với hiệu suất nhặt $70\%$.
  * **Trần Zen:** $2,147,483,647$ Zen (`int.MaxValue`).
  * **Ngọc (Bless/Soul/Chaos/Creation/Life):** Rớt từ quái thường với tỷ lệ $0.1\%$ ($0.001$) và từ sự kiện Blood Castle, Chaos Castle.

---

## 2. TRÍCH XUẤT CÔNG THỨC GỐC TỪ MÃ NGUỒN (GROUND TRUTH CODE)

### 2.1. Công thức EXP Nhân vật & Cơ chế Level-up
* `[ĐỌC TỪ CODE]` Trích từ [`GameContext.cs:L27`](file:///d:/Project/mu/OpenMU-master/src/GameLogic/GameContext.cs#L27):
  ```csharp
  private const string DefaultExperienceFormula = 
      "if(level == 0, 0, if(level < 256, 10 * (level + 8) * (level - 1) * (level - 1), " +
      "(10 * (level + 8) * (level - 1) * (level - 1)) + (1000 * (level - 247) * (level - 256) * (level - 256))))";
  ```
* `[ĐỌC TỪ CODE]` Trích từ [`Player.cs:L2165-L2173`](file:///d:/Project/mu/OpenMU-master/src/GameLogic/Player.cs#L2165-L2173):
  Mảng `ExperienceTable[level]` được gán trực tiếp làm **ngưỡng EXP tích lũy tuyệt đối** để đạt cấp độ, **không phải** EXP của riêng từng level đơn lẻ.
  * **Tổng EXP từ Lv 1 $\rightarrow$ 400:** **$3,822,148,080\text{ EXP}$** ($3.822$ tỷ).

### 2.2. Công thức EXP Quái vật & Cơ chế Phạt Level (Level Penalty)
* `[ĐỌC TỪ CODE]` Trích từ [`AttackableExtensions.cs:L596-L618`](file:///d:/Project/mu/OpenMU-master/src/GameLogic/AttackableExtensions.cs#L596-L618):
  ```csharp
  public static double CalculateBaseExperience(this IAttackable killedObject, float killerLevel)
  {
      if (killedObject.IsSummonedMonster()) return 0;

      var targetLevel = killedObject.Attributes[Stats.Level];
      var tempExperience = (targetLevel + 25) * targetLevel / 3.0;

      // Phạt giảm tuyến tính nếu cấp nhân vật vượt cấp quái > 10 level
      if (killerLevel > targetLevel + 10)
      {
          tempExperience *= (targetLevel + 10) / killerLevel;
      }

      // Bonus quái cấp cao (>= 65) cộng thêm sau kiểm tra phạt
      if (killedObject.Attributes[Stats.Level] >= 65)
      {
          tempExperience += (targetLevel - 64) * (targetLevel / 4);
      }

      return Math.Max(tempExperience, 0) * 1.25; // Hệ số nhân gốc 1.25
  }
  ```

---

## 3. BẢNG THAM CHIẾU EXP QUÁI HIỆU CHUẨN (CALIBRATED MONSTER BASE)

Dữ liệu tính toán từ công thức trên cho 9 dải bản đồ đại diện:

| Dải Level | Bản đồ & Quái đại diện | Level quái ($L_m$) | EXP Gốc (`BaseExp`) | Kills / Giờ | EXP / Giờ tại 1× |
| :---: | :--- | :---: | :---: | :---: | :---: |
| **1 – 30** | Lorencia/Noria *(Bull Fighter)* | 12 | **185** | 900 | $166,500$ |
| **31 – 60** | Devias/Dungeon 1 *(Elite Yeti)* | 36 | **915** | 850 | $777,750$ |
| **61 – 100** | Dungeon 3/LostTower 1-3 *(Devil)* | 52 | **1,668** | 800 | $1,334,400$ |
| **101 – 150**| LostTower 7/Atlans 2 *(Death Knight)*| 72 | **3,090** | 750 | $2,317,500$ |
| **151 – 200**| Atlans 3/Tarkan 1 *(Bloody Wolf)* | 84 | **4,340** | 700 | $3,038,000$ |
| **201 – 260**| Tarkan 2/Icarus *(Iron Wheel)* | 102 | **6,585** | 650 | $4,280,250$ |
| **261 – 320**| Aida 2/Kanturu 1 *(Splinter Wolf)* | 118 | **8,988** | 580 | $5,213,040$ |
| **321 – 360**| Kanturu Relics/Karutan 1 *(Persona)*| 138 | **12,518** | 500 | $6,259,000$ |
| **361 – 400**| Karutan 2/Raklion/Swamp *(Condor)* | 156 | **16,250** | 420 | $6,825,000$ |

---

## 4. CÁC ĐỊNH LUẬT TOÁN HỌC & CÔNG THỨC GIẢI TÍCH CỐT LÕI

### 4.1. Tổng EXP 36 Chu kỳ Reset Bậc Thang
$$\text{TotalExp}_{36} = \sum_{k=1}^{36} \Big(\text{ExpTable}\big[50 + (k-1) \times 10\big] - \text{ExpTable}[10]\Big) = \mathbf{20,139,543,000\text{ EXP}} \approx \mathbf{20.14 \times 10^9\text{ EXP}}$$

### 4.2. Định luật Thời gian hoàn thành 36 Reset theo Rate EXP
Do $>85\%$ EXP phân bổ tại các map cấp cao ($\overline{\text{BaseExp}} \approx 13,800$, $\overline{\text{Kills/h}} \approx 460$):
$$\overline{\text{Kills/h}} \times \overline{\text{BaseExp}} \approx 6.35 \times 10^6\text{ EXP/h tại 1x}$$

$$T(\text{hours}) \approx \frac{20.14 \times 10^9}{6.35 \times 10^6 \times \text{Rate}} \approx \mathbf{\frac{3,170}{\text{Rate}}} \iff \mathbf{\text{Rate EXP} \approx \frac{3,170}{T(\text{hours})}}$$

### 4.3. Định luật Cung Cầu Ngọc & Nạn Đói Ngọc
* **Tổng cầu ngọc 1 nhân vật:** Nâng đồ $+11$ ($56.5$ viên) + Phí reset ngọc ($32$ viên) = $\mathbf{88.5\text{ viên}}$.
* **Lượng ngọc nhặt được từ quái thường (tỷ lệ drop $0.1\%$, nhặt $70\%$):**
  $$\text{Jewels} = \text{Total Kills} \times 0.001 \times 0.70 = \left( \frac{20.14 \times 10^9}{13,800 \times \text{Rate}} \right) \times 0.0007 = \mathbf{\frac{1,021.6}{\text{Rate}}}$$
* **Hệ quả thiết kế:**
  * Điểm cân bằng tự nhiên tại Drop $0.1\%$ là **$\text{Rate} \le 11.5\text{x}$** (đạt đúng $88.5$ ngọc).
  * Khi nâng Rate EXP lên cao ($>11.5\text{x}$), bắt buộc phải điều chỉnh tỷ lệ rơi ngọc để tránh sụp đổ kinh tế:
    $$\mathbf{\text{JewelDropChance}} = 0.001 \times \frac{\text{Rate EXP}}{11.5}$$

### 4.4. Tính chất Bất biến của Phí Reset theo % EXP (Scale Invariance)
* $\text{Gross Zen nhặt được} = \text{Kills} \times 0.50 \times (\text{EXP}_{\text{kill}} + 7) \times 0.70 \approx 0.35 \times \text{CycleExp}$.
* $\text{Phí Reset} = 0.15 \times \text{CycleExp}$.
* **Tỷ lệ Chi/Thu:**
  $$\text{CostToEarnRatio} = \frac{0.15 \times \text{CycleExp}}{0.35 \times \text{CycleExp}} = \frac{15}{35} = \mathbf{42.86\% \approx 43.00\%}$$
  *(Tỷ lệ này triệt tiêu hoàn toàn hệ số Rate EXP, giữ cho kinh tế tự cân đối ở mọi cấu hình).*

---

## 5. PHÂN TÍCH CHUYÊN SÂU RESET 27 (NGUỒN LỆCH $12\%$)

Bảng phân rã từng dải của Reset 27 (Lv $10 \rightarrow 310$, Tổng $487.3\text{M}$ EXP) theo Gold Preset:

| Dải Level | Quái đại diện | EXP dải | % EXP | Rate | Exp/Kill | Kills | Kills/h | Giờ cày | % Thời gian |
| :---: | :--- | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: |
| **10 – 30** | Bull Fighter | $336\text{k}$ | $0.07\%$ | $50\text{x}$ | $9,250$ | $36.4$ | 900 | $0.0404\text{ h}$ | $0.60\%$ |
| **31 – 60** | Elite Yeti | $2.13\text{M}$ | $0.44\%$ | $50\text{x}$ | $45,750$ | $46.6$ | 850 | $0.0549\text{ h}$ | $0.82\%$ |
| **61 – 100**| Devil | $8.42\text{M}$ | $1.73\%$ | $50\text{x}$ | $83,400$ | $100.9$ | 800 | $0.1261\text{ h}$ | $1.88\%$ |
| **101 – 150**| Death Knight | $24.88\text{M}$ | $5.10\%$ | $30\text{x}$ | $92,700$ | $268.3$ | 750 | $0.3578\text{ h}$ | $5.34\%$ |
| **151 – 200**| Bloody Wolf | $47.83\text{M}$ | $9.81\%$ | $30\text{x}$ | $130,200$ | $367.3$ | 700 | $0.5247\text{ h}$ | $7.83\%$ |
| **201 – 260**| Iron Wheel | $98.59\text{M}$ | $20.23\%$ | $15\text{x}$ | $98,775$ | $998.2$ | 650 | $1.5356\text{ h}$ | $22.90\%$ |
| **261 – 310**| Splinter Wolf | $304.57\text{M}$ | **$65.25\%$** | $15\text{x}$ | $134,820$ | $2,258.4$ | 580 | **$4.0663\text{ h}$** | **$60.64\%$** |
| **TỔNG** | | **$487.3\text{M}$** | **$100\%$** | | | **$4,076$** | | **$6.7058\text{ h}$** | **$100\%$** |

* **Nguyên nhân lệch 12%:** Do dải 261-310 chiếm tới $65.25\%$ EXP nhưng chạy ở Rate thấp ($15\text{x}$), khiến người chơi lưu lại ở Rate thấp tới $83.5\%$ tổng thời gian chu kỳ. Trung bình đại số Rate ($31.6\text{x}$) sẽ nhanh hơn thực tế $12\%$.

---

## 6. DANH MỤC 10 TỆP DỮ LIỆU THỰC NGHIỆM ĐÍNH KÈM

Toàn bộ các tệp CSV được lưu trữ tại thư mục: [`d:/Project/mu/simulation/csv_results/`](file:///d:/Project/mu/simulation/csv_results/)

1. **[`exp_table_verification.csv`](file:///d:/Project/mu/simulation/csv_results/exp_table_verification.csv):** 400 dòng EXP tích lũy từ cấp 1 đến 400.
2. **[`monster_exp_calibration.csv`](file:///d:/Project/mu/simulation/csv_results/monster_exp_calibration.csv):** Bảng Base EXP, Rate, EXP hiệu dụng và Kills/h của 9 dải bản đồ.
3. **[`reset_27_breakdown.csv`](file:///d:/Project/mu/simulation/csv_results/reset_27_breakdown.csv):** Phân tích chi tiết từng dải cấp độ của chu kỳ Reset 27.
4. **[`scenario_a.csv`](file:///d:/Project/mu/simulation/csv_results/scenario_a.csv):** Baseline 1× chuẩn (629.6 giờ cắm bãi / ~1.050 - 1.260 giờ chơi thực).
5. **[`gold_preset_audited.csv`](file:///d:/Project/mu/simulation/csv_results/gold_preset_audited.csv):** Mô phỏng chi tiết 36 chu kỳ của Gold Preset ($107.6\text{h}$, phí bậc thang).
6. **[`scenario_f_timeline.csv`](file:///d:/Project/mu/simulation/csv_results/scenario_f_timeline.csv):** Tiến trình 90 ngày (Day 1 $\rightarrow$ Day 90) cho 3 nhóm người chơi (3h, 8h, 16h/ngày).
7. **[`jewel_economy.csv`](file:///d:/Project/mu/simulation/csv_results/jewel_economy.csv):** Mô hình cung cầu ngọc tại các mức drop rate $0.1\%, 0.5\%, 1.0\%, 2.0\%$.
8. **[`scenario_b_audited.csv`](file:///d:/Project/mu/simulation/csv_results/scenario_b_audited.csv):** Mô phỏng có cơ chế chờ cày bù Zen ($23.5\text{h}$, tăng $52\%$ thời gian).
9. **[`scenario_c2.csv`](file:///d:/Project/mu/simulation/csv_results/scenario_c2.csv) & [`scenario_d2.csv`](file:///d:/Project/mu/simulation/csv_results/scenario_d2.csv):** Chạy lại trần 200 ($3.6\text{h}$) và trần 300 ($6.1\text{h}$) với phí 15% EXP.
10. **[`scenario_e_invariance.csv`](file:///d:/Project/mu/simulation/csv_results/scenario_e_invariance.csv):** Bảng chứng minh tỷ lệ Chi/Thu bất biến ở mức 43% qua các mức Rate 1x, 50x, 200x.

---

## 7. BẢNG TỔNG HỢP CẤU HÌNH SERVER KHUYẾN NGHỊ

| Định hướng Máy chủ | Rate EXP Bậc Thang | Tỷ lệ Rớt Ngọc (`JewelDropChance`) | Phí Reset Khuyến nghị | Thời lượng 36 Reset | Chu kỳ Casual (3h/ngày) | Chu kỳ Auto (16h/ngày) |
| :--- | :---: | :---: | :---: | :---: | :---: | :---: |
| **1. Server Nhanh (Fast/PvP)** | $64\text{x} \rightarrow 20\text{x}$ (TB $\approx 50\text{x}$) | **$0.50\% \sim 0.55\%$** ($1/180$) | Bậc thang $+ 1$ Chaos | **$50\text{ giờ}$** | $16.7\text{ ngày}$ | $3.1\text{ ngày}$ |
| **2. Gold Preset (Chuẩn Cân Bằng)**| **$50\text{x} \rightarrow 5\text{x}$** (TB $\approx 30\text{x}$) | **$0.26\% \sim 0.30\%$** ($1/350$) | Bậc thang $+ 1$ Chaos $+ 1$ Creation | **$107.6\text{ giờ}$** | **$35.8\text{ ngày}$** | **$6.7\text{ ngày}$** |
| **3. Server Bền Vững (Mid-Rate)** | $30\text{x} \rightarrow 3\text{x}$ (TB $\approx 16\text{x}$) | **$0.14\% \sim 0.15\%$** ($1/700$) | $15\%$ Cycle EXP $+ 1$ Chaos | **$200\text{ giờ}$** | $66.7\text{ ngày}$ | $12.5\text{ ngày}$ |
| **4. Server Cày Cuốc (Hardcore)** | $10\text{x} \rightarrow 1\text{x}$ (TB $\approx 6\text{x}$) | **$0.08\% \sim 0.10\%$** (Gốc) | $15\%$ Cycle EXP | **$500\text{ giờ}$** | $166.7\text{ ngày}$ | $31.2\text{ ngày}$ |
